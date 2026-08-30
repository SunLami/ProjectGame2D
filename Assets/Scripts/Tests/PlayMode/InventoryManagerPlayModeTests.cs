using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class InventoryManagerPlayModeTests
{
    private GameObject _root;
    private ItemSO _stackable;
    private ItemSO _nonStackable;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("InventoryManagerFixture");
        _root.AddComponent<InventoryManager>();

        _stackable = ScriptableObject.CreateInstance<ItemSO>();
        _stackable.itemId = "test.stackable";
        _stackable.isStackable = true;
        _stackable.maxStackSize = 5;

        _nonStackable = ScriptableObject.CreateInstance<ItemSO>();
        _nonStackable.itemId = "test.nonstackable";
        _nonStackable.isStackable = false;
        _nonStackable.maxStackSize = 1;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_stackable);
        Object.DestroyImmediate(_nonStackable);
    }

    [Test]
    public void AddItem_StacksUpToMax_ThenUsesNewSlot()
    {
        InventoryManager.Instance.AddItem(_stackable, 5);
        InventoryManager.Instance.AddItem(_stackable, 3);

        Assert.IsTrue(InventoryManager.Instance.HasItem(_stackable, 8));

        int slotsUsed = 0;
        foreach (InventorySlot slot in InventoryManager.Instance.Slots)
        {
            if (slot.item == _stackable) slotsUsed++;
        }
        Assert.AreEqual(2, slotsUsed, "5+3 with max stack 5 must occupy two slots (5, 3).");
    }

    [Test]
    public void RemoveItem_RemovesAcrossStacksAndClearsEmptySlots()
    {
        InventoryManager.Instance.AddItem(_stackable, 8);
        Assert.IsTrue(InventoryManager.Instance.RemoveItem(_stackable, 6));
        Assert.IsTrue(InventoryManager.Instance.HasItem(_stackable, 2));
        Assert.IsFalse(InventoryManager.Instance.HasItem(_stackable, 3));
    }

    [Test]
    public void RemoveItem_InsufficientQuantity_FailsWithoutMutating()
    {
        InventoryManager.Instance.AddItem(_stackable, 2);
        Assert.IsFalse(InventoryManager.Instance.RemoveItem(_stackable, 5));
        Assert.IsTrue(InventoryManager.Instance.HasItem(_stackable, 2));
    }

    [Test]
    public void HasCapacityFor_MatchesWhatAddItemWouldDo()
    {
        Assert.IsTrue(InventoryManager.Instance.HasCapacityFor(_nonStackable, 1));

        // Fill every slot with the non-stackable item.
        int slotCount = InventoryManager.Instance.Slots.Count;
        InventoryManager.Instance.AddItem(_nonStackable, slotCount);

        Assert.IsFalse(InventoryManager.Instance.HasCapacityFor(_nonStackable, 1));
        Assert.IsFalse(InventoryManager.Instance.AddItem(_stackable, 1));
    }

    [Test]
    public void BatchCapacity_ReservesSlotsAcrossEveryLootEntry()
    {
        int slotCount = InventoryManager.Instance.Slots.Count;
        InventoryManager.Instance.AddItem(_nonStackable, slotCount - 1);
        var secondItem = ScriptableObject.CreateInstance<ItemSO>();
        secondItem.itemId = "test.second";
        secondItem.isStackable = false;
        secondItem.maxStackSize = 1;

        try
        {
            var twoDrops = new List<InventoryItemGrant>
            {
                new(_stackable, 1),
                new(secondItem, 1)
            };

            Assert.IsFalse(InventoryManager.Instance.HasCapacityForBatch(twoDrops));
            Assert.IsFalse(InventoryManager.Instance.TryAddBatch(twoDrops));
            Assert.IsFalse(InventoryManager.Instance.HasItem(_stackable, 1),
                "A rejected loot transaction must not partially add its first entry.");
            Assert.IsFalse(InventoryManager.Instance.HasItem(secondItem, 1));
        }
        finally
        {
            Object.DestroyImmediate(secondItem);
        }
    }

    [Test]
    public void ToSaveData_LoadFromSaveData_RoundTripsSlotsAndGold()
    {
        InventoryManager.Instance.AddItem(_stackable, 3);
        InventoryManager.Instance.AddGold(500);

        InventorySaveData data = InventoryManager.Instance.ToSaveData();

        var resolver = new FakeResolver(new Dictionary<string, ItemSO> { [_stackable.itemId] = _stackable });

        // Simulate a fresh load target by clearing first.
        InventoryManager.Instance.RemoveItem(_stackable, 3);
        InventoryManager.Instance.SpendGold(500);

        InventoryManager.Instance.LoadFromSaveData(data, resolver);

        Assert.AreEqual(500, InventoryManager.Instance.Gold);
        Assert.IsTrue(InventoryManager.Instance.HasItem(_stackable, 3));
    }

    [Test]
    public void LoadFromSaveData_UnresolvableItemId_ReportsMissingAndLeavesSlotEmpty()
    {
        InventorySaveData data = new();
        data.slots.Add(new InventorySaveData.SlotData { itemId = "item.unknown", quantity = 1 });

        var resolver = new FakeResolver(new Dictionary<string, ItemSO>());
        List<string> missing = new();

        InventoryManager.Instance.LoadFromSaveData(data, resolver, missing);

        Assert.AreEqual(1, missing.Count);
        Assert.AreEqual("item.unknown", missing[0]);
        Assert.IsTrue(InventoryManager.Instance.Slots[0].IsEmpty);
    }

    private sealed class FakeResolver : IItemResolver
    {
        private readonly Dictionary<string, ItemSO> _map;
        public FakeResolver(Dictionary<string, ItemSO> map) => _map = map;
        public bool TryResolve(string itemId, out ItemSO item) => _map.TryGetValue(itemId ?? "", out item);
    }
}
