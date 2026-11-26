using UnityEngine;

/// <summary>Four-way facing with hysteresis (D-109): a new facing must win by a margin
/// (<see cref="Dominance"/>) and persist for <see cref="HoldTime"/> before the sprite turns, so
/// diagonal movement and path corners do not make the animation flip back and forth.</summary>
public sealed class FacingFilter
{
    public const float HoldTime = 0.15f;
    public const float Dominance = 1.25f;

    private Vector2 _pending;
    private float _pendingSince;

    public FacingFilter(Vector2 initial)
    {
        Current = ToCardinal(initial == Vector2.zero ? Vector2.down : initial);
        _pending = Current;
    }

    public Vector2 Current { get; private set; }

    /// <summary>Feeds a movement direction; <paramref name="immediate"/> turns at once (attack
    /// wind-up toward the player, pulls).</summary>
    public Vector2 Update(Vector2 direction, float now, bool immediate = false)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return Current;

        Vector2 candidate = Biased(direction);
        if (immediate || candidate == Current)
        {
            Current = candidate;
            _pending = candidate;
            _pendingSince = now;
            return Current;
        }

        if (candidate != _pending)
        {
            _pending = candidate;
            _pendingSince = now;
        }

        if (now - _pendingSince >= HoldTime)
            Current = candidate;
        return Current;
    }

    private Vector2 Biased(Vector2 direction)
    {
        float ax = Mathf.Abs(direction.x);
        float ay = Mathf.Abs(direction.y);
        bool currentHorizontal = Current.x != 0f;
        bool horizontal = currentHorizontal ? ax * Dominance >= ay : ax > ay * Dominance;
        return horizontal ? new Vector2(Mathf.Sign(direction.x), 0f) : new Vector2(0f, Mathf.Sign(direction.y));
    }

    public static Vector2 ToCardinal(Vector2 direction)
        => Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
}
