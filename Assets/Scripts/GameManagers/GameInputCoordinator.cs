using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)]
public sealed class GameInputCoordinator : MonoBehaviour
{
    private const string GameplayMapName = "Gameplay";
    private const string InventoryActionName = "Inventory";
    private const string UiCancelActionPath = "UI/Cancel";

    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private InputActionAsset _projectActions;
    [SerializeField] private InventoryWindowUI _inventoryWindow;

    private InputAction _inventoryAction;
    private InputAction _cancelAction;

    private void Awake()
    {
        if (_playerInput == null)
            _playerInput = GetComponentInChildren<PlayerInput>(true);

        _inventoryAction = _playerInput != null
            ? _playerInput.actions.FindAction(GameplayMapName + "/" + InventoryActionName, false)
            : null;
        _cancelAction = _projectActions != null
            ? _projectActions.FindAction(UiCancelActionPath, false)
            : null;
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged += HandleStateChanged;

        if (_inventoryAction != null)
            _inventoryAction.performed += HandleInventoryPerformed;

        if (_cancelAction != null)
        {
            _cancelAction.performed += HandleCancelPerformed;
            _cancelAction.Enable();
        }

        RefreshGameplayInput();
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= HandleStateChanged;

        if (_inventoryAction != null)
            _inventoryAction.performed -= HandleInventoryPerformed;

        if (_cancelAction != null)
        {
            _cancelAction.performed -= HandleCancelPerformed;
            _cancelAction.Disable();
        }

        if (_playerInput != null)
            _playerInput.DeactivateInput();
    }

    private void HandleStateChanged(GameStateChange change)
    {
        RefreshGameplayInput();
    }

    private void RefreshGameplayInput()
    {
        // EventSystem needs only the shared UI map. PlayerInput owns its gameplay action copy.
        if (_projectActions != null)
            _projectActions.FindActionMap(GameplayMapName, false)?.Disable();

        if (_playerInput == null)
            return;

        if (GameStateManager.AllowsGameplayInput)
        {
            _playerInput.ActivateInput();
            if (_playerInput.currentActionMap == null
                || _playerInput.currentActionMap.name != GameplayMapName)
            {
                _playerInput.SwitchCurrentActionMap(GameplayMapName);
            }
        }
        else
        {
            _playerInput.DeactivateInput();
        }
    }


    private void HandleInventoryPerformed(InputAction.CallbackContext context)
    {
        if (GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing
            || _inventoryWindow == null)
        {
            return;
        }

        _inventoryWindow.OpenWindow();
    }

    private void HandleCancelPerformed(InputAction.CallbackContext context)
    {
        if (GameStateManager.Instance == null)
            return;

        switch (GameStateManager.Instance.CurrentState)
        {
            case GameState.Playing:
                GameStateManager.Instance.Pause();
                break;
            case GameState.Paused:
            case GameState.GameplayMenu:
                GameStateManager.Instance.ReturnToPreviousState();
                break;
        }
    }
}
