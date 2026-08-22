using System;

/// <summary>
/// Pure matching rules -- one function per objective type, no quest-specific branching
/// (DataDrivenDevelopment.md handler-registry guidance). No UI/save/QuestManager dependency, so
/// these are unit-testable in isolation.
/// </summary>
public static class QuestObjectiveMatchers
{
    public static bool MatchesTalk(QuestObjectiveDefinition objective, string npcId) =>
        objective != null
        && objective.Type == QuestObjectiveType.Talk
        && HasSameId(objective.TargetId, npcId);

    public static bool MatchesObtain(QuestObjectiveDefinition objective, string itemId) =>
        objective != null
        && objective.Type == QuestObjectiveType.Obtain
        && HasSameId(objective.TargetId, itemId);

    public static bool MatchesCraft(QuestObjectiveDefinition objective, string itemId) =>
        objective != null
        && objective.Type == QuestObjectiveType.Craft
        && HasSameId(objective.TargetId, itemId);

    public static bool MatchesPurchase(QuestObjectiveDefinition objective, string itemId) =>
        objective != null
        && objective.Type == QuestObjectiveType.Purchase
        && HasSameId(objective.TargetId, itemId);

    public static bool MatchesGather(QuestObjectiveDefinition objective, string resourceId, string areaId) =>
        objective != null
        && objective.Type == QuestObjectiveType.Gather
        && HasSameId(objective.TargetId, resourceId)
        && MatchesArea(objective.TargetAreaId, areaId);

    public static bool MatchesKill(QuestObjectiveDefinition objective, string enemyId, string areaId) =>
        objective != null
        && objective.Type == QuestObjectiveType.Kill
        && HasSameId(objective.TargetId, enemyId)
        && MatchesArea(objective.TargetAreaId, areaId);

    private static bool HasSameId(string targetId, string actualId) =>
        !string.IsNullOrEmpty(targetId) && string.Equals(targetId, actualId, StringComparison.Ordinal);

    private static bool MatchesArea(string requiredAreaId, string actualAreaId) =>
        string.IsNullOrEmpty(requiredAreaId) || string.Equals(requiredAreaId, actualAreaId, StringComparison.Ordinal);
}
