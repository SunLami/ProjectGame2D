using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _goldText;

    private void OnEnable()
    {
        if (InventoryManager.Instance == null)
        {
            if (_goldText != null) _goldText.text = "0";
            return;
        }

        InventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (InventoryManager.Instance == null || _goldText == null) return;
        _goldText.text = InventoryManager.Instance.Gold.ToString();
    }
}
