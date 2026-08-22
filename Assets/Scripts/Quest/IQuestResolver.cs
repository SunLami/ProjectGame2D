using System.Collections.Generic;

/// <summary>Resolves a stable questId to its QuestDefinition, mirroring IItemResolver (D-020) so
/// QuestManager depends on this abstraction rather than a concrete catalog/loading mechanism.</summary>
public interface IQuestResolver
{
    bool TryResolve(string questId, out QuestDefinition definition);
    IReadOnlyList<QuestDefinition> AllQuests { get; }
}
