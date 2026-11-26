using UnityEngine;

/// <summary>Authoring data for the scene navigation grid (D-109). Read-only at runtime.</summary>
[CreateAssetMenu(menuName = "Game/Navigation/Path Grid Settings")]
public sealed class PathGridSettings : ScriptableObject
{
    [Header("Grid")]
    [Tooltip("World size of one navigation cell.")]
    [SerializeField, Min(0.1f)] private float _cellSize = 0.5f;
    [Tooltip("Layers whose static (or Rigidbody-less) non-trigger colliders block movement. Dynamic and " +
        "kinematic bodies (player, enemies, animals) are never treated as obstacles.")]
    [SerializeField] private LayerMask _obstacleLayers = 1 << 0;
    [Tooltip("Extra margin added around the actor's collider box when probing cells, so paths keep " +
        "actors slightly away from walls and tree trunks.")]
    [SerializeField, Min(0f)] private float _clearance = 0.08f;
    [Tooltip("Seconds a probed cell stays cached before it is re-probed (picks up harvested trees, " +
        "respawned nodes and skill walls).")]
    [SerializeField, Min(0.5f)] private float _cellCacheLifetime = 8f;

    [Header("Search budget")]
    [SerializeField, Min(50)] private int _maxNodesPerSearch = 1500;
    [Tooltip("Total A* nodes all actors may expand in one frame. Further requests wait a frame.")]
    [SerializeField, Min(50)] private int _maxNodesPerFrame = 4000;
    [SerializeField, Min(1)] private int _goalSnapRadiusCells = 4;

    [Header("Path following")]
    [SerializeField, Min(0.05f)] private float _repathInterval = 0.4f;
    [Tooltip("Target must move at least this far before a periodic repath happens.")]
    [SerializeField, Min(0f)] private float _repathTargetMoveThreshold = 0.75f;
    [SerializeField, Min(0.05f)] private float _lineOfSightInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float _stuckWindow = 0.6f;
    [SerializeField, Min(0.01f)] private float _stuckDistance = 0.08f;
    [Tooltip("Consecutive stuck/failed repaths before the actor is told the target is unreachable.")]
    [SerializeField, Min(1)] private int _maxStuckRepaths = 3;
    [SerializeField, Min(0f)] private float _failedSearchCooldown = 0.5f;

    public float CellSize => _cellSize;
    public LayerMask ObstacleLayers => _obstacleLayers;
    public float Clearance => _clearance;
    public float CellCacheLifetime => _cellCacheLifetime;
    public int MaxNodesPerSearch => _maxNodesPerSearch;
    public int MaxNodesPerFrame => _maxNodesPerFrame;
    public int GoalSnapRadiusCells => _goalSnapRadiusCells;
    public float RepathInterval => _repathInterval;
    public float RepathTargetMoveThreshold => _repathTargetMoveThreshold;
    public float LineOfSightInterval => _lineOfSightInterval;
    public float StuckWindow => _stuckWindow;
    public float StuckDistance => _stuckDistance;
    public int MaxStuckRepaths => _maxStuckRepaths;
    public float FailedSearchCooldown => _failedSearchCooldown;
}
