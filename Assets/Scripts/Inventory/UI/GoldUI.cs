using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _goldText;

    private void OnEnable()
    {
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
        _goldText.text = InventoryManager.Instance.Gold.ToString();
    }
}
