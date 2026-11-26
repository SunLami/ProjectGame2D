using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene navigation grid for ordinary enemies and wildlife (D-109). Cells are probed lazily
/// with Physics2D the first time A* touches them and cached for
/// <see cref="PathGridSettings.CellCacheLifetime"/>, so there is no load-time bake spike and
/// harvested trees / skill walls are picked up automatically. Walkability is evaluated per
/// <see cref="NavFootprint"/> (the actor's real collider box), with one cache per footprint shape.
/// Runtime-only: nothing is saved. A scene without this component keeps the legacy straight-line
/// movement.</summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class PathGridBuilder : MonoBehaviour
{
    public enum Query { Found, Partial, NoPath, Deferred }

    [SerializeField] private PathGridSettings _settings;
    [SerializeField] private bool _drawCachedCells;

    internal struct CellEntry
    {
        public bool Walkable;
        public float ProbedAt;
    }

    /// <summary>Walkability view of the grid for one footprint; what A* consumes.</summary>
    public sealed class FootprintGrid : IWalkableGrid
    {
        private readonly PathGridBuilder _owner;
        internal readonly Dictionary<Vector2Int, CellEntry> Cells = new();
        public readonly NavFootprint Footprint;

        internal FootprintGrid(PathGridBuilder owner, NavFootprint footprint)
        {
            _owner = owner;
            Footprint = footprint;
        }

        public bool IsWalkable(Vector2Int cell) => _owner.IsWalkable(this, cell);
    }

    private readonly Dictionary<int, FootprintGrid> _grids = new();
    private readonly List<Collider2D> _overlaps = new();
    private readonly List<RaycastHit2D> _hits = new();
    private readonly List<Vector2Int> _rawPath = new();
    private readonly List<Vector2Int> _smoothPath = new();
    private ContactFilter2D _filter;
    private int _budgetFrame = -1;
    private int _nodesThisFrame;

    public static PathGridBuilder Active { get; private set; }
    public PathGridSettings Settings => _settings;
    public float CellSize => _settings.CellSize;

    /// <summary>Total A* nodes expanded this frame, exposed for profiling/tests.</summary>
    public int NodesThisFrame => _budgetFrame == Time.frameCount ? _nodesThisFrame : 0;

    private void Awake()
    {
        if (_settings == null)
            _settings = ScriptableObject.CreateInstance<PathGridSettings>();
        RebuildFilter();
    }

    private void OnEnable()
    {
        if (Active != null && Active != this)
            Debug.LogWarning($"[PathGridBuilder] More than one active grid; '{name}' replaces '{Active.name}'.", this);
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    private void RebuildFilter()
    {
        _filter = new ContactFilter2D { useTriggers = false };
        _filter.SetLayerMask(_settings.ObstacleLayers);
    }

    /// <summary>Drops every cached cell (e.g. after a large world change).</summary>
    public void InvalidateAll()
    {
        foreach (FootprintGrid grid in _grids.Values)
            grid.Cells.Clear();
    }

    /// <summary>Drops cached cells overlapping <paramref name="worldBounds"/>.</summary>
    public void Invalidate(Bounds worldBounds)
    {
        Vector2Int min = WorldToCell(worldBounds.min);
        Vector2Int max = WorldToCell(worldBounds.max);
        foreach (FootprintGrid grid in _grids.Values)
            for (int x = min.x - 2; x <= max.x + 2; x++)
                for (int y = min.y - 2; y <= max.y + 2; y++)
                    grid.Cells.Remove(new Vector2Int(x, y));
    }

    public Vector2Int WorldToCell(Vector2 world)
    {
        float size = _settings.CellSize;
        return new Vector2Int(Mathf.FloorToInt(world.x / size), Mathf.FloorToInt(world.y / size));
    }

    public Vector2 CellToWorld(Vector2Int cell)
    {
        float size = _settings.CellSize;
        return new Vector2((cell.x + 0.5f) * size, (cell.y + 0.5f) * size);
    }

    public FootprintGrid GetGrid(NavFootprint footprint)
    {
        int key = footprint.CacheKey;
        if (!_grids.TryGetValue(key, out FootprintGrid grid))
        {
            grid = new FootprintGrid(this, footprint);
            _grids.Add(key, grid);
        }

        return grid;
    }

    public bool IsWalkable(Vector2 world, NavFootprint footprint)
        => IsWalkable(GetGrid(footprint), WorldToCell(world));

    private bool IsWalkable(FootprintGrid grid, Vector2Int cell)
    {
        float now = Time.time;
        if (grid.Cells.TryGetValue(cell, out CellEntry entry) && now - entry.ProbedAt < _settings.CellCacheLifetime)
            return entry.Walkable;

        // Pivot at the cell centre ± a quarter cell: the actor's real collider box must fit.
        NavFootprint footprint = grid.Footprint;
        float slack = _settings.CellSize * 0.5f + _settings.Clearance * 2f;
        Vector2 probe = footprint.HalfExtents * 2f + new Vector2(slack, slack);
        _overlaps.Clear();
        Physics2D.OverlapBox(CellToWorld(cell) + footprint.Offset, probe, 0f, _filter, _overlaps);
        bool walkable = true;
        foreach (Collider2D overlap in _overlaps)
        {
            if (IsObstacle(overlap))
            {
                walkable = false;
                break;
            }
        }

        grid.Cells[cell] = new CellEntry { Walkable = walkable, ProbedAt = now };
        return walkable;
    }

    /// <summary>Physics check that the actor's collider box can slide straight from
    /// <paramref name="from"/> to <paramref name="to"/> (pivot positions) without touching a static
    /// obstacle.</summary>
    public bool HasClearPath(Vector2 from, Vector2 to, NavFootprint footprint)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.001f)
            return true;

        _hits.Clear();
        Physics2D.BoxCast(from + footprint.Offset, footprint.HalfExtents * 1.9f, 0f, delta / distance, _filter,
            _hits, distance);
        foreach (RaycastHit2D hit in _hits)
        {
            if (IsObstacle(hit.collider))
                return false;
        }

        return true;
    }

    /// <summary>Thin line check between two points (e.g. attack line of sight through a tree).</summary>
    public bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.001f)
            return true;

        _hits.Clear();
        Physics2D.Raycast(from, delta / distance, _filter, _hits, distance);
        foreach (RaycastHit2D hit in _hits)
        {
            if (IsObstacle(hit.collider))
                return false;
        }

        return true;
    }

    /// <summary>Local avoidance: returns <paramref name="desired"/> if a short probe ahead is clear,
    /// otherwise the closest clear direction rotated up to ±135°, or zero if boxed in.</summary>
    public Vector2 SteerAround(Vector2 position, Vector2 desired, NavFootprint footprint, float lookAhead)
    {
        if (desired.sqrMagnitude < 0.0001f)
            return Vector2.zero;

        desired.Normalize();
        if (HasClearPath(position, position + desired * lookAhead, footprint))
            return desired;

        for (int step = 1; step <= 3; step++)
        {
            float angle = step * 45f;
            Vector2 left = Rotate(desired, angle);
            if (HasClearPath(position, position + left * lookAhead, footprint))
                return left;
            Vector2 right = Rotate(desired, -angle);
            if (HasClearPath(position, position + right * lookAhead, footprint))
                return right;
        }

        return Vector2.zero;
    }

    /// <summary>Finds smoothed world waypoints from <paramref name="from"/> towards
    /// <paramref name="to"/>. The last waypoint is the snapped goal cell centre.</summary>
    public Query FindPath(Vector2 from, Vector2 to, NavFootprint footprint, List<Vector2> waypoints)
    {
        waypoints.Clear();
        if (_budgetFrame != Time.frameCount)
        {
            _budgetFrame = Time.frameCount;
            _nodesThisFrame = 0;
        }

        if (_nodesThisFrame >= _settings.MaxNodesPerFrame)
            return Query.Deferred;

        FootprintGrid grid = GetGrid(footprint);
        Vector2Int start = WorldToCell(from);
        if (!grid.IsWalkable(start) && GridPathfinder.TryFindNearestWalkable(grid, start, 3, out Vector2Int freeStart))
            start = freeStart;

        if (!GridPathfinder.TryFindNearestWalkable(grid, WorldToCell(to), _settings.GoalSnapRadiusCells,
                out Vector2Int goal))
            return Query.NoPath;

        GridPathfinder.Result result = GridPathfinder.FindPath(grid, start, goal, _settings.MaxNodesPerSearch,
            _rawPath, out int expanded);
        _nodesThisFrame += expanded;
        if (result == GridPathfinder.Result.NoPath)
            return Query.NoPath;

        GridPathfinder.Smooth(grid, _rawPath, _smoothPath);
        foreach (Vector2Int cell in _smoothPath)
            waypoints.Add(CellToWorld(cell));

        return result == GridPathfinder.Result.Found ? Query.Found : Query.Partial;
    }

    private static bool IsObstacle(Collider2D collider)
    {
        if (collider == null || collider.isTrigger || !collider.enabled)
            return false;

        Rigidbody2D body = collider.attachedRigidbody;
        return body == null || body.bodyType == RigidbodyType2D.Static;
    }

    private static Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    private void OnValidate()
    {
        if (_settings != null && Application.isPlaying)
            RebuildFilter();
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawCachedCells || _settings == null)
            return;

        float size = _settings.CellSize * 0.9f;
        foreach (FootprintGrid grid in _grids.Values)
        foreach (KeyValuePair<Vector2Int, CellEntry> pair in grid.Cells)
        {
            Gizmos.color = pair.Value.Walkable ? new Color(0f, 1f, 0f, 0.15f) : new Color(1f, 0f, 0f, 0.45f);
            Gizmos.DrawCube(CellToWorld(pair.Key), new Vector3(size, size, 0.01f));
        }
    }
}
