using UnityEngine;

// Persistent background-music controller; each scene can provide the track for its context.
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

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
        source.Play();
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
