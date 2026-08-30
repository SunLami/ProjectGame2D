// Runtime-only quest lifecycle. Never serialized as a save-contract-facing bool; Locked/Available
// are always derived from prerequisites, not stored.
public enum QuestStatus
{
    Locked,
    Available,
    Active,
    ReadyToTurnIn,
    Completed,
    Failed
}
