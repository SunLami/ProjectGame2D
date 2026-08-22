// Result of QuestManager.TryTurnIn -- always returned even on failure so callers (NPC
// interaction/UI) can show a specific reason instead of a generic false.
public enum QuestTurnInResult
{
    Success,
    QuestNotFound,
    ObjectivesIncomplete,
    InsufficientInventoryCapacity,
    AlreadyCompleted
}
