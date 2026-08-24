using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Presentation adapter for the Main Menu save-slot flow.</summary>
public sealed class MainMenuSaveSlotsUI : MonoBehaviour
{
    [Serializable]
    private sealed class SlotView
    {
        public int slotId;
        public TMP_Text title;
        public TMP_Text status;
        public TMP_Text details;
        public Button primaryButton;
        public TMP_Text primaryLabel;
        public Button deleteButton;
    }

    private enum SlotMode
    {
        NewGame,
        Continue
    }

    [Header("Contract")]
    [SerializeField] private MainMenuController _controller;
    [SerializeField] private InputSystemUIInputModule _inputModule;

    [Header("Pages")]
    [SerializeField] private GameObject _landingPage;
    [SerializeField] private GameObject _slotPage;
    [SerializeField] private TMP_Text _slotPageTitle;
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private SlotView[] _slots;

    [Header("Main Menu Settings")]
    [SerializeField] private GameObject _settingsPage;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Toggle _fullScreenToggle;
    [SerializeField] private Button _settingsSaveButton;
    [SerializeField] private Button _settingsCancelButton;

    [Header("Feedback")]
    [SerializeField] private CanvasGroup _interactionGroup;
    [SerializeField] private GameObject _confirmPopup;
    [SerializeField] private TMP_Text _confirmMessage;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _confirmCancelButton;
    [SerializeField] private GameObject _errorPopup;
    [SerializeField] private TMP_Text _errorMessage;
    [SerializeField] private Button _errorCloseButton;
    [SerializeField] private GameObject _loadingOverlay;

    private SlotMode _mode;
    private SaveSlotInfo[] _slotInfo = Array.Empty<SaveSlotInfo>();
    private Action _pendingConfirmation;
    private bool _isLoading;
    private SettingsSnapshot _settingsSnapshot;

    private void Awake()
    {
        _newGameButton.onClick.AddListener(() => OpenSlots(SlotMode.NewGame));
        _continueButton.onClick.AddListener(() => OpenSlots(SlotMode.Continue));
        _settingsButton.onClick.AddListener(OpenSettings);
        _quitButton.onClick.AddListener(RequestQuit);
        _backButton.onClick.AddListener(ShowLanding);
        _sfxSlider.onValueChanged.AddListener(value => SettingsService.Instance.SetSfxVolume(value));
        _musicSlider.onValueChanged.AddListener(value => SettingsService.Instance.SetMusicVolume(value));
        _fullScreenToggle.onValueChanged.AddListener(value => SettingsService.Instance.SetFullScreen(value));
        _settingsSaveButton.onClick.AddListener(SaveSettings);
        _settingsCancelButton.onClick.AddListener(CancelSettings);
        _confirmButton.onClick.AddListener(ConfirmPendingAction);
        _confirmCancelButton.onClick.AddListener(CloseConfirm);
        _errorCloseButton.onClick.AddListener(CloseError);

        foreach (SlotView slot in _slots)
        {
            int slotId = slot.slotId;
            slot.primaryButton.onClick.AddListener(() => OnPrimaryAction(slotId));
            slot.deleteButton.onClick.AddListener(() => RequestDelete(slotId));
        }
    }

    private void OnEnable()
    {
        _controller.OnSaveSlotListChanged += RebuildSlots;
        _controller.OnOperationFailed += ShowError;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged += OnGameStateChanged;

        if (_inputModule != null && _inputModule.cancel != null)
            _inputModule.cancel.action.performed += OnCancelPerformed;

        if (SettingsService.Instance != null)
            SettingsService.Instance.Changed += ApplySettings;

        SetLoading(GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Loading);
        ShowLanding();
        RebuildSlots(_controller.RefreshSlots());
    }

    private IEnumerator Start()
    {
        yield return null;

        if (!_isLoading && _landingPage.activeSelf)
            Select(_newGameButton);
    }

    private void OnDisable()
    {
        _controller.OnSaveSlotListChanged -= RebuildSlots;
        _controller.OnOperationFailed -= ShowError;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= OnGameStateChanged;

        if (_inputModule != null && _inputModule.cancel != null)
            _inputModule.cancel.action.performed -= OnCancelPerformed;

        if (SettingsService.Instance != null)
            SettingsService.Instance.Changed -= ApplySettings;
    }

