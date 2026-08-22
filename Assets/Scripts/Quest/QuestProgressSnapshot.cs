using System;
using System.Collections.Generic;

/// <summary>
/// Read-only presentation snapshot for QuestManager.TryGetProgress -- lets UI render "1/2", "2/3"
/// without reading ToSaveData() (a persistence DTO, not a UI read-model) or reflecting into private
/// runtime state. ObjectiveCounters is a defensive copy taken at snapshot time: mutating it never
/// touches QuestRuntimeState, and the caller holding a stale snapshot after further progress simply
/// has stale data, not a live window into runtime internals.
/// </summary>
public readonly struct QuestProgressSnapshot
{
    private readonly int[] _objectiveCounters;

    public QuestProgressSnapshot(QuestStatus status, int currentObjectiveIndex, int[] objectiveCounters)
    {
        Status = status;
        CurrentObjectiveIndex = currentObjectiveIndex;
        _objectiveCounters = objectiveCounters;
    }

    public QuestStatus Status { get; }
    public int CurrentObjectiveIndex { get; }
    public IReadOnlyList<int> ObjectiveCounters => _objectiveCounters ?? Array.Empty<int>();
}
