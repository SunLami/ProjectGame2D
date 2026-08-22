using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance;

    [SerializeField] private Transform _playerFootPos;

    private static AudioSource _audioSource;

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

    public static void PlayFootSteps(float volumeScale)
    {
        AudioClip clip = MapManager.Instance.GetCurrentTileAudioClip(Instance._playerFootPos.position);

        if (clip != null)
            _audioSource.PlayOneShot(clip, volumeScale);
    }

    public static void SetVolume(float volume)
    {
        if (_audioSource != null)
            _audioSource.volume = volume;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        _audioSource = null;
    }
}
