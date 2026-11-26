using System.Collections.Generic;
using UnityEngine;

/// <summary>Lightweight separation steering (D-109): actors that are moving push their direction
/// away from overlapping neighbours so enemies/animals spread out instead of stacking into one
/// blob. Registry of active bodies; O(n²) over ~100 actors is negligible. Runtime only.</summary>
public static class CrowdSeparation
{
    private struct Member
    {
        public Rigidbody2D Body;
        public float Radius;
    }

    private static readonly List<Member> Members = new();

    public static void Register(Rigidbody2D body, float radius)
    {
        if (body == null)
            return;
        Unregister(body);
        Members.Add(new Member { Body = body, Radius = Mathf.Max(0.05f, radius) });
    }

    public static void Unregister(Rigidbody2D body)
    {
        for (int i = Members.Count - 1; i >= 0; i--)
            if (Members[i].Body == body || Members[i].Body == null)
                Members.RemoveAt(i);
    }

    /// <summary>Sum of push-away vectors from neighbours closer than the combined radii plus
    /// <paramref name="padding"/>; magnitude grows with overlap (0..~1 per neighbour).</summary>
    public static Vector2 Compute(Rigidbody2D self, float radius, float padding = 0.25f)
    {
        if (self == null)
            return Vector2.zero;

        Vector2 position = self.position;
        Vector2 push = Vector2.zero;
        for (int i = 0; i < Members.Count; i++)
        {
            Member other = Members[i];
            if (other.Body == null || other.Body == self)
                continue;

            Vector2 offset = position - other.Body.position;
            float reach = radius + other.Radius + padding;
            float sqr = offset.sqrMagnitude;
            if (sqr >= reach * reach)
                continue;

            float distance = Mathf.Sqrt(sqr);
            Vector2 away = distance > 0.0001f ? offset / distance : Random.insideUnitCircle.normalized;
            push += away * (1f - distance / reach);
        }

        return push;
    }

    /// <summary>Blends separation into a unit move direction. Returns a unit vector (or zero).</summary>
    public static Vector2 Apply(Rigidbody2D self, float radius, Vector2 direction, float weight = 0.8f)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return direction;

        Vector2 blended = direction + Compute(self, radius) * weight;
        return blended.sqrMagnitude > 0.0001f ? blended.normalized : direction;
    }
}
