using System;

/// <summary>
/// Capability seam a future NPC component composes instead of touching QuestManager internals
/// directly (TutorialAndQuestProgression.md NPC roles: "NPC MonoBehaviour khong truc tiep sua
/// QuestManager internals"). Plain C# so it is unit-testable without a scene/GameObject. No NPC
/// MonoBehaviour/prefab exists yet in this project -- that scene-authored wiring plus Quest Log/
/// NPC marker UI is Codex's job once this backend is handed off (see ClaudeToCodex.md).
/// </summary>
public sealed class QuestNpcInteractionService
{
    private readonly QuestManager _questManager;

    public QuestNpcInteractionService(QuestManager questManager)
    {
        _questManager = questManager ?? throw new ArgumentNullException(nameof(questManager));
    }

    /// <summary>Quest this npcId currently offers (Available and giverNpcId matches), if any.</summary>
    public bool TryGetOfferedQuest(string npcId, out QuestDefinition quest)
    {
        quest = null;
        if (_questManager.Catalog == null || string.IsNullOrEmpty(npcId))
            return false;

        foreach (QuestDefinition candidate in _questManager.Catalog.AllQuests)
        {
            if (!string.Equals(candidate.GiverNpcId, npcId, StringComparison.Ordinal))
                continue;
            if (_questManager.GetStatus(candidate.QuestId) != QuestStatus.Available)
                continue;

            quest = candidate;
            return true;
        }
        return false;
    }

    /// <summary>Accepts questId only if npcId is actually its giver and it is currently offered.</summary>
    public bool TryAcceptQuest(string npcId, string questId)
    {
        if (!TryGetOfferedQuest(npcId, out QuestDefinition offered) || offered.QuestId != questId)
            return false;

        return _questManager.TryAcceptQuest(questId);
    }

    /// <summary>Reports a completed conversation with npcId for Talk objective tracking. Integration
    /// gap: no dialogue system exists yet, so a future dialogue system is the real caller; until
    /// then this is exercised via a test/fake producer (see QuestDomainEvents remarks).</summary>
    public void ReportConversation(string npcId, string outcomeId) =>
        QuestDomainEvents.RaiseNpcConversationCompleted(npcId, outcomeId);

    /// <summary>Quest ready to turn in at this npcId, if any.</summary>
    public bool TryGetTurnInQuest(string npcId, out QuestDefinition quest)
    {
        quest = null;
        if (_questManager.Catalog == null || string.IsNullOrEmpty(npcId))
            return false;

        foreach (QuestDefinition candidate in _questManager.Catalog.AllQuests)
        {
            if (!string.Equals(candidate.TurnInNpcId, npcId, StringComparison.Ordinal))
                continue;
            if (_questManager.GetStatus(candidate.QuestId) != QuestStatus.ReadyToTurnIn)
                continue;

            quest = candidate;
            return true;
        }
        return false;
    }

    /// <summary>Turns in questId only if npcId is actually its turn-in target and it is ready.</summary>
    public bool TryTurnIn(string npcId, string questId, out QuestTurnInResult result)
    {
        if (!TryGetTurnInQuest(npcId, out QuestDefinition candidate) || candidate.QuestId != questId)
        {
            result = QuestTurnInResult.QuestNotFound;
            return false;
        }

        return _questManager.TryTurnIn(questId, out result);
    }
}
