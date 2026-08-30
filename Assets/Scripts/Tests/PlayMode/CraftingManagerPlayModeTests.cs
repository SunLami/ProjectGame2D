using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CraftingManagerPlayModeTests
{
    private GameObject _root;
    private GameObject _inventoryRoot;
    private CraftingManager _manager;
    private readonly List<Object> _scratchAssets = new();

    [SetUp]
    public void SetUp()
    {
        GameStateManager.Instance.ResetToPlaying();

        _inventoryRoot = new GameObject("InventoryManagerFixture");
        _inventoryRoot.AddComponent<InventoryManager>();

        _root = new GameObject("CraftingManagerFixture");
        _manager = _root.AddComponent<CraftingManager>();
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

    private RecipeDefinition MakeRecipe(
        string recipeId, string npcId, string stationTag, string outputItemId, int outputQuantity,
        params (string itemId, int quantity)[] ingredients)
    {
        var entries = new RecipeIngredientEntry[ingredients.Length];
        for (int i = 0; i < ingredients.Length; i++)
        {
            var entry = new RecipeIngredientEntry();
            SetPrivate(entry, "_itemId", ingredients[i].itemId);
            SetPrivate(entry, "_quantity", ingredients[i].quantity);
            entries[i] = entry;
        }

        var definition = ScriptableObject.CreateInstance<RecipeDefinition>();
        SetPrivate(definition, "_recipeId", recipeId);
        SetPrivate(definition, "_npcId", npcId);
        SetPrivate(definition, "_requiredStationTag", stationTag);
        SetPrivate(definition, "_ingredients", entries);
        SetPrivate(definition, "_outputItemId", outputItemId);
        SetPrivate(definition, "_outputQuantity", outputQuantity);
        _scratchAssets.Add(definition);
        return definition;
    }

    private RecipeCatalog MakeCatalog(params RecipeDefinition[] recipes)
    {
        var catalog = ScriptableObject.CreateInstance<RecipeCatalog>();
        SetPrivate(catalog, "_recipes", recipes);
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
    public void TryCraft_ConsumesIngredientsAndGrantsOutput_RaisesItemCraftedExactlyOnce()
    {
        ItemSO wood = MakeItem("item.material.wood");
        ItemSO plank = MakeItem("item.material.plank");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        resolver.Register(plank);
        RecipeDefinition recipe = MakeRecipe(
            "recipe.material.plank", "npc.town.elder", null, "item.material.plank", 1, ("item.material.wood", 3));
        _manager.ConfigureForTests(MakeCatalog(recipe), resolver);
        InventoryManager.Instance.AddItem(wood, 3);

        int craftedCount = 0;
        void OnItemCrafted(string id, int qty, string stationId)
        {
            craftedCount++;
            Assert.AreEqual("item.material.plank", id);
            Assert.AreEqual(1, qty);
        }

        QuestDomainEvents.ItemCrafted += OnItemCrafted;
        try
        {
            Assert.IsTrue(_manager.TryCraft("recipe.material.plank", null, out CraftingTransactionResult result));
            Assert.AreEqual(CraftingTransactionResult.Success, result);
            Assert.IsFalse(InventoryManager.Instance.HasItem(wood, 1), "All 3 wood must be consumed.");
            Assert.IsTrue(InventoryManager.Instance.HasItem(plank, 1));
            Assert.AreEqual(1, craftedCount);
        }
        finally
        {
            QuestDomainEvents.ItemCrafted -= OnItemCrafted;
        }
    }

    [Test]
    public void TryCraft_InsufficientIngredients_ConsumesNothing()
    {
        ItemSO wood = MakeItem("item.material.wood");
        ItemSO plank = MakeItem("item.material.plank");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        resolver.Register(plank);
        RecipeDefinition recipe = MakeRecipe(
            "recipe.material.plank", "npc.town.elder", null, "item.material.plank", 1, ("item.material.wood", 3));
        _manager.ConfigureForTests(MakeCatalog(recipe), resolver);
        InventoryManager.Instance.AddItem(wood, 2);

        Assert.IsFalse(_manager.TryCraft("recipe.material.plank", null, out CraftingTransactionResult result));
        Assert.AreEqual(CraftingTransactionResult.InsufficientIngredients, result);
        Assert.IsTrue(InventoryManager.Instance.HasItem(wood, 2), "Failed craft must not consume any ingredient.");
    }

    [Test]
    public void TryCraft_WrongStation_Fails()
    {
        ItemSO wood = MakeItem("item.material.wood");
        ItemSO potion = MakeItem("item.consumable.health_potion");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        resolver.Register(potion);
        RecipeDefinition recipe = MakeRecipe(
            "recipe.consumable.health_potion", "npc.town.elder", "station.forge",
            "item.consumable.health_potion", 1, ("item.material.wood", 2));
        _manager.ConfigureForTests(MakeCatalog(recipe), resolver);
        InventoryManager.Instance.AddItem(wood, 2);

        Assert.IsFalse(_manager.TryCraft("recipe.consumable.health_potion", null, out CraftingTransactionResult wrongStation));
        Assert.AreEqual(CraftingTransactionResult.WrongStation, wrongStation);
        Assert.IsFalse(_manager.TryCraft("recipe.consumable.health_potion", "station.anvil", out CraftingTransactionResult otherStation));
        Assert.AreEqual(CraftingTransactionResult.WrongStation, otherStation);

        Assert.IsTrue(_manager.TryCraft("recipe.consumable.health_potion", "station.forge", out CraftingTransactionResult result));
        Assert.AreEqual(CraftingTransactionResult.Success, result);
    }

    [Test]
    public void TryCraft_InsufficientOutputCapacity_ConsumesNothing()
    {
        ItemSO wood = MakeItem("item.material.wood");
        ItemSO plank = MakeItem("item.material.plank", stackable: false, maxStack: 1);
        ItemSO filler = MakeItem("item.filler", stackable: false, maxStack: 1);
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        resolver.Register(plank);
        RecipeDefinition recipe = MakeRecipe(
            "recipe.material.plank", "npc.town.elder", null, "item.material.plank", 1, ("item.material.wood", 3));
        _manager.ConfigureForTests(MakeCatalog(recipe), resolver);

        // Fill inventory except leave room for the 3 wood ingredients; no room for the non-stackable output.
        int slotCount = InventoryManager.Instance.Slots.Count;
        InventoryManager.Instance.AddItem(wood, 3);
        InventoryManager.Instance.AddItem(filler, slotCount - 1);

        Assert.IsFalse(_manager.TryCraft("recipe.material.plank", null, out CraftingTransactionResult result));
        Assert.AreEqual(CraftingTransactionResult.InsufficientOutputCapacity, result);
        Assert.IsTrue(InventoryManager.Instance.HasItem(wood, 3), "Failed craft must not consume ingredients.");
    }

    [Test]
    public void TryCraft_GameplayNotAllowed_Fails()
    {
        ItemSO wood = MakeItem("item.material.wood");
        ItemSO plank = MakeItem("item.material.plank");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        resolver.Register(plank);
        RecipeDefinition recipe = MakeRecipe(
            "recipe.material.plank", "npc.town.elder", null, "item.material.plank", 1, ("item.material.wood", 3));
        _manager.ConfigureForTests(MakeCatalog(recipe), resolver);
        InventoryManager.Instance.AddItem(wood, 3);

        GameStateManager.Instance.PushState(GameState.Paused);
        try
        {
            Assert.IsFalse(_manager.TryCraft("recipe.material.plank", null, out CraftingTransactionResult result));
            Assert.AreEqual(CraftingTransactionResult.GameplayNotAllowed, result);
        }
        finally
        {
            GameStateManager.Instance.ResetToPlaying();
        }
    }
}
