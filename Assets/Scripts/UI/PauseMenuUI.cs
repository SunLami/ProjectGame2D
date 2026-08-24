using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pause/gameplay menu presentation. Save/load/return/quit orchestration belongs to GameplaySessionController.
public class PauseMenuUI : MonoBehaviour
{
    [Serializable]
    private sealed class LoadSlotView
    {
        public int slotId;
        public TMP_Text title;
        public TMP_Text status;
        public TMP_Text details;
        public Button loadButton;
        public TMP_Text actionLabel;
        public Button deleteButton;
    }

    private enum SlotConfirmationKind
    {
        None,
        Overwrite,
        Delete
    }

    [Header("Existing Pause Menu")]
    [SerializeField] private GameObject _windowRoot;
    [SerializeField] private InventoryWindowUI _inventoryWindow;

    [Header("Phase 9 Contract")]
    [SerializeField] private GameplaySessionController _sessionController;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _returnButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private TMP_Text _feedbackText;
    [SerializeField] private TMP_Text _dirtyIndicator;

    [Header("Load Slots")]
    [SerializeField] private GameObject _loadOverlay;
    [SerializeField] private TMP_Text _slotOverlayTitle;
    [SerializeField] private LoadSlotView[] _loadSlots;
    [SerializeField] private Button _loadBackButton;

    [Header("Confirmation")]
    [SerializeField] private GameObject _confirmationPopup;
    [SerializeField] private TMP_Text _confirmationTitle;
    [SerializeField] private Button _confirmationSaveButton;
    [SerializeField] private TMP_Text _confirmationSaveLabel;
    [SerializeField] private Button _confirmationWithoutSaveButton;
    [SerializeField] private TMP_Text _confirmationWithoutSaveLabel;
    [SerializeField] private Button _confirmationCancelButton;

    private GameplaySessionConfirmationKind? _confirmationKind;
    private SlotConfirmationKind _slotConfirmationKind;
    private int _pendingDeleteSlotId;
    private bool _isSaveSlotMode;
    private int _lastSlotSubmitFrame = -1;

    public bool IsOpen => _windowRoot != null && _windowRoot.activeSelf;

    private void Update()
    {
        bool cancel = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        cancel |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        if (!cancel || IsBusy())
            return;

        if (_confirmationPopup.activeSelf)
            CancelConfirmation();
        else if (_loadOverlay.activeSelf)
            CloseLoadOverlay();
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged += HandleStateChanged;

        if (_sessionController != null)
        {
            _sessionController.OnSaveSlotListChanged += RebuildLoadSlots;
            _sessionController.OnSaveSucceeded += HandleSaveSucceeded;
            _sessionController.OnOperationFailed += HandleOperationFailed;
            _sessionController.OnConfirmationRequired += ShowConfirmation;
            _sessionController.OnSaveSlotConfirmationRequired += ShowSaveSlotConfirmation;
        }

        if (GameSessionManager.Instance != null)
            GameSessionManager.Instance.DirtyStateChanged += HandleDirtyChanged;

        _saveButton.onClick.AddListener(OpenSaveOverlay);
        _loadButton.onClick.AddListener(OpenLoadOverlay);
        _quitButton.onClick.AddListener(RequestQuit);
        _loadBackButton.onClick.AddListener(CloseLoadOverlay);
        _confirmationSaveButton.onClick.AddListener(ConfirmSave);
        _confirmationWithoutSaveButton.onClick.AddListener(ConfirmWithoutSave);
        _confirmationCancelButton.onClick.AddListener(CancelConfirmation);

        CloseLoadOverlay();
        CloseConfirmation();
        Refresh();
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= HandleStateChanged;

        if (_sessionController != null)
        {
            _sessionController.OnSaveSlotListChanged -= RebuildLoadSlots;
            _sessionController.OnSaveSucceeded -= HandleSaveSucceeded;
            _sessionController.OnOperationFailed -= HandleOperationFailed;
            _sessionController.OnConfirmationRequired -= ShowConfirmation;
            _sessionController.OnSaveSlotConfirmationRequired -= ShowSaveSlotConfirmation;
        }

        if (GameSessionManager.Instance != null)
            GameSessionManager.Instance.DirtyStateChanged -= HandleDirtyChanged;

        _saveButton.onClick.RemoveListener(OpenSaveOverlay);
        _loadButton.onClick.RemoveListener(OpenLoadOverlay);
        _quitButton.onClick.RemoveListener(RequestQuit);
        _loadBackButton.onClick.RemoveListener(CloseLoadOverlay);
        _confirmationSaveButton.onClick.RemoveListener(ConfirmSave);
        _confirmationWithoutSaveButton.onClick.RemoveListener(ConfirmWithoutSave);
        _confirmationCancelButton.onClick.RemoveListener(CancelConfirmation);
    }

