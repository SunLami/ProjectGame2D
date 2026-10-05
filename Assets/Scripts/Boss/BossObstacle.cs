using System.Collections.Generic;
using UnityEngine;

/// <summary>A solid thing in a boss arena (teleport pillar, coral totem...) that the boss must walk around instead of
/// through (D-106). The boss pushes its own position out of every obstacle circle; see BossController.ClampInside.
/// Registered automatically by <see cref="BossArenaController"/>.</summary>
public sealed class BossObstacle : MonoBehaviour
{
    public static readonly List<BossObstacle> All = new List<BossObstacle>();

    [SerializeField, Min(0.1f)] private float _radius = 1.2f;
    [SerializeField] private Vector2 _centerOffset;

    public Vector2 Center => (Vector2)transform.position + _centerOffset;
    public float Radius => _radius;

    public static BossObstacle Add(GameObject target, Vector2 center, float radius)
    {
        var obstacle = target.GetComponent<BossObstacle>();
        if (obstacle == null)
            obstacle = target.AddComponent<BossObstacle>();
        obstacle._centerOffset = center - (Vector2)target.transform.position;
        obstacle._radius = Mathf.Max(0.1f, radius);
        return obstacle;
    }

    private void OnEnable() => All.Add(this);

    private void OnDisable() => All.Remove(this);

    /// <summary>Moves `point` out of every obstacle circle, keeping `clearance` (the boss's own radius) from its edge.</summary>
    public static Vector2 PushOut(Vector2 point, float clearance)
    {
        for (int i = 0; i < All.Count; i++)
        {
            BossObstacle obstacle = All[i];
            if (obstacle == null)
                continue;

            Vector2 delta = point - obstacle.Center;
            float minimum = obstacle._radius + clearance;
            float distance = delta.magnitude;
            if (distance >= minimum)
                continue;

            Vector2 direction = distance > 0.01f ? delta / distance : Vector2.up;
            point = obstacle.Center + direction * minimum;
        }

        return point;
    }
}
