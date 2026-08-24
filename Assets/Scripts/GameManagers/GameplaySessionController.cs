using System;
using UnityEngine;

/// <summary>
/// Non-visual gameplay-scene controller (mirrors MainMenuController) for Save Game/Load Game/
/// Return Main Menu/Quit Desktop from the Pause Menu. Owns no Canvas/layout -- only orchestrates
/// GameSessionManager/ISaveSlotRepository/GameStateManager/SceneFlowService so Codex can build the
/// Pause Save/Load/Return/Quit UI against a stable API (Phase 9 Codex handoff).
///
/// Save capture always goes through PlayerSaveCapture + each domain's own ToSaveData() -- never
/// hand-builds or mutates GameSaveData fields directly. Every operation is atomic through
/// ISaveSlotRepository.WriteSave (FileSaveSlotRepository's existing temp-file/backup/replace
/// pipeline); this controller does no file I/O itself.
/// </summary>
public sealed class GameplaySessionController : MonoBehaviour
{
    [SerializeField] private string _gameplaySceneName = "DemoScene";
    [SerializeField] private PlayerStat _playerStat;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private WorldObjectRegistry _worldRegistry;

    private IApplicationQuitter _quitter;
    private int? _pendingSaveSlotId;

    /// <summary>Fired after RefreshSlots() or a successful Save/Load so UI can rebuild its slot list.</summary>
    public event Action<SaveSlotInfo[]> OnSaveSlotListChanged;

    /// <summary>Fired after a successful Save Game (manual, Save-and-Return or Save-and-Quit alike).
    /// Slot metadata/timestamp has already been refreshed by the time this fires.</summary>
    public event Action OnSaveSucceeded;

    /// <summary>Fired with a specific reason + user-facing message whenever a requested operation
    /// could not complete. Never fired for a Cancel -- Cancel is not a failure.</summary>
    public event Action<GameplaySessionOperationResult, string> OnOperationFailed;

    /// <summary>Fired instead of acting immediately when Return/Quit is requested on a dirty
    /// session. UI must present Save-and-X / X-Without-Saving / Cancel and call the matching
    /// Confirm*/Cancel* method -- this event itself changes no GameState (pure UI navigation).</summary>
    public event Action<GameplaySessionConfirmationKind> OnConfirmationRequired;

    /// <summary>Fired instead of writing immediately when RequestSaveToSlot targets a non-Empty
    /// slot (Valid, Corrupted or IncompatibleVersion -- carried in the second parameter so UI can
    /// word the confirm popup accordingly). UI must present an overwrite confirm and call
    /// ConfirmOverwriteAndSave() or CancelSaveToSlot() -- this event itself writes nothing.</summary>
    public event Action<int, SaveSlotStatus> OnSaveSlotConfirmationRequired;

    public int ActiveSlotId => GameSessionManager.Instance.Current.SlotId;
    public bool IsDirty => GameSessionManager.Instance.IsDirty;

    /// <summary>True while a Save or Load transition is in flight -- UI must block double-submit
    /// (Save/Load/Return/Quit buttons) while this is true. Backed by GameStateManager, not a
    /// separate flag, so it can never drift from the actual state machine.</summary>
    public bool IsBusy => GameStateManager.Instance.CurrentState is GameState.Saving or GameState.Loading;

    private IApplicationQuitter Quitter => _quitter ??= new UnityApplicationQuitter();

    internal void ConfigureForTests(
        PlayerStat playerStat, Transform playerTransform, WorldObjectRegistry worldRegistry,
        IApplicationQuitter quitter = null, string gameplaySceneName = null)
    {
        _playerStat = playerStat;
        _playerTransform = playerTransform;
        _worldRegistry = worldRegistry;
        _quitter = quitter;
        if (gameplaySceneName != null)
            _gameplaySceneName = gameplaySceneName;
    }

    public SaveSlotInfo[] RefreshSlots()
    {
        SaveSlotInfo[] slots = GameSessionManager.Instance.SaveRepository.GetAllSlotInfo();
        OnSaveSlotListChanged?.Invoke(slots);
        return slots;
    }

