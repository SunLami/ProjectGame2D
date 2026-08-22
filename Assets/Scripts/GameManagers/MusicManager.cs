using UnityEngine;

// Minimal background-music volume controller. Future music playback can reuse this AudioSource.
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _audioSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);

        if (SettingsService.Instance != null)
            SettingsService.Instance.ApplyAudioSettings();
    }

    public static void SetVolume(float volume)
    {
        if (Instance != null && Instance._audioSource != null)
            Instance._audioSource.volume = volume;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
