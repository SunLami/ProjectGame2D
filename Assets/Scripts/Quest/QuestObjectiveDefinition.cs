using UnityEngine;

/// <summary>
/// Immutable authored objective. targetId means npcId (Talk), itemId (Obtain/Craft/Purchase),
/// resourceId (Gather) or enemyId (Kill) depending on Type -- one field, meaning fixed by type so
/// content never needs a union of unused columns. targetAreaId is optional (empty/null = any area)
/// and only consulted by Gather/Kill matchers.
/// </summary>
[System.Serializable]
public sealed class QuestObjectiveDefinition
{
    [SerializeField] private QuestObjectiveType _type;
    [SerializeField] private string _targetId;
    [SerializeField] private string _targetAreaId;
    [SerializeField, Min(1)] private int _targetCount = 1;
    [SerializeField] private ObtainObjectiveMode _obtainMode = ObtainObjectiveMode.CountAcquired;
    [SerializeField, TextArea] private string _description;

    public QuestObjectiveType Type => _type;
    public string TargetId => _targetId;

    /// <summary>Only meaningful for Gather/Kill. Empty/null means "any area".</summary>
    public string TargetAreaId => _targetAreaId;
    public int TargetCount => _targetCount;

    /// <summary>Only meaningful when Type == Obtain.</summary>
    public ObtainObjectiveMode ObtainMode => _obtainMode;
    public string Description => _description;
}
