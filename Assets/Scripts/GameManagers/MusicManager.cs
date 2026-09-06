using UnityEngine;

// Persistent background-music controller; each scene can provide the track for its context.
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private static bool _backgroundMusicSuppressed;

    private AudioSource _audioSource;
    private float _trackVolume = 1f;
    private float _settingsVolume = 1f;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _trackVolume = Mathf.Clamp01(_audioSource.volume);

        if (Instance != null && Instance != this)
        {
            if (_audioSource.clip != null)
                SetClip(_audioSource.clip, _audioSource.loop, _trackVolume);

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_backgroundMusicSuppressed)
            _audioSource.Stop();

        if (SettingsService.Instance != null)
            SettingsService.Instance.ApplyAudioSettings();
    }

    public static void SetVolume(float volume)
    {
        if (Instance != null && Instance._audioSource != null)
        {
            Instance._settingsVolume = Mathf.Clamp01(volume);
            Instance.ApplyVolume();
        }
    }

    public static void SetClip(AudioClip clip, bool loop = true, float trackVolume = 1f)
    {
        if (Instance == null || Instance._audioSource == null || clip == null)
            return;

        Instance._trackVolume = Mathf.Clamp01(trackVolume);

        AudioSource source = Instance._audioSource;
        source.loop = loop;
        Instance.ApplyVolume();

        if (source.clip == clip && source.isPlaying)
            return;

        source.clip = clip;
        if (!_backgroundMusicSuppressed)
            source.Play();
    }

    /// <summary>Temporarily keeps scene background music silent while a cinematic owns presentation.</summary>
    public static void SuppressBackgroundMusic()
    {
        _backgroundMusicSuppressed = true;
        if (Instance?._audioSource != null)
            Instance._audioSource.Stop();
    }

    /// <summary>Starts the scene's configured background track after the cinematic hand-off finishes.</summary>
    public static void ResumeBackgroundMusic()
    {
        _backgroundMusicSuppressed = false;
        if (Instance?._audioSource != null && Instance._audioSource.clip != null)
            Instance._audioSource.Play();
    }

    private void ApplyVolume()
    {
        _audioSource.volume = _trackVolume * _settingsVolume;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
