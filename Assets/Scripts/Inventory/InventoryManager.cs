using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [SerializeField] private int _startingSlotCount = 40;
    private List<InventorySlot> _slots;

    public IReadOnlyList<InventorySlot> Slots => _slots;
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

    private InventorySlot FindEmptySlot()
    {
        foreach (InventorySlot slot in _slots)
        {
            if (slot.IsEmpty) return slot;
        }

        return null;
    }
}
