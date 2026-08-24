using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One reward item line: stable itemId (resolved via IItemResolver at turn-in time,
/// never a direct ScriptableObject reference in the save/runtime boundary) plus quantity.</summary>
[Serializable]
public sealed class QuestRewardItemEntry
{
    [SerializeField] private string _itemId;
    [SerializeField, Min(1)] private int _quantity = 1;

    public string ItemId => _itemId;
    public int Quantity => _quantity;
}

/// <summary>Immutable authored reward set for one QuestDefinition. Not mutated at runtime;
/// TryTurnIn reads it to grant items/gold/experience atomically.</summary>
[Serializable]
public sealed class QuestRewardDefinition
{
    [SerializeField] private QuestRewardItemEntry[] _items;
    [SerializeField, Min(0)] private int _gold;
    [SerializeField, Min(0)] private int _experience;

    public IReadOnlyList<QuestRewardItemEntry> Items => _items ?? Array.Empty<QuestRewardItemEntry>();
    public int Gold => _gold;
    public int Experience => _experience;
}
