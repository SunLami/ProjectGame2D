using UnityEngine;

/// <summary>
/// One bubble of the crab's Bubble Trap (D-101). Drifts slowly in a straight line; touching the player pops it
/// (damage + knock-back), hitting it with an attack pops it harmlessly. Two colliders like the boss hurtbox: a
/// trigger (melee/projectiles) and a solid one that excludes every layer (overlap-based player skills skip triggers).
/// </summary>
public sealed class BossBubble : MonoBehaviour, IDamageable
{
    private Vector2 _direction;
    private float _speed;
    private float _life;
    private float _hitRadius;
    private float _damage;
    private float _knockback;
    private BossController _owner;
    private Player _player;
    private bool _popped;

    public bool IsDead => _popped;

    public static BossBubble Spawn(Vector2 position, Vector2 direction, BossDefinition definition, BossController owner,
        float damage, Transform parent)
    {
        var go = new GameObject("BossBubble");
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 9;
        Sprite[] frames = BossVfx.Frames("Water/WaterOrb_Wobble");
        if (frames != null && frames.Length > 0)
            go.AddComponent<SkillFrameAnimator>().Play(frames, 8f, true, Random.value);
        else
        {
            renderer.sprite = BossTelegraph.CircleSprite;
            renderer.color = new Color(0.5f, 0.85f, 1f, 0.6f);
        }

        // WaterOrb_Wobble frames are large: scale so the visible bubble matches the hit radius (diameter ~ 2 x radius)
        float scale = 1f;
        if (frames != null && frames.Length > 0 && frames[0].bounds.size.x > 0.01f)
            scale = definition.bubbleHitRadius * 2f / frames[0].bounds.size.x;
        else
            scale = definition.bubbleHitRadius * 2f;
        go.transform.localScale = Vector3.one * scale;

        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        float colliderRadius = definition.bubbleHitRadius / Mathf.Max(0.01f, scale);
        trigger.radius = colliderRadius;
        var solid = go.AddComponent<CircleCollider2D>();
        solid.isTrigger = false;
        solid.radius = colliderRadius;
        solid.excludeLayers = ~0;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        var bubble = go.AddComponent<BossBubble>();
        bubble._direction = direction.normalized;
        bubble._speed = definition.bubbleSpeed;
        bubble._life = definition.bubbleLifeSeconds;
        bubble._hitRadius = definition.bubbleHitRadius;
        bubble._damage = damage;
        bubble._knockback = definition.bubbleKnockback;
        bubble._owner = owner;
        return bubble;
    }

    private void Update()
    {
        if (_popped)
            return;

        _life -= Time.deltaTime;
        transform.position += (Vector3)(_direction * (_speed * Time.deltaTime));
        if (_life <= 0f)
        {
            Pop(false);
            return;
        }

        if (_player == null)
            _player = FindAnyObjectByType<Player>();
        if (_player == null || _player.IsDead)
            return;

        Vector2 playerPoint = (Vector2)_player.transform.position + Vector2.up * 0.4f;
        if (Vector2.Distance(transform.position, playerPoint) <= _hitRadius + 0.3f)
        {
            _player.TakeDamage(_damage, ((Vector2)_player.transform.position - (Vector2)transform.position).normalized, _knockback);
            Pop(true);
        }
    }

    /// <summary>Called by the player's attacks: popped before it reaches anyone.</summary>
    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        if (!_popped)
            Pop(false);
    }

    private void Pop(bool hit)
    {
        _popped = true;
        BossVfx.Spawn("Water/WaterDropletProjectile_Impact", transform.position, hit ? 1.4f : 1f, 14f, false, 0.8f, 10);
        Destroy(gameObject);
    }
}
