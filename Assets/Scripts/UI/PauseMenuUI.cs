using UnityEngine;
using UnityEngine.InputSystem;

// Esc-triggered pause/main menu window (Resume/Settings/Inventory/Shop/Craft/Quit).
// Settings/Shop/Craft buttons are wired up but left non-interactable until those systems exist.
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _windowRoot;
    [SerializeField] private InventoryWindowUI _inventoryWindow;

    public bool IsOpen => _windowRoot != null && _windowRoot.activeSelf;

    private void Awake()
    {
        // Guard against a stuck Time.timeScale (e.g. left at 0 from Editor testing) leaking
        // into a fresh play session and making the game look paused before Esc is ever pressed.
        if (!IsOpen)
            Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsOpen)
                CloseMenu();
            else
                OpenMenu();
        }
    }

    public void OpenMenu()
    {
        _windowRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseMenu()
    {
        _windowRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnResumeClicked()
    {
        CloseMenu();
    }

    public void OnInventoryClicked()
    {
        CloseMenu();
        if (_inventoryWindow != null)
            _inventoryWindow.OpenWindow();
    }

    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
