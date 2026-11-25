using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Save V13 -> V14: the 13 out-of-sheet ItemSO retired 2026-11-25 (DecisionRegister).
/// The fixture holds every retired ID in every DTO that persists item/crop IDs.</summary>
public sealed class RetiredItemMigrationTests
{
    private static GameSaveData LegacyV13Fixture()
    {
        var data = new GameSaveData { saveVersion = 13, saveId = "legacy-v13-retired", totalPlayTimeSeconds = 777 };
        data.player = new PlayerSaveData { level = 4, skills = new SkillProgressionSaveData() };
        data.inventory = new InventorySaveData { gold = 321 };
        int quantity = 1;
        foreach (string oldId in SaveMigration.RetiredItemReplacements.Keys)
            data.inventory.slots.Add(new InventorySaveData.SlotData { itemId = oldId, quantity = quantity++ });
        data.inventory.slots.Add(new InventorySaveData.SlotData { itemId = null, quantity = 0 });
        data.inventory.slots.Add(new InventorySaveData.SlotData { itemId = "sword_lvl1", quantity = 1 });
        data.equipment = new EquipmentSaveData();
        data.quickBar = new QuickBarSaveData { selectedIndex = 2 };
        data.quickBar.assignedItemIds[0] = "item.food.bread";
        data.quickBar.assignedItemIds[1] = "item.food.cheese";
        data.quickBar.assignedItemIds[2] = "item.seed.eggplant";
        data.farmStorage = new FarmStorageSaveData();
        data.farmStorage.slots.Add(new FarmStorageSaveData.SlotData { itemId = "item.seed.eggplant", quantity = 12 });
        data.farmStorage.slots.Add(new FarmStorageSaveData.SlotData { itemId = "item.crop.eggplant", quantity = 9 });
        data.farmStorage.slots.Add(new FarmStorageSaveData.SlotData { itemId = "item.seed.carrot", quantity = 3 });
        data.farming = new FarmingSaveData();
        data.farming.plots.Add(new FarmPlotSaveData { plotId = "farm.mapnhat.plot.007", cropId = "crop.eggplant", plantedAtUtcTicks = 638000000000000000L });
        data.farming.plots.Add(new FarmPlotSaveData { plotId = "farm.mapnhat.plot.008", cropId = "crop.grape", plantedAtUtcTicks = 638000000000000001L });
        data.quests = new QuestSaveData { trackedQuestId = "quest.side.potion_supply.001" };
        data.quests.quests.Add(new QuestProgressSaveData { questId = "quest.side.potion_supply.001", status = QuestStatus.Active, currentObjectiveIndex = 0, objectiveCounters = new[] { 0 } });
        data.quests.quests.Add(new QuestProgressSaveData { questId = "quest.act1.cast_a_line", status = QuestStatus.Completed, currentObjectiveIndex = 1, objectiveCounters = new[] { 5 } });
        data.world = new WorldSaveData();
        data.world.objects.Add(new WorldObjectSaveData { persistentId = "world.pickup.tutorial.relic.01", kind = WorldObjectKind.UniquePickup, flag = true });
        return data;
    }

    [Test]
    public void V13Save_RemapsEveryRetiredId_KeepingQuantitiesPositionsAndOtherState()
    {
        GameSaveData migrated = SaveMigration.Migrate(LegacyV13Fixture());

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, migrated.saveVersion);
        Assert.AreEqual("legacy-v13-retired", migrated.saveId);
        Assert.AreEqual(777, migrated.totalPlayTimeSeconds);
        Assert.AreEqual(321, migrated.inventory.gold);

        int expectedQuantity = 1;
        int index = 0;
        foreach (KeyValuePair<string, string> pair in SaveMigration.RetiredItemReplacements)
        {
            InventorySaveData.SlotData slot = migrated.inventory.slots[index++];
            Assert.AreEqual(pair.Value, slot.itemId, $"{pair.Key} must become {pair.Value} in the same slot.");
            Assert.AreEqual(expectedQuantity++, slot.quantity, "Quantity is preserved per slot (no merge, no split).");
        }
        Assert.IsNull(migrated.inventory.slots[index].itemId, "Empty slots stay empty.");
        Assert.AreEqual("sword_lvl1", migrated.inventory.slots[index + 1].itemId, "Non-retired items are untouched.");

