// Objective behaviors QuestManager supports. Designers pick one per QuestObjectiveDefinition;
// adding a new quest with an existing type never requires touching QuestManager.
public enum QuestObjectiveType
{
    Talk,
    Obtain,
    Craft,
    Purchase,
    Gather,
    Kill,
    Equip,
    // Appended, not inserted: existing QuestDefinition assets serialize _type by enum index, so
    // new values must always go at the end.
    Replant,
    Fish
}
