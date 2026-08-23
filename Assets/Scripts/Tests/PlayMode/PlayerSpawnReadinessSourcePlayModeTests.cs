using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerSpawnReadinessSourcePlayModeTests
{
    private ISaveSlotRepository _originalRepository;

    [SetUp]
    public void SetUp()
    {
        _originalRepository = GameSessionManager.Instance.SaveRepository;
        GameSessionManager.Instance.SetSaveRepositoryForTests(new InMemorySaveSlotRepository());
    }

    [TearDown]
    public void TearDown()
    {
        GameSessionManager.Instance.SetSaveRepositoryForTests(_originalRepository);
        GameSessionManager.Instance.ClearSession();
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.ResetToMainMenu();
    }

    private static (GameObject root, PlayerStat stat, Transform playerTransform, SpawnRegistry spawnRegistry)
        BuildFixture(Vector3 spawnPosition)
    {
        GameObject root = new("PlayerSpawnFixture");
        PlayerStat stat = root.AddComponent<PlayerStat>();

        GameObject spawnPoint = new("SpawnPoint");
        spawnPoint.transform.position = spawnPosition;
        spawnPoint.transform.SetParent(root.transform);

        GameObject registryObject = new("SpawnRegistry");
        registryObject.transform.SetParent(root.transform);
        SpawnRegistry registry = registryObject.AddComponent<SpawnRegistry>();
        registry.ConfigureForTests(NewGameFactory.TutorialStartSpawnId, spawnPoint.transform);

        return (root, stat, root.transform, registry);
    }

    [UnityTest]
    public IEnumerator NewGame_RestoresDefaultsAndPositionsAtTutorialSpawn()
    {
        Vector3 spawnPosition = new(5f, 7f, 0f);
        var (root, stat, playerTransform, registry) = BuildFixture(spawnPosition);
        playerTransform.position = Vector3.zero;

        GameSaveData saveData = NewGameFactory.CreateDefault();
        Assert.IsTrue(GameSessionManager.Instance.TryStartNewGame(1, "TestScene", saveData));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry);

        yield return null;

        Assert.IsTrue(source.IsReady);
        Assert.AreEqual(1, stat.Level);
        Assert.AreEqual(spawnPosition.x, playerTransform.position.x, 0.001f);
        Assert.AreEqual(spawnPosition.y, playerTransform.position.y, 0.001f);

        Assert.IsTrue(GameSessionManager.Instance.SaveRepository.TryReadSave(1, out GameSaveData written),
            "New Game restore should write the initial save (D-011).");
        Assert.AreEqual(saveData.saveId, written.saveId);

        Object.Destroy(root);
        Object.Destroy(sourceObject);
    }

    [UnityTest]
    public IEnumerator Continue_DoesNotRewriteSaveAndRestoresSavedProgression()
    {
        var (root, stat, playerTransform, registry) = BuildFixture(new Vector3(1f, 1f, 0f));

        GameSaveData saveData = new()
        {
            saveId = "existing-save",
            player = new PlayerSaveData
            {
                level = 3,
                currentExperience = 20,
                health = 5f,
                location = new PlayerLocationSaveData
                {
                    areaId = "area.town",
                    positionX = 42f,
                    positionY = -3f
                }
            }
        };
        GameSessionManager.Instance.SaveRepository.WriteSave(2, saveData);

        Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(2, "TestScene", saveData));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry);

        yield return null;

        Assert.IsTrue(source.IsReady);
        Assert.AreEqual(3, stat.Level);
        Assert.AreEqual(20, stat.CurrentExperience);
        Assert.AreEqual(42f, playerTransform.position.x, 0.001f);
        Assert.AreEqual(-3f, playerTransform.position.y, 0.001f);

        Object.Destroy(root);
        Object.Destroy(sourceObject);
    }

    [UnityTest]
    public IEnumerator CapturedSnapshot_RoundTripsThroughWriteAndContinueRestore()
    {
        // Simulates a real "leave with progress, come back" cycle: capture live state with
        // PlayerSaveCapture (not a hand-built PlayerSaveData), persist it, then restore it into a
        // fresh fixture the way Continue would.
        var (captureRoot, captureStat, captureTransform, _) = BuildFixture(Vector3.zero);
        captureStat.RestoreProgression(level: 6, currentExperience: 18, health: 30f);
        captureTransform.position = new Vector3(8f, -6f, 0f);

        PlayerSaveData captured = PlayerSaveCapture.Capture(
            captureStat, captureTransform, "area.town", "spawn.town.gate");
        GameSaveData saveData = new() { saveId = "captured-save", player = captured };
        GameSessionManager.Instance.SaveRepository.WriteSave(3, saveData);

        // PlayerStat is a static singleton; Object.Destroy() defers to end-of-frame, so building
        // the second fixture before that frame ends would see captureStat still "alive" and
        // self-destroy the new fixture instead. DestroyImmediate frees the singleton slot now.
        Object.DestroyImmediate(captureRoot);

        var (restoreRoot, restoreStat, restoreTransform, restoreRegistry) = BuildFixture(Vector3.zero);
        Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(3, "TestScene", saveData));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(restoreStat, restoreTransform, restoreRegistry);

        yield return null;

        Assert.AreEqual(6, restoreStat.Level);
        Assert.AreEqual(18, restoreStat.CurrentExperience);
        Assert.AreEqual(8f, restoreTransform.position.x, 0.001f);
        Assert.AreEqual(-6f, restoreTransform.position.y, 0.001f);

        Object.Destroy(restoreRoot);
        Object.Destroy(sourceObject);
    }

    // Real resolvable assets under Assets/Resources/Items -- restore goes through
    // ResourcesItemResolver internally, so integration tests need real, resolvable itemIds.
    // The ring is used for equip-slot tests specifically because Ring has no ApplyVisual/
    // SpriteLibrary wiring, unlike Head/Body/Weapon which would NRE without a scene SpriteLibrary.
    private const string RealItemId = "sword_lvl1";
    private const string RealRingItemId = "ring_lvl1";

    private static (GameObject root, PlayerStat stat, Transform playerTransform, SpawnRegistry spawnRegistry,
        InventorySeeder seeder) BuildFixtureWithInventory(Vector3 spawnPosition, ItemDatabase seedDatabase)
    {
        var (root, stat, playerTransform, registry) = BuildFixture(spawnPosition);
        root.AddComponent<InventoryManager>();
        root.AddComponent<EquipmentManager>();

        InventorySeeder seeder = root.AddComponent<InventorySeeder>();
        typeof(InventorySeeder)
            .GetField("_database", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(seeder, seedDatabase);

        return (root, stat, playerTransform, registry, seeder);
    }

    [UnityTest]
    public IEnumerator NewGame_SeedsStartingInventoryOnceAndCapturesItIntoInitialSave()
    {
        ItemSO realSword = ResolveRealItem();
        ItemDatabase database = ScriptableObject.CreateInstance<ItemDatabase>();
        database.items = new[] { new ItemDatabase.Entry { item = realSword, amount = 2 } };

        GameObject root = null;
        GameObject sourceObject = null;
        try
        {
            var fixture = BuildFixtureWithInventory(Vector3.zero, database);
            root = fixture.root;
            PlayerStat stat = fixture.stat;
            Transform playerTransform = fixture.playerTransform;
            SpawnRegistry registry = fixture.spawnRegistry;

            GameSaveData saveData = NewGameFactory.CreateDefault();
            Assert.IsTrue(GameSessionManager.Instance.TryStartNewGame(1, "TestScene", saveData));

            sourceObject = new GameObject("PlayerSpawnReadinessSource");
            PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
            source.ConfigureForTests(stat, playerTransform, registry, root.GetComponent<InventorySeeder>());

            yield return null;

            Assert.IsTrue(InventoryManager.Instance.HasItem(realSword, 2), "Starting loadout must be seeded once.");

            Assert.IsTrue(GameSessionManager.Instance.SaveRepository.TryReadSave(1, out GameSaveData written));
            Assert.IsNotNull(written.inventory);
            int totalSeeded = written.inventory.slots.Where(s => s.itemId == RealItemId).Sum(s => s.quantity);
            Assert.AreEqual(2, totalSeeded,
                "Initial save must capture the seeded inventory (non-stackable equipment occupies "
                + "one slot per unit), not an empty snapshot.");
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            if (sourceObject != null) Object.Destroy(sourceObject);
            Object.Destroy(database);
        }
    }

    [UnityTest]
    public IEnumerator Continue_RestoresInventoryEquipmentAndGold_WithoutSeeding()
    {
        ItemSO realSword = ResolveRealItem();
        GameObject root = null;
        GameObject sourceObject = null;
        try
        {
            var fixture = BuildFixtureWithInventory(Vector3.zero, null);
            root = fixture.root;
            PlayerStat stat = fixture.stat;
            Transform playerTransform = fixture.playerTransform;
            SpawnRegistry registry = fixture.spawnRegistry;

            GameSaveData saveData = new()
            {
                saveId = "save-with-loadout",
                player = new PlayerSaveData { level = 1, location = new PlayerLocationSaveData { areaId = "area.tutorial" } },
                inventory = new InventorySaveData { gold = 77 },
                equipment = new EquipmentSaveData()
            };
            IItemResolver resolver = new ResourcesItemResolver();
            Assert.IsTrue(resolver.TryResolve(RealRingItemId, out ItemSO realRing));

            saveData.inventory.slots.Add(new InventorySaveData.SlotData { itemId = RealItemId, quantity = 1 });
            saveData.equipment.slots.Add(new EquipmentSaveData.SlotData { slot = EquipSlot.Ring, itemId = RealRingItemId });

            Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(2, "TestScene", saveData));

            sourceObject = new GameObject("PlayerSpawnReadinessSource");
            PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
            source.ConfigureForTests(stat, playerTransform, registry, root.GetComponent<InventorySeeder>());

            yield return null;

            Assert.AreEqual(77, InventoryManager.Instance.Gold);
            Assert.IsTrue(InventoryManager.Instance.HasItem(realSword, 1));
            Assert.AreEqual(realRing, EquipmentManager.Instance.GetEquipped(EquipSlot.Ring));
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            if (sourceObject != null) Object.Destroy(sourceObject);
        }
    }

    [UnityTest]
    public IEnumerator LoadSlotA_ThenSlotB_DoesNotLeakInventoryBetweenSessions()
    {
        ItemSO realSword = ResolveRealItem();
        GameObject rootA = null;
        GameObject sourceA = null;
        GameObject rootB = null;
        GameObject sourceB = null;
        try
        {
            var fixtureA = BuildFixtureWithInventory(Vector3.zero, null);
            rootA = fixtureA.root;
            PlayerStat statA = fixtureA.stat;
            Transform transformA = fixtureA.playerTransform;
            SpawnRegistry registryA = fixtureA.spawnRegistry;
            GameSaveData saveA = new()
            {
                saveId = "slot-a",
                player = new PlayerSaveData { level = 1, location = new PlayerLocationSaveData { areaId = "area.tutorial" } },
                inventory = new InventorySaveData(),
                equipment = new EquipmentSaveData()
            };
            saveA.inventory.slots.Add(new InventorySaveData.SlotData { itemId = RealItemId, quantity = 3 });
            Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(1, "TestScene", saveA));

            sourceA = new GameObject("PlayerSpawnReadinessSourceA");
            sourceA.AddComponent<PlayerSpawnReadinessSource>()
                .ConfigureForTests(statA, transformA, registryA, rootA.GetComponent<InventorySeeder>());

            yield return null;

            Assert.IsTrue(InventoryManager.Instance.HasItem(realSword, 3));

            // Simulate a fresh scene load for a different slot: destroy the old singletons first
            // (GameplaySceneLifetime does this via scene unload in production) and start over.
            Object.DestroyImmediate(rootA);
            rootA = null;
            Object.Destroy(sourceA);
            sourceA = null;
            GameSessionManager.Instance.ClearSession();

            var fixtureB = BuildFixtureWithInventory(Vector3.zero, null);
            rootB = fixtureB.root;
            PlayerStat statB = fixtureB.stat;
            Transform transformB = fixtureB.playerTransform;
            SpawnRegistry registryB = fixtureB.spawnRegistry;
            GameSaveData saveB = new()
            {
                saveId = "slot-b",
                player = new PlayerSaveData { level = 1, location = new PlayerLocationSaveData { areaId = "area.tutorial" } },
                inventory = new InventorySaveData(),
                equipment = new EquipmentSaveData()
            };
            Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(2, "TestScene", saveB));

            sourceB = new GameObject("PlayerSpawnReadinessSourceB");
            sourceB.AddComponent<PlayerSpawnReadinessSource>()
                .ConfigureForTests(statB, transformB, registryB, rootB.GetComponent<InventorySeeder>());

            yield return null;

            Assert.IsFalse(InventoryManager.Instance.HasItem(realSword, 1),
                "Slot B's fresh InventoryManager must not carry over Slot A's items.");
        }
        finally
        {
            if (rootA != null) Object.DestroyImmediate(rootA);
            if (sourceA != null) Object.Destroy(sourceA);
            if (rootB != null) Object.DestroyImmediate(rootB);
            if (sourceB != null) Object.Destroy(sourceB);
        }
    }

    private static ItemSO ResolveRealItem()
    {
        IItemResolver resolver = new ResourcesItemResolver();
        Assert.IsTrue(resolver.TryResolve(RealItemId, out ItemSO item), $"Test relies on real asset '{RealItemId}' existing under Resources/Items.");
        return item;
    }

    [UnityTest]
    public IEnumerator NewGame_CapturesWorldRegistryIntoInitialSave()
    {
        var (root, stat, playerTransform, registry) = BuildFixture(Vector3.zero);

        GameObject worldRoot = new("WorldObjectRegistryFixture");
        WorldObjectRegistry worldRegistry = worldRoot.AddComponent<WorldObjectRegistry>();
        GameObject chestGo = new("Chest");
        ChestInteractable chest = chestGo.AddComponent<ChestInteractable>();
        chest.ConfigureForTests("world.chest.town.01", "item.reward.gem", 1, new ResourcesItemResolver());
        worldRegistry.ConfigureForTests(new IPersistentWorldObject[] { chest });

        GameSaveData saveData = NewGameFactory.CreateDefault();
        Assert.IsTrue(GameSessionManager.Instance.TryStartNewGame(1, "TestScene", saveData));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry, worldRegistry: worldRegistry);

        yield return null;

        Assert.IsTrue(source.IsReady);
        Assert.IsTrue(GameSessionManager.Instance.SaveRepository.TryReadSave(1, out GameSaveData written));
        Assert.IsNotNull(written.world, "Initial save must capture world state alongside the other domains.");
        Assert.AreEqual(1, written.world.objects.Count);
        Assert.AreEqual("world.chest.town.01", written.world.objects[0].persistentId);

        Object.Destroy(root);
        Object.Destroy(worldRoot);
        Object.Destroy(chestGo);
        Object.Destroy(sourceObject);
    }

    [UnityTest]
    public IEnumerator Continue_RestoresWorldStateBeforeReady_WithoutGrantingRewardsOrThrowingOnUnknownId()
    {
        var (root, stat, playerTransform, registry) = BuildFixture(Vector3.zero);

        GameObject worldRoot = new("WorldObjectRegistryFixture");
        WorldObjectRegistry worldRegistry = worldRoot.AddComponent<WorldObjectRegistry>();
        GameObject chestGo = new("Chest");
        ChestInteractable chest = chestGo.AddComponent<ChestInteractable>();
        var resolver = new ResourcesItemResolver();
        chest.ConfigureForTests("world.chest.town.01", "item.reward.gem", 1, resolver);
        worldRegistry.ConfigureForTests(new IPersistentWorldObject[] { chest });

        GameSaveData saveData = new()
        {
            saveId = "save-with-world",
            player = new PlayerSaveData { level = 1, location = new PlayerLocationSaveData { areaId = "area.tutorial" } },
            world = new WorldSaveData()
        };
        saveData.world.objects.Add(new WorldObjectSaveData { persistentId = "world.chest.town.01", kind = WorldObjectKind.Chest, flag = true });
        saveData.world.objects.Add(new WorldObjectSaveData { persistentId = "world.chest.removed_content", kind = WorldObjectKind.Chest, flag = true });

        Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(2, "TestScene", saveData));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry, worldRegistry: worldRegistry);

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("world.chest.removed_content"));

        yield return null;

        Assert.IsTrue(source.IsReady, "Restore must complete (world restore is part of the readiness gate).");
        Assert.IsTrue(chest.IsOpened, "Chest state must be restored before Playing is reached.");
        Assert.IsFalse(chest.TryOpen(out bool granted), "Restored-open chest must not be openable again.");
        Assert.IsFalse(granted);

        Object.Destroy(root);
        Object.Destroy(worldRoot);
        Object.Destroy(chestGo);
        Object.Destroy(sourceObject);
    }

    [UnityTest]
    public IEnumerator MigratedV1Save_RestoresWithoutThrowing_AndSpawnsAtTutorialDefault()
    {
        var (root, stat, playerTransform, registry) = BuildFixture(new Vector3(9f, 9f, 0f));

        // A real Phase 2-era save: only saveVersion/saveId/totalPlayTimeSeconds ever existed.
        GameSaveData legacy = new() { saveVersion = 1, saveId = "legacy-v1", totalPlayTimeSeconds = 30 };
        GameSaveData migrated = SaveMigration.Migrate(legacy);

        Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(1, "TestScene", migrated));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry);

        yield return null;

        Assert.IsTrue(source.IsReady, "A migrated legacy save must still complete restore, not stall the readiness gate.");
        Assert.AreEqual(1, stat.Level, "Migrated defaults match NewGameFactory -- level 1.");
        // BuildFixture registered NewGameFactory.TutorialStartSpawnId at (9,9,0); migration's
        // synthesized location has no saved position (NaN), so restore must fall back to it.
        Assert.AreEqual(9f, playerTransform.position.x, 0.001f);
        Assert.AreEqual(9f, playerTransform.position.y, 0.001f);

        Object.Destroy(root);
        Object.Destroy(sourceObject);
    }

    [UnityTest]
    public IEnumerator NoActiveSession_ReportsReadyWithoutTouchingPlayer()
    {
        var (root, stat, playerTransform, registry) = BuildFixture(new Vector3(9f, 9f, 0f));
        playerTransform.position = Vector3.zero;

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry);

        yield return null;

        Assert.IsTrue(source.IsReady);
        Assert.AreEqual(Vector3.zero, playerTransform.position);

        Object.Destroy(root);
        Object.Destroy(sourceObject);
    }
}
