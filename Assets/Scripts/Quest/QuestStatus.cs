// Runtime-only quest lifecycle. Never serialized as a save-contract-facing bool; Locked/Available
// normally derive from prerequisites. V16 migration uses Locked to defer existing harvest counters.
public enum QuestStatus
{
    Locked,
    Available,
    Active,
    ReadyToTurnIn,
    Completed,
    Failed
}
