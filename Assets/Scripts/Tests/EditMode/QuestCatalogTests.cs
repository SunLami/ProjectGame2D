using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class QuestCatalogTests
{
    private static QuestDefinition MakeDefinition(string questId)
    {
        var definition = ScriptableObject.CreateInstance<QuestDefinition>();
        SetPrivate(definition, "_questId", questId);
        return definition;
    }

    private static QuestCatalog MakeCatalog(params QuestDefinition[] quests)
    {
        var catalog = ScriptableObject.CreateInstance<QuestCatalog>();
        SetPrivate(catalog, "_quests", quests);
        return catalog;
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    [Test]
    public void TryResolve_FindsQuestByStableId()
    {
        QuestDefinition tutorial = MakeDefinition("quest.tutorial.crafting.001");
        QuestDefinition main = MakeDefinition("quest.main.001");
        QuestCatalog catalog = MakeCatalog(tutorial, main);
        try
        {
            Assert.IsTrue(catalog.TryResolve("quest.main.001", out QuestDefinition resolved));
            Assert.AreEqual(main, resolved);
            Assert.AreEqual(2, catalog.AllQuests.Count);
        }
        finally
        {
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(tutorial);
            Object.DestroyImmediate(main);
        }
    }

    [Test]
    public void TryResolve_UnknownOrEmptyId_ReturnsFalse()
    {
        QuestCatalog catalog = MakeCatalog(MakeDefinition("quest.tutorial.crafting.001"));
        try
        {
            Assert.IsFalse(catalog.TryResolve("quest.unknown", out QuestDefinition resolved));
            Assert.IsNull(resolved);
            Assert.IsFalse(catalog.TryResolve(null, out _));
            Assert.IsFalse(catalog.TryResolve("", out _));
        }
        finally
        {
            Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void ProductionQuestFlow_GatesCenhelmTutorialBehindWigheardTraining()
    {
        QuestDefinition wigheardGreeting = AssetDatabase.LoadAssetAtPath<QuestDefinition>(
            "Assets/Quests/Definitions/Quest_TrainerGreeting.asset");
        QuestDefinition cenhelmTutorial = AssetDatabase.LoadAssetAtPath<QuestDefinition>(
            "Assets/Quests/Definitions/Quest_TutorialCrafting001.asset");
        QuestDefinition cenhelmMain = AssetDatabase.LoadAssetAtPath<QuestDefinition>(
            "Assets/Quests/Definitions/Quest_Main001.asset");

        Assert.NotNull(wigheardGreeting);
        Assert.NotNull(cenhelmTutorial);
        Assert.NotNull(cenhelmMain);
        Assert.AreEqual(wigheardGreeting.GiverNpcId, wigheardGreeting.Objectives[0].TargetId,
            "Wigheard's Talk objective must resolve to the same registered NPC that offers it.");
        CollectionAssert.Contains(cenhelmTutorial.PrerequisiteQuestIds, "quest.trainer_killquest");
        CollectionAssert.Contains(cenhelmMain.PrerequisiteQuestIds, cenhelmTutorial.QuestId);
    }
}
