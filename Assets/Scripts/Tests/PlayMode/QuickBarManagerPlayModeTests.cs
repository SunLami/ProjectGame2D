using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class QuickBarManagerPlayModeTests
{
    private ItemSO _item;
    private FakeResolver _resolver;

    [SetUp]
    public void SetUp()
    {
        if (QuickBarManager.Instance == null)
            new GameObject("QuickBarManagerTest").AddComponent<QuickBarManager>();
        _item = ScriptableObject.CreateInstance<ItemSO>();
        _item.itemId = "item.test.quickbar";
        _item.itemName = "Quick Bar Test";
        _resolver = new FakeResolver(new Dictionary<string, ItemSO> { [_item.itemId] = _item });
        QuickBarManager.Instance.ConfigureResolverForTests(_resolver);
        QuickBarManager.Instance.RestoreState(new QuickBarSaveData());
    }

    [TearDown]
    public void TearDown()
    {
        if (QuickBarManager.Instance != null)
            QuickBarManager.Instance.RestoreState(new QuickBarSaveData());
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

    private sealed class FakeResolver : IItemResolver
    {
        private readonly Dictionary<string, ItemSO> _items;
        public FakeResolver(Dictionary<string, ItemSO> items) => _items = items;
        public bool TryResolve(string itemId, out ItemSO item) => _items.TryGetValue(itemId ?? "", out item);
    }
}
