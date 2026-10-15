using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform _slotContainer;

    private readonly List<InventorySlotUI> _slotUIs = new List<InventorySlotUI>();
    private int _currentPage;
    private InventoryCategory _category;
    private readonly List<InventorySlot> _visibleSlots = new();

    public void SelectCategory(InventoryCategory category)
    {
        _category = category;
        _currentPage = 0;
        InventoryItemTooltipUI.Instance?.Hide();
        Refresh();
        var scroll = _slotContainer.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    private int SlotsPerPage => _slotUIs.Count;
    private int PageCount => InventoryManager.Instance == null || SlotsPerPage == 0
        ? 1
        : Mathf.Max(1, Mathf.CeilToInt(_visibleSlots.Count / (float)SlotsPerPage));

    private void OnEnable()
    {
        _category = InventoryCategory.All;
        _currentPage = 0;
        if (InventoryManager.Instance == null) return;
        InventoryManager.Instance.OnInventoryChanged += Refresh;

        if (_slotUIs.Count == 0)
        {
            _slotContainer.GetComponentsInChildren(true, _slotUIs);
        }

        Refresh();
        StartCoroutine(ForceLayoutNextFrame());
    }

    // GridLayoutGroup's own OnEnable runs after this component's OnEnable (parent-before-child),
    // so forcing a layout rebuild synchronously here would run before it's ready. Deferring one
    // frame guarantees the grid is correctly sized as soon as the (previously inactive) window
    // becomes visible, instead of flashing at stale/default sizes for a frame.
    private IEnumerator ForceLayoutNextFrame()
    {
        yield return null;
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_slotContainer as RectTransform);
        var scroll = _slotContainer.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }
    }

    public void NextPage()
    {
        if (_currentPage >= PageCount - 1) return;
        _currentPage++;
        Refresh();
    }

    public void PreviousPage()
    {
        if (_currentPage <= 0) return;
        _currentPage--;
        Refresh();
    }

    private void Refresh()
    {
        if (_slotUIs.Count == 0 || InventoryManager.Instance == null) return;

        int offset = _currentPage * SlotsPerPage;
        _visibleSlots.Clear();
        foreach (InventorySlot slot in InventoryManager.Instance.Slots)
            if (_category == InventoryCategory.All || (!slot.IsEmpty &&
                (_category == InventoryCategory.Equipment ? slot.item is EquipmentItemSO :
                (slot.item.inventoryCategory & _category) != 0)))
                _visibleSlots.Add(slot);
        for (int i = 0; i < _slotUIs.Count; i++)
        {
            int globalIndex = offset + i;
            InventorySlot slot = globalIndex < _visibleSlots.Count ? _visibleSlots[globalIndex] : null;
            _slotUIs[i].SetSlot(slot);
            int visibleCells = Mathf.Max(30, Mathf.CeilToInt(Mathf.Min(_visibleSlots.Count - offset, SlotsPerPage) / 6f) * 6);
            _slotUIs[i].gameObject.SetActive(i < visibleCells);
        }
    }
}
