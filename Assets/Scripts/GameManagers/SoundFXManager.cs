using UnityEngine;
using UnityEngine.UI;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance;
    [SerializeField] private Transform _playerFootPos;
    private static AudioSource _audioSource;
    private static SoundFXLibrary _soundFXLibrary;
    [SerializeField] private Slider _masterSoundSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _audioSource = GetComponent<AudioSource>();
            //_soundFXLibrary = GetComponent<SoundFXLibrary>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        _masterSoundSlider.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static void PlayFootSteps(float volumeScale)
    {
        AudioClip clip = MapManager.Instance.GetCurrentTileAudioClip(Instance._playerFootPos.position);

        if (clip != null)
        {
            Instance._audioSource.PlayOneShot(clip, volumeScale);
        }
    }

    public static void SetVolume(float volume)
    {
        if (Instance != null && Instance._audioSource != null)
        {
            Instance._audioSource.volume = volume;
        }
    }

    public void OnValueChanged()
    {
        if (_masterSoundSlider != null)
        {
            SetVolume(_masterSoundSlider.value);
        }
    }
}
