using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [SerializeField] private int _startingSlotCount = 40;
    [SerializeField] private int _gold;
    private List<InventorySlot> _slots;

    public IReadOnlyList<InventorySlot> Slots => _slots;
    public int Gold => _gold;
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _slots = new List<InventorySlot>(_startingSlotCount);
            for (int i = 0; i < _startingSlotCount; i++)
            {
                _slots.Add(new InventorySlot());
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddSlots(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            _slots.Add(new InventorySlot());
        }

        OnInventoryChanged?.Invoke();
    }

    public bool AddItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (item.isStackable)
        {
            foreach (InventorySlot slot in _slots)
            {
                if (slot.item != item || slot.quantity >= item.maxStackSize) continue;

                int space = item.maxStackSize - slot.quantity;
                int toAdd = Mathf.Min(space, amount);
                slot.quantity += toAdd;
                amount -= toAdd;

                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        bool addedAny = false;
        while (amount > 0)
        {
            InventorySlot emptySlot = FindEmptySlot();
            if (emptySlot == null) break;

            int toAdd = item.isStackable ? Mathf.Min(item.maxStackSize, amount) : 1;
            emptySlot.item = item;
            emptySlot.quantity = toAdd;
            amount -= toAdd;
            addedAny = true;
        }

        OnInventoryChanged?.Invoke();
        return addedAny && amount <= 0;
    }

    public bool RemoveItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0 || !HasItem(item, amount)) return false;

        int remaining = amount;
        foreach (InventorySlot slot in _slots)
        {
            if (slot.item != item) continue;

            int toRemove = Mathf.Min(slot.quantity, remaining);
            slot.quantity -= toRemove;
            remaining -= toRemove;

            if (slot.quantity <= 0) slot.Clear();
            if (remaining <= 0) break;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(ItemSO item, int amount = 1)
    {
        if (item == null) return false;

        int total = 0;
        foreach (InventorySlot slot in _slots)
        {
            if (slot.item == item) total += slot.quantity;
        }

        return total >= amount;
    }

    public void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void SwapItems(InventorySlot a, InventorySlot b)
    {
        if (a == null || b == null || a == b) return;

        (a.item, b.item) = (b.item, a.item);
        (a.quantity, b.quantity) = (b.quantity, a.quantity);

        OnInventoryChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        _gold += amount;
        OnInventoryChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || _gold < amount) return false;

        _gold -= amount;
        OnInventoryChanged?.Invoke();
        return true;
    }

    // Snapshots current contents into a plain serializable DTO (not tied to any ScriptableObject
    // asset) so it can be written to a per-player save file later.
    public InventorySaveData ToSaveData()
    {
        var data = new InventorySaveData();
        foreach (InventorySlot slot in _slots)
        {
            data.slots.Add(new InventorySaveData.SlotData
            {
                itemId = slot.IsEmpty ? null : slot.item.itemId,
                quantity = slot.quantity
            });
        }

        return data;
    }

    // Restores contents from a save-data snapshot, resolving each itemId through the given lookup
    // (build one via ItemLookup.BuildFromResources()). Grows slot count if the save has more slots
    // than currently exist; unmatched or unknown items are left empty rather than throwing.
    public void LoadFromSaveData(InventorySaveData data, IReadOnlyDictionary<string, ItemSO> itemLookup)
    {
        if (data == null || data.slots == null || itemLookup == null) return;

        if (data.slots.Count > _slots.Count)
        {
            AddSlots(data.slots.Count - _slots.Count);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i >= data.slots.Count)
            {
                _slots[i].Clear();
                continue;
            }

            InventorySaveData.SlotData slotData = data.slots[i];
            if (string.IsNullOrEmpty(slotData.itemId) || !itemLookup.TryGetValue(slotData.itemId, out ItemSO item))
            {
                _slots[i].Clear();
                continue;
            }

            _slots[i].item = item;
            _slots[i].quantity = slotData.quantity;
        }

        OnInventoryChanged?.Invoke();
    }

    private InventorySlot FindEmptySlot()
    {
        foreach (InventorySlot slot in _slots)
        {
            if (slot.IsEmpty) return slot;
        }

        return null;
    }
}
