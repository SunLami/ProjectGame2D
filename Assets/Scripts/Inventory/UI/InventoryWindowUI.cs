using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryWindowUI : MonoBehaviour
{
    [SerializeField] private GameObject _windowRoot;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            _windowRoot.SetActive(!_windowRoot.activeSelf);
        }
    }

    public void CloseWindow()
    {
        _windowRoot.SetActive(false);
    }
}
