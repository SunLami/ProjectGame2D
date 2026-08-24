using UnityEngine;

/// <summary>
/// Runtime progress for one accepted quest, separate from the QuestDefinition asset and from the
/// save DTO (DataDrivenDevelopment.md three-layer model). Objectives are completed strictly in
/// authored order (index-based), matching the save contract (currentObjectiveIndex + counters).
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
        QuestObjectiveDefinition objective = CurrentObjective;
        if (objective == null || amount <= 0)
            return false;

        ObjectiveCounters[CurrentObjectiveIndex] = Mathf.Min(
            objective.TargetCount, ObjectiveCounters[CurrentObjectiveIndex] + amount);

        if (ObjectiveCounters[CurrentObjectiveIndex] >= objective.TargetCount)
            AdvanceToNextObjective();

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
        AdvanceToNextObjective();
        return true;
    }

    private void AdvanceToNextObjective()
    {
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
