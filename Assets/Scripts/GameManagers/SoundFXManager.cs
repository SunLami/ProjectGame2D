using System.Collections.Generic;
using UnityEngine;

/// <summary>One recorded <see cref="SoundFXManager.PlaySfx(string, float)"/>-style request (diagnostics and tests).</summary>
public readonly struct SfxRequest
{
    public readonly string Id;
    public readonly float Time;
    public readonly bool Played;

    public SfxRequest(string id, float time, bool played)
    {
        Id = id;
        Time = time;
        Played = played;
    }
}

[RequireComponent(typeof(AudioSource))]
public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance;

    private const int PoolSize = 12;
    private const int RecentCapacity = 96;

    [SerializeField] private Transform _playerFootPos;
    [Tooltip("SfxPlayAt: full volume inside this distance (world units) from the listener.")]
    [SerializeField, Min(0f)] private float _nearDistance = 5f;
    [Tooltip("SfxPlayAt: silent beyond this distance.")]
    [SerializeField, Min(0.1f)] private float _farDistance = 24f;

    private static AudioSource _audioSource;
    private static SoundFXLibrary _library;
    private static float _master = 1f;
    private static readonly SfxThrottle Throttle = new SfxThrottle();
    private static readonly List<SfxRequest> Recent = new List<SfxRequest>(RecentCapacity);
    private static readonly HashSet<string> WarnedMissing = new HashSet<string>();

    private AudioSource[] _pool;
    private int _poolCursor;
    private readonly List<SfxLoopHandle> _loops = new List<SfxLoopHandle>();

    /// <summary>The most recent requests (oldest first, at most 96) with whether a clip actually started.</summary>
    public static IReadOnlyList<SfxRequest> RecentRequests => Recent;

    public static void ClearRecentRequests() => Recent.Clear();

    /// <summary>Rebinds to the current scene's own Player -- SoundFXManager is a shared Bootstrap
    /// singleton now, so an Inspector-time reference to one scene's Player would break the moment
    /// another map's Player becomes active. Called by Player.Awake().</summary>
    public void BindPlayerFootPos(Transform footPos) => _playerFootPos = footPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _audioSource = GetComponent<AudioSource>();
        _library = GetComponent<SoundFXLibrary>();
        Throttle.Clear();
        WarnedMissing.Clear();
        BuildPool();
        // Editor-only parent (e.g. "_Managers") keeps the Hierarchy tidy; detach before
        // DontDestroyOnLoad, which only works on root GameObjects.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (SettingsService.Instance != null)
            SettingsService.Instance.ApplyAudioSettings();
    }

    private void BuildPool()
    {
        var holder = new GameObject("SfxPool").transform;
        holder.SetParent(transform, false);
        _pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var source = new GameObject("Voice" + i).AddComponent<AudioSource>();
            source.transform.SetParent(holder, false);
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = _master;
            _pool[i] = source;
        }
    }

    public static void PlayFootSteps(float volumeScale)
    {
        AudioClip clip = null;
        if (Instance != null && Instance._playerFootPos != null && MapManager.Instance != null)
            clip = MapManager.Instance.GetCurrentTileAudioClip(Instance._playerFootPos.position);

        if (clip != null)
            _audioSource.PlayOneShot(clip, volumeScale);
        else if (!string.IsNullOrEmpty(FallbackFootstepId))
            PlaySfx(FallbackFootstepId, volumeScale);
    }

    /// <summary>Footstep SFX id used when the tile under the player has no clip (set by <see cref="SceneAmbience"/>
    /// for arenas without walkable-tile audio). Null/empty = silent.</summary>
    public static string FallbackFootstepId { get; set; }

    /// <summary>Plays a one-shot SFX resolved by ID from the shared SoundFXLibrary (see
    /// AudioSfxSystem.md for the catalog). No-ops silently if the ID or library is missing so a
    /// missing/renamed clip never breaks gameplay flow. Ids with a profile in the library (pitch jitter, cooldown,
    /// voice limit, volume) play through a small voice pool; plain ids keep using the shared AudioSource.</summary>
    public static void PlaySfx(string sfxId, float volumeScale = 1f)
    {
        Play(sfxId, volumeScale, -1f, null);
    }

    /// <summary>Same as <see cref="PlaySfx(string, float)"/> with an explicit pitch variation (+- fraction).</summary>
    public static void PlaySfx(string sfxId, float volumeScale, float pitchJitter)
    {
        Play(sfxId, volumeScale, pitchJitter, null);
    }

    /// <summary>World-positioned one-shot: quieter with distance from the listener (player, else the main camera) and
    /// panned left/right by its horizontal offset.</summary>
    public static void PlaySfxAt(string sfxId, Vector2 worldPosition, float volumeScale = 1f)
    {
        Play(sfxId, volumeScale, -1f, worldPosition);
    }

    public static bool HasSfx(string sfxId) => _library != null && _library.HasGroup(sfxId);

    /// <summary>Starts a seamless loop (whirlpool, beam, wing flaps, ambience). Never returns null; stop it with
    /// <see cref="SfxLoopHandle.Stop"/> when the effect ends.</summary>
    public static SfxLoopHandle StartLoop(string sfxId, float level = 1f, float pitch = 1f, float fadeInSeconds = 0.2f)
    {
        var handle = new SfxLoopHandle { TargetLevel = Mathf.Clamp01(level), FadeInSeconds = Mathf.Max(0.01f, fadeInSeconds) };
        if (Instance == null || _library == null)
        {
            Record(sfxId, false);
            return handle;
        }

        if (!_library.TryGetClip(sfxId, out AudioClip clip, out SfxProfile profile))
        {
            WarnMissing(sfxId);
            Record(sfxId, false);
            return handle;
        }

        float sceneMix = SfxSceneMix.Multiplier(sfxId, _library.CategoryOf(sfxId));
        if (sceneMix <= 0f)
        {
            Record(sfxId, false);
            return handle;
        }

        handle.TargetLevel = Mathf.Clamp01(handle.TargetLevel * Mathf.Min(sceneMix, 1f));
        var go = new GameObject("SfxLoop_" + sfxId);
        go.transform.SetParent(Instance.transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
        source.volume = 0f;
        source.Play();
        handle.Source = source;
        handle.ProfileVolume = profile.Volume;
        Instance._loops.Add(handle);
        Record(sfxId, true);
        return handle;
    }

    public static void StopAllLoops(float fadeSeconds = 0.3f)
    {
        if (Instance == null)
            return;

        foreach (SfxLoopHandle handle in Instance._loops)
            handle.Stop(fadeSeconds);
    }

    public static void SetVolume(float volume)
    {
        _master = Mathf.Clamp01(volume);
        if (_audioSource != null)
            _audioSource.volume = volume;
        if (Instance != null && Instance._pool != null)
        {
            foreach (AudioSource source in Instance._pool)
                source.volume = _master;
        }
    }

    // ------------------------------------------------------------------ internals

    private static void Play(string sfxId, float volumeScale, float jitterOverride, Vector2? worldPosition)
    {
        if (Instance == null || _audioSource == null || _library == null)
            return;

        if (!_library.TryGetClip(sfxId, out AudioClip clip, out SfxProfile profile))
        {
            WarnMissing(sfxId);
            Record(sfxId, false);
            return;
        }

        float sceneMix = SfxSceneMix.Multiplier(sfxId, _library.CategoryOf(sfxId));
        if (sceneMix <= 0f)
        {
            Record(sfxId, false);
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (!Throttle.TryAcquire(sfxId, now, profile.MinInterval, profile.MaxVoices, clip.length))
        {
            Record(sfxId, false);
            return;
        }

        float volume = volumeScale * profile.Volume * sceneMix;
        float pan = 0f;
        if (worldPosition.HasValue)
        {
            volume *= Instance.Attenuation(worldPosition.Value, out pan);
            if (volume <= 0.001f)
            {
                Record(sfxId, false);
                return;
            }
        }

        if (profile.IsPlain && jitterOverride < 0f && !worldPosition.HasValue)
        {
            _audioSource.PlayOneShot(clip, volume);
            Record(sfxId, true);
            return;
        }

        float jitter = jitterOverride >= 0f ? jitterOverride : profile.PitchJitter;
        AudioSource voice = Instance.NextVoice();
        voice.panStereo = pan;
        voice.pitch = jitter > 0f ? 1f + Random.Range(-jitter, jitter) : 1f;
        voice.volume = _master;
        voice.PlayOneShot(clip, volume);
        Record(sfxId, true);
    }

    private AudioSource NextVoice()
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            int index = (_poolCursor + i) % _pool.Length;
            if (!_pool[index].isPlaying)
            {
                _poolCursor = (index + 1) % _pool.Length;
                return _pool[index];
            }
        }

        AudioSource stolen = _pool[_poolCursor]; // everything busy: reuse the oldest-assigned voice
        _poolCursor = (_poolCursor + 1) % _pool.Length;
        return stolen;
    }

    private float Attenuation(Vector2 worldPosition, out float pan)
    {
        Vector2 listener = ListenerPosition();
        Vector2 offset = worldPosition - listener;
        float distance = offset.magnitude;
        pan = Mathf.Clamp(offset.x / 14f, -0.7f, 0.7f);
        if (distance <= _nearDistance)
            return 1f;
        if (distance >= _farDistance)
            return 0f;

        float t = 1f - (distance - _nearDistance) / (_farDistance - _nearDistance);
        return t * t;
    }

    private Vector2 ListenerPosition()
    {
        if (_playerFootPos != null)
            return _playerFootPos.position;

        Camera camera = Camera.main;
        return camera != null ? (Vector2)camera.transform.position : Vector2.zero;
    }

    private void Update()
    {
        if (_loops.Count == 0)
            return;

        float dt = Time.unscaledDeltaTime;
        for (int i = _loops.Count - 1; i >= 0; i--)
        {
            SfxLoopHandle handle = _loops[i];
            if (handle.Source == null)
            {
                _loops.RemoveAt(i);
                continue;
            }

            if (handle.Stopping)
                handle.CurrentLevel -= dt / handle.FadeOutSeconds;
            else
                handle.CurrentLevel = Mathf.MoveTowards(handle.CurrentLevel, handle.TargetLevel, dt / handle.FadeInSeconds);

            handle.CurrentLevel = Mathf.Max(0f, handle.CurrentLevel);
            handle.Source.volume = _master * handle.ProfileVolume * handle.CurrentLevel;
            if (handle.Stopping && handle.CurrentLevel <= 0f)
            {
                Destroy(handle.Source.gameObject);
                handle.Source = null;
                _loops.RemoveAt(i);
            }
        }
    }

    private static void Record(string id, bool played)
    {
        if (Recent.Count >= RecentCapacity)
            Recent.RemoveAt(0);
        Recent.Add(new SfxRequest(id, Time.realtimeSinceStartup, played));
    }

    private static void WarnMissing(string id)
    {
        if (WarnedMissing.Add(id))
            Debug.LogWarning("SoundFXManager: no clip for sfx id '" + id + "' (SoundFXLibrary group missing or empty).");
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        _audioSource = null;
        _library = null;
    }
}
