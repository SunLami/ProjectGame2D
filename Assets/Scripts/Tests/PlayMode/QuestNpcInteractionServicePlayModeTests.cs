using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class QuestNpcInteractionServicePlayModeTests
{
    private GameObject _root;
    private QuestManager _manager;
    private QuestNpcInteractionService _service;
    private readonly List<Object> _scratchAssets = new();

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("QuestManagerFixture");
        _manager = _root.AddComponent<QuestManager>();
        _service = new QuestNpcInteractionService(_manager);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        foreach (Object asset in _scratchAssets)
            Object.DestroyImmediate(asset);
        _scratchAssets.Clear();
    }

    private QuestDefinition MakeDefinition(
        string questId, string giverNpcId, string turnInNpcId, string[] prerequisites = null)
    {
        var objective = new QuestObjectiveDefinition();
        SetPrivate(objective, "_type", QuestObjectiveType.Kill);
        SetPrivate(objective, "_targetId", "enemy.slime.green");
        SetPrivate(objective, "_targetCount", 1);

        var definition = ScriptableObject.CreateInstance<QuestDefinition>();
        SetPrivate(definition, "_questId", questId);
        SetPrivate(definition, "_objectives", new[] { objective });
        SetPrivate(definition, "_prerequisiteQuestIds", prerequisites ?? System.Array.Empty<string>());
        SetPrivate(definition, "_giverNpcId", giverNpcId);
        SetPrivate(definition, "_turnInNpcId", turnInNpcId);
        _scratchAssets.Add(definition);
        return definition;
    }

    private QuestCatalog MakeCatalog(params QuestDefinition[] quests)
    {
        var catalog = ScriptableObject.CreateInstance<QuestCatalog>();
        SetPrivate(catalog, "_quests", quests);
        _scratchAssets.Add(catalog);
        return catalog;
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    [Test]
    public void TryGetOfferedQuest_OnlyReturnsAvailableQuestFromTheMatchingGiver()
    {
        QuestDefinition quest = MakeDefinition("quest.tutorial.001", "npc.town.blacksmith", "npc.town.blacksmith");
        _manager.ConfigureForTests(MakeCatalog(quest));

        Assert.IsTrue(_service.TryGetOfferedQuest("npc.town.blacksmith", out QuestDefinition offered));
        Assert.AreEqual(quest, offered);
        Assert.IsFalse(_service.TryGetOfferedQuest("npc.town.other", out _), "Wrong NPC must not offer this quest.");
    }

    [Test]
    public void TryAcceptQuest_RejectsWrongNpcOrWrongQuestId()
    {
        QuestDefinition quest = MakeDefinition("quest.tutorial.001", "npc.town.blacksmith", "npc.town.blacksmith");
        _manager.ConfigureForTests(MakeCatalog(quest));

        Assert.IsFalse(_service.TryAcceptQuest("npc.town.other", "quest.tutorial.001"));
        Assert.IsFalse(_service.TryAcceptQuest("npc.town.blacksmith", "quest.unrelated"));
        Assert.AreEqual(QuestStatus.Available, _manager.GetStatus("quest.tutorial.001"), "A rejected accept attempt must not mutate state.");

        Assert.IsTrue(_service.TryAcceptQuest("npc.town.blacksmith", "quest.tutorial.001"));
        Assert.AreEqual(QuestStatus.Active, _manager.GetStatus("quest.tutorial.001"));
    }

    [Test]
    public void TryTurnIn_OnlySucceedsAtTheConfiguredTurnInNpcWhenReady()
    {
        QuestDefinition quest = MakeDefinition("quest.tutorial.001", "npc.town.blacksmith", "npc.town.blacksmith");
        _manager.ConfigureForTests(MakeCatalog(quest));
        _manager.TryAcceptQuest("quest.tutorial.001");

        Assert.IsFalse(_service.TryTurnIn("npc.town.blacksmith", "quest.tutorial.001", out QuestTurnInResult tooEarly));
        Assert.AreEqual(QuestTurnInResult.QuestNotFound, tooEarly, "Not ReadyToTurnIn yet -- service must not even reach QuestManager.TryTurnIn.");

        QuestDomainEvents.RaiseEnemyKilled("enemy.slime.green", null);

        Assert.IsFalse(_service.TryTurnIn("npc.town.other", "quest.tutorial.001", out _), "Wrong NPC must not accept turn-in.");
        Assert.IsTrue(_service.TryTurnIn("npc.town.blacksmith", "quest.tutorial.001", out QuestTurnInResult result));
        Assert.AreEqual(QuestTurnInResult.Success, result);
    }

    [Test]
    public void ReportConversation_RaisesNpcConversationCompletedForTalkObjectives()
    {
        var objective = new QuestObjectiveDefinition();
        SetPrivate(objective, "_type", QuestObjectiveType.Talk);
        SetPrivate(objective, "_targetId", "npc.town.elder");
        SetPrivate(objective, "_targetCount", 1);

        var definition = ScriptableObject.CreateInstance<QuestDefinition>();
        SetPrivate(definition, "_questId", "quest.talk");
        SetPrivate(definition, "_objectives", new[] { objective });
        SetPrivate(definition, "_prerequisiteQuestIds", System.Array.Empty<string>());
        _scratchAssets.Add(definition);
        _manager.ConfigureForTests(MakeCatalog(definition));
        _manager.TryAcceptQuest("quest.talk");

        _service.ReportConversation("npc.town.elder", "greet");

        Assert.AreEqual(QuestStatus.ReadyToTurnIn, _manager.GetStatus("quest.talk"));
    }
}
