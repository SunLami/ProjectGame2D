using UnityEngine;

// Pause/gameplay menu window (Resume/Settings/Inventory/Shop/Craft/Main Menu).
// Settings/Shop/Craft buttons are wired up but left non-interactable until those systems exist.
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _windowRoot;
    [SerializeField] private InventoryWindowUI _inventoryWindow;

    public bool IsOpen => _windowRoot != null && _windowRoot.activeSelf;

    private void OnEnable()
    {
        GameStateManager.Instance.StateChanged += HandleStateChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= HandleStateChanged;
    }

    public void OpenMenu()
    {
        GameStateManager.Instance.Pause();
    }

    public void CloseMenu()
    {
        GameStateManager.Instance.Resume();
    }

    public void OnResumeClicked()
    {
        CloseMenu();
    }

    public void OnInventoryClicked()
    {
        if (_inventoryWindow != null)
            _inventoryWindow.OpenWindow();
    }

    public void OnReturnToMainMenuClicked()
    {
        if (!SceneFlowService.Instance.TryReturnToMainMenu())
            Debug.LogWarning("Return to Main Menu was ignored because a scene transition is already running.", this);
    }


    private void HandleStateChanged(GameStateChange change) => Refresh();

    private void Refresh()
    {
        if (_windowRoot != null)
            _windowRoot.SetActive(GameStateManager.Instance.CurrentState == GameState.Paused);
    }
}
