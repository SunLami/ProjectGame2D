using UnityEngine;

public class InventoryWindowUI : MonoBehaviour
{
    [SerializeField] private GameObject _windowRoot;

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

    public void CloseWindow()
    {
        if (IsInventoryOpen())
            GameStateManager.Instance.ReturnToPreviousState();
    }

    public void OpenWindow()
    {
        GameStateManager.Instance.OpenMenu(GameplayMenuPage.Inventory);
    }

    private bool IsInventoryOpen() =>
        GameStateManager.Instance.CurrentState == GameState.GameplayMenu
        && GameStateManager.Instance.CurrentMenuPage == GameplayMenuPage.Inventory;

    private void HandleStateChanged(GameStateChange change) => Refresh();

    private void Refresh()
    {
        if (_windowRoot != null)
            _windowRoot.SetActive(IsInventoryOpen());
    }
}