        CollectionAssert.AreEqual(new[] { "item.food.harvest_stew", "item.food.harvest_stew", "item.seed.carrot", null },
            migrated.quickBar.assignedItemIds);
        Assert.AreEqual(2, migrated.quickBar.selectedIndex);

        Assert.AreEqual("item.seed.carrot", migrated.farmStorage.slots[0].itemId);
        Assert.AreEqual(12, migrated.farmStorage.slots[0].quantity);
        Assert.AreEqual("item.crop.carrot", migrated.farmStorage.slots[1].itemId);
        Assert.AreEqual(9, migrated.farmStorage.slots[1].quantity);
        Assert.AreEqual(3, migrated.farmStorage.slots[2].quantity, "An existing Carrot Seeds stack is not merged into.");

        Assert.AreEqual("farm.mapnhat.plot.007", migrated.farming.plots[0].plotId);
        Assert.AreEqual("crop.carrot", migrated.farming.plots[0].cropId);
        Assert.AreEqual(638000000000000000L, migrated.farming.plots[0].plantedAtUtcTicks, "Planting timestamp is kept.");
        Assert.AreEqual("crop.grape", migrated.farming.plots[1].cropId);

        Assert.AreEqual(QuestStatus.Active, migrated.quests.quests[0].status, "Potion Supply progress is kept.");
        Assert.AreEqual("quest.side.potion_supply.001", migrated.quests.trackedQuestId);
        Assert.AreEqual(QuestStatus.Completed, migrated.quests.quests[1].status);
        Assert.AreEqual("world.pickup.tutorial.relic.01", migrated.world.objects[0].persistentId);
        Assert.IsTrue(migrated.world.objects[0].flag, "A collected relic pickup stays collected.");
    }

    [Test]
    public void Migration_IsIdempotent_OnCurrentVersion()
    {
        GameSaveData once = SaveMigration.Migrate(LegacyV13Fixture());
        string json = JsonUtility.ToJson(once);
        GameSaveData twice = SaveMigration.Migrate(JsonUtility.FromJson<GameSaveData>(json));
        Assert.AreEqual(json, JsonUtility.ToJson(twice));
    }

    [Test]
    public void EveryReplacement_ExistsAndStacksAtLeastAsHighAsTheRetiredItem()
    {
        // Max stack of the retired items before deletion: 20 (drinks/food/potions), 99 (materials,
        // seeds, crops), 1 (relic, badge). A per-slot remap is only lossless if targets hold >= 99.
        var items = new Dictionary<string, ItemSO>();
        foreach (string guid in AssetDatabase.FindAssets("t:ItemSO"))
        {
            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && !string.IsNullOrEmpty(item.itemId)) items[item.itemId] = item;
        }

        foreach (KeyValuePair<string, string> pair in SaveMigration.RetiredItemReplacements)
        {
            Assert.IsFalse(items.ContainsKey(pair.Key), $"Retired ItemSO {pair.Key} must no longer exist.");
            Assert.IsTrue(items.TryGetValue(pair.Value, out ItemSO target), $"Replacement {pair.Value} is missing.");
            Assert.IsTrue(target.isStackable && target.maxStackSize >= 99, $"{pair.Value} must stack to at least 99.");
        }
    }

    [Test]
    public void FarmingCatalog_NoLongerContainsEggplant_AndResolvesTheReplacementCrop()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<FarmingCatalog>("Assets/Game/Farming/Definitions/FarmingCatalog.asset");
        Assert.IsFalse(catalog.TryResolve("crop.eggplant", out _));
        Assert.IsTrue(catalog.TryResolve(SaveMigration.RetiredCropReplacements["crop.eggplant"], out _));
        foreach (CropDefinition crop in catalog.Crops)
            Assert.IsNotNull(crop, "FarmingCatalog has a dangling crop reference.");
    }
}
