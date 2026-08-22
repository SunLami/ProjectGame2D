using UnityEngine;
using UnityEngine.UI;

// Gameplay settings presentation. Persistence and runtime application belong to SettingsService.
public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject _windowRoot;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Toggle _fullScreenToggle;
    [SerializeField] private Toggle _windowModeToggle;
    [SerializeField] private Image _fullScreenToggleImage;
    [SerializeField] private Image _windowModeToggleImage;
    [SerializeField] private Sprite _checkedSprite;
    [SerializeField] private Sprite _uncheckedSprite;

    private SettingsSnapshot _openSnapshot;

    public bool IsOpen => _windowRoot != null && _windowRoot.activeSelf;

    private void Awake()
    {
        ApplyToUIWithoutNotify(SettingsService.Instance.Current);
    }

    private void OnEnable()
    {
        GameStateManager.Instance.StateChanged += HandleStateChanged;
        SettingsService.Instance.Changed += HandleSettingsChanged;
        ApplyToUIWithoutNotify(SettingsService.Instance.Current);
        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= HandleStateChanged;

        if (SettingsService.Instance != null)
            SettingsService.Instance.Changed -= HandleSettingsChanged;
    }

    public void OpenWindow()
    {
        _openSnapshot = SettingsService.Instance.Current;
        ApplyToUIWithoutNotify(_openSnapshot);
        GameStateManager.Instance.OpenMenu(GameplayMenuPage.Settings);
    }

    public void CloseWindow()
    {
        if (_windowRoot != null)
            _windowRoot.SetActive(false);

        if (GameStateManager.Instance.CurrentState == GameState.GameplayMenu
            && GameStateManager.Instance.CurrentMenuPage == GameplayMenuPage.Settings)
        {
            GameStateManager.Instance.ReturnToPreviousState();
        }
    }

    public void OnSfxSliderChanged(float value)
    {
        SettingsService.Instance.SetSfxVolume(value);
    }

    public void OnMusicSliderChanged(float value)
    {
        SettingsService.Instance.SetMusicVolume(value);
    }

    public void OnFullScreenToggled(bool isOn)
    {
        RefreshToggleImages();
        if (isOn)
            SettingsService.Instance.SetFullScreen(true);
    }

    public void OnWindowModeToggled(bool isOn)
    {
        RefreshToggleImages();
        if (isOn)
            SettingsService.Instance.SetFullScreen(false);
    }

    public void OnSaveClicked()
    {
        SettingsService.Instance.Save();
        CloseWindow();
    }

    public void OnDeclineClicked()
    {
        SettingsService.Instance.Restore(_openSnapshot);
        CloseWindow();
    }

    private void HandleSettingsChanged(SettingsSnapshot snapshot)
    {
        ApplyToUIWithoutNotify(snapshot);
    }

    private void ApplyToUIWithoutNotify(SettingsSnapshot snapshot)
    {
        _sfxSlider.SetValueWithoutNotify(snapshot.SfxVolume);
        _musicSlider.SetValueWithoutNotify(snapshot.MusicVolume);
        _fullScreenToggle.SetIsOnWithoutNotify(snapshot.FullScreen);
        _windowModeToggle.SetIsOnWithoutNotify(!snapshot.FullScreen);
        RefreshToggleImages();
    }

    private void RefreshToggleImages()
    {
        _fullScreenToggleImage.sprite = _fullScreenToggle.isOn ? _checkedSprite : _uncheckedSprite;
        _windowModeToggleImage.sprite = _windowModeToggle.isOn ? _checkedSprite : _uncheckedSprite;
    }

    private void HandleStateChanged(GameStateChange change)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool shouldBeOpen = GameStateManager.Instance.CurrentState == GameState.GameplayMenu
            && GameStateManager.Instance.CurrentMenuPage == GameplayMenuPage.Settings;

        if (_windowRoot != null && _windowRoot.activeSelf != shouldBeOpen)
            _windowRoot.SetActive(shouldBeOpen);
    }
}
