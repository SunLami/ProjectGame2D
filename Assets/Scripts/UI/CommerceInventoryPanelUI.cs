using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CommerceInventoryPanelUI : MonoBehaviour
{
    [SerializeField] private Transform _gridRoot;
    [SerializeField] private InventorySlotUI _slotTemplate;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private int _slotCapacity;
    private readonly List<InventorySlotUI> _slots = new();

    private void OnEnable()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_gridRoot == null || _slotTemplate == null || InventoryManager.Instance == null) return;
        IReadOnlyList<InventorySlot> source = InventoryManager.Instance.Slots;
        int visible = _slotCapacity > 0 ? Mathf.Min(_slotCapacity, source.Count) : source.Count;
        while (_slots.Count < visible)
        {
            InventorySlotUI slot = Instantiate(_slotTemplate, _gridRoot);
            slot.gameObject.SetActive(true);
            _slots.Add(slot);
        }
        for (int i = 0; i < _slots.Count; i++)
        {
            bool active = i < visible;
            _slots[i].gameObject.SetActive(active);
            if (active) _slots[i].SetSlot(source[i]);
        }
        if (_goldText != null) _goldText.text = InventoryManager.Instance.Gold.ToString("N0");
    }
}
