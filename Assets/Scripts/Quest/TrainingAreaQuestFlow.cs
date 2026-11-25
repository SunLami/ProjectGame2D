using UnityEngine;

/// <summary>
/// Thin scene glue for the Act I (Heorte) Intro Quest Flow. Every link in the chain is a plain
/// giver/turnIn NPC visit resolved naturally by QuestNpcInteractionUI once its prerequisite
/// completes (so its own _questDialogues entry can show -- TutorialAndQuestProgression.md
/// "Production content in MapNhat") and needs no glue here. The one exception is
/// quest.act1.back_to_fields, which has no giver NPC at all (nobody to offer it in person): it is
/// auto-accepted the instant quest.act1.stocking_up_eadric is turned in, so the player starts
/// tracking it passively while walking to the farm.
///
/// Do not add more glue links for quests that DO have a giver -- auto-accepting one jumps it
/// straight to Active, skipping the Available state QuestNpcInteractionUI needs to resolve that
/// quest's own dialogue/marker at its giver; the NPC falls back to its generic greeting instead
/// (and that generic dialogue's choices make Continue() spin forever without ever reporting the
/// conversation, since QuestObjectiveMatchers.MatchesTalk still has nothing to match against).
/// </summary>
public sealed class TrainingAreaQuestFlow : MonoBehaviour
{
    private const string StockingUpEadricQuestId = "quest.act1.stocking_up_eadric";
    private const string BackToFieldsQuestId = "quest.act1.back_to_fields";

    private void OnEnable() => QuestManager.QuestTurnedIn += HandleQuestTurnedIn;
    private void OnDisable() => QuestManager.QuestTurnedIn -= HandleQuestTurnedIn;

    private void HandleQuestTurnedIn(string questId)
    {
        if (questId == StockingUpEadricQuestId && QuestManager.Instance != null)
            QuestManager.Instance.TryAcceptQuest(BackToFieldsQuestId);
    }
}