    public void OpenMenu() => GameStateManager.Instance.Pause();
    public void CloseMenu() => GameStateManager.Instance.Resume();
    public void OnResumeClicked() => CloseMenu();

    public void OnInventoryClicked()
    {
        if (_inventoryWindow != null)
            _inventoryWindow.OpenWindow();
    }

    public void OnReturnToMainMenuClicked()
    {
        if (!IsBusy())
            _sessionController.RequestReturnToMainMenu();
    }

    private void OpenSaveOverlay() => OpenSlotOverlay(true);

    private void OpenLoadOverlay() => OpenSlotOverlay(false);

    private void OpenSlotOverlay(bool saveMode)
    {
        if (IsBusy())
            return;

        _isSaveSlotMode = saveMode;
        _slotOverlayTitle.text = saveMode ? "SAVE GAME" : "LOAD GAME";
        _loadOverlay.SetActive(true);
        RebuildLoadSlots(_sessionController.RefreshSlots());
        SelectFirstSlotAction();
    }

    private void CloseLoadOverlay()
    {
        bool wasSaveMode = _isSaveSlotMode;
        _loadOverlay.SetActive(false);
        _isSaveSlotMode = false;
        Select(wasSaveMode ? _saveButton : _loadButton);
    }

    private void RequestSaveToSlot(int slotId)
    {
        if (IsBusy() || _lastSlotSubmitFrame == Time.frameCount || !_sessionController.CanSaveToSlot(slotId))
            return;

        _lastSlotSubmitFrame = Time.frameCount;
        _sessionController.RequestSaveToSlot(slotId);
    }

    private void RequestDeleteSlot(int slotId)
    {
        if (IsBusy())
            return;

        _pendingDeleteSlotId = slotId;
        _slotConfirmationKind = SlotConfirmationKind.Delete;
        _confirmationKind = null;
        _confirmationTitle.text = GetDeleteConfirmationText(slotId);
        _confirmationSaveLabel.text = "DELETE";
        _confirmationWithoutSaveButton.gameObject.SetActive(false);
        _confirmationPopup.SetActive(true);
        Select(_confirmationCancelButton);
    }

    private void RequestLoad(int slotId)
    {
        if (!IsBusy() && _sessionController.CanLoad(slotId))
            _sessionController.RequestLoad(slotId);
    }

    private void RequestQuit()
    {
        if (!IsBusy())
            _sessionController.RequestQuit();
    }

    private void RebuildLoadSlots(SaveSlotInfo[] slots)
    {
        foreach (LoadSlotView view in _loadSlots)
        {
            SaveSlotInfo info = FindSlot(slots, view.slotId);
            bool active = view.slotId == _sessionController.ActiveSlotId;
            view.title.text = $"SLOT {view.slotId}" + (active ? "  ACTIVE" : string.Empty);
            ApplySlot(view, info);
            view.loadButton.onClick.RemoveAllListeners();
            int slotId = view.slotId;
            view.loadButton.onClick.AddListener(() =>
            {
                if (_isSaveSlotMode)
                    RequestSaveToSlot(slotId);
                else
                    RequestLoad(slotId);
            });
            view.actionLabel.text = _isSaveSlotMode ? "SAVE" : "LOAD";
            view.loadButton.interactable = _isSaveSlotMode
                ? _sessionController.CanSaveToSlot(slotId)
                : !IsBusy() && _sessionController.CanLoad(slotId);

            view.deleteButton.onClick.RemoveAllListeners();
            view.deleteButton.onClick.AddListener(() => RequestDeleteSlot(slotId));
            view.deleteButton.gameObject.SetActive(_isSaveSlotMode && info.Status != SaveSlotStatus.Empty);
            view.deleteButton.interactable = !IsBusy();
        }
    }

