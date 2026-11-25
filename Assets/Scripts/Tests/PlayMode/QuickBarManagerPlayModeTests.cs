using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class QuickBarManagerPlayModeTests
{
    private ItemSO _item;
    private InventoryTestScope _inventoryScope;
    private FakeResolver _resolver;

    [SetUp]
    public void SetUp()
    {
        if (QuickBarManager.Instance == null)
            new GameObject("QuickBarManagerTest").AddComponent<QuickBarManager>();
        _inventoryScope = new InventoryTestScope();
        _item = ScriptableObject.CreateInstance<ItemSO>();
        _item.itemId = "item.test.quickbar";
        _item.usedItems = true;
        _item.itemName = "Quick Bar Test";
        InventoryManager.Instance.AddItem(_item, 1);
        _resolver = new FakeResolver(new Dictionary<string, ItemSO> { [_item.itemId] = _item });
        QuickBarManager.Instance.ConfigureResolverForTests(_resolver);
        QuickBarManager.Instance.RestoreState(new QuickBarSaveData());
    }

    [TearDown]
    public void TearDown()
    {
        if (QuickBarManager.Instance != null)
            QuickBarManager.Instance.RestoreState(new QuickBarSaveData());
        _inventoryScope.Dispose();
        Object.DestroyImmediate(_item);
    }

    [Test]
    public void AssignSelectAndSave_RoundTripsStableItemId()
    {
        Assert.IsTrue(QuickBarManager.Instance.Assign(3, _item));
        Assert.IsTrue(QuickBarManager.Instance.Select(3));
        QuickBarSaveData saved = QuickBarManager.Instance.ToSaveData();

        QuickBarManager.Instance.RestoreState(new QuickBarSaveData());
        QuickBarManager.Instance.RestoreState(saved);

        Assert.AreEqual(3, QuickBarManager.Instance.SelectedIndex);
        Assert.AreEqual(_item.itemId, QuickBarManager.Instance.GetAssignedItemId(3));
        Assert.AreSame(_item, QuickBarManager.Instance.SelectedItem);
    }

    [Test]
    public void Restore_UnknownItemClearsAssignmentAndReportsIt()
    {
        var data = new QuickBarSaveData();
        data.assignedItemIds[1] = "item.missing";
        List<string> missing = new();

        QuickBarManager.Instance.RestoreState(data, missing);

        Assert.IsNull(QuickBarManager.Instance.GetAssignedItemId(1));
        CollectionAssert.AreEqual(new[] { "item.missing" }, missing);
    }

    [Test]
    public void DragAssignedSlotOutsideQuickBar_ClearsOnlyTheAssignment()
    {
        GameObject eventSystemObject = null;
        if (EventSystem.current == null)
            eventSystemObject = new GameObject("QuickBarTestEventSystem", typeof(EventSystem));

        var slotObject = new GameObject("QuickBarSlotTest", typeof(RectTransform), typeof(QuickBarSlotUI));
        QuickBarSlotUI slot = slotObject.GetComponent<QuickBarSlotUI>();
        slot.Configure(0, null, null, null);
        Assert.IsTrue(QuickBarManager.Instance.Assign(0, _item));

        var eventData = new PointerEventData(EventSystem.current)
        {
            pointerDrag = slotObject,
            position = new Vector2(-1000f, -1000f)
        };
        slot.OnBeginDrag(eventData);
        slot.OnEndDrag(eventData);

        Assert.IsNull(QuickBarManager.Instance.GetAssignedItemId(0));
        Assert.AreSame(_item, _resolver.Resolve(_item.itemId));

        Object.DestroyImmediate(slotObject);
        if (eventSystemObject != null) Object.DestroyImmediate(eventSystemObject);
    }

    [Test]
    public void DropBetweenInventoryAndHudMirrors_KeepsSameIndexAssignment()
    {
        var events = new GameObject("MirrorTestEvents", typeof(EventSystem));
        var hud = new GameObject("HudMirror", typeof(RectTransform), typeof(QuickBarSlotUI));
        var inventory = new GameObject("InventoryMirror", typeof(RectTransform), typeof(QuickBarSlotUI));
        try
        {
            hud.GetComponent<QuickBarSlotUI>().Configure(0, null, null, null);
            inventory.GetComponent<QuickBarSlotUI>().Configure(0, null, null, null);
            QuickBarManager.Instance.Assign(0, _item);
            inventory.GetComponent<QuickBarSlotUI>().OnDrop(new PointerEventData(events.GetComponent<EventSystem>())
            {
                pointerDrag = hud
            });
            Assert.AreEqual(_item.itemId, QuickBarManager.Instance.GetAssignedItemId(0));
        }
        finally
        {
            Object.DestroyImmediate(hud);
            Object.DestroyImmediate(inventory);
            Object.DestroyImmediate(events);
        }
    }

    [Test]
    public void FourSlotContractRejectsLegacyIndicesAndRestoresFirstFour()
    {
        for (int i = 4; i < 8; i++)
        {
            Assert.IsFalse(QuickBarManager.Instance.Assign(i, _item));
            Assert.IsFalse(QuickBarManager.Instance.Select(i));
        }
        QuickBarManager.Instance.RestoreState(new QuickBarSaveData { selectedIndex = 7,
            assignedItemIds = new List<string> { _item.itemId, null, null, null, _item.itemId, null, null, null } });
        Assert.AreEqual(0, QuickBarManager.Instance.SelectedIndex);
        Assert.AreEqual(_item.itemId, QuickBarManager.Instance.GetAssignedItemId(0));
        Assert.AreEqual(4, QuickBarManager.Instance.ToSaveData().assignedItemIds.Count);
    }

    [Test]
    public void ReassignMovesExistingBindingAndDepletionClearsIt()
    {
        var quickBar = QuickBarManager.Instance;
        Assert.IsTrue(quickBar.Assign(0, _item));
        Assert.IsTrue(quickBar.Assign(3, _item));
        Assert.IsNull(quickBar.GetAssignedItemId(0));
        Assert.AreEqual(_item.itemId, quickBar.GetAssignedItemId(3));

        var inventory = InventoryManager.Instance;
        inventory.AddItem(_item, 1);
        Assert.IsTrue(inventory.RemoveItem(_item));
        Assert.AreEqual(_item.itemId, quickBar.GetAssignedItemId(3));
        Assert.IsTrue(inventory.RemoveItem(_item));
        Assert.IsNull(quickBar.GetAssignedItemId(3));
        Assert.IsFalse(quickBar.Assign(1, _item));
        inventory.AddItem(_item, 1);
        Assert.IsNull(quickBar.GetAssignedItemId(3));
    }

    [Test]
    public void RestoreKeepsFirstOwnedAssignmentFromLegacyDuplicates()
    {
        var quickBar = QuickBarManager.Instance;
        var saved = new QuickBarSaveData
        {
            assignedItemIds = new List<string> { _item.itemId, _item.itemId, _item.itemId, _item.itemId }
        };

        quickBar.RestoreState(saved);
        CollectionAssert.AreEqual(new[] { _item.itemId, null, null, null }, quickBar.ToSaveData().assignedItemIds);

        Assert.IsTrue(InventoryManager.Instance.RemoveItem(_item));
        quickBar.RestoreState(saved);
        CollectionAssert.AreEqual(new string[4], quickBar.ToSaveData().assignedItemIds);
    }

    private sealed class FakeResolver : IItemResolver
    {
        private readonly Dictionary<string, ItemSO> _items;
        public FakeResolver(Dictionary<string, ItemSO> items) => _items = items;
        public bool TryResolve(string itemId, out ItemSO item) => _items.TryGetValue(itemId ?? "", out item);
        public ItemSO Resolve(string itemId) => _items.TryGetValue(itemId ?? "", out ItemSO item) ? item : null;
    }
}
