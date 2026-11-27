using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent controller for the Main Menu background track.
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
            _audioSource.Stop();
            if (gameObject.scene.name == "MainMenu" && _audioSource.clip != null)
                SetClip(_audioSource.clip, _audioSource.loop, _trackVolume);

            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Editor-only parent (e.g. "_Managers") keeps the Hierarchy tidy; detach before
        // DontDestroyOnLoad, which only works on root GameObjects.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshPlayback();

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

        if (source.clip != clip)
            source.clip = clip;
        Instance.RefreshPlayback();
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
        if (Instance != null)
            Instance.RefreshPlayback();
    }

    private void OnActiveSceneChanged(Scene previous, Scene next) => RefreshPlayback();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshPlayback();

    private void RefreshPlayback()
    {
        string active = SceneManager.GetActiveScene().name;
        bool inMainMenu = active == "MainMenu" ||
            active == "Bootstrap" && SceneManager.GetSceneByName("MainMenu").isLoaded;
        if (_backgroundMusicSuppressed || !inMainMenu)
            _audioSource.Stop();
        else if (_audioSource.clip != null && !_audioSource.isPlaying)
            _audioSource.Play();
    }

    private void ApplyVolume()
    {
        _audioSource.volume = _trackVolume * _settingsVolume;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }
}
