using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

[RequireComponent(typeof(PolygonCollider2D))]
public sealed class PlayerAttackHitbox : MonoBehaviour
{
    private readonly HashSet<Component> _hitEnemies = new();
    private Player _owner;
    private PolygonCollider2D _collider;
    private SpriteRenderer _cachedAttackRenderer;
    private SpriteResolver _cachedAttackResolver;

    public void Initialize(Player owner)
    {
        _owner = owner;
        _collider = GetComponent<PolygonCollider2D>();
        _collider.isTrigger = true;
        _collider.enabled = false;
        gameObject.tag = "Player_Hitbox";
    }

    public void Configure(SpriteRenderer attackSpriteRenderer, Vector2 direction, float offset)
    {
        if (TryMatchSpritePhysicsShape(attackSpriteRenderer))
            return;

        Vector2 cardinal = ToCardinalDirection(direction);
        transform.localPosition = cardinal * offset;
        transform.localRotation = Quaternion.Euler(0f, 0f, DirectionToAngle(cardinal));
        _collider.offset = Vector2.zero;
    }

    private bool TryMatchSpritePhysicsShape(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer != _cachedAttackRenderer)
        {
            _cachedAttackRenderer = spriteRenderer;
            _cachedAttackResolver = spriteRenderer != null
                ? spriteRenderer.GetComponent<SpriteResolver>()
                : null;
        }

        // Animation Events can run before SpriteResolver has copied its newly
        // animated label to the SpriteRenderer. Resolve first so every attack
        // state reads the Physics Shape belonging to its current swing frame.
        _cachedAttackResolver?.ResolveSpriteToSpriteRenderer();

        Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        int shapeCount = sprite != null ? sprite.GetPhysicsShapeCount() : 0;
        if (shapeCount <= 0)
            return false;

        transform.localPosition = spriteRenderer.transform.localPosition;
        transform.localRotation = spriteRenderer.transform.localRotation;
        transform.localScale = spriteRenderer.transform.localScale;
        _collider.offset = Vector2.zero;
        _collider.pathCount = shapeCount;

        List<Vector2> points = new();
        for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
        {
            points.Clear();
            sprite.GetPhysicsShape(shapeIndex, points);
            if (spriteRenderer.flipX || spriteRenderer.flipY)
            {
                for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
                {
                    Vector2 point = points[pointIndex];
                    points[pointIndex] = new Vector2(
                        spriteRenderer.flipX ? -point.x : point.x,
                        spriteRenderer.flipY ? -point.y : point.y);
                }
            }

            _collider.SetPath(shapeIndex, points);
        }

        return true;
    }

    public void BeginAttack()
    {
        _hitEnemies.Clear();
        _collider.enabled = true;
    }

    public void EndAttack()
    {
        if (_collider != null)
            _collider.enabled = false;
    }

    private void LateUpdate()
    {
        if (_collider == null || !_collider.enabled || _cachedAttackRenderer == null)
            return;

        // AttackFX can change sprite several times while one damage window is
        // active. Keep the collider synchronized with the visible swing frame.
        TryMatchSpritePhysicsShape(_cachedAttackRenderer);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (_owner != null && enemy != null && _hitEnemies.Add(enemy))
        {
            _owner.DamageEnemyFromHitbox(enemy);
            return;
        }

        EnemyUniversal universalEnemy = other.GetComponentInParent<EnemyUniversal>();
        if (_owner != null && universalEnemy != null && _hitEnemies.Add(universalEnemy))
        {
            _owner.DamageEnemyFromHitbox(universalEnemy);
            return;
        }

        ResourceNodeInteractable resource = other.GetComponentInParent<ResourceNodeInteractable>();
        if (_owner != null && resource != null && _hitEnemies.Add(resource))
            resource.TryApplyHarvestHit();
    }

    private static Vector2 ToCardinalDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Vector2.down;

        return Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
    }

    private static float DirectionToAngle(Vector2 direction)
    {
        if (direction == Vector2.right) return 90f;
        if (direction == Vector2.up) return 180f;
        if (direction == Vector2.left) return -90f;
        return 0f;
    }

    private void OnDisable()
    {
        EndAttack();
    }
}
