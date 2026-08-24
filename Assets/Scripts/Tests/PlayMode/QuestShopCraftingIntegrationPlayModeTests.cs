using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Proves the Phase 6 integration gap is closed: a real ShopManager/CraftingManager transaction
/// (not QuestDomainEvents.Raise* called directly) progresses a Purchase/Craft quest objective.
/// "Quest objective khong phu thuoc click UI; chi phu thuoc transaction thanh cong" (Roadmap Phase 7
/// acceptance criteria).
/// </summary>
public sealed class QuestShopCraftingIntegrationPlayModeTests
{
    private GameObject _inventoryRoot;
    private GameObject _questRoot;
    private GameObject _shopRoot;
    private GameObject _craftingRoot;
    private QuestManager _questManager;
    private ShopManager _shopManager;
    private CraftingManager _craftingManager;
    private readonly List<Object> _scratchAssets = new();

    [SetUp]
    public void SetUp()
    {
        GameStateManager.Instance.ResetToPlaying();
        _inventoryRoot = new GameObject("InventoryManagerFixture");
        _inventoryRoot.AddComponent<InventoryManager>();
        _questRoot = new GameObject("QuestManagerFixture");
        _questManager = _questRoot.AddComponent<QuestManager>();
        _shopRoot = new GameObject("ShopManagerFixture");
        _shopManager = _shopRoot.AddComponent<ShopManager>();
        _craftingRoot = new GameObject("CraftingManagerFixture");
        _craftingManager = _craftingRoot.AddComponent<CraftingManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_questRoot);
        Object.DestroyImmediate(_shopRoot);
        Object.DestroyImmediate(_craftingRoot);
        Object.DestroyImmediate(_inventoryRoot);
        foreach (Object asset in _scratchAssets)
            Object.DestroyImmediate(asset);
        _scratchAssets.Clear();
        GameStateManager.Instance.ResetToPlaying();
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private ItemSO MakeItem(string itemId)
    {
        var item = ScriptableObject.CreateInstance<ItemSO>();
        item.itemId = itemId;
        item.isStackable = true;
        item.maxStackSize = 99;
        _scratchAssets.Add(item);
        return item;
    }

    private sealed class FakeItemResolver : IItemResolver
    {
        private readonly Dictionary<string, ItemSO> _map = new();
        public void Register(ItemSO item) => _map[item.itemId] = item;
        public bool TryResolve(string itemId, out ItemSO item) => _map.TryGetValue(itemId ?? "", out item);
    }

    [Test]
    public void RealPurchaseTransaction_ProgressesPurchaseObjective()
    {
        ItemSO potion = MakeItem("item.consumable.health_potion");
        var resolver = new FakeItemResolver();
        resolver.Register(potion);

        var stock = new ShopStockEntry();
        SetPrivate(stock, "_itemId", "item.consumable.health_potion");
        SetPrivate(stock, "_price", 20);
        var shop = ScriptableObject.CreateInstance<ShopDefinition>();
        SetPrivate(shop, "_shopId", "shop.town.general");
        SetPrivate(shop, "_stock", new[] { stock });
        var shopCatalog = ScriptableObject.CreateInstance<ShopCatalog>();
        SetPrivate(shopCatalog, "_shops", new[] { shop });
        _scratchAssets.Add(shop);
        _scratchAssets.Add(shopCatalog);
        _shopManager.ConfigureForTests(shopCatalog, resolver);
        InventoryManager.Instance.AddGold(100);

        var objective = new QuestObjectiveDefinition();
        SetPrivate(objective, "_type", QuestObjectiveType.Purchase);
        SetPrivate(objective, "_targetId", "item.consumable.health_potion");
        SetPrivate(objective, "_targetCount", 1);
        var quest = ScriptableObject.CreateInstance<QuestDefinition>();
        SetPrivate(quest, "_questId", "quest.buy_potion");
        SetPrivate(quest, "_objectives", new[] { objective });
        var questCatalog = ScriptableObject.CreateInstance<QuestCatalog>();
        SetPrivate(questCatalog, "_quests", new[] { quest });
        _scratchAssets.Add(quest);
        _scratchAssets.Add(questCatalog);
        _questManager.ConfigureForTests(questCatalog);
        _questManager.TryAcceptQuest("quest.buy_potion");

        Assert.AreEqual(QuestStatus.Active, _questManager.GetStatus("quest.buy_potion"));

        bool purchased = _shopManager.TryPurchase("shop.town.general", "item.consumable.health_potion", 1, out ShopTransactionResult result);

        Assert.IsTrue(purchased);
        Assert.AreEqual(ShopTransactionResult.Success, result);
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _questManager.GetStatus("quest.buy_potion"),
            "A real ShopManager.TryPurchase must progress the Purchase objective, not just QuestDomainEvents.RaiseItemPurchased directly.");
    }

    [Test]
    public void RealCraftTransaction_ProgressesCraftObjective()
    {
        ItemSO wood = MakeItem("item.material.wood");
        ItemSO plank = MakeItem("item.material.plank");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        resolver.Register(plank);

        var ingredient = new RecipeIngredientEntry();
        SetPrivate(ingredient, "_itemId", "item.material.wood");
        SetPrivate(ingredient, "_quantity", 3);
        var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        SetPrivate(recipe, "_recipeId", "recipe.material.plank");
        SetPrivate(recipe, "_ingredients", new[] { ingredient });
        SetPrivate(recipe, "_outputItemId", "item.material.plank");
        SetPrivate(recipe, "_outputQuantity", 1);
        var recipeCatalog = ScriptableObject.CreateInstance<RecipeCatalog>();
        SetPrivate(recipeCatalog, "_recipes", new[] { recipe });
        _scratchAssets.Add(recipe);
        _scratchAssets.Add(recipeCatalog);
        _craftingManager.ConfigureForTests(recipeCatalog, resolver);
        InventoryManager.Instance.AddItem(wood, 3);

        var objective = new QuestObjectiveDefinition();
        SetPrivate(objective, "_type", QuestObjectiveType.Craft);
        SetPrivate(objective, "_targetId", "item.material.plank");
        SetPrivate(objective, "_targetCount", 1);
        var quest = ScriptableObject.CreateInstance<QuestDefinition>();
        SetPrivate(quest, "_questId", "quest.craft_plank");
        SetPrivate(quest, "_objectives", new[] { objective });
        var questCatalog = ScriptableObject.CreateInstance<QuestCatalog>();
        SetPrivate(questCatalog, "_quests", new[] { quest });
        _scratchAssets.Add(quest);
        _scratchAssets.Add(questCatalog);
        _questManager.ConfigureForTests(questCatalog);
        _questManager.TryAcceptQuest("quest.craft_plank");

        bool crafted = _craftingManager.TryCraft("recipe.material.plank", null, out CraftingTransactionResult result);

        Assert.IsTrue(crafted);
        Assert.AreEqual(CraftingTransactionResult.Success, result);
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _questManager.GetStatus("quest.craft_plank"),
            "A real CraftingManager.TryCraft must progress the Craft objective, not just QuestDomainEvents.RaiseItemCrafted directly.");
    }
}
