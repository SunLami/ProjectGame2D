using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class QuestManagerPlayModeTests
{
    private GameObject _root;
    private GameObject _inventoryRoot;
    private GameObject _playerStatRoot;
    private QuestManager _manager;
    private readonly List<Object> _scratchAssets = new();

    [SetUp]
    public void SetUp()
    {
        _inventoryRoot = new GameObject("InventoryManagerFixture");
        _inventoryRoot.AddComponent<InventoryManager>();

        _playerStatRoot = new GameObject("PlayerStatFixture");
        _playerStatRoot.AddComponent<PlayerStat>();

        _root = new GameObject("QuestManagerFixture");
        _manager = _root.AddComponent<QuestManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_inventoryRoot);
        Object.DestroyImmediate(_playerStatRoot);
        foreach (Object asset in _scratchAssets)
            Object.DestroyImmediate(asset);
        _scratchAssets.Clear();
    }

    private QuestObjectiveDefinition MakeObjective(
        QuestObjectiveType type, string targetId, int targetCount = 1, string targetAreaId = null,
        ObtainObjectiveMode obtainMode = ObtainObjectiveMode.CountAcquired)
    {
        var objective = new QuestObjectiveDefinition();
        SetPrivate(objective, "_type", type);
        SetPrivate(objective, "_targetId", targetId);
        SetPrivate(objective, "_targetAreaId", targetAreaId);
        SetPrivate(objective, "_targetCount", targetCount);
        SetPrivate(objective, "_obtainMode", obtainMode);
        return objective;
    }

    private QuestDefinition MakeDefinition(
        string questId, QuestObjectiveDefinition[] objectives, string[] prerequisites = null,
        bool isTutorialQuest = false, bool isMainQuest = false, QuestRewardDefinition rewards = null)
    {
        var definition = ScriptableObject.CreateInstance<QuestDefinition>();
        SetPrivate(definition, "_questId", questId);
        SetPrivate(definition, "_objectives", objectives);
        SetPrivate(definition, "_prerequisiteQuestIds", prerequisites ?? System.Array.Empty<string>());
        SetPrivate(definition, "_isTutorialQuest", isTutorialQuest);
        SetPrivate(definition, "_isMainQuest", isMainQuest);
        SetPrivate(definition, "_rewards", rewards);
        _scratchAssets.Add(definition);
        return definition;
    }

    private QuestRewardDefinition MakeRewards(string itemId, int quantity, int gold = 0, int experience = 0)
    {
        var entry = new QuestRewardItemEntry();
        SetPrivate(entry, "_itemId", itemId);
        SetPrivate(entry, "_quantity", quantity);

        var rewards = new QuestRewardDefinition();
        SetPrivate(rewards, "_items", string.IsNullOrEmpty(itemId) ? System.Array.Empty<QuestRewardItemEntry>() : new[] { entry });
        SetPrivate(rewards, "_gold", gold);
        SetPrivate(rewards, "_experience", experience);
        return rewards;
    }

    private QuestCatalog MakeCatalog(params QuestDefinition[] quests)
    {
        var catalog = ScriptableObject.CreateInstance<QuestCatalog>();
        SetPrivate(catalog, "_quests", quests);
        _scratchAssets.Add(catalog);
        return catalog;
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

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private sealed class FakeItemResolver : IItemResolver
    {
        private readonly Dictionary<string, ItemSO> _map = new();
        public void Register(ItemSO item) => _map[item.itemId] = item;
        public bool TryResolve(string itemId, out ItemSO item) => _map.TryGetValue(itemId ?? "", out item);
    }

    [Test]
    public void GetStatus_PrerequisitesGateAvailability()
    {
        QuestDefinition questA = MakeDefinition("quest.a", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green") });
        QuestDefinition questB = MakeDefinition("quest.b", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.blue") },
            prerequisites: new[] { "quest.a" });
        _manager.ConfigureForTests(MakeCatalog(questA, questB));

        Assert.AreEqual(QuestStatus.Available, _manager.GetStatus("quest.a"));
        Assert.AreEqual(QuestStatus.Locked, _manager.GetStatus("quest.b"));

        Assert.IsTrue(_manager.TryAcceptQuest("quest.a"));
        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);
        Assert.IsTrue(_manager.TryTurnIn("quest.a", out QuestTurnInResult result));
        Assert.AreEqual(QuestTurnInResult.Success, result);

        Assert.AreEqual(QuestStatus.Available, _manager.GetStatus("quest.b"), "Prerequisite completed -- quest.b must unlock.");
    }

    [Test]
    public void TryAcceptQuest_OnlyOnceWhileAvailable()
    {
        QuestDefinition quest = MakeDefinition("quest.a", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green") });
        _manager.ConfigureForTests(MakeCatalog(quest));

        Assert.IsTrue(_manager.TryAcceptQuest("quest.a"));
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.a"));
        Assert.IsFalse(_manager.TryAcceptQuest("quest.a"), "Accepting an already-Active quest must fail.");
        Assert.IsFalse(_manager.TryAcceptQuest("quest.unknown"));
    }

    [Test]
    public void EachObjectiveType_ProgressesOnlyFromItsOwnMatchingEvent()
    {
        QuestDefinition quest = MakeDefinition("quest.all_types", new[]
        {
            MakeObjective(QuestObjectiveType.Talk, "npc.town.blacksmith"),
            MakeObjective(QuestObjectiveType.Craft, "item.weapon.sword.iron"),
            MakeObjective(QuestObjectiveType.Purchase, "item.potion.health"),
            MakeObjective(QuestObjectiveType.Gather, "resource.wood.log", targetCount: 2, targetAreaId: "area.forest"),
            MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green", targetAreaId: "area.tutorial"),
        });
        _manager.ConfigureForTests(MakeCatalog(quest));
        _manager.TryAcceptQuest("quest.all_types");

        QuestDomainEvents.RaiseNpcConversationCompleted("npc.town.other", "greet"); // wrong npc, must not advance
        Assert.AreEqual(0, _manager.ToSaveData().quests[0].currentObjectiveIndex);
        QuestDomainEvents.RaiseNpcConversationCompleted("npc.town.blacksmith", "greet");
        Assert.AreEqual(1, _manager.ToSaveData().quests[0].currentObjectiveIndex);

        QuestDomainEvents.RaiseItemCrafted("item.weapon.sword.bronze", 1, null); // wrong item
        Assert.AreEqual(1, _manager.ToSaveData().quests[0].currentObjectiveIndex);
        QuestDomainEvents.RaiseItemCrafted("item.weapon.sword.iron", 1, "station.forge");
        Assert.AreEqual(2, _manager.ToSaveData().quests[0].currentObjectiveIndex);

        QuestDomainEvents.RaiseItemPurchased("item.potion.health", 1, "shop.town.general");
        Assert.AreEqual(3, _manager.ToSaveData().quests[0].currentObjectiveIndex);

        QuestDomainEvents.RaiseResourceGathered("resource.wood.log", 1, "area.tutorial"); // wrong area
        Assert.AreEqual(3, _manager.ToSaveData().quests[0].currentObjectiveIndex);
        QuestDomainEvents.RaiseResourceGathered("resource.wood.log", 2, "area.forest");
        Assert.AreEqual(4, _manager.ToSaveData().quests[0].currentObjectiveIndex);

        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.blue", "area.tutorial"); // wrong enemy
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.all_types"));
        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", "area.tutorial");
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _manager.GetStatus("quest.all_types"));
    }

    [Test]
    public void Obtain_CountAcquired_AccumulatesAcrossPickups()
    {
        QuestDefinition quest = MakeDefinition("quest.obtain_count", new[]
            { MakeObjective(QuestObjectiveType.Obtain, "item.material.wood", targetCount: 3) });
        _manager.ConfigureForTests(MakeCatalog(quest));
        _manager.TryAcceptQuest("quest.obtain_count");

        QuestDomainEvents.RaiseInventoryItemAdded("item.material.iron", 5); // wrong item
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.obtain_count"));

        QuestDomainEvents.RaiseInventoryItemAdded("item.material.wood", 2);
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.obtain_count"));
        QuestDomainEvents.RaiseInventoryItemAdded("item.material.wood", 1);
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _manager.GetStatus("quest.obtain_count"));
    }

    [Test]
    public void Obtain_RequirePossession_ChecksLiveInventoryNotCumulativeCounter()
    {
        ItemSO item = MakeItem("item.material.wood");
        QuestDefinition quest = MakeDefinition("quest.obtain_possess", new[]
        {
            MakeObjective(QuestObjectiveType.Obtain, "item.material.wood", targetCount: 3,
                obtainMode: ObtainObjectiveMode.RequirePossession)
        });
        _manager.ConfigureForTests(MakeCatalog(quest));
        _manager.TryAcceptQuest("quest.obtain_possess");

        InventoryManager.Instance.AddItem(item, 2); // fires InventoryItemAdded(2) -- below target, must not complete
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.obtain_possess"));

        InventoryManager.Instance.AddItem(item, 1); // now possesses 3 total -- crosses target on this event
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _manager.GetStatus("quest.obtain_possess"));
    }

    [Test]
    public void TryTurnIn_GrantsRewardsAndMarksCompletedExactlyOnce()
    {
        ItemSO rewardItem = MakeItem("item.reward.badge", stackable: false, maxStack: 1);
        var resolver = new FakeItemResolver();
        resolver.Register(rewardItem);

        QuestDefinition quest = MakeDefinition(
            "quest.turnin", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green") },
            rewards: MakeRewards("item.reward.badge", 1, gold: 10, experience: 5));
        _manager.ConfigureForTests(MakeCatalog(quest), resolver);
        _manager.TryAcceptQuest("quest.turnin");

        int completedCount = 0;
        _manager.QuestCompleted += _ => completedCount++;

        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _manager.GetStatus("quest.turnin"));

        Assert.IsTrue(_manager.TryTurnIn("quest.turnin", out QuestTurnInResult result));
        Assert.AreEqual(QuestTurnInResult.Success, result);
        Assert.AreEqual(1, completedCount);
        Assert.AreEqual(QuestStatus.Completed, _manager.GetStatus("quest.turnin"));
        Assert.IsTrue(InventoryManager.Instance.HasItem(rewardItem, 1));
        Assert.AreEqual(10, InventoryManager.Instance.Gold);

        // Double turn-in must not regrant or fire QuestCompleted again.
        Assert.IsFalse(_manager.TryTurnIn("quest.turnin", out QuestTurnInResult secondResult));
        Assert.AreEqual(QuestTurnInResult.AlreadyCompleted, secondResult);
        Assert.AreEqual(1, completedCount);
        Assert.AreEqual(10, InventoryManager.Instance.Gold);
        Assert.IsFalse(InventoryManager.Instance.HasItem(rewardItem, 2));
    }

    [Test]
    public void TryTurnIn_InsufficientCapacity_GrantsNothingAndStaysReadyToTurnIn()
    {
        ItemSO rewardItem = MakeItem("item.reward.badge", stackable: false, maxStack: 1);
        var resolver = new FakeItemResolver();
        resolver.Register(rewardItem);

        QuestDefinition quest = MakeDefinition(
            "quest.turnin_full", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green") },
            rewards: MakeRewards("item.reward.badge", 1, gold: 10));
        _manager.ConfigureForTests(MakeCatalog(quest), resolver);
        _manager.TryAcceptQuest("quest.turnin_full");
        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);

        // Fill every inventory slot so there is no room for the reward item.
        ItemSO filler = MakeItem("item.filler", stackable: false, maxStack: 1);
        InventoryManager.Instance.AddItem(filler, InventoryManager.Instance.Slots.Count);

        Assert.IsFalse(_manager.TryTurnIn("quest.turnin_full", out QuestTurnInResult result));
        Assert.AreEqual(QuestTurnInResult.InsufficientInventoryCapacity, result);
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _manager.GetStatus("quest.turnin_full"));
        Assert.AreEqual(0, InventoryManager.Instance.Gold, "Gold must not be granted when the reward transaction as a whole fails.");
    }

    [Test]
    public void RestoreState_ReproducesRuntimeStateWithoutFiringEvents()
    {
        QuestDefinition quest = MakeDefinition("quest.restore", new[]
        {
            MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green", targetCount: 3),
            MakeObjective(QuestObjectiveType.Kill, "enemy.slime.blue"),
        });
        _manager.ConfigureForTests(MakeCatalog(quest));

        int accepted = 0, progressed = 0, completed = 0;
        _manager.QuestAccepted += _ => accepted++;
        _manager.QuestProgressChanged += _ => progressed++;
        _manager.QuestCompleted += _ => completed++;

        var saveData = new QuestSaveData();
        saveData.quests.Add(new QuestProgressSaveData
        {
            questId = "quest.restore",
            status = QuestStatus.Active,
            currentObjectiveIndex = 0,
            objectiveCounters = new[] { 2, 0 }
        });

        _manager.RestoreState(saveData);

        Assert.AreEqual(0, accepted);
        Assert.AreEqual(0, progressed);
        Assert.AreEqual(0, completed);
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.restore"));

        QuestSaveData roundTrip = _manager.ToSaveData();
        Assert.AreEqual(1, roundTrip.quests.Count);
        Assert.AreEqual(2, roundTrip.quests[0].objectiveCounters[0]);

        // Restored progress must still respond to further real events normally: counter was 2/3,
        // one more matching kill crosses the target and advances to the second objective.
        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);
        Assert.AreEqual(1, progressed);
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.restore"));
        Assert.AreEqual(1, _manager.ToSaveData().quests[0].currentObjectiveIndex);
    }

    [Test]
    public void RestoreState_UnknownQuestId_DroppedWithoutThrowing()
    {
        QuestDefinition quest = MakeDefinition("quest.known", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green") });
        _manager.ConfigureForTests(MakeCatalog(quest));

        var saveData = new QuestSaveData();
        saveData.quests.Add(new QuestProgressSaveData { questId = "quest.removed_content", status = QuestStatus.Active });

        Assert.DoesNotThrow(() => _manager.RestoreState(saveData));
        Assert.AreEqual(0, _manager.ToSaveData().quests.Count);
    }

    [Test]
    public void MainQuestUnlocked_FiresExactlyOnceWhenTutorialChainCompletes()
    {
        QuestDefinition tutorialQuest = MakeDefinition(
            "quest.tutorial.001", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green") },
            isTutorialQuest: true);
        QuestDefinition mainQuest = MakeDefinition(
            "quest.main.001", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.blue") },
            prerequisites: new[] { "quest.tutorial.001" }, isMainQuest: true);
        _manager.ConfigureForTests(MakeCatalog(tutorialQuest, mainQuest));

        int unlockedCount = 0;
        _manager.MainQuestUnlocked += () => unlockedCount++;

        Assert.IsFalse(_manager.IsMainQuestUnlocked);
        Assert.AreEqual(QuestStatus.Locked, _manager.GetStatus("quest.main.001"));

        _manager.TryAcceptQuest("quest.tutorial.001");
        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);
        Assert.IsTrue(_manager.TryTurnIn("quest.tutorial.001", out _));

        Assert.IsTrue(_manager.IsMainQuestUnlocked);
        Assert.AreEqual(1, unlockedCount);
        Assert.AreEqual(QuestStatus.Available, _manager.GetStatus("quest.main.001"));

        // Turning in another quest afterwards must not fire MainQuestUnlocked again.
        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);
        Assert.AreEqual(1, unlockedCount);
    }

    [Test]
    public void DisabledManager_DoesNotReactToDomainEvents()
    {
        QuestDefinition quest = MakeDefinition("quest.disabled", new[] { MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green") });
        _manager.ConfigureForTests(MakeCatalog(quest));
        _manager.TryAcceptQuest("quest.disabled");

        _root.SetActive(false);
        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.disabled"), "A disabled manager must not react to domain events.");

        _root.SetActive(true);
        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);
        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _manager.GetStatus("quest.disabled"));
    }
}
