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

    private bool PerformSave()
    {
        GameStateManager.Instance.PushState(GameState.Saving);
        bool success = TryCaptureAndWriteActiveSave(out string errorMessage);
        GameStateManager.Instance.ReturnToPreviousState();

        if (success)
        {
            RefreshSlots();
            OnSaveSucceeded?.Invoke();
        }
        else
        {
            Fail(GameplaySessionOperationResult.WriteFailed, errorMessage);
        }
        return success;
    }

    private bool TryCaptureAndWriteActiveSave(out string errorMessage)
    {
        GameSession session = GameSessionManager.Instance.Current;
        if (!session.IsActive || session.SaveData?.player?.location == null || _playerStat == null || _playerTransform == null)
        {
            errorMessage = "No active session to capture.";
            return false;
        }

        GameSaveData snapshot = CaptureSnapshot(session);
        SaveOperationResult result = GameSessionManager.Instance.SaveRepository.WriteSave(session.SlotId, snapshot);
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
