using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ShopCraftingNpcInteractionServicePlayModeTests
{
    private GameObject _shopRoot;
    private GameObject _craftingRoot;
    private GameObject _inventoryRoot;
    private ShopManager _shopManager;
    private CraftingManager _craftingManager;
    private readonly List<Object> _scratchAssets = new();

    [SetUp]
    public void SetUp()
    {
        GameStateManager.Instance.ResetToPlaying();
        _inventoryRoot = new GameObject("InventoryManagerFixture");
        _inventoryRoot.AddComponent<InventoryManager>();
        _shopRoot = new GameObject("ShopManagerFixture");
        _shopManager = _shopRoot.AddComponent<ShopManager>();
        _craftingRoot = new GameObject("CraftingManagerFixture");
        _craftingManager = _craftingRoot.AddComponent<CraftingManager>();
    }

    [TearDown]
    public void TearDown()
    {
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

    private ShopDefinition MakeShop(string shopId, string npcId, string itemId, int price)
    {
        var entry = new ShopStockEntry();
        SetPrivate(entry, "_itemId", itemId);
        SetPrivate(entry, "_price", price);

        var definition = ScriptableObject.CreateInstance<ShopDefinition>();
        SetPrivate(definition, "_shopId", shopId);
        SetPrivate(definition, "_npcId", npcId);
        SetPrivate(definition, "_stock", new[] { entry });
        _scratchAssets.Add(definition);
        return definition;
    }

    private RecipeDefinition MakeRecipe(string recipeId, string npcId, string outputItemId, string ingredientItemId)
    {
        var ingredient = new RecipeIngredientEntry();
        SetPrivate(ingredient, "_itemId", ingredientItemId);
        SetPrivate(ingredient, "_quantity", 1);

        var definition = ScriptableObject.CreateInstance<RecipeDefinition>();
        SetPrivate(definition, "_recipeId", recipeId);
        SetPrivate(definition, "_npcId", npcId);
        SetPrivate(definition, "_ingredients", new[] { ingredient });
        SetPrivate(definition, "_outputItemId", outputItemId);
        SetPrivate(definition, "_outputQuantity", 1);
        _scratchAssets.Add(definition);
        return definition;
    }

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
    public void ShopService_TryGetShopAndPurchase_OnlyThroughOwningNpc()
    {
        ItemSO wood = MakeItem("item.material.wood");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        ShopDefinition shop = MakeShop("shop.town.general", "npc.town.elder", "item.material.wood", 5);
        var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
        SetPrivate(catalog, "_shops", new[] { shop });
        _scratchAssets.Add(catalog);
        _shopManager.ConfigureForTests(catalog, resolver);
        InventoryManager.Instance.AddGold(100);

        var service = new ShopNpcInteractionService(_shopManager);
        Assert.IsTrue(service.TryGetShop("npc.town.elder", out ShopDefinition offered));
        Assert.AreEqual(shop, offered);
        Assert.IsFalse(service.TryGetShop("npc.town.other", out _));

        Assert.IsFalse(service.TryPurchase("npc.town.other", "shop.town.general", "item.material.wood", 1, out ShopTransactionResult wrongNpc));
        Assert.AreEqual(ShopTransactionResult.ShopNotFound, wrongNpc);
        Assert.AreEqual(100, InventoryManager.Instance.Gold);

        Assert.IsTrue(service.TryPurchase("npc.town.elder", "shop.town.general", "item.material.wood", 1, out ShopTransactionResult ok));
        Assert.AreEqual(ShopTransactionResult.Success, ok);
        Assert.AreEqual(95, InventoryManager.Instance.Gold);
    }

    [Test]
    public void CraftingService_GetOfferedRecipesAndCraft_OnlyThroughOfferingNpc()
    {
        ItemSO wood = MakeItem("item.material.wood");
        ItemSO plank = MakeItem("item.material.plank");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        resolver.Register(plank);
        RecipeDefinition recipe = MakeRecipe("recipe.material.plank", "npc.town.elder", "item.material.plank", "item.material.wood");
        var catalog = ScriptableObject.CreateInstance<RecipeCatalog>();
        SetPrivate(catalog, "_recipes", new[] { recipe });
        _scratchAssets.Add(catalog);
        _craftingManager.ConfigureForTests(catalog, resolver);
        InventoryManager.Instance.AddItem(wood, 1);

        var service = new CraftingNpcInteractionService(_craftingManager);
        CollectionAssert.Contains(service.GetOfferedRecipes("npc.town.elder"), recipe);
        Assert.AreEqual(0, service.GetOfferedRecipes("npc.town.other").Count);

        Assert.IsFalse(service.TryCraft("npc.town.other", "recipe.material.plank", null, out CraftingTransactionResult wrongNpc));
        Assert.AreEqual(CraftingTransactionResult.RecipeNotFound, wrongNpc);
        Assert.IsTrue(InventoryManager.Instance.HasItem(wood, 1));

        Assert.IsTrue(service.TryCraft("npc.town.elder", "recipe.material.plank", null, out CraftingTransactionResult ok));
        Assert.AreEqual(CraftingTransactionResult.Success, ok);
        Assert.IsTrue(InventoryManager.Instance.HasItem(plank, 1));
    }
}
