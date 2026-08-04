using UnityEngine;
using UnityEngine.UI;

public class SoundFXManager : MonoBehaviour
{
    private static SoundFXManager Instance;
    private static AudioSource _audioSource;
    private static SoundFXLibrary _soundFXLibrary;
    [SerializeField] private Slider _soundFXSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _audioSource = GetComponent<AudioSource>();
            _soundFXLibrary = GetComponent<SoundFXLibrary>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        _soundFXSlider.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static void Play(string soundClipName, float volumeScale)
    {
        AudioClip clip = _soundFXLibrary.GetRandomClip(soundClipName);

        if (clip != null)
        {
            _audioSource.PlayOneShot(clip, volumeScale);
        }
    }

    public static void SetVolume(float volume)
    {
        if (_audioSource != null)
        {
            _audioSource.volume = volume;
        }
    }

    public void OnValueChanged()
    {
        if (_soundFXSlider != null)
        {
            SetVolume(_soundFXSlider.value);
        }
    }
}
