using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance;

    [SerializeField] private Transform _playerFootPos;

    private static AudioSource _audioSource;

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
        // Editor-only parent (e.g. "_Managers") keeps the Hierarchy tidy; detach before
        // DontDestroyOnLoad, which only works on root GameObjects.
        transform.SetParent(null);
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
