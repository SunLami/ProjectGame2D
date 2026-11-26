using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class QuestRuntimeStateTests
{
    private static QuestObjectiveDefinition MakeObjective(QuestObjectiveType type, string targetId, int targetCount = 1)
    {
        var objective = new QuestObjectiveDefinition();
        SetPrivate(objective, "_type", type);
        SetPrivate(objective, "_targetId", targetId);
        SetPrivate(objective, "_targetCount", targetCount);
        return objective;
    }

    private static QuestDefinition MakeDefinition(params QuestObjectiveDefinition[] objectives)
    {
        var definition = ScriptableObject.CreateInstance<QuestDefinition>();
        SetPrivate(definition, "_questId", "quest.test");
        SetPrivate(definition, "_objectives", objectives);
        return definition;
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    [Test]
    public void HarvestAnyOrder_RecordsLaterHarvestAndCreditsHeldProduceIndependently()
    {
        var carrot = MakeObjective(QuestObjectiveType.Gather, "crop.carrot", 15);
        var grape = MakeObjective(QuestObjectiveType.Gather, "crop.grape", 15);
        var lemon = MakeObjective(QuestObjectiveType.Gather, "crop.lemon", 15);
        SetPrivate(grape, "_gatherPossessionItemId", "item.crop.grape");
        QuestDefinition definition = MakeDefinition(carrot, grape, lemon);
        SetPrivate(definition, "_objectivesInAnyOrder", true);
        try
        {
            var state = new QuestRuntimeState(definition);
            Assert.IsTrue(state.TryProgressMatchingObjective(
                objective => QuestObjectiveMatchers.MatchesGather(objective, "crop.lemon", "area.farm"), 3));
            Assert.IsTrue(state.ReconcileGatherPossession((id, count) => id == "item.crop.grape" && count <= 15));
            CollectionAssert.AreEqual(new[] { 0, 15, 3 }, state.ObjectiveCounters);
            Assert.AreEqual(0, state.CurrentObjectiveIndex);

            Assert.IsTrue(state.TryProgressMatchingObjective(o => o.TargetId == "crop.carrot", 15));
            Assert.AreEqual(2, state.CurrentObjectiveIndex);
            Assert.IsTrue(state.TryProgressMatchingObjective(o => o.TargetId == "crop.lemon", 12));
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, state.Status);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void PlantingAnyOrder_ResumesExistingPlotsAndFinishesAfterLastMissingCrop()
    {
        QuestDefinition definition = MakeDefinition(
            MakeObjective(QuestObjectiveType.Replant, "crop.carrot", 5),
            MakeObjective(QuestObjectiveType.Replant, "crop.grape", 5),
            MakeObjective(QuestObjectiveType.Replant, "crop.lemon", 5));
        SetPrivate(definition, "_objectivesInAnyOrder", true);
        try
        {
            var state = new QuestRuntimeState(definition);
            state.RestoreProgress(QuestStatus.Active, 0, new[] { 0, 1, 0 });

            Assert.IsTrue(state.ReconcilePlantingCounts((crop, area) => crop == "crop.grape" ? 4 : 0));
            Assert.IsTrue(state.TryProgressMatchingObjective(o => o.TargetId == "crop.lemon", 5));
            Assert.IsFalse(state.TryProgressMatchingObjective(o => o.TargetId == "crop.lemon", 1));
            CollectionAssert.AreEqual(new[] { 0, 4, 5 }, state.ObjectiveCounters);
            Assert.AreEqual(0, state.CurrentObjectiveIndex);

            Assert.IsTrue(state.TryProgressMatchingObjective(o => o.TargetId == "crop.grape", 1));
            Assert.IsTrue(state.TryProgressMatchingObjective(o => o.TargetId == "crop.carrot", 5));
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, state.Status);
            Assert.AreEqual(3, state.CurrentObjectiveIndex);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void ReconcileGatherPossession_CompletesRestoredLemonObjectiveWhenInventoryHasEnough()
    {
        var carrot = MakeObjective(QuestObjectiveType.Gather, "crop.carrot", 15);
        var lemon = MakeObjective(QuestObjectiveType.Gather, "crop.lemon", 15);
        SetPrivate(lemon, "_gatherPossessionItemId", "item.crop.lemon");
        QuestDefinition definition = MakeDefinition(carrot, lemon);
        try
        {
            var state = new QuestRuntimeState(definition);
            state.RestoreProgress(QuestStatus.Active, 1, new[] { 15, 0 });

            Assert.IsFalse(state.ReconcileGatherPossession((itemId, quantity) => 14 >= quantity));
            Assert.IsTrue(state.ReconcileGatherPossession((itemId, quantity) =>
                itemId == "item.crop.lemon" && 24 >= quantity));
            CollectionAssert.AreEqual(new[] { 15, 15 }, state.ObjectiveCounters);
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, state.Status);
            Assert.IsFalse(state.ReconcileGatherPossession((itemId, quantity) => true));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void TryProgressCurrentObjective_ClampsAtTargetCountThenAdvances()
    {
        QuestDefinition definition = MakeDefinition(
            MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green", 3),
            MakeObjective(QuestObjectiveType.Kill, "enemy.slime.blue", 1));
        try
        {
            var state = new QuestRuntimeState(definition);

            Assert.IsTrue(state.TryProgressCurrentObjective(2));
            Assert.AreEqual(2, state.ObjectiveCounters[0]);
            Assert.AreEqual(QuestStatus.Active, state.Status);
            Assert.AreEqual(0, state.CurrentObjectiveIndex);

            Assert.IsTrue(state.TryProgressCurrentObjective(10)); // overshoot must clamp, not spill into next objective
            Assert.AreEqual(3, state.ObjectiveCounters[0]);
            Assert.AreEqual(1, state.CurrentObjectiveIndex);
            Assert.AreEqual(QuestStatus.Active, state.Status);

            Assert.IsTrue(state.TryProgressCurrentObjective(1));
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, state.Status);
            Assert.IsNull(state.CurrentObjective);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void TryProgressCurrentObjective_NoCurrentObjective_ReturnsFalse()
    {
        QuestDefinition definition = MakeDefinition(MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green", 1));
        try
        {
            var state = new QuestRuntimeState(definition);
            state.TryProgressCurrentObjective(1);
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, state.Status);

            Assert.IsFalse(state.TryProgressCurrentObjective(1), "Progressing past the last objective must be a no-op.");
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void CompleteCurrentObjective_JumpsStraightToTargetAndAdvances()
    {
        QuestDefinition definition = MakeDefinition(
            MakeObjective(QuestObjectiveType.Obtain, "item.material.wood", 5));
        try
        {
            var state = new QuestRuntimeState(definition);
            Assert.IsTrue(state.CompleteCurrentObjective());
            Assert.AreEqual(5, state.ObjectiveCounters[0]);
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, state.Status);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void RestoreProgress_SetsStateWithoutSideEffectsAndClampsToDefinitionLength()
    {
        QuestDefinition definition = MakeDefinition(
            MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green", 3),
            MakeObjective(QuestObjectiveType.Kill, "enemy.slime.blue", 1));
        try
        {
            var state = new QuestRuntimeState(definition);
            state.RestoreProgress(QuestStatus.Active, 1, new[] { 3, 0 });

            Assert.AreEqual(QuestStatus.Active, state.Status);
            Assert.AreEqual(1, state.CurrentObjectiveIndex);
            Assert.AreEqual(3, state.ObjectiveCounters[0]);
            Assert.AreEqual(0, state.ObjectiveCounters[1]);

            // Objective list shrank (content update) -- index must clamp, never throw.
            state.RestoreProgress(QuestStatus.Active, 99, null);
            Assert.AreEqual(2, state.CurrentObjectiveIndex);
            Assert.AreEqual(2, state.ObjectiveCounters.Length);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }
}
