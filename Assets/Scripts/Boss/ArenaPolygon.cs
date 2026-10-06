using UnityEngine;

/// <summary>Point-in-polygon helpers for arenas that are not a plain rectangle (the Wind arena's cross-shaped sky platform, D-110).</summary>
public static class ArenaPolygon
{
    public static bool Contains(Vector2[] polygon, Vector2 point)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }

        return inside;
    }

    /// <summary>Keeps `point` at least `margin` inside the polygon: a point outside (or closer to an edge than `margin`) is moved to the
    /// nearest boundary point and pushed inward along that edge's normal.</summary>
    public static Vector2 Constrain(Vector2[] polygon, Vector2 point, float margin)
    {
        if (polygon == null || polygon.Length < 3)
            return point;

        float area = 0f;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            area += (polygon[j].x * polygon[i].y) - (polygon[i].x * polygon[j].y);
        bool counterClockwise = area > 0f;

        float bestSqr = float.PositiveInfinity;
        Vector2 bestPoint = point;
        Vector2 bestNormal = Vector2.zero;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Length];
            Vector2 edge = b - a;
            float lengthSqr = edge.sqrMagnitude;
            if (lengthSqr < 0.0001f)
                continue;

            float t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / lengthSqr);
            Vector2 closest = a + edge * t;
            float sqr = (point - closest).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestPoint = closest;
                Vector2 left = new Vector2(-edge.y, edge.x).normalized;
                bestNormal = counterClockwise ? left : -left;
            }
        }

        if (Contains(polygon, point) && bestSqr >= margin * margin)
            return point;

        return bestPoint + bestNormal * margin;
    }
}