    private void ShowLanding()
    {
        if (_isLoading)
            return;

        CloseConfirm();
        CloseError();
        _slotPage.SetActive(false);
        _settingsPage.SetActive(false);
        _landingPage.SetActive(true);
        Select(_newGameButton);
    }

    private void OpenSlots(SlotMode mode)
    {
        if (_isLoading)
            return;

        _mode = mode;
        _slotPageTitle.text = mode == SlotMode.NewGame ? "NEW GAME" : "CONTINUE";
        _landingPage.SetActive(false);
        _slotPage.SetActive(true);
        RebuildSlots(_controller.RefreshSlots());
        SelectFirstAvailableSlot();
    }

    private void RebuildSlots(SaveSlotInfo[] slots)
    {
        _slotInfo = slots ?? Array.Empty<SaveSlotInfo>();

        foreach (SlotView view in _slots)
        {
            if (!TryGetSlot(view.slotId, out SaveSlotInfo info))
                continue;

            view.title.text = $"SLOT {info.SlotId}";
            ApplySlotInfo(view, info);
        }

        _continueButton.interactable = Array.Exists(_slotInfo, slot => slot.Status == SaveSlotStatus.Valid);
    }

    private void ApplySlotInfo(SlotView view, SaveSlotInfo info)
    {
        switch (info.Status)
        {
            case SaveSlotStatus.Empty:
                view.status.text = "EMPTY";
                view.details.text = "No saved adventure";
                view.primaryLabel.text = _mode == SlotMode.NewGame ? "CREATE" : "UNAVAILABLE";
                view.primaryButton.interactable = _mode == SlotMode.NewGame;
                view.deleteButton.gameObject.SetActive(false);
                break;

            case SaveSlotStatus.Valid:
                view.status.text = "SAVED GAME";
                view.details.text = FormatMetadata(info.Metadata);
                view.primaryLabel.text = _mode == SlotMode.NewGame ? "OVERWRITE" : "LOAD";
                view.primaryButton.interactable = _mode == SlotMode.NewGame || _controller.CanContinue(info.SlotId);
                view.deleteButton.gameObject.SetActive(true);
                break;

            case SaveSlotStatus.Corrupted:
                view.status.text = "CORRUPTED SAVE";
                view.details.text = "This slot cannot be loaded";
                view.primaryLabel.text = _mode == SlotMode.NewGame ? "OVERWRITE" : "UNAVAILABLE";
                view.primaryButton.interactable = _mode == SlotMode.NewGame;
                view.deleteButton.gameObject.SetActive(true);
                break;

            case SaveSlotStatus.IncompatibleVersion:
                view.status.text = "INCOMPATIBLE VERSION";
                view.details.text = "Update or delete this slot";
                view.primaryLabel.text = _mode == SlotMode.NewGame ? "OVERWRITE" : "UNAVAILABLE";
                view.primaryButton.interactable = _mode == SlotMode.NewGame;
                view.deleteButton.gameObject.SetActive(true);
                break;
        }
    }

    private void OnPrimaryAction(int slotId)
    {
        if (_isLoading || !TryGetSlot(slotId, out SaveSlotInfo info))
            return;

        if (_mode == SlotMode.Continue)
        {
            if (_controller.CanContinue(slotId))
                _controller.RequestContinue(slotId);
            return;
        }

        if (_controller.SlotRequiresOverwriteConfirm(slotId))
        {
            ShowConfirm($"OVERWRITE SLOT {slotId}?\nExisting save data will be replaced.",
                () => _controller.RequestNewGame(slotId));
            return;
        }

        _controller.RequestNewGame(info.SlotId);
    }

    private void RequestDelete(int slotId)
    {
        if (_isLoading)
            return;

        ShowConfirm($"DELETE SLOT {slotId}?\nThis action cannot be undone.",
            () => _controller.DeleteSlot(slotId));
    }

    private void ShowConfirm(string message, Action action)
    {
        _pendingConfirmation = action;
        _confirmMessage.text = message;
        _confirmPopup.SetActive(true);
        Select(_confirmCancelButton);
    }

