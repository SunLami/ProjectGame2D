using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform _slotContainer;

    private readonly List<InventorySlotUI> _slotUIs = new List<InventorySlotUI>();
    private int _currentPage;

    private int SlotsPerPage => _slotUIs.Count;
    private int PageCount => Mathf.Max(1, Mathf.CeilToInt(InventoryManager.Instance.Slots.Count / (float)SlotsPerPage));

    private void OnEnable()
    {
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
        if (_slotUIs.Count == 0) return;

        int offset = _currentPage * SlotsPerPage;
        for (int i = 0; i < _slotUIs.Count; i++)
        {
            int globalIndex = offset + i;
            InventorySlot slot = globalIndex < InventoryManager.Instance.Slots.Count ? InventoryManager.Instance.Slots[globalIndex] : null;
            _slotUIs[i].SetSlot(slot);
        }
    }
}
