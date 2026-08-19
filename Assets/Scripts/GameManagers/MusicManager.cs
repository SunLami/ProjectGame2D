using UnityEngine;

// Minimal background-music volume controller. No music clip is wired up yet — this exists so the
// Settings menu's Music slider has something real to control, and future background-music
// playback can just assign a clip to this same AudioSource without touching the Settings UI.
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _audioSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void SetVolume(float volume)
    {
        if (Instance != null && Instance._audioSource != null)
            Instance._audioSource.volume = volume;
    }
}
