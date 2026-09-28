/// <summary>
/// Completion condition types the tutorial handler supports. Code defines these behavior types;
/// TutorialStepDefinition data selects which one a given step uses (Data-Driven Development
/// Guide's handler-registry pattern) -- adding a new tutorial step with an existing type is a data
/// change, not a code change.
/// </summary>
public enum TutorialStepType
{
    Move,
    Sprint,
    Attack,
    OpenInventory,
    EquipItem,

    /// <summary>Completed when the player enters the area named by TutorialStepDefinition.TargetAreaId
    /// (fired by AreaTriggerZone). Generalizes the doc's "TravelToTown" example to any area.</summary>
    ReachArea,

    /// <summary>Completed when the quest named by TutorialStepDefinition.TargetQuestId is turned in
    /// (QuestManager.QuestCompleted) -- lets an authored tutorial chain pause on a Tutorial Quest
    /// chain step (e.g. "go talk to the NPC") without the two systems otherwise depending on each
    /// other.</summary>
    WaitForQuest
}
