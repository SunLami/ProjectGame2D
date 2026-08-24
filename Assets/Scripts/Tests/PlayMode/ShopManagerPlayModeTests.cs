using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ShopManagerPlayModeTests
{
    private GameObject _root;
    private GameObject _inventoryRoot;
    private ShopManager _manager;
    private readonly List<Object> _scratchAssets = new();

    [SetUp]
    public void SetUp()
    {
        // Every test starts from a known, gameplay-allowing state -- ShopManager gates on
        // GameStateManager.AllowsGameplayInput, and the singleton can carry state between tests.
        GameStateManager.Instance.ResetToPlaying();

        _inventoryRoot = new GameObject("InventoryManagerFixture");
        _inventoryRoot.AddComponent<InventoryManager>();

        _root = new GameObject("ShopManagerFixture");
        _manager = _root.AddComponent<ShopManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_inventoryRoot);
        foreach (Object asset in _scratchAssets)
            Object.DestroyImmediate(asset);
        _scratchAssets.Clear();
        GameStateManager.Instance.ResetToPlaying();
    }

    private ItemSO MakeItem(string itemId, bool stackable = true, int maxStack = 99)
    {
        var item = ScriptableObject.CreateInstance<ItemSO>();
        item.itemId = itemId;
        item.isStackable = stackable;
        item.maxStackSize = maxStack;
        _scratchAssets.Add(item);
        return item;
    }

    private ShopDefinition MakeShop(string shopId, string npcId, params (string itemId, int price)[] stock)
    {
        var entries = new ShopStockEntry[stock.Length];
        for (int i = 0; i < stock.Length; i++)
        {
            var entry = new ShopStockEntry();
            SetPrivate(entry, "_itemId", stock[i].itemId);
            SetPrivate(entry, "_price", stock[i].price);
            entries[i] = entry;
        }

        var definition = ScriptableObject.CreateInstance<ShopDefinition>();
        SetPrivate(definition, "_shopId", shopId);
        SetPrivate(definition, "_npcId", npcId);
        SetPrivate(definition, "_stock", entries);
        SetPrivate(definition, "_sellPriceMultiplier", 0.5f);
        _scratchAssets.Add(definition);
        return definition;
    }

    private ShopCatalog MakeCatalog(params ShopDefinition[] shops)
    {
        var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
        SetPrivate(catalog, "_shops", shops);
        _scratchAssets.Add(catalog);
        return catalog;
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private sealed class FakeItemResolver : IItemResolver
    {
        private readonly Dictionary<string, ItemSO> _map = new();
        public void Register(ItemSO item) => _map[item.itemId] = item;
        public bool TryResolve(string itemId, out ItemSO item) => _map.TryGetValue(itemId ?? "", out item);
    }

    [Test]
    public void TryPurchase_SpendsGoldAndGrantsItem_RaisesItemPurchasedExactlyOnce()
    {
        ItemSO wood = MakeItem("item.material.wood");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        ShopDefinition shop = MakeShop("shop.town.general", "npc.town.elder", ("item.material.wood", 5));
        _manager.ConfigureForTests(MakeCatalog(shop), resolver);
        InventoryManager.Instance.AddGold(100);

        int purchasedCount = 0;
        void OnItemPurchased(string id, int qty, string shopId)
        {
            purchasedCount++;
            Assert.AreEqual("item.material.wood", id);
            Assert.AreEqual(3, qty);
            Assert.AreEqual("shop.town.general", shopId);
        }

        QuestDomainEvents.ItemPurchased += OnItemPurchased;
        try
        {
            Assert.IsTrue(_manager.TryPurchase("shop.town.general", "item.material.wood", 3, out ShopTransactionResult result));
            Assert.AreEqual(ShopTransactionResult.Success, result);
            Assert.AreEqual(85, InventoryManager.Instance.Gold);
            Assert.IsTrue(InventoryManager.Instance.HasItem(wood, 3));
            Assert.AreEqual(1, purchasedCount);
        }
        finally
        {
            QuestDomainEvents.ItemPurchased -= OnItemPurchased;
        }
    }

    [Test]
    public void TryPurchase_InsufficientGold_SpendsNothingAndGrantsNothing()
    {
        ItemSO wood = MakeItem("item.material.wood");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        ShopDefinition shop = MakeShop("shop.town.general", "npc.town.elder", ("item.material.wood", 50));
        _manager.ConfigureForTests(MakeCatalog(shop), resolver);
        InventoryManager.Instance.AddGold(10);

        Assert.IsFalse(_manager.TryPurchase("shop.town.general", "item.material.wood", 1, out ShopTransactionResult result));
        Assert.AreEqual(ShopTransactionResult.InsufficientGold, result);
        Assert.AreEqual(10, InventoryManager.Instance.Gold);
        Assert.IsFalse(InventoryManager.Instance.HasItem(wood, 1));
    }

    [Test]
    public void TryPurchase_InsufficientCapacity_SpendsNothing()
    {
        ItemSO wood = MakeItem("item.material.wood", stackable: false, maxStack: 1);
        ItemSO filler = MakeItem("item.filler", stackable: false, maxStack: 1);
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        ShopDefinition shop = MakeShop("shop.town.general", "npc.town.elder", ("item.material.wood", 5));
        _manager.ConfigureForTests(MakeCatalog(shop), resolver);
        InventoryManager.Instance.AddGold(100);
        InventoryManager.Instance.AddItem(filler, InventoryManager.Instance.Slots.Count);

        Assert.IsFalse(_manager.TryPurchase("shop.town.general", "item.material.wood", 1, out ShopTransactionResult result));
        Assert.AreEqual(ShopTransactionResult.InsufficientInventoryCapacity, result);
        Assert.AreEqual(100, InventoryManager.Instance.Gold, "Failed capacity check must not spend gold.");
    }

    [Test]
    public void TryPurchase_ItemNotInStock_Fails()
    {
        ShopDefinition shop = MakeShop("shop.town.general", "npc.town.elder", ("item.material.wood", 5));
        _manager.ConfigureForTests(MakeCatalog(shop));

        Assert.IsFalse(_manager.TryPurchase("shop.town.general", "item.unknown", 1, out ShopTransactionResult result));
        Assert.AreEqual(ShopTransactionResult.ItemNotInStock, result);
    }

    [Test]
    public void TryPurchase_GameplayNotAllowed_Fails()
    {
        ItemSO wood = MakeItem("item.material.wood");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        ShopDefinition shop = MakeShop("shop.town.general", "npc.town.elder", ("item.material.wood", 5));
        _manager.ConfigureForTests(MakeCatalog(shop), resolver);
        InventoryManager.Instance.AddGold(100);

        GameStateManager.Instance.PushState(GameState.Paused);
        try
        {
            Assert.IsFalse(_manager.TryPurchase("shop.town.general", "item.material.wood", 1, out ShopTransactionResult result));
            Assert.AreEqual(ShopTransactionResult.GameplayNotAllowed, result);
        }
        finally
        {
            GameStateManager.Instance.ResetToPlaying();
        }
    }

    [Test]
    public void TrySell_OnlySellsItemsInThisShopsOwnStock_AtDiscountedPrice()
    {
        ItemSO wood = MakeItem("item.material.wood");
        ItemSO iron = MakeItem("item.material.iron");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        resolver.Register(iron);
        ShopDefinition shop = MakeShop("shop.town.general", "npc.town.elder", ("item.material.wood", 10));
        _manager.ConfigureForTests(MakeCatalog(shop), resolver);
        InventoryManager.Instance.AddItem(wood, 5);
        InventoryManager.Instance.AddItem(iron, 5);

        Assert.IsFalse(_manager.TrySell("shop.town.general", "item.material.iron", 1, out ShopTransactionResult ironResult),
            "iron is not in this shop's stock, so this shop must not buy it.");
        Assert.AreEqual(ShopTransactionResult.ItemNotInStock, ironResult);

        Assert.IsTrue(_manager.TrySell("shop.town.general", "item.material.wood", 2, out ShopTransactionResult woodResult));
        Assert.AreEqual(ShopTransactionResult.Success, woodResult);
        Assert.AreEqual(10, InventoryManager.Instance.Gold, "2 * (10 price * 0.5 multiplier) = 10.");
        Assert.IsTrue(InventoryManager.Instance.HasItem(wood, 3));
    }

    [Test]
    public void TrySell_InsufficientQuantity_Fails()
    {
        ItemSO wood = MakeItem("item.material.wood");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        ShopDefinition shop = MakeShop("shop.town.general", "npc.town.elder", ("item.material.wood", 10));
        _manager.ConfigureForTests(MakeCatalog(shop), resolver);
        InventoryManager.Instance.AddItem(wood, 1);

        Assert.IsFalse(_manager.TrySell("shop.town.general", "item.material.wood", 5, out ShopTransactionResult result));
        Assert.AreEqual(ShopTransactionResult.InsufficientItemQuantity, result);
        Assert.AreEqual(0, InventoryManager.Instance.Gold);
    }
}
