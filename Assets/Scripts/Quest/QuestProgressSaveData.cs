using System;

// Plain serializable per-quest progress snapshot. Only quests that have been touched (Active,
// ReadyToTurnIn or Completed) are stored; Locked/Available are always derived from prerequisites
// on load, never saved (TutorialAndQuestProgression.md Main Quest gate note).
[Serializable]
public sealed class QuestProgressSaveData
{
    public string questId;
    public QuestStatus status;
    public int currentObjectiveIndex;
    public int[] objectiveCounters;
}
