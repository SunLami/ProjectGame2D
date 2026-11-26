using NUnit.Framework;

public sealed class SaveMigrationTests
{
    [TestCase(false, QuestStatus.Active, 0, 0, 0, 0)]
    [TestCase(true, QuestStatus.Active, 0, 0, 0, 140)]
    [TestCase(true, QuestStatus.Active, 5, 2, 10, 48)]
    [TestCase(true, QuestStatus.Active, 5, 2, 80, 80)]
    [TestCase(true, QuestStatus.ReadyToTurnIn, 5, 5, 0, 0)]
    [TestCase(true, QuestStatus.Completed, 5, 5, 0, 0)]
    public void V14_PaidDrinksMigration_FundsOnlyOutstandingPurchases(
        bool completedTraining, QuestStatus drinksStatus, int carrot, int grape, int gold, int expected)
    {
        var save = NewGameFactory.CreateDefault();
        save.saveVersion = 14;
        save.inventory.gold = gold;
        if (completedTraining)
            save.quests.quests.Add(new QuestProgressSaveData
                { questId = "quest.act1.new_journey", status = QuestStatus.Completed });
        var drinks = new QuestProgressSaveData
        {
            questId = "quest.act1.drink_before_road",
            status = drinksStatus,
            objectiveCounters = new[] { 1, carrot, grape }
        };
        save.quests.quests.Add(drinks);

        SaveMigration.Migrate(save);

        Assert.AreEqual(expected, save.inventory.gold);
        Assert.AreEqual(drinksStatus, drinks.status);
        CollectionAssert.AreEqual(new[] { 1, carrot, grape }, drinks.objectiveCounters);
        save.inventory.gold = 0;
        SaveMigration.Migrate(save);
        Assert.AreEqual(0, save.inventory.gold);
    }

    [TestCase(QuestStatus.ReadyToTurnIn, QuestStatus.Active, 1)]
    [TestCase(QuestStatus.Completed, QuestStatus.Completed, 2)]
    public void V17_AgnesMigration_RequiresTrainingForUnfinishedQuests(
        QuestStatus oldStatus, QuestStatus expectedStatus, int expectedIndex)
    {
        var save = NewGameFactory.CreateDefault();
        save.saveVersion = 17;
        var quest = new QuestProgressSaveData
        {
            questId = "quest.act1.far_from_your_kind",
            status = oldStatus,
            currentObjectiveIndex = 1,
            objectiveCounters = new[] { 1 }
        };
        save.quests.quests.Add(quest);

        SaveMigration.Migrate(save);

        Assert.AreEqual(expectedStatus, quest.status);
        Assert.AreEqual(expectedIndex, quest.currentObjectiveIndex);
        if (expectedStatus == QuestStatus.Active)
            CollectionAssert.AreEqual(new[] { 1, 0 }, quest.objectiveCounters);
    }

    [Test]
    public void V16_CompletedPreForgeQuests_CreditLevelFiveOnce()
    {
        var save = NewGameFactory.CreateDefault();
        save.saveVersion = 16;
        foreach (string id in new[]
        {
            "new_journey", "drink_before_road", "stocking_up_oswin", "stocking_up_eadric",
            "back_to_fields", "harvest_the_fields", "back_to_eadric", "back_to_oswin", "cast_a_line"
        })
            save.quests.quests.Add(new QuestProgressSaveData
                { questId = "quest.act1." + id, status = QuestStatus.Completed });

        SaveMigration.Migrate(save);
        SaveMigration.Migrate(save);

        Assert.AreEqual(5, save.player.level);
        Assert.AreEqual(4, save.player.currentExperience);
        Assert.AreEqual(4, save.player.skills.points);
    }

    [Test]
    public void V15_FarmMigration_DefersHarvestWithoutLosingEitherCounter()
    {
        var save = NewGameFactory.CreateDefault();
        save.saveVersion = 15;
        save.quests.trackedQuestId = "quest.act1.back_to_fields";
        save.quests.quests.Add(new QuestProgressSaveData
        {
            questId = save.quests.trackedQuestId,
            status = QuestStatus.Active,
            currentObjectiveIndex = 4,
            objectiveCounters = new[] { 15, 15, 15, 5, 2, 0 }
        });

        SaveMigration.Migrate(save);
        SaveMigration.Migrate(save);

        Assert.AreEqual("quest.act1.back_to_fields", save.quests.trackedQuestId);
        QuestProgressSaveData planting = save.quests.quests.Find(quest => quest.questId == save.quests.trackedQuestId);
        QuestProgressSaveData harvest = save.quests.quests.Find(quest => quest.questId == "quest.act1.harvest_the_fields");
        Assert.AreEqual(QuestStatus.Active, planting.status);
        Assert.AreEqual(1, planting.currentObjectiveIndex);
        CollectionAssert.AreEqual(new[] { 5, 2, 0 }, planting.objectiveCounters);
        Assert.AreEqual(QuestStatus.Locked, harvest.status);
        Assert.AreEqual(3, harvest.currentObjectiveIndex);
        CollectionAssert.AreEqual(new[] { 15, 15, 15 }, harvest.objectiveCounters);
        Assert.AreEqual(2, save.quests.quests.Count);
    }