    private void ConfirmPendingAction()
    {
        Action action = _pendingConfirmation;
        CloseConfirm();
        action?.Invoke();
    }

    private void CloseConfirm()
    {
        _pendingConfirmation = null;
        _confirmPopup.SetActive(false);
        if (_slotPage.activeSelf)
            SelectFirstAvailableSlot();
    }

    private void ShowError(string message)
    {
        SetLoading(false);
        _errorMessage.text = string.IsNullOrWhiteSpace(message) ? "The operation could not be completed." : message;
        _errorPopup.SetActive(true);
        Select(_errorCloseButton);
    }

    private void CloseError()
    {
        _errorPopup.SetActive(false);
        if (_slotPage.activeSelf)
            SelectFirstAvailableSlot();
        else
            Select(_newGameButton);
    }

    private void OnGameStateChanged(GameStateChange change) => SetLoading(change.Current.State == GameState.Loading);

    private void OpenSettings()
    {
        if (_isLoading)
            return;

        _settingsSnapshot = SettingsService.Instance.Current;
        ApplySettings(_settingsSnapshot);
        _landingPage.SetActive(false);
        _settingsPage.SetActive(true);
        Select(_sfxSlider);
    }

    private void SaveSettings()
    {
        SettingsService.Instance.Save();
        ShowLanding();
    }

    private void CancelSettings()
    {
        SettingsService.Instance.Restore(_settingsSnapshot);
        ShowLanding();
    }

    private void ApplySettings(SettingsSnapshot snapshot)
    {
        _sfxSlider.SetValueWithoutNotify(snapshot.SfxVolume);
        _musicSlider.SetValueWithoutNotify(snapshot.MusicVolume);
        _fullScreenToggle.SetIsOnWithoutNotify(snapshot.FullScreen);
    }

    private void RequestQuit()
    {
        if (_isLoading)
            return;

        ShowConfirm("QUIT TO DESKTOP?", UnityEngine.Application.Quit);
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (_isLoading)
            return;

        if (_errorPopup.activeSelf)
            CloseError();
        else if (_confirmPopup.activeSelf)
            CloseConfirm();
        else if (_settingsPage.activeSelf)
            CancelSettings();
        else if (_slotPage.activeSelf)
            ShowLanding();
    }

    private void SetLoading(bool loading)
    {
        _isLoading = loading;
        _interactionGroup.interactable = !loading;
        _interactionGroup.blocksRaycasts = !loading;
        _loadingOverlay.SetActive(loading);
        if (loading)
            EventSystem.current?.SetSelectedGameObject(null);
    }

    private void SelectFirstAvailableSlot()
    {
        foreach (SlotView slot in _slots)
        {
            if (slot.primaryButton.interactable)
            {
                Select(slot.primaryButton);
                return;
            }
        }

        Select(_backButton);
    }

    private static void Select(Selectable selectable)
    {
        if (selectable != null && selectable.IsActive() && selectable.IsInteractable())
            EventSystem.current?.SetSelectedGameObject(selectable.gameObject);
    }

    private bool TryGetSlot(int slotId, out SaveSlotInfo result)
    {
        foreach (SaveSlotInfo slot in _slotInfo)
        {
            if (slot.SlotId == slotId)
            {
                result = slot;
                return true;
            }
        }

        result = default;
        return false;
    }

    private static string FormatMetadata(SaveSlotMetadata metadata)
    {
        if (metadata == null)
            return "Save metadata unavailable";

        TimeSpan playTime = TimeSpan.FromSeconds(Math.Max(0, metadata.totalPlayTimeSeconds));
        string savedAt = metadata.lastSavedUtcTicks > 0
            ? new DateTime(metadata.lastSavedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd  HH:mm")
            : "Unknown";

        string areaId = string.IsNullOrWhiteSpace(metadata.areaId) ? "Unknown" : metadata.areaId;

        return $"LEVEL  {metadata.characterLevel}\nAREA  {areaId}"
            + $"\nPLAY TIME  {playTime.TotalHours:00}:{playTime.Minutes:00}:{playTime.Seconds:00}"
            + $"\nLAST SAVE  {savedAt}";
    }
}
