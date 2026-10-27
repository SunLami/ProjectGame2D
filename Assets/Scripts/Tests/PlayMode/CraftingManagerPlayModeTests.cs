using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CraftingManagerPlayModeTests
{
    private GameObject _root;
    private InventoryTestScope _inventoryScope;
    private CraftingManager _manager;
    private readonly List<Object> _scratchAssets = new();

    [SetUp]
    public void SetUp()
    {
        GameStateManager.Instance.ResetToPlaying();

        _inventoryScope = new InventoryTestScope();

        _root = new GameObject("CraftingManagerFixture");
        _manager = _root.AddComponent<CraftingManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        _inventoryScope.Dispose();
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
    public void TryCraft_EquipmentRequiresItsEquipLevel_ConsumesNothingWhenLevelIsTooLow()
    {
        ItemSO iron = MakeItem("item.material.iron_bar");
        var sword = ScriptableObject.CreateInstance<EquipmentItemSO>();
        sword.itemId = "sword_lvl3";
        sword.isStackable = false;
        sword.maxStackSize = 1;
        sword.requiredLevel = 10;
        _scratchAssets.Add(sword);
        var resolver = new FakeItemResolver();
        resolver.Register(iron);
        resolver.Register(sword);
        RecipeDefinition recipe = MakeRecipe(
            "recipe.equipment.sword_lvl3", "npc.town.artificer", "station.forge",
            "sword_lvl3", 1, ("item.material.iron_bar", 5));
        _manager.ConfigureForTests(MakeCatalog(recipe), resolver);
        InventoryManager.Instance.AddItem(iron, 5);

        var playerRoot = new GameObject("PlayerStatFixture");
        PlayerStat stats = playerRoot.AddComponent<PlayerStat>();
        stats.RestoreProgression(9, 0);
        try
        {
            Assert.IsFalse(_manager.TryCraft(recipe.RecipeId, "station.forge", out CraftingTransactionResult tooLow));
            Assert.AreEqual(CraftingTransactionResult.LevelTooLow, tooLow);
            Assert.IsTrue(InventoryManager.Instance.HasItem(iron, 5), "Failed craft must not consume ingredients.");

            stats.RestoreProgression(10, 0);
            Assert.IsTrue(_manager.TryCraft(recipe.RecipeId, "station.forge", out CraftingTransactionResult success));
            Assert.AreEqual(CraftingTransactionResult.Success, success);
        }
        finally
        {
            Object.DestroyImmediate(playerRoot);
        }
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

        // Leave one wood after consumption so no slot is freed for the non-stackable output.
        int slotCount = InventoryManager.Instance.Slots.Count;
        InventoryManager.Instance.AddItem(wood, 4);
        InventoryManager.Instance.AddItem(filler, slotCount - 1);

        Assert.IsFalse(_manager.TryCraft("recipe.material.plank", null, out CraftingTransactionResult result));
        Assert.AreEqual(CraftingTransactionResult.InsufficientOutputCapacity, result);
        Assert.IsTrue(InventoryManager.Instance.HasItem(wood, 4), "Failed craft must not consume ingredients.");
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

    private (FishDefinitionSO fish, ItemSO fillet, RecipeDefinition recipe) ConfigureFilleting(int maxStack = 99)
    {
        var fish = ScriptableObject.CreateInstance<FishDefinitionSO>();
        fish.itemId = "fish.test.carp";
        SetPrivate(fish, "_minimumWeightGrams", 100); SetPrivate(fish, "_maximumWeightGrams", 10000);
        _scratchAssets.Add(fish);
        var fillet = MakeItem("item.material.fish_fillet", true, maxStack);
        var resolver = new FakeItemResolver(); resolver.Register(fish); resolver.Register(fillet);
        var recipe = MakeRecipe("recipe.test.fillet", "npc.town.gastronome", null, fillet.itemId, 2, (fish.itemId, 1));
        _manager.ConfigureForTests(MakeCatalog(recipe), resolver);
        return (fish, fillet, recipe);
    }

    [Test]
    public void FilletConsumesSelectedInstanceAndPreservesOtherFishThroughSaveRoundTrip()
    {
        var data = ConfigureFilleting(); var inventory = InventoryManager.Instance;
        Assert.IsTrue(inventory.TryAddFish(data.fish, 250, "small"));
        Assert.IsTrue(inventory.TryAddFish(data.fish, 2500, "large"));
        var service = new CraftingNpcInteractionService(_manager);
        Assert.IsFalse(service.TryCraft("npc.town.gastronome", data.recipe.RecipeId, null, out _));
        Assert.IsFalse(service.TryCraft("npc.town.mixologist", data.recipe.RecipeId, null, out _, "large"));
        Assert.IsFalse(service.TryCraft("npc.town.gastronome", data.recipe.RecipeId, null, out _, "missing"));
        Assert.IsTrue(service.TryCraft("npc.town.gastronome", data.recipe.RecipeId, null, out _, "large"));
        Assert.AreEqual(2, inventory.GetTotalQuantity(data.fillet.itemId));
        Assert.IsFalse(service.TryCraft("npc.town.gastronome", data.recipe.RecipeId, null, out _, "large"));
        var saved = inventory.ToSaveData();
        inventory.LoadFromSaveData(saved, new Dictionary<string, ItemSO> { [data.fish.itemId] = data.fish, [data.fillet.itemId] = data.fillet });
        var survivor = System.Linq.Enumerable.Single(inventory.Slots, s => s.fish != null);
        Assert.AreEqual("small", survivor.fish.instanceId); Assert.AreEqual(250, survivor.fish.weightGrams);
        Assert.AreEqual(2, inventory.GetTotalQuantity(data.fillet.itemId));
    }

    [Test]
    public void FilletUsesFreedFishSlotEvenWhenBagStartsFullAndPublishesOnce()
    {
        var data = ConfigureFilleting(); var inventory = InventoryManager.Instance;
        inventory.TryAddFish(data.fish, 500, "selected");
        var filler = MakeItem("item.test.filler", false, 1);
        for (int i = 1; i < inventory.Slots.Count; i++) inventory.AddItem(filler);
        Assert.IsFalse(inventory.HasEmptySlot);
        int notifications = 0, obtained = 0;
        void Added(string id, int quantity)
        {
            Assert.AreEqual(data.fillet.itemId, id); Assert.AreEqual(2, quantity); obtained++;
            Assert.IsFalse(_manager.TryCraft(data.recipe.RecipeId, null, out _, "selected"));
        }
        void Changed()
        {
            notifications++;
            Assert.AreEqual(0, inventory.GetTotalQuantity(data.fish.itemId));
            Assert.AreEqual(2, inventory.GetTotalQuantity(data.fillet.itemId));
            Assert.IsFalse(_manager.TryCraft(data.recipe.RecipeId, null, out _, "selected"));
        }
        inventory.OnInventoryChanged += Changed;
        QuestDomainEvents.InventoryItemAdded += Added;
        try { Assert.IsTrue(_manager.TryCraft(data.recipe.RecipeId, null, out _, "selected")); }
        finally { inventory.OnInventoryChanged -= Changed; QuestDomainEvents.InventoryItemAdded -= Added; }
        Assert.AreEqual(1, notifications); Assert.AreEqual(1, obtained);
    }

    [Test]
    public void FilletInsufficientPostExchangeCapacityDoesNotLoseFishOrNotify()
    {
        var data = ConfigureFilleting(1); var inventory = InventoryManager.Instance;
        inventory.TryAddFish(data.fish, 500, "selected");
        var filler = MakeItem("item.test.filler", false, 1);
        for (int i = 1; i < inventory.Slots.Count; i++) inventory.AddItem(filler);
        int notifications = 0; void Changed() => notifications++;
        inventory.OnInventoryChanged += Changed;
        try
        {
            Assert.IsFalse(_manager.TryCraft(data.recipe.RecipeId, null, out var result, "selected"));
            Assert.AreEqual(CraftingTransactionResult.InsufficientOutputCapacity, result);
        }
        finally { inventory.OnInventoryChanged -= Changed; }
        Assert.AreEqual(0, notifications); Assert.AreEqual(1, inventory.GetTotalQuantity(data.fish.itemId));
        Assert.AreEqual("selected", inventory.Slots[0].fish.instanceId);
        Assert.AreEqual(0, inventory.GetTotalQuantity(data.fillet.itemId));
    }

    [Test]
    public void DuplicateIngredientEntriesCannotOverdrawInventory()
    {
        var input = MakeItem("item.test.input"); var output = MakeItem("item.test.output");
        InventoryManager.Instance.AddItem(input, 3);
        Assert.IsFalse(InventoryManager.Instance.TryExchangeItems(new[]{ input, input }, new[]{2,2}, output, 1));
        Assert.AreEqual(3, InventoryManager.Instance.GetTotalQuantity(input.itemId));
        Assert.AreEqual(0, InventoryManager.Instance.GetTotalQuantity(output.itemId));
    }
}