    public bool CanLoad(int slotId) =>
        GameSessionManager.Instance.SaveRepository.GetSlotInfo(slotId).Status == SaveSlotStatus.Valid;

    // ---- Save Game ----

    public bool RequestSave()
    {
        if (IsBusy)
        {
            Fail(GameplaySessionOperationResult.AlreadyBusy, "A save/load operation is already in progress.");
            return false;
        }

        if (!GameSessionManager.Instance.HasActiveSession)
        {
            Fail(GameplaySessionOperationResult.NoActiveSession, "There is no active game session to save.");
            return false;
        }

        return PerformSave();
    }

    /// <summary>True when slotId is a real slot (1..MaximumSlotId) and neither IsBusy nor
    /// "no active session" would reject a save right now -- UI can use this to enable/disable each
    /// slot button in a Save Game picker without duplicating the guard logic below.</summary>
    public bool CanSaveToSlot(int slotId) =>
        !IsBusy && GameSessionManager.Instance.HasActiveSession && IsValidSlotId(slotId);

    /// <summary>True when slotId already holds data (Valid, Corrupted or IncompatibleVersion) --
    /// RequestSaveToSlot will ask for confirmation instead of writing immediately. Mirrors
    /// MainMenuController.SlotRequiresOverwriteConfirm's semantics for the gameplay-scene picker.</summary>
    public bool SlotRequiresOverwriteConfirm(int slotId) =>
        IsValidSlotId(slotId) && GameSessionManager.Instance.SaveRepository.GetSlotInfo(slotId).Status != SaveSlotStatus.Empty;

    /// <summary>Save Game from the in-gameplay slot picker (distinct from the plain RequestSave()
    /// used by Save-and-Return/Save-and-Quit, which always targets the current ActiveSlotId
    /// without asking). Writes immediately into an Empty slot; fires OnSaveSlotConfirmationRequired
    /// instead of writing for a slot that already holds data (Valid/Corrupted/IncompatibleVersion --
    /// requirement: never overwrite any of those silently). On success, ActiveSlotId becomes
    /// slotId -- this is what makes saving into a slot other than the current one a "Save As".</summary>
    public void RequestSaveToSlot(int slotId)
    {
        if (IsBusy)
        {
            Fail(GameplaySessionOperationResult.AlreadyBusy, "A save/load operation is already in progress.");
            return;
        }

        if (!GameSessionManager.Instance.HasActiveSession)
        {
            Fail(GameplaySessionOperationResult.NoActiveSession, "There is no active game session to save.");
            return;
        }

        if (!IsValidSlotId(slotId))
        {
            Fail(GameplaySessionOperationResult.InvalidSlot, "That is not a valid save slot.");
            return;
        }

        SaveSlotStatus status = GameSessionManager.Instance.SaveRepository.GetSlotInfo(slotId).Status;
        if (status == SaveSlotStatus.Empty)
        {
            PerformSaveToSlot(slotId);
            return;
        }

        _pendingSaveSlotId = slotId;
        OnSaveSlotConfirmationRequired?.Invoke(slotId, status);
    }

    /// <summary>Writes the save that RequestSaveToSlot deferred for confirmation. No-op if there is
    /// no pending confirmation (e.g. called twice, or after CancelSaveToSlot) or while IsBusy.</summary>
    public void ConfirmOverwriteAndSave()
    {
        if (IsBusy || _pendingSaveSlotId == null)
            return;

        int slotId = _pendingSaveSlotId.Value;
        _pendingSaveSlotId = null;
        PerformSaveToSlot(slotId);
    }

    /// <summary>Pure UI navigation -- discards the pending slot, writes nothing, changes no other
    /// state. Present for API symmetry with CancelReturnToMainMenu/CancelQuit.</summary>
    public void CancelSaveToSlot()
    {
        _pendingSaveSlotId = null;
    }

