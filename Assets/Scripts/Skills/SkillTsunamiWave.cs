using System.Collections.Generic;
using UnityEngine;

/// <summary>One wave of Skill 4 Thủy "Sóng Thần Tam Trùng" (SkillVfxPipeline.md §7.7, D-074): a crest
/// that flies straight along the aimed direction and widens as it travels so it covers a fan of
/// `_fanAngle` degrees out to `_maxRange`. Each damageable target is hit once per wave (damage +
/// knockback away from the wave's origin). The wave is blocked by any non-trigger collider that isn't
/// the Player -- it stops there, same philosophy as `SkillBeam` -- while only `IDamageable` ones take
/// the hit.
///
/// Sprite is drawn with the leading edge pointing along +X (pivot centered) and the Transform is
/// rotated to the aim angle (D-071). Visual scale is driven from the same width the hitbox uses, so
/// what's drawn matches what's hit.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillTsunamiWave : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 12f;

    [Header("Motion / Shape")]
    [SerializeField] private float _speed = 7f;
    [Tooltip("Crest span (world units) at the moment of spawn, before the fan widening kicks in.")]
    [SerializeField] private float _startWidth = 1f;
    [Tooltip("Hitbox depth along the travel direction as a fraction of the wave's current width. The sprite itself scales uniformly (undistorted); this only sizes the hit area to the crescent's actual thickness.")]
    [SerializeField] private float _hitDepthRatio = 0.55f;
    [Tooltip("Only colliders touching this central fraction of the crest span stop the wave; objects clipping the outer edges are still damaged (if damageable) but don't end the wave.")]
    [SerializeField, Range(0.05f, 1f)] private float _blockWidthRatio = 0.3f;
    [Tooltip("Upper limit on the crest span (world units). The sprite always scales uniformly -- never squashed on one axis, which would break the pixel art -- so this is what keeps the wave from becoming screen-sized.")]
    [SerializeField] private float _maxWidth = 6f;
    [Tooltip("Fraction of the fan's width at the wave's front that the crest span fills. Below 1 keeps the foam and spray inside the aim indicator's outline.")]
    [SerializeField, Range(0.3f, 1f)] private float _fanFill = 0.85f;
    [Tooltip("How far the sprite's foamy leading edge sits ahead of its center, as a fraction of the crest span. The wave is positioned so this leading edge -- not the sprite center -- is what travels out to the fan's outer arc.")]
    [SerializeField] private float _frontOffsetRatio = 0.35f;

    [Header("Damage")]
    [SerializeField] private float _damage = 25f;
    [SerializeField] private float _knockbackForce = 6f;
    [SerializeField] private LayerMask _hitLayers = ~0;
    [SerializeField] private GameObject _impactVfxPrefab;

    /// <summary>Raised once per damageable target the wave hits (used by the ultimate coordinator to add
    /// the "wet" debuff). Does not change damage/knockback behaviour.</summary>
    public event System.Action<MonoBehaviour> TargetHit;

    /// <summary>Raised right before the wave is destroyed: leading-edge position and whether it ran its full
    /// range (true) or was stopped early by a blocker (false).</summary>
    public event System.Action<Vector2, bool> Ended;

    /// <summary>Leading edge of the wave in world space (valid after Launch).</summary>
    public Vector2 FrontPosition => _origin + _direction * _travelled;
    public Vector2 Direction => _direction;
    public float CrestWidth => CurrentWidth;

    private Vector2 _origin;
    private Vector2 _direction = Vector2.right;
    private float _maxRange = 7f;
    private float _fanHalfTan = 0.7f;
    private float _damageMultiplier = 1f;
    private float _travelled;
    private float _naturalWidth = 1f;
    private float _naturalHeight = 1f;
    private float _frameTimer;
    private int _frameIndex;
    private readonly HashSet<MonoBehaviour> _alreadyHit = new HashSet<MonoBehaviour>();

    private void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        if (_frames is { Length: > 0 })
            _renderer.sprite = _frames[0];

        if (_renderer.sprite != null)
        {
            _naturalWidth = _renderer.sprite.rect.width / _renderer.sprite.pixelsPerUnit;
            _naturalHeight = _renderer.sprite.rect.height / _renderer.sprite.pixelsPerUnit;
        }
    }

    /// <summary>Starts the wave at `origin` flying along `direction`. The crest widens to follow the fan
    /// and its leading edge stops at `maxRange`, so it never leaves the aim indicator's outline.
    /// `damageMultiplier` scales damage (later waves hit harder).</summary>
    public void Launch(Vector2 origin, Vector2 direction, float maxRange, float fanAngleDegrees, float damageMultiplier)
    {
        _origin = origin;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _maxRange = Mathf.Max(0.1f, maxRange);
        _fanHalfTan = Mathf.Tan(Mathf.Clamp(fanAngleDegrees, 1f, 170f) * 0.5f * Mathf.Deg2Rad);
        _damageMultiplier = Mathf.Max(0f, damageMultiplier);
        _travelled = 0f;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        ApplyShape();
    }

    private void Update()
    {
        _travelled += _speed * Time.deltaTime;
        if (_travelled >= _maxRange)
        {
            Ended?.Invoke(_origin + _direction * _maxRange, true);
            Destroy(gameObject);
            return;
        }

        TickFrames();
        ApplyShape();

        if (ApplyHits())
        {
            Ended?.Invoke(FrontPosition, false);
            Destroy(gameObject);
        }
    }

    /// <summary>Current crest span: the fan's width at the wave's front (`_travelled` is the leading
    /// edge's distance from the origin), never narrower than the spawn width, kept slightly inside the
    /// fan by `_fanFill` and capped at `_maxWidth`.</summary>
    private float CurrentWidth => Mathf.Min(Mathf.Max(_startWidth, 2f * _travelled * _fanHalfTan) * _fanFill, _maxWidth);

    private float CurrentDepth => CurrentWidth * _hitDepthRatio;

    private void ApplyShape()
    {
        // `_travelled` tracks the leading edge, so the sprite center trails it by the front offset.
        transform.position = _origin + _direction * (_travelled - _frontOffsetRatio * CurrentWidth);
        // Uniform scale only: squashing one axis distorts the pixel art.
        float scale = CurrentWidth / Mathf.Max(0.01f, _naturalHeight);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>Damages/knocks back everything the wave overlaps this frame. Returns true when the wave
    /// is blocked by a solid collider and should end. Damage uses the full crest span, but only a
    /// collider touching the central strip (`_blockWidthRatio` of the span) blocks the wave: a wide fan
    /// would otherwise vanish entirely because of one object clipping its outer edge.</summary>
    private bool ApplyHits()
    {
        Vector2 size = new Vector2(CurrentDepth, CurrentWidth);
        float angle = transform.eulerAngles.z;
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(transform.position, size, angle, _hitLayers);

        var centralBlockers = new HashSet<Collider2D>(
            Physics2D.OverlapBoxAll(transform.position, new Vector2(CurrentDepth, CurrentWidth * _blockWidthRatio), angle, _hitLayers));

        bool blocked = false;
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            bool isDamageable = false;
            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target)
                    continue;

                isDamageable = true;
                if (target.IsDead)
                    break;

                // Keyed on the damageable component itself (not transform.root): enemies often share a
                // parent, and a root key would count the whole group as one target.
                if (!_alreadyHit.Add(candidate))
                    break;

                Vector2 away = ((Vector2)candidate.transform.position - _origin).normalized;
                target.TakeDamage(_damage * _damageMultiplier, away, _knockbackForce);

                var flash = candidate.GetComponent<EnemyHitFlash>();
                if (flash == null)
                    flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
                flash.PlayFlash();

                if (_impactVfxPrefab != null)
                    Instantiate(_impactVfxPrefab, candidate.transform.position, Quaternion.identity);

                TargetHit?.Invoke(candidate);
                break;
            }

            // Damageable targets are pass-through AoE victims; only non-damageable solid colliders
            // (walls, trees, pickups) in the central strip stop the wave.
            if (!isDamageable && centralBlockers.Contains(overlap))
                blocked = true;
        }

        return blocked;
    }

    private void TickFrames()
    {
        if (_frames is not { Length: > 1 })
            return;

        _frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(0.01f, _frameRate);
        if (_frameTimer < frameDuration)
            return;

        _frameTimer -= frameDuration;
        _frameIndex = (_frameIndex + 1) % _frames.Length;
        _renderer.sprite = _frames[_frameIndex];
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(CurrentDepth, CurrentWidth, 0f));
        Gizmos.matrix = previous;
    }
}
