using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-driven quest content. questId is the save contract -- renaming displayName/description
/// never changes it. Read-only at runtime; QuestManager never mutates this asset (progress lives
/// in QuestRuntimeState, see DataDrivenDevelopment.md three-layer model).
/// </summary>
[CreateAssetMenu(fileName = "NewQuestDefinition", menuName = "Game/Quest/Quest Definition")]
public sealed class QuestDefinition : ScriptableObject
{
    [SerializeField] private string _questId;
    [SerializeField] private string _displayName;
    [SerializeField] private string[] _prerequisiteQuestIds;
    [SerializeField] private QuestObjectiveDefinition[] _objectives;
    [SerializeField] private QuestRewardDefinition _rewards;
    [SerializeField] private bool _isTutorialQuest;
    [SerializeField] private bool _isMainQuest;

    [Tooltip("Stable npcId that offers this quest while it is Available.")]
    [SerializeField] private string _giverNpcId;

    [Tooltip("Stable npcId that accepts turn-in while this quest is ReadyToTurnIn.")]
    [SerializeField] private string _turnInNpcId;

    public string QuestId => _questId;
    public string DisplayName => _displayName;
    public IReadOnlyList<string> PrerequisiteQuestIds => _prerequisiteQuestIds ?? Array.Empty<string>();
    public IReadOnlyList<QuestObjectiveDefinition> Objectives => _objectives ?? Array.Empty<QuestObjectiveDefinition>();
    public QuestRewardDefinition Rewards => _rewards;
    public bool IsTutorialQuest => _isTutorialQuest;
    public bool IsMainQuest => _isMainQuest;
    public string GiverNpcId => _giverNpcId;
    public string TurnInNpcId => _turnInNpcId;
}
