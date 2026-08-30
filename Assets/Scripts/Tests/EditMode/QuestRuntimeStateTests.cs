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
