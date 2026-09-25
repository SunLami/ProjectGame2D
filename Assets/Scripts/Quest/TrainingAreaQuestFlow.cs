using UnityEngine;

/// <summary>
/// Thin scene glue for the Training Area onboarding chain: auto-accepts quest.equip_weapon the
/// instant quest.trainer_greeting is turned in, since the equip quest has no NPC to offer it in
/// person -- it just needs to start tracking passively while the player opens their inventory and
/// equips the sword they were just given. Nothing else in this chain needs glue:
/// quest.trainer_killquest becomes naturally Available (prerequisite met) once quest.equip_weapon
/// completes (it AutoTurnIn's itself -- see QuestManager.ProgressMatching), and the Trainer NPC
/// offers it in person like any other quest via QuestNpcInteractionUI.
/// </summary>
public sealed class TrainingAreaQuestFlow : MonoBehaviour
{
    private const string GreetingQuestId = "quest.trainer_greeting";
    private const string EquipQuestId = "quest.equip_weapon";

    private void OnEnable() => QuestManager.QuestTurnedIn += HandleQuestTurnedIn;
    private void OnDisable() => QuestManager.QuestTurnedIn -= HandleQuestTurnedIn;

    private void HandleQuestTurnedIn(string questId)
    {
        if (questId == GreetingQuestId && QuestManager.Instance != null)
            QuestManager.Instance.TryAcceptQuest(EquipQuestId);
    }
}