    /// <summary>Deletes a save slot (Empty/Valid/Corrupted/IncompatibleVersion alike -- the
    /// repository's delete is unconditional). UI must have already shown its own confirm popup
    /// before calling this, exactly like MainMenuController.DeleteSlot -- this method performs the
    /// delete, it does not ask. Deleting the current ActiveSlotId is safe and does not touch the
    /// live session: GameSessionManager.Current keeps playing normally, and the next save to that
    /// slot id is simply treated as an Empty-slot save (SlotRequiresOverwriteConfirm goes back to
    /// false the moment the file is gone) -- there is no autosave in this codebase (D-012) that
    /// could "accidentally" target the deleted slot in the background.</summary>
    public bool DeleteSlot(int slotId)
    {
        if (IsBusy)
        {
            Fail(GameplaySessionOperationResult.AlreadyBusy, "A save/load operation is already in progress.");
            return false;
        }

        if (!IsValidSlotId(slotId))
        {
            Fail(GameplaySessionOperationResult.InvalidSlot, "That is not a valid save slot.");
            return false;
        }

        SaveOperationResult result = GameSessionManager.Instance.SaveRepository.DeleteSlot(slotId);
        if (!result.Success)
        {
            Fail(GameplaySessionOperationResult.WriteFailed, result.ErrorMessage);
            return false;
        }

        if (_pendingSaveSlotId == slotId)
            _pendingSaveSlotId = null;

        RefreshSlots();
        return true;
    }

    private static bool IsValidSlotId(int slotId) =>
        slotId >= GameSessionManager.MinimumSlotId && slotId <= GameSessionManager.MaximumSlotId;

    private bool PerformSave() => PerformSaveToSlot(GameSessionManager.Instance.Current.SlotId);

    private bool PerformSaveToSlot(int slotId)
    {
        GameStateManager.Instance.PushState(GameState.Saving);
        bool success = TryCaptureAndWriteSave(slotId, out string errorMessage);
        GameStateManager.Instance.ReturnToPreviousState();

        if (success)
        {
            GameSessionManager.Instance.SetActiveSlotId(slotId);
            RefreshSlots();
            OnSaveSucceeded?.Invoke();
        }
        else
        {
            Fail(GameplaySessionOperationResult.WriteFailed, errorMessage);
        }
        return success;
    }

    private bool TryCaptureAndWriteSave(int slotId, out string errorMessage)
    {
        GameSession session = GameSessionManager.Instance.Current;
        if (!session.IsActive || session.SaveData?.player?.location == null || _playerStat == null || _playerTransform == null)
        {
            errorMessage = "No active session to capture.";
            return false;
        }

        GameSaveData snapshot = CaptureSnapshot(session);
        SaveOperationResult result = GameSessionManager.Instance.SaveRepository.WriteSave(slotId, snapshot);
        if (result.Success)
        {
            GameSessionManager.Instance.ClearDirty();
            errorMessage = null;
            return true;
        }

        errorMessage = result.ErrorMessage;
        return false;
    }

    private GameSaveData CaptureSnapshot(GameSession session)
    {
        PlayerLocationSaveData previousLocation = session.SaveData.player.location;
        PlayerSaveData playerData = PlayerSaveCapture.Capture(
            _playerStat, _playerTransform, previousLocation.areaId, previousLocation.fallbackSpawnId);

        return new GameSaveData
        {
            saveId = session.SaveData.saveId,
            totalPlayTimeSeconds = GameSessionManager.Instance.GetTotalPlayTimeSeconds(),
            player = playerData,
            inventory = InventoryManager.Instance != null ? InventoryManager.Instance.ToSaveData() : session.SaveData.inventory,
            equipment = EquipmentManager.Instance != null ? EquipmentManager.Instance.ToSaveData() : session.SaveData.equipment,
            tutorial = TutorialManager.Instance != null ? TutorialManager.Instance.ToSaveData() : session.SaveData.tutorial,
            quests = QuestManager.Instance != null ? QuestManager.Instance.ToSaveData() : session.SaveData.quests,
            world = _worldRegistry != null ? _worldRegistry.ToSaveData() : session.SaveData.world
        };
    }

