using System;
using UnityEngine;

/// <summary>
/// Runtime progress for one accepted quest, separate from the QuestDefinition asset and from the
/// save DTO (DataDrivenDevelopment.md three-layer model). The current index points to the first unfinished objective.
/// Authored quests can opt into counting later objectives before that index advances.
/// </summary>
public sealed class QuestRuntimeState
{
    public QuestDefinition Definition { get; }
    public QuestStatus Status { get; private set; }
    public int CurrentObjectiveIndex { get; private set; }
    public int[] ObjectiveCounters { get; private set; }

    public QuestRuntimeState(QuestDefinition definition)
    {
        Definition = definition;
        Status = QuestStatus.Active;
        CurrentObjectiveIndex = 0;
        ObjectiveCounters = new int[definition.Objectives.Count];
    }

    public QuestObjectiveDefinition CurrentObjective =>
        Status == QuestStatus.Active
        && CurrentObjectiveIndex >= 0
        && CurrentObjectiveIndex < Definition.Objectives.Count
            ? Definition.Objectives[CurrentObjectiveIndex]
            : null;

    /// <summary>Increments the current objective's counter by amount (clamped to targetCount) and
    /// advances past it once the target is reached. Returns false if there is no active current
    /// objective to progress. Caller (QuestManager) must have already confirmed the event matches
    /// this objective before calling.</summary>
    public bool TryProgressCurrentObjective(int amount)
    {
        return CurrentObjective != null && TryProgressObjective(CurrentObjectiveIndex, amount);
    }

    public bool TryProgressMatchingObjective(Func<QuestObjectiveDefinition, bool> matches, int amount)
    {
        if (Status != QuestStatus.Active)
            return false;
        if (!Definition.ObjectivesInAnyOrder)
            return matches(CurrentObjective) && TryProgressCurrentObjective(amount);

        for (int i = CurrentObjectiveIndex; i < ObjectiveCounters.Length; i++)
            if (matches(Definition.Objectives[i]) && TryProgressObjective(i, amount))
                return true;

        return false;
    }

    public bool ReconcilePlantingCounts(Func<string, string, int> countPlants)
    {
        if (Status != QuestStatus.Active || !Definition.ObjectivesInAnyOrder)
            return false;

        bool progressed = false;
        for (int i = CurrentObjectiveIndex; i < ObjectiveCounters.Length; i++)
        {
            QuestObjectiveDefinition objective = Definition.Objectives[i];
            if (objective.Type == QuestObjectiveType.Replant && ObjectiveCounters[i] < objective.TargetCount)
                progressed |= TryProgressObjective(i, countPlants(objective.TargetId, objective.TargetAreaId) - ObjectiveCounters[i]);
        }

        return progressed;
    }

    private bool TryProgressObjective(int index, int amount)
    {
        int target = Definition.Objectives[index].TargetCount;
        if (amount <= 0 || ObjectiveCounters[index] >= target)
            return false;

        ObjectiveCounters[index] += Mathf.Min(amount, target - ObjectiveCounters[index]);
        AdvancePastCompletedObjectives();
        return true;
    }

    /// <summary>Marks the current objective's counter at target and advances -- used by
    /// Obtain(RequirePossession), which is a boolean gate re-checked against live inventory
    /// rather than an incrementing counter.</summary>
    public bool CompleteCurrentObjective()
    {
        QuestObjectiveDefinition objective = CurrentObjective;
        if (objective == null)
            return false;

        ObjectiveCounters[CurrentObjectiveIndex] = objective.TargetCount;
        AdvancePastCompletedObjectives();
        return true;
    }

    public bool ReconcileGatherPossession(Func<string, int, bool> hasItem)
    {
        bool progressed = false;
        for (int i = CurrentObjectiveIndex; Status == QuestStatus.Active && i < ObjectiveCounters.Length; i++)
        {
            if (!Definition.ObjectivesInAnyOrder && i != CurrentObjectiveIndex)
                break;

            QuestObjectiveDefinition objective = Definition.Objectives[i];
            int remaining = objective.TargetCount - ObjectiveCounters[i];
            if (remaining > 0 && !string.IsNullOrEmpty(objective.GatherPossessionItemId)
                && hasItem(objective.GatherPossessionItemId, objective.TargetCount))
            {
                progressed |= TryProgressObjective(i, remaining);
            }
        }

        return progressed;
    }

    private void AdvancePastCompletedObjectives()
    {
        while (CurrentObjectiveIndex < ObjectiveCounters.Length
               && ObjectiveCounters[CurrentObjectiveIndex] >= Definition.Objectives[CurrentObjectiveIndex].TargetCount)
            CurrentObjectiveIndex++;
        if (CurrentObjectiveIndex >= Definition.Objectives.Count)
            Status = QuestStatus.ReadyToTurnIn;
    }

    public void MarkCompleted() => Status = QuestStatus.Completed;

    /// <summary>Restore-only: sets state directly with no progression side effects (no reward
    /// grant, no event). Counters are re-sized to the current definition so a shortened/lengthened
    /// objective list after a content update never indexes out of range.</summary>
    public void RestoreProgress(QuestStatus status, int currentObjectiveIndex, int[] counters)
    {
        Status = status;
        CurrentObjectiveIndex = Mathf.Clamp(currentObjectiveIndex, 0, Definition.Objectives.Count);

        ObjectiveCounters = new int[Definition.Objectives.Count];
        if (counters == null)
            return;

        for (int i = 0; i < ObjectiveCounters.Length && i < counters.Length; i++)
            ObjectiveCounters[i] = Mathf.Max(0, counters[i]);
    }
}
