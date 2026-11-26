using System.Collections.Generic;
using UnityEngine;

/// <summary>Axis-aligned collision footprint of an actor relative to its Rigidbody2D position
/// (D-109). Navigation plans with the real solid collider shape, so tall full-body boxes (e.g. the
/// scaled Ent) path wide enough around tree trunks instead of being treated as a small circle.</summary>
public readonly struct NavFootprint
{
    public readonly Vector2 HalfExtents;
    public readonly Vector2 Offset;

    public NavFootprint(Vector2 halfExtents, Vector2 offset)
    {
        HalfExtents = new Vector2(Mathf.Max(0.05f, halfExtents.x), Mathf.Max(0.05f, halfExtents.y));
        Offset = offset;
    }

    public static NavFootprint Default => new(new Vector2(0.25f, 0.25f), Vector2.zero);

    /// <summary>Quantised (0.05 units) key so actors with the same shape share one cell cache.</summary>
    public int CacheKey
    {
        get
        {
            int hx = Mathf.RoundToInt(HalfExtents.x * 20f);
            int hy = Mathf.RoundToInt(HalfExtents.y * 20f);
            int ox = Mathf.RoundToInt(Offset.x * 20f) + 128;
            int oy = Mathf.RoundToInt(Offset.y * 20f) + 128;
            return (hx & 0xFF) | ((hy & 0xFF) << 8) | ((ox & 0xFF) << 16) | ((oy & 0xFF) << 24);
        }
    }

    /// <summary>Union of the body's solid (non-trigger) collider bounds, relative to body.position.
    /// Call after physics has synced transforms (first FixedUpdate/Update), not in Awake.</summary>
    public static NavFootprint FromBody(Rigidbody2D body)
    {
        if (body == null)
            return Default;

        List<Collider2D> colliders = new();
        body.GetAttachedColliders(colliders);
        bool any = false;
        Bounds union = default;
        foreach (Collider2D collider in colliders)
        {
            if (collider == null || collider.isTrigger || !collider.enabled)
                continue;
            if (!any)
            {
                union = collider.bounds;
                any = true;
            }
            else
            {
                union.Encapsulate(collider.bounds);
            }
        }

        if (!any || union.extents.sqrMagnitude < 0.0001f)
            return Default;

        return new NavFootprint(union.extents, (Vector2)union.center - body.position);
    }
}
