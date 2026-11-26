using System.Collections.Generic;
using UnityEngine;

/// <summary>Per-actor navigation state (D-109): goes straight while the way is physically clear,
/// otherwise follows smoothed A* waypoints from <see cref="PathGridBuilder.Active"/>, repaths when
/// the target moves or the actor gets stuck, and reports <see cref="Status.NoPath"/> so the owner
/// can give up instead of pushing into a wall. Without an active grid it returns the legacy
/// straight-line direction. Plain C# object: owned by EnemyUniversal / WildlifeActor.</summary>
public sealed class PathFollower
{
    public enum Status
    {
        /// <summary>No grid in scene: legacy straight-line direction.</summary>
        Legacy,
        /// <summary>Clear straight line to the target.</summary>
        Direct,
        /// <summary>Following A* waypoints.</summary>
        Following,
        /// <summary>Waiting for the frame budget or a retry cooldown; direction may be a local steer.</summary>
        Waiting,
        /// <summary>Target unreachable (or repeatedly stuck). Owner should pick another goal.</summary>
        NoPath,
    }

    private readonly Rigidbody2D _body;
    private NavFootprint _footprint;
    private bool _footprintResolved;
    private readonly List<Vector2> _waypoints = new();
    private int _index;
    private Vector2 _pathTarget;
    private float _lastRepathAt = float.NegativeInfinity;
    private float _retryAt = float.NegativeInfinity;
    private float _nextLineOfSightAt = float.NegativeInfinity;
    private bool _lineOfSightClear;
    private Vector2 _lineOfSightTarget;
    private Vector2 _stuckAnchor;
    private float _stuckAnchorAt = float.NegativeInfinity;
    private float _lastSteerAt = float.NegativeInfinity;
    private int _stuckCount;
    private float _directSuppressedUntil = float.NegativeInfinity;
    private float _sidestepUntil = float.NegativeInfinity;
    private Vector2 _sidestep;
    private float _nextShortcutCheckAt = float.NegativeInfinity;

    public PathFollower(Rigidbody2D body)
    {
        _body = body;
    }

    public IReadOnlyList<Vector2> Waypoints => _waypoints;
    public int WaypointIndex => _index;

    /// <summary>The actor's collider footprint, resolved on first use (after physics sync).</summary>
    public NavFootprint Footprint
    {
        get
        {
            if (!_footprintResolved)
            {
                _footprint = NavFootprint.FromBody(_body);
                _footprintResolved = true;
            }

            return _footprint;
        }
    }

    /// <summary>Call if the actor's collider shape or scale changes at runtime.</summary>
    public void RefreshFootprint() => _footprintResolved = false;

    public void Reset()
    {
        _waypoints.Clear();
        _index = 0;
        _lastRepathAt = float.NegativeInfinity;
        _retryAt = float.NegativeInfinity;
        _nextLineOfSightAt = float.NegativeInfinity;
        _lineOfSightClear = false;
        _stuckAnchorAt = float.NegativeInfinity;
        _stuckCount = 0;
        _directSuppressedUntil = float.NegativeInfinity;
        _sidestepUntil = float.NegativeInfinity;
    }

