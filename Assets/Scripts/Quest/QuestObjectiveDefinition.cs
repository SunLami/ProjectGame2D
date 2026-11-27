using UnityEngine;

/// <summary>
/// Immutable authored objective. targetId means npcId (Talk), itemId (Obtain/Craft/Purchase),
/// resourceId (Gather) or enemyId (Kill) depending on Type -- one field, meaning fixed by type so
/// content never needs a union of unused columns. targetAreaId is optional (empty/null = any area)
/// For a Craft objective this field identifies the required station.
/// </summary>
[System.Serializable]
public sealed class QuestObjectiveDefinition
{
    [SerializeField] private QuestObjectiveType _type;
    [SerializeField] private string _targetId;
    [SerializeField] private string _requiredDialogueOutcomeId;
    [SerializeField] private string _targetAreaId;
    [SerializeField, Min(1)] private int _targetCount = 1;
    [SerializeField] private ObtainObjectiveMode _obtainMode = ObtainObjectiveMode.CountAcquired;
    [Tooltip("For Gather objectives, optional inventory item ID that satisfies the objective when already held in the required quantity.")]
    [SerializeField] private string _gatherPossessionItemId;
    [SerializeField, TextArea] private string _description;

    [Tooltip("Optional how-to line shown by TutorialOverlayUI while this is the tracked quest's "
        + "current objective (presentation only, never progression).")]
    [SerializeField, TextArea] private string _controlHint;
    [Tooltip("Optional AreaTriggerZone areaId the direction indicator points at for this objective.")]
    [SerializeField] private string _navigationAreaId;
    [Tooltip("Optional npcId the direction indicator points at for a non-Talk objective performed "
        + "at an NPC (e.g. Purchase at a shop keeper).")]
    [SerializeField] private string _navigationNpcId;

    [Tooltip("When set, NPC navigation and its hint apply only while this item is missing.")]
    [SerializeField] private string _navigationMissingItemId;
    [SerializeField, TextArea] private string _navigationMissingItemHint;

    public string NavigationMissingItemId => _navigationMissingItemId;
    public string NavigationMissingItemHint => _navigationMissingItemHint;
    public string ControlHint => _controlHint;
    public string NavigationAreaId => _navigationAreaId;
    public string NavigationNpcId => _navigationNpcId;
    public QuestObjectiveType Type => _type;
    public string TargetId => _targetId;
    public string RequiredDialogueOutcomeId => _requiredDialogueOutcomeId;

    /// <summary>Optional area for world objectives or station for Craft.</summary>
    public string TargetAreaId => _targetAreaId;
    public int TargetCount => _targetCount;

    /// <summary>Only meaningful when Type == Obtain.</summary>
    public ObtainObjectiveMode ObtainMode => _obtainMode;
    public string GatherPossessionItemId => Type == QuestObjectiveType.Gather ? _gatherPossessionItemId : null;
    public string Description => _description;
}
