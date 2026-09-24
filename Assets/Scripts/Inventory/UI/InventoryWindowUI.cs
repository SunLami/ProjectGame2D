using System;
using UnityEngine;

public class InventoryWindowUI : MonoBehaviour
{
    public static event Action InventoryOpened;

    [SerializeField] private GameObject _windowRoot;

    private void OnEnable()
    {
        if (GameStateManager.Instance == null)
        {
            if (_windowRoot != null) _windowRoot.SetActive(false);
            return;
        }

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
        if (GameStateManager.Instance != null && IsInventoryOpen())
            GameStateManager.Instance.ReturnToPreviousState();
    }

    public void OpenWindow()
    {
        if (GameStateManager.Instance == null) return;
        GameStateManager.Instance.OpenMenu(GameplayMenuPage.Inventory);
        InventoryOpened?.Invoke();
    }

    private bool IsInventoryOpen() =>
        GameStateManager.Instance != null
        && GameStateManager.Instance.CurrentState == GameState.GameplayMenu
        && GameStateManager.Instance.CurrentMenuPage == GameplayMenuPage.Inventory;

    private void HandleStateChanged(GameStateChange change) => Refresh();

    private void Refresh()
    {
        if (_windowRoot != null)
        {
            bool open = IsInventoryOpen();
            _windowRoot.SetActive(open);
            if (open) _windowRoot.transform.SetAsLastSibling();
        }
    }
}
