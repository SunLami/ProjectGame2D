using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryCategoryTabUI : MonoBehaviour
{
    [SerializeField] private InventoryUI _inventory;
    [SerializeField] private InventoryCategory _category;
    [SerializeField] private Image _focus;

    private void OnEnable()
    {
        SetSelected(_category == InventoryCategory.All);
        GetComponent<Button>().onClick.AddListener(Select);
    }

    private void OnDisable() => GetComponent<Button>().onClick.RemoveListener(Select);

    private void Select()
    {
        _inventory.SelectCategory(_category);
        foreach (var tab in transform.parent.GetComponentsInChildren<InventoryCategoryTabUI>())
            tab.SetSelected(tab == this);
    }

    private void SetSelected(bool selected)
    {
        if (_focus != null) _focus.enabled = selected;
    }
}