    private static void ApplySlot(LoadSlotView view, SaveSlotInfo info)
    {
        switch (info.Status)
        {
            case SaveSlotStatus.Valid:
                view.status.text = "SAVED GAME";
                view.details.text = FormatMetadata(info.Metadata);
                break;
            case SaveSlotStatus.Empty:
                view.status.text = "EMPTY";
                view.details.text = "No saved adventure";
                break;
            case SaveSlotStatus.Corrupted:
                view.status.text = "CORRUPTED SAVE";
                view.details.text = "This slot cannot be loaded";
                break;
            case SaveSlotStatus.IncompatibleVersion:
                view.status.text = "INCOMPATIBLE VERSION";
                view.details.text = "Update or delete this slot";
                break;
        }
    }

    private void HandleSaveSucceeded()
    {
        SaveSlotInfo active = FindSlot(_sessionController.RefreshSlots(), _sessionController.ActiveSlotId);
        string timestamp = active.Metadata != null && active.Metadata.lastSavedUtcTicks > 0
            ? new DateTime(active.Metadata.lastSavedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd  HH:mm:ss")
            : "now";
        _feedbackText.text = $"GAME SAVED  {timestamp}";
        CloseConfirmation();
        if (_loadOverlay.activeSelf)
            CloseLoadOverlay();
        Refresh();
    }

    private void HandleOperationFailed(GameplaySessionOperationResult result, string message)
    {
        _feedbackText.text = string.IsNullOrWhiteSpace(message) ? "The operation could not be completed." : message;
        Refresh();
    }

    private void ShowConfirmation(GameplaySessionConfirmationKind kind)
    {
        _slotConfirmationKind = SlotConfirmationKind.None;
        _confirmationKind = kind;
        bool returning = kind == GameplaySessionConfirmationKind.ReturnToMainMenu;
        _confirmationTitle.text = returning ? "SAVE BEFORE RETURNING?" : "SAVE BEFORE QUITTING?";
        _confirmationSaveLabel.text = returning ? "SAVE AND RETURN" : "SAVE AND QUIT";
        _confirmationWithoutSaveLabel.text = returning ? "RETURN WITHOUT SAVING" : "QUIT WITHOUT SAVING";
        _confirmationWithoutSaveButton.gameObject.SetActive(true);
        _confirmationPopup.SetActive(true);
        Select(_confirmationCancelButton);
    }

    private void ShowSaveSlotConfirmation(int slotId, SaveSlotStatus status)
    {
        _slotConfirmationKind = SlotConfirmationKind.Overwrite;
        _confirmationKind = null;
        _confirmationTitle.text = GetOverwriteConfirmationText(slotId, status);
        _confirmationSaveLabel.text = "OVERWRITE";
        _confirmationWithoutSaveButton.gameObject.SetActive(false);
        _confirmationPopup.SetActive(true);
        Select(_confirmationCancelButton);
    }

    private void ConfirmSave()
    {
        if (_slotConfirmationKind == SlotConfirmationKind.Overwrite)
        {
            CloseConfirmation();
            _sessionController.ConfirmOverwriteAndSave();
            return;
        }

        if (_slotConfirmationKind == SlotConfirmationKind.Delete)
        {
            int slotId = _pendingDeleteSlotId;
            CloseConfirmation();
            _sessionController.DeleteSlot(slotId);
            return;
        }

        GameplaySessionConfirmationKind? kind = _confirmationKind;
        CloseConfirmation();
        if (kind == GameplaySessionConfirmationKind.ReturnToMainMenu)
            _sessionController.ConfirmSaveAndReturn();
        else if (kind == GameplaySessionConfirmationKind.Quit)
            _sessionController.ConfirmSaveAndQuit();
        Refresh();
    }

    private void ConfirmWithoutSave()
    {
        GameplaySessionConfirmationKind? kind = _confirmationKind;
        CloseConfirmation();
        if (kind == GameplaySessionConfirmationKind.ReturnToMainMenu)
            _sessionController.ConfirmReturnWithoutSaving();
        else if (kind == GameplaySessionConfirmationKind.Quit)
            _sessionController.ConfirmQuitWithoutSaving();
        Refresh();
    }

    private void CancelConfirmation()
    {
        if (_slotConfirmationKind == SlotConfirmationKind.Overwrite)
            _sessionController.CancelSaveToSlot();

        if (_slotConfirmationKind != SlotConfirmationKind.None)
        {
            CloseConfirmation();
            SelectFirstSlotAction();
            return;
        }

        GameplaySessionConfirmationKind? kind = _confirmationKind;
        CloseConfirmation();
        if (kind == GameplaySessionConfirmationKind.ReturnToMainMenu)
            _sessionController.CancelReturnToMainMenu();
        else if (kind == GameplaySessionConfirmationKind.Quit)
            _sessionController.CancelQuit();
    }

    private void CloseConfirmation()
    {
        _confirmationKind = null;
        _slotConfirmationKind = SlotConfirmationKind.None;
        _pendingDeleteSlotId = 0;
        _confirmationPopup.SetActive(false);
        _confirmationWithoutSaveButton.gameObject.SetActive(true);
    }

    private void HandleStateChanged(GameStateChange change) => Refresh();
    private void HandleDirtyChanged(bool dirty) => Refresh();

    private void Refresh()
    {
        if (_windowRoot != null)
            _windowRoot.SetActive(GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Paused);

        bool busy = IsBusy();
        _saveButton.interactable = !busy;
        _loadButton.interactable = !busy;
        _returnButton.interactable = !busy;
        _quitButton.interactable = !busy;
        _dirtyIndicator.text = _sessionController != null && _sessionController.IsDirty ? "UNSAVED CHANGES" : "ALL CHANGES SAVED";

        if (_loadOverlay.activeSelf)
            RebuildLoadSlots(_sessionController.RefreshSlots());
    }

    private bool IsBusy() => _sessionController == null || _sessionController.IsBusy;

    private void SelectFirstSlotAction()
    {
        foreach (LoadSlotView slot in _loadSlots)
        {
            if (slot.loadButton.interactable)
            {
                Select(slot.loadButton);
                return;
            }
        }
        Select(_loadBackButton);
    }

    private static void Select(Selectable selectable)
    {
        if (selectable != null && selectable.IsActive() && selectable.IsInteractable())
            EventSystem.current?.SetSelectedGameObject(selectable.gameObject);
    }

    private static SaveSlotInfo FindSlot(SaveSlotInfo[] slots, int slotId)
    {
        if (slots != null)
            foreach (SaveSlotInfo slot in slots)
                if (slot.SlotId == slotId)
                    return slot;
        return new SaveSlotInfo(slotId, SaveSlotStatus.Empty, null);
    }

    private static string GetOverwriteConfirmationText(int slotId, SaveSlotStatus status) => status switch
    {
        SaveSlotStatus.Valid => $"OVERWRITE THE SAVE IN SLOT {slotId}?",
        SaveSlotStatus.Corrupted => $"SLOT {slotId} IS CORRUPTED.\nDELETE IT AND SAVE HERE?",
        SaveSlotStatus.IncompatibleVersion => $"SLOT {slotId} IS INCOMPATIBLE.\nDELETE IT AND SAVE HERE?",
        _ => $"OVERWRITE SLOT {slotId}?"
    };

    private static string GetDeleteConfirmationText(int slotId) =>
        $"DELETE SAVE IN SLOT {slotId}?\nTHIS CANNOT BE UNDONE.";

    private static string FormatMetadata(SaveSlotMetadata metadata)
    {
        if (metadata == null)
            return "Save metadata unavailable";

        TimeSpan playTime = TimeSpan.FromSeconds(Math.Max(0, metadata.totalPlayTimeSeconds));
        string savedAt = metadata.lastSavedUtcTicks > 0
            ? new DateTime(metadata.lastSavedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd  HH:mm")
            : "Unknown";
        string area = string.IsNullOrWhiteSpace(metadata.areaId) ? "Unknown" : metadata.areaId;
        return $"LEVEL  {metadata.characterLevel}\nAREA  {area}\nPLAY TIME  {playTime.TotalHours:00}:{playTime.Minutes:00}:{playTime.Seconds:00}\nLAST SAVE  {savedAt}";
    }
}
