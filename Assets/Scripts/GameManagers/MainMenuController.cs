using System;
using UnityEngine;

/// <summary>
/// Non-visual MainMenu contract for UI to call. Owns no Canvas/layout — only orchestrates
/// GameSessionManager/SaveRepository/SceneFlowService so the New Game/Continue slot UI can be
/// built independently (Codex UI handoff).
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string _gameplaySceneName = "DemoScene";

    /// <summary>Fired after RefreshSlots(), DeleteSlot(), or OnEnable so UI can rebuild its list.</summary>
    public event Action<SaveSlotInfo[]> OnSaveSlotListChanged;

    /// <summary>Fired with a user-facing reason when a requested operation could not start.</summary>
    public event Action<string> OnOperationFailed;

    private void OnEnable()
    {
        RefreshSlots();
    }

    public SaveSlotInfo[] RefreshSlots()
    {
        SaveSlotInfo[] slots = GameSessionManager.Instance.SaveRepository.GetAllSlotInfo();
        OnSaveSlotListChanged?.Invoke(slots);
        return slots;
    }

    public bool CanContinue(int slotId) =>
        GameSessionManager.Instance.SaveRepository.GetSlotInfo(slotId).Status == SaveSlotStatus.Valid;

    /// <summary>True when the slot already holds data and the UI should show an overwrite confirm
    /// popup before calling RequestNewGame.</summary>
    public bool SlotRequiresOverwriteConfirm(int slotId) =>
        GameSessionManager.Instance.SaveRepository.GetSlotInfo(slotId).Status != SaveSlotStatus.Empty;

    public void RequestNewGame(int slotId)
    {
        if (!CanStartRequest())
        {
            OnOperationFailed?.Invoke("A game session is already loading.");
            return;
        }

        GameSaveData saveData = NewGameFactory.CreateDefault();

        if (!GameSessionManager.Instance.TryStartNewGame(slotId, _gameplaySceneName, saveData))
        {
            OnOperationFailed?.Invoke("Could not start a new game session.");
            return;
        }

        if (!SceneFlowService.Instance.TryLoadGameplay(_gameplaySceneName))
            OnOperationFailed?.Invoke("Could not start loading the gameplay scene.");
    }

    public void RequestContinue(int slotId)
    {
        if (!CanStartRequest())
        {
            OnOperationFailed?.Invoke("A game session is already loading.");
            return;
        }

        if (!GameSessionManager.Instance.SaveRepository.TryReadSave(slotId, out GameSaveData saveData))
        {
            OnOperationFailed?.Invoke("Save slot is empty or unreadable.");
            return;
        }

        if (!GameSessionManager.Instance.TryStartLoadedGame(slotId, _gameplaySceneName, saveData))
        {
            OnOperationFailed?.Invoke("Could not start the loaded game session.");
            return;
        }

        if (!SceneFlowService.Instance.TryLoadGameplay(_gameplaySceneName))
            OnOperationFailed?.Invoke("Could not start loading the gameplay scene.");
    }

    /// <summary>Guards against double-submit: TryStartNewGame/TryStartLoadedGame would otherwise
    /// silently overwrite GameSessionManager.Current with a second GameSaveData while the first
    /// scene load is still in flight, and the eventually-loaded scene would restore whichever
    /// session happened to be current when it finished loading.</summary>
    private static bool CanStartRequest() =>
        SceneFlowService.Instance != null && !SceneFlowService.Instance.IsTransitioning
        && GameSessionManager.Instance != null && !GameSessionManager.Instance.HasActiveSession;

    public void DeleteSlot(int slotId)
    {
        SaveOperationResult result = GameSessionManager.Instance.SaveRepository.DeleteSlot(slotId);
        if (result.Success)
            RefreshSlots();
        else
            OnOperationFailed?.Invoke(result.ErrorMessage);
    }
}
