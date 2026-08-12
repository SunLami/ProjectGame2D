using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public sealed class EnemyAttackHitbox : MonoBehaviour
{
    private readonly HashSet<Player> _hitPlayers = new();
    private Enemy _owner;
    private PolygonCollider2D _collider;
    private Vector3 _authoredLocalPosition;
    private Quaternion _authoredLocalRotation;

    public void Initialize(Enemy owner)
    {
        _owner = owner;
        _collider = GetComponent<PolygonCollider2D>();
        _collider.isTrigger = true;
        _collider.enabled = false;
        _authoredLocalPosition = transform.localPosition;
        _authoredLocalRotation = transform.localRotation;
    }

    public void Configure(Vector2 direction, float offset, bool rotateWithEnemyDirection)
    {
        if (!rotateWithEnemyDirection)
        {
            transform.localPosition = _authoredLocalPosition;
            transform.localRotation = _authoredLocalRotation;
            _collider.offset = Vector2.zero;
            return;
        }

        Vector2 cardinal = ToCardinalDirection(direction);
        transform.localPosition = cardinal * offset;
        transform.localRotation = Quaternion.Euler(0f, 0f, DirectionToAngle(cardinal));
        _collider.offset = Vector2.zero;
    }

    private static Vector2 ToCardinalDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Vector2.down;

        return Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
    }

    // The polygon is authored facing Down in the prefab.
    private static float DirectionToAngle(Vector2 direction)
    {
        if (direction == Vector2.right) return 90f;
        if (direction == Vector2.up) return 180f;
        if (direction == Vector2.left) return -90f;
        return 0f;
    }

    public void BeginAttack()
    {
        _hitPlayers.Clear();
        _collider.enabled = true;
    }

    public void EndAttack()
    {
        if (_collider != null)
            _collider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (_owner == null || player == null || !_hitPlayers.Add(player))
            return;

        _owner.DamagePlayerFromHitbox(player);
    }

    private void OnDisable()
    {
        EndAttack();
    }
}