    /// <summary>Returns the unit direction (or zero) an actor at <paramref name="position"/> should
    /// move this frame to reach <paramref name="target"/>.</summary>
    public Status Steer(Vector2 position, Vector2 target, out Vector2 direction)
    {
        Vector2 toTarget = target - position;
        float distance = toTarget.magnitude;
        direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

        PathGridBuilder grid = PathGridBuilder.Active;
        if (grid == null || grid.Settings == null)
            return Status.Legacy;

        PathGridSettings settings = grid.Settings;
        float now = Time.time;
        bool stuck = UpdateStuck(position, distance, now, settings);

        bool targetJumped = (target - _lineOfSightTarget).sqrMagnitude > 1f;
        if (now >= _nextLineOfSightAt || stuck || targetJumped)
        {
            _lineOfSightClear = grid.HasClearPath(position, target, Footprint);
            _lineOfSightTarget = target;
            _nextLineOfSightAt = now + settings.LineOfSightInterval;
        }

        if (stuck && _lineOfSightClear)
        {
            // No static obstacle in the way: we are pressed against a moving body (player, another
            // animal). Not a navigation failure; sidestep around it instead of giving up.
            stuck = false;
            _stuckCount = 0;
            _sidestep = grid.SteerAround(position, Random.value < 0.5f ? Rotate90(direction) : -Rotate90(direction),
                Footprint, 0.5f);
            _sidestepUntil = now + 0.35f;
        }

        if (now < _sidestepUntil && _sidestep != Vector2.zero)
        {
            direction = (_sidestep + direction * 0.5f).normalized;
            return Status.Direct;
        }

        if (stuck)
        {
            _directSuppressedUntil = now + 1f;
            if (++_stuckCount > settings.MaxStuckRepaths)
            {
                _stuckCount = 0;
                ClearPath();
                _retryAt = now + settings.FailedSearchCooldown;
                direction = Vector2.zero;
                return Status.NoPath;
            }
        }

        if (_lineOfSightClear && now >= _directSuppressedUntil)
        {
            ClearPath();
            _stuckCount = 0;
            return Status.Direct;
        }

        float threshold = settings.RepathTargetMoveThreshold;
        bool needRepath = _waypoints.Count == 0
            || _index >= _waypoints.Count
            || stuck
            || (target - _pathTarget).sqrMagnitude > 4f
            || (now - _lastRepathAt >= settings.RepathInterval
                && (target - _pathTarget).sqrMagnitude > threshold * threshold);

        if (needRepath && now >= _retryAt)
        {
            PathGridBuilder.Query query = grid.FindPath(position, target, Footprint, _waypoints);
            if (query == PathGridBuilder.Query.Deferred)
            {
                direction = grid.SteerAround(position, direction, Footprint, 0.6f);
                return Status.Waiting;
            }

            _index = 0;
            _lastRepathAt = now;
            _pathTarget = target;
            if (query == PathGridBuilder.Query.NoPath)
            {
                ClearPath();
                _retryAt = now + settings.FailedSearchCooldown;
                direction = Vector2.zero;
                return Status.NoPath;
            }
        }

        if (_waypoints.Count == 0)
        {
            direction = grid.SteerAround(position, direction, Footprint, 0.6f);
            return Status.Waiting;
        }

        float reach = grid.CellSize * 0.5f;
        while (_index < _waypoints.Count && (position - _waypoints[_index]).sqrMagnitude <= reach * reach)
            _index++;

        // Look ahead: cut to a later waypoint as soon as the body can slide there directly, so the
        // actor curves around corners instead of visiting every waypoint.
        if (now >= _nextShortcutCheckAt)
        {
            _nextShortcutCheckAt = now + settings.LineOfSightInterval * 0.5f;
            for (int ahead = Mathf.Min(_index + 2, _waypoints.Count - 1); ahead > _index; ahead--)
            {
                if (grid.HasClearPath(position, _waypoints[ahead], Footprint))
                {
                    _index = ahead;
                    break;
                }
            }
        }

        if (_index >= _waypoints.Count)
        {
            // Reached the snapped goal cell; final approach to the real target.
            direction = grid.SteerAround(position, direction, Footprint, 0.4f);
            return Status.Following;
        }

        Vector2 toWaypoint = _waypoints[_index] - position;
        direction = toWaypoint.sqrMagnitude > 0.0001f ? toWaypoint.normalized : direction;
        return Status.Following;
    }

    private bool UpdateStuck(Vector2 position, float distanceToTarget, float now, PathGridSettings settings)
    {
        // Steering was paused (attack, idle, hurt): restart the observation window.
        if (now - _lastSteerAt > 0.25f || distanceToTarget < 1f)
        {
            _stuckAnchor = position;
            _stuckAnchorAt = now;
        }

        _lastSteerAt = now;
        if (now - _stuckAnchorAt < settings.StuckWindow)
            return false;

        bool stuck = (position - _stuckAnchor).sqrMagnitude < settings.StuckDistance * settings.StuckDistance;
        _stuckAnchor = position;
        _stuckAnchorAt = now;
        if (!stuck)
            _stuckCount = 0;
        return stuck;
    }

    private static Vector2 Rotate90(Vector2 value) => new(-value.y, value.x);

    private void ClearPath()
    {
        _waypoints.Clear();
        _index = 0;
    }
}