    // ---- Load Game ----

    public bool RequestLoad(int slotId)
    {
        if (IsBusy)
        {
            Fail(GameplaySessionOperationResult.AlreadyBusy, "A save/load operation is already in progress.");
            return false;
        }

        SaveSlotInfo info = GameSessionManager.Instance.SaveRepository.GetSlotInfo(slotId);
        if (info.Status != SaveSlotStatus.Valid)
        {
            Fail(GameplaySessionOperationResult.SlotNotValid, DescribeSlotStatus(info.Status));
            return false;
        }

        if (!GameSessionManager.Instance.SaveRepository.TryReadSave(slotId, out GameSaveData saveData))
        {
            Fail(GameplaySessionOperationResult.ReadFailed, "Could not read the selected save slot.");
            return false;
        }

        if (!GameSessionManager.Instance.TryStartLoadedGame(slotId, _gameplaySceneName, saveData))
        {
            Fail(GameplaySessionOperationResult.TransitionFailed, "Could not start the loaded game session.");
            return false;
        }

        if (!SceneFlowService.Instance.TryLoadGameplay(_gameplaySceneName))
        {
            Fail(GameplaySessionOperationResult.TransitionFailed, "Could not start loading the gameplay scene.");
            return false;
        }

        return true;
    }

    private static string DescribeSlotStatus(SaveSlotStatus status) => status switch
    {
        SaveSlotStatus.Empty => "That save slot is empty.",
        SaveSlotStatus.Corrupted => "That save file is corrupted and cannot be loaded.",
        SaveSlotStatus.IncompatibleVersion => "That save was created by an incompatible game version.",
        _ => "That save slot cannot be loaded."
    };

    // ---- Return to Main Menu ----

    public void RequestReturnToMainMenu()
    {
        if (IsBusy)
        {
            Fail(GameplaySessionOperationResult.AlreadyBusy, "A save/load operation is already in progress.");
            return;
        }

        if (!GameSessionManager.Instance.IsDirty)
        {
            DoReturnToMainMenu();
            return;
        }

        OnConfirmationRequired?.Invoke(GameplaySessionConfirmationKind.ReturnToMainMenu);
    }

    public void ConfirmSaveAndReturn()
    {
        if (IsBusy) return;

        if (!PerformSave())
            return; // Save failed: OnOperationFailed already fired, stay in the current Pause flow.

        DoReturnToMainMenu();
    }

    public void ConfirmReturnWithoutSaving()
    {
        if (IsBusy) return;
        DoReturnToMainMenu();
    }

    /// <summary>Pure UI navigation -- no backend state to change. Present for API symmetry/clarity.</summary>
    public void CancelReturnToMainMenu()
    {
    }

    private void DoReturnToMainMenu()
    {
        if (!SceneFlowService.Instance.TryReturnToMainMenu())
            Fail(GameplaySessionOperationResult.TransitionFailed, "Could not start returning to the main menu.");
    }

    // ---- Quit Desktop ----

    public void RequestQuit()
    {
        if (IsBusy)
        {
            Fail(GameplaySessionOperationResult.AlreadyBusy, "A save/load operation is already in progress.");
            return;
        }

        if (!GameSessionManager.Instance.IsDirty)
        {
            Quitter.Quit();
            return;
        }

        OnConfirmationRequired?.Invoke(GameplaySessionConfirmationKind.Quit);
    }

    public void ConfirmSaveAndQuit()
    {
        if (IsBusy) return;

        if (!PerformSave())
            return; // Save failed: OnOperationFailed already fired, never quit on a failed save.

        Quitter.Quit();
    }

    public void ConfirmQuitWithoutSaving()
    {
        if (IsBusy) return;
        Quitter.Quit();
    }

    /// <summary>Pure UI navigation -- no backend state to change. Present for API symmetry/clarity.</summary>
    public void CancelQuit()
    {
    }

    private void Fail(GameplaySessionOperationResult result, string message) =>
        OnOperationFailed?.Invoke(result, message);
}