    [Test]
    public void V15_ReadyActOneQuests_StillRequireNpcTurnIn()
    {
        var save = NewGameFactory.CreateDefault();
        save.saveVersion = 15;
        save.quests.trackedQuestId = "quest.act1.back_to_fields";
        var planting = new QuestProgressSaveData
        {
            questId = save.quests.trackedQuestId,
            status = QuestStatus.ReadyToTurnIn,
            objectiveCounters = new[] { 15, 15, 15, 5, 5, 5 }
        };
        var oswin = new QuestProgressSaveData
        {
            questId = "quest.act1.stocking_up_oswin",
            status = QuestStatus.ReadyToTurnIn
        };
        save.quests.quests.Add(planting);
        save.quests.quests.Add(oswin);
        int originalGold = save.inventory.gold;

        SaveMigration.Migrate(save);

        const string harvestId = "quest.act1.harvest_the_fields";
        QuestProgressSaveData harvest = save.quests.quests.Find(quest => quest.questId == harvestId);
        Assert.AreEqual(QuestStatus.Completed, planting.status);
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, harvest.status);
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, oswin.status);
        Assert.AreEqual(harvestId, save.quests.trackedQuestId);
        Assert.AreEqual(originalGold, save.inventory.gold);
    }

    [Test]
    public void V1_MigratesToCurrent_WithNewGameEquivalentDefaults()
    {
        var v1 = new GameSaveData { saveVersion = 1, saveId = "legacy-v1", totalPlayTimeSeconds = 120 };

        GameSaveData migrated = SaveMigration.Migrate(v1);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, migrated.saveVersion);
        Assert.AreEqual("legacy-v1", migrated.saveId, "Migration must never change identity fields.");
        Assert.AreEqual(120, migrated.totalPlayTimeSeconds, "Migration must never touch fields the save already had.");

        Assert.IsNotNull(migrated.player);
        Assert.AreEqual(1, migrated.player.level);
        Assert.AreEqual(-1f, migrated.player.health, "Sentinel for 'use current MaxHealth', matching NewGameFactory.");
        Assert.AreEqual(NewGameFactory.TutorialAreaId, migrated.player.location.areaId);
        Assert.AreEqual(NewGameFactory.TutorialStartSpawnId, migrated.player.location.fallbackSpawnId);

        Assert.IsNotNull(migrated.inventory);
        Assert.AreEqual(0, migrated.inventory.slots.Count);
        Assert.IsNotNull(migrated.equipment);
        Assert.IsNotNull(migrated.tutorial);
        Assert.IsNull(migrated.tutorial.currentStepId, "Null currentStepId means 'start at the first step'.");
        Assert.IsNotNull(migrated.quests);
        Assert.AreEqual(0, migrated.quests.quests.Count);
        Assert.IsNotNull(migrated.world);
        Assert.AreEqual(0, migrated.world.objects.Count);
    }

    [Test]
    public void V2_KeepsExistingPlayerData_OnlyDefaultsLaterDomains()
    {
        var v2 = new GameSaveData
        {
            saveVersion = 2,
            saveId = "legacy-v2",
            player = new PlayerSaveData { level = 5, currentExperience = 40, health = 30f, location = new PlayerLocationSaveData { areaId = "area.town" } }
        };

        GameSaveData migrated = SaveMigration.Migrate(v2);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, migrated.saveVersion);
        Assert.AreEqual(5, migrated.player.level, "A field the save already had must survive migration untouched.");
        Assert.AreEqual("area.town", migrated.player.location.areaId);
        Assert.IsNotNull(migrated.inventory);
        Assert.IsNotNull(migrated.equipment);
        Assert.IsNotNull(migrated.tutorial);
        Assert.IsNotNull(migrated.quests);
        Assert.IsNotNull(migrated.world);
    }

    [Test]
    public void V5_OnlyDefaultsWorld()
    {
        var v5 = new GameSaveData
        {
            saveVersion = 5,
            saveId = "legacy-v5",
            player = new PlayerSaveData { level = 3, location = new PlayerLocationSaveData() },
            inventory = new InventorySaveData { gold = 250 },
            equipment = new EquipmentSaveData(),
            tutorial = new TutorialSaveData { completed = true },
            quests = new QuestSaveData()
        };
        v5.quests.quests.Add(new QuestProgressSaveData { questId = "quest.tutorial.crafting.001", status = QuestStatus.Completed });

        GameSaveData migrated = SaveMigration.Migrate(v5);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, migrated.saveVersion);
        Assert.AreEqual(250, migrated.inventory.gold, "Existing inventory must survive untouched.");
        Assert.IsTrue(migrated.tutorial.completed, "Existing tutorial completion must survive untouched.");
        Assert.AreEqual(1, migrated.quests.quests.Count, "Existing quest progress must survive untouched -- no re-grant, no reset.");
        Assert.IsNotNull(migrated.world);
        Assert.AreEqual(0, migrated.world.objects.Count);
    }

    [Test]
    public void V6_AddsInventoryInstanceContractWithoutChangingExistingSlots()
    {
        var v6 = new GameSaveData
        {
            saveVersion = 6,
            saveId = "legacy-v6",
            inventory = new InventorySaveData { gold = 75 }
        };
        v6.inventory.slots.Add(new InventorySaveData.SlotData { itemId = "sword_lvl1", quantity = 1 });

        GameSaveData migrated = SaveMigration.Migrate(v6);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, migrated.saveVersion);
        Assert.AreEqual(75, migrated.inventory.gold);
        Assert.AreEqual("sword_lvl1", migrated.inventory.slots[0].itemId);
        Assert.IsNull(migrated.inventory.slots[0].fish);
    }

    [Test]
    public void V7_SelectsFirstActiveQuestAsTrackedQuest()
    {
        var v7 = new GameSaveData { saveVersion = 7, quests = new QuestSaveData() };
        v7.quests.quests.Add(new QuestProgressSaveData { questId = "quest.completed", status = QuestStatus.Completed });
        v7.quests.quests.Add(new QuestProgressSaveData { questId = "quest.active", status = QuestStatus.Active });
        v7.quests.quests.Add(new QuestProgressSaveData { questId = "quest.ready", status = QuestStatus.ReadyToTurnIn });

        GameSaveData migrated = SaveMigration.Migrate(v7);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, migrated.saveVersion);
        Assert.AreEqual("quest.active", migrated.quests.trackedQuestId);
        Assert.AreEqual(3, migrated.quests.quests.Count, "Migration must preserve all quest progress records.");
    }

    [Test]
    public void V7_AddsQuickBarAndFarmingThroughSequentialSteps()
    {
        var legacy = new GameSaveData { saveVersion = 7, saveId = "legacy-v7" };

        GameSaveData migrated = SaveMigration.Migrate(legacy);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, migrated.saveVersion);
        Assert.IsNotNull(migrated.quickBar);
        Assert.AreEqual(QuickBarSaveData.SlotCount, migrated.quickBar.assignedItemIds.Count);
        Assert.IsNotNull(migrated.farming);
        Assert.AreEqual(0, migrated.farming.plots.Count);
    }

    [Test]
    public void V8_PreservesQuickBarWhileAddingEmptyFarming()
    {
        var quickBar = new QuickBarSaveData { selectedIndex = 3 };
        quickBar.assignedItemIds[3] = "item.seed.carrot";
        var legacy = new GameSaveData { saveVersion = 8, saveId = "legacy-v8", quickBar = quickBar };

        GameSaveData migrated = SaveMigration.Migrate(legacy);

        Assert.AreSame(quickBar, migrated.quickBar);
        Assert.AreEqual("item.seed.carrot", migrated.quickBar.assignedItemIds[3]);
        Assert.IsNotNull(migrated.farming);
    }

    [Test]
    public void AlreadyCurrentVersion_MigrateIsANoOp()
    {
        var current = new GameSaveData { saveVersion = GameSaveData.CurrentSaveVersion, saveId = "already-current" };

        GameSaveData migrated = SaveMigration.Migrate(current);

        Assert.AreSame(current, migrated);
        Assert.AreEqual(GameSaveData.CurrentSaveVersion, migrated.saveVersion);
    }

    [Test]
    public void Migrate_IsIdempotent()
    {
        var v1 = new GameSaveData { saveVersion = 1, saveId = "idempotent-check" };

        GameSaveData once = SaveMigration.Migrate(v1);
        string playerFingerprint = JsonUtilityFingerprint(once.player);

        GameSaveData twice = SaveMigration.Migrate(once);

        Assert.AreEqual(playerFingerprint, JsonUtilityFingerprint(twice.player), "Migrating already-migrated data must not change it further.");
        Assert.AreEqual(GameSaveData.CurrentSaveVersion, twice.saveVersion);
    }

    [Test]
    public void CanMigrate_RejectsBelowMinimumOrAboveCurrent()
    {
        Assert.IsFalse(SaveMigration.CanMigrate(0));
        Assert.IsFalse(SaveMigration.CanMigrate(-1));
        Assert.IsTrue(SaveMigration.CanMigrate(SaveMigration.MinimumSupportedVersion));
        Assert.IsTrue(SaveMigration.CanMigrate(GameSaveData.CurrentSaveVersion));
        Assert.IsFalse(SaveMigration.CanMigrate(GameSaveData.CurrentSaveVersion + 1));
    }

    [Test]
    public void Migrate_NullInput_ReturnsNull()
    {
        Assert.IsNull(SaveMigration.Migrate(null));
    }

    private static string JsonUtilityFingerprint(object value) => UnityEngine.JsonUtility.ToJson(value);
}
