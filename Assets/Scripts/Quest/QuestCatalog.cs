using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single source of truth for authored QuestDefinitions (DataDrivenDevelopment.md catalog
/// pattern). Builds an id lookup once instead of scattering Resources.LoadAll across services.
/// Editor validation (ContentValidationRunner) is responsible for rejecting duplicate/empty
/// questId; at runtime a duplicate simply keeps the first entry so a bad asset never throws.
/// </summary>
[CreateAssetMenu(fileName = "QuestCatalog", menuName = "Game/Quest/Quest Catalog")]
public sealed class QuestCatalog : ScriptableObject, IQuestResolver
{
    [SerializeField] private QuestDefinition[] _quests;

    private Dictionary<string, QuestDefinition> _byId;

    public IReadOnlyList<QuestDefinition> AllQuests => _quests ?? Array.Empty<QuestDefinition>();

    public bool TryResolve(string questId, out QuestDefinition definition)
    {
        if (string.IsNullOrEmpty(questId))
        {
            definition = null;
            return false;
        }

        EnsureLookup();
        return _byId.TryGetValue(questId, out definition);
    }

    private void EnsureLookup()
    {
        if (_byId != null)
            return;

        _byId = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
        if (_quests == null)
            return;

        foreach (QuestDefinition quest in _quests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.QuestId) || _byId.ContainsKey(quest.QuestId))
                continue;

            _byId.Add(quest.QuestId, quest);
        }
    }
}
