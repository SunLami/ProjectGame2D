using NUnit.Framework;

public sealed class SaveMigrationTests
{
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
