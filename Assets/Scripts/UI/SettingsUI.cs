using UnityEngine;
using UnityEngine.UI;

// Settings window: SFX/Music volume, Full Screen vs Window Mode (mutually exclusive via ToggleGroup).
// Save persists to PlayerPrefs and closes; Decline reverts any unsaved live-preview changes and closes.
public class SettingsUI : MonoBehaviour
{
    private const string SfxVolumeKey = "Settings_SfxVolume";
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string FullScreenKey = "Settings_FullScreen";

    [SerializeField] private GameObject _windowRoot;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Toggle _fullScreenToggle;
    [SerializeField] private Toggle _windowModeToggle;
    [SerializeField] private Image _fullScreenToggleImage;
    [SerializeField] private Image _windowModeToggleImage;
    [SerializeField] private Sprite _checkedSprite;
    [SerializeField] private Sprite _uncheckedSprite;

    private float _savedSfxVolume;
    private float _savedMusicVolume;
    private bool _savedFullScreen;

    public bool IsOpen => _windowRoot != null && _windowRoot.activeSelf;

    private void Awake()
    {
        LoadSettings();
        ApplyToUIWithoutNotify();
    }

    private void Start()
    {
        // Deferred to Start so other manager singletons (SoundFXManager, MusicManager) have
        // finished their own Awake before we push volume values into them.
        ApplyToGame();
    }

    public void OpenWindow()
    {
        _savedSfxVolume = _sfxSlider.value;
        _savedMusicVolume = _musicSlider.value;
        _savedFullScreen = _fullScreenToggle.isOn;
        _windowRoot.SetActive(true);
    }

    public void CloseWindow()
    {
        _windowRoot.SetActive(false);
    }

    public void OnSfxSliderChanged(float value)
    {
        SoundFXManager.SetVolume(value);
    }

    public void OnMusicSliderChanged(float value)
    {
        MusicManager.SetVolume(value);
    }

    public void OnFullScreenToggled(bool isOn)
    {
        _fullScreenToggleImage.sprite = isOn ? _checkedSprite : _uncheckedSprite;
        if (isOn)
            Screen.fullScreen = true;
    }

    public void OnWindowModeToggled(bool isOn)
    {
        _windowModeToggleImage.sprite = isOn ? _checkedSprite : _uncheckedSprite;
        if (isOn)
            Screen.fullScreen = false;
    }

    public void OnSaveClicked()
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, _sfxSlider.value);
        PlayerPrefs.SetFloat(MusicVolumeKey, _musicSlider.value);
        PlayerPrefs.SetInt(FullScreenKey, _fullScreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
        CloseWindow();
    }

    public void OnDeclineClicked()
    {
        _sfxSlider.value = _savedSfxVolume;
        _musicSlider.value = _savedMusicVolume;
        _fullScreenToggle.isOn = _savedFullScreen;
        _windowModeToggle.isOn = !_savedFullScreen;

        SoundFXManager.SetVolume(_savedSfxVolume);
        MusicManager.SetVolume(_savedMusicVolume);
        Screen.fullScreen = _savedFullScreen;

        CloseWindow();
    }

    private void LoadSettings()
    {
        _savedSfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        _savedMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        _savedFullScreen = PlayerPrefs.GetInt(FullScreenKey, Screen.fullScreen ? 1 : 0) == 1;
    }

    private void ApplyToUIWithoutNotify()
    {
        _sfxSlider.SetValueWithoutNotify(_savedSfxVolume);
        _musicSlider.SetValueWithoutNotify(_savedMusicVolume);
        _fullScreenToggle.SetIsOnWithoutNotify(_savedFullScreen);
        _windowModeToggle.SetIsOnWithoutNotify(!_savedFullScreen);
        _fullScreenToggleImage.sprite = _savedFullScreen ? _checkedSprite : _uncheckedSprite;
        _windowModeToggleImage.sprite = !_savedFullScreen ? _checkedSprite : _uncheckedSprite;
    }

    private void ApplyToGame()
    {
        SoundFXManager.SetVolume(_savedSfxVolume);
        MusicManager.SetVolume(_savedMusicVolume);
        Screen.fullScreen = _savedFullScreen;
    }
}
