using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Player-owned storage for Farming-domain items only (seeds and crop harvest items) --
/// separate from the general InventoryManager and from the one-shot persistent Chest. Deposit/
/// withdraw are free (no price), gated purely by domain validation and slot capacity. See
/// FarmingSystem.md "Farm storage" for the accepted contract.</summary>
[DefaultExecutionOrder(-850)]
public sealed class FarmStorageManager : MonoBehaviour
{
    [SerializeField] private int _slotCount = 40;
    private readonly List<FarmStorageSlot> _slots = new();

    public static FarmStorageManager Instance { get; private set; }
    public IReadOnlyList<FarmStorageSlot> Slots => _slots;
    public event Action Changed;

    public void EnsureSlotCount(int slotCount)
    {
        int target = Mathf.Max(0, slotCount);
        bool changed = false;
        while (_slots.Count < target)
        {
            _slots.Add(new FarmStorageSlot());
            changed = true;
        }

        if (changed)
            Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject(nameof(FarmStorageManager)).AddComponent<FarmStorageManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < _slotCount; i++)
            _slots.Add(new FarmStorageSlot());
    }

    /// <summary>A SeedItemSO, or any item that is some FarmingCatalog crop's harvest item. Nothing
    /// else may enter this storage -- this is what makes it a Farming silo rather than a second
    /// general inventory.</summary>
    public bool IsFarmingItem(ItemSO item)
    {
        if (item == null) return false;
        if (item is SeedItemSO) return true;

        FarmingCatalog catalog = FarmingManager.Instance != null ? FarmingManager.Instance.Catalog : null;
        if (catalog?.Crops == null) return false;

        foreach (CropDefinition crop in catalog.Crops)
            if (crop != null && crop.HarvestItem != null && crop.HarvestItem.itemId == item.itemId)
                return true;

        return false;
    }

    public bool HasCapacityFor(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return false;

        int remaining = amount;
        if (item.isStackable)
        {
            foreach (FarmStorageSlot slot in _slots)
            {
                if (slot.item != item || slot.quantity >= item.maxStackSize) continue;
                remaining -= item.maxStackSize - slot.quantity;
                if (remaining <= 0) return true;
            }
        }

        foreach (FarmStorageSlot slot in _slots)
        {
            if (!slot.IsEmpty) continue;
            remaining -= item.isStackable ? item.maxStackSize : 1;
            if (remaining <= 0) return true;
        }

        return remaining <= 0;
    }

    public bool TryDeposit(ItemSO item, int amount = 1)
    {
        if (!IsFarmingItem(item) || amount <= 0 || !HasCapacityFor(item, amount))
            return false;

        int remaining = amount;
        if (item.isStackable)
        {
            foreach (FarmStorageSlot slot in _slots)
            {
                if (slot.item != item || slot.quantity >= item.maxStackSize) continue;
                int toAdd = Mathf.Min(item.maxStackSize - slot.quantity, remaining);
                slot.quantity += toAdd;
                remaining -= toAdd;
                if (remaining <= 0) break;
            }
        }

        while (remaining > 0)
        {
            FarmStorageSlot emptySlot = FindEmptySlot();
            if (emptySlot == null) break;

            int toAdd = item.isStackable ? Mathf.Min(item.maxStackSize, remaining) : 1;
            emptySlot.item = item;
            emptySlot.quantity = toAdd;
            remaining -= toAdd;
        }

        Changed?.Invoke();
        return remaining <= 0;
    }

    public bool HasItem(ItemSO item, int amount = 1)
    {
        if (item == null) return false;
        int total = 0;
        foreach (FarmStorageSlot slot in _slots)
            if (slot.item == item) total += slot.quantity;
        return total >= amount;
    }

    public bool TryWithdraw(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0 || !HasItem(item, amount))
            return false;

        int remaining = amount;
        foreach (FarmStorageSlot slot in _slots)
        {
            if (slot.item != item) continue;
            int toRemove = Mathf.Min(slot.quantity, remaining);
            slot.quantity -= toRemove;
            remaining -= toRemove;
            if (slot.quantity <= 0) slot.Clear();
            if (remaining <= 0) break;
        }

        Changed?.Invoke();
        return true;
    }

    private FarmStorageSlot FindEmptySlot()
    {
        foreach (FarmStorageSlot slot in _slots)
            if (slot.IsEmpty) return slot;
        return null;
    }

    public FarmStorageSaveData ToSaveData()
    {
        var data = new FarmStorageSaveData();
        foreach (FarmStorageSlot slot in _slots)
        {
            data.slots.Add(new FarmStorageSaveData.SlotData
            {
                itemId = slot.IsEmpty ? null : slot.item.itemId,
                quantity = slot.quantity
            });
        }
        return data;
    }

    public void RestoreState(FarmStorageSaveData data, IItemResolver resolver, List<string> missingItemIds = null)
    {
        if (data?.slots != null)
            EnsureSlotCount(data.slots.Count);
        foreach (FarmStorageSlot slot in _slots) slot.Clear();
        if (data?.slots == null || resolver == null) return;

        int count = Mathf.Min(_slots.Count, data.slots.Count);
        for (int i = 0; i < count; i++)
        {
            FarmStorageSaveData.SlotData slotData = data.slots[i];
            if (slotData == null || string.IsNullOrWhiteSpace(slotData.itemId)) continue;

            if (!resolver.TryResolve(slotData.itemId, out ItemSO item))
            {
                missingItemIds?.Add(slotData.itemId);
                continue;
            }

            _slots[i].item = item;
            _slots[i].quantity = Mathf.Max(0, slotData.quantity);
        }

        Changed?.Invoke();
    }

    internal void ConfigureForTests(int slotCount)
    {
        _slots.Clear();
        for (int i = 0; i < slotCount; i++)
            _slots.Add(new FarmStorageSlot());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
