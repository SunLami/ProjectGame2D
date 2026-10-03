using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Continuous water laser for the "Tia liên tục" skill shape (SkillVfxPipeline.md §7.1 khuôn
/// #4, đổi cho Skill 3 Thủy theo D-073): player aims a straight line like a projectile (reuses
/// `Player.BeginAimSkill`) to confirm the cast, then actively steers the beam's direction with the
/// mouse for the whole `_duration` -- a true channel, not a fire-and-lock direction. The origin also
/// follows the caster every frame via `SetFollowTarget`. The beam stops at the first blocking collider instead of passing through it
/// (re-swept every frame, so it shortens/extends live as targets die or move) and deals periodic tick
/// damage only to that first target, rooting it in place (via `ISlowable.ApplySlow(0f, ...)`) instead
/// of knocking it back.
///
/// Sprite is drawn as a single horizontal segment (nose/flow direction along +X, pivot at the left
/// edge) and stretched via `transform.localScale.x` to reach the current effective length. (Originally
/// planned as `SpriteDrawMode.Tiled` per §7.6, but that silently failed to render anything in this
/// project's URP 2D setup -- confirmed empirically that `Simple` mode renders correctly and `Tiled`
/// does not -- so stretching is used instead, at the cost of minor pixel stretching on long beams.)</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillBeam : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 10f;
    [SerializeField] private float _duration = 2.5f;
    [Tooltip("Hitbox thickness in world units, independent of the visual sprite's natural height.")]
    [SerializeField] private float _thickness = 1f;
    [Tooltip("Vertical visual scale of the sprite (1 = natural height).")]
    [SerializeField] private float _visualThicknessScale = 1f;

    [Header("Damage / Root")]
    [SerializeField] private float _tickInterval = 0.3f;
    [SerializeField] private float _tickDamage = 4f;
    [SerializeField, Range(0f, 1f)] private float _rootSpeedMultiplier = 0f;
    [SerializeField] private LayerMask _targetLayers = ~0;
    [Tooltip("Burst VFX spawned at the blocking target's position every tick it's hit (reuses Skill 1's impact burst, same art style).")]
    [SerializeField] private GameObject _impactVfxPrefab;

    private float _maxLength = 5f;
    private float _naturalWidth = 1f;
    private float _frameTimer;
    private int _frameIndex;
    private float _tickTimer;
    private float _elapsed;
    private Transform _followTarget;
    private Vector2 _followOffset;
    private Camera _mainCamera;

    /// <summary>The beam re-anchors to `target.position + offset` every frame (e.g. the caster's body
    /// center), so it follows them instead of staying planted at the cast-time spawn point. Steering
    /// toward the mouse is handled separately every frame in `SteerTowardMouse`.</summary>
    public void SetFollowTarget(Transform target, Vector2 offset)
    {
        _followTarget = target;
        _followOffset = offset;
    }

    private void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        _renderer.drawMode = SpriteDrawMode.Simple;

        if (_frames is { Length: > 0 })
            _renderer.sprite = _frames[0];

        _naturalWidth = _renderer.sprite != null ? _renderer.sprite.rect.width / _renderer.sprite.pixelsPerUnit : 1f;
    }

    /// <summary>Aligns the beam to the aimed direction and sets its maximum range. Call right after
    /// instantiating at the player's position.</summary>
    public void Launch(Vector2 direction, float maxLength)
    {
        _maxLength = Mathf.Max(0.01f, maxLength);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        SetVisualLength(_maxLength);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        if (_followTarget != null)
            transform.position = (Vector2)_followTarget.position + _followOffset;

        SteerTowardMouse();

        TickFrames();

        Collider2D blocker = FindFirstBlocker(out float distance);
        float effectiveLength = blocker != null ? distance : _maxLength;
        SetVisualLength(effectiveLength);

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            if (blocker != null)
                ApplyHitTick(blocker);
        }
    }

    /// <summary>Continuously re-aims the beam at the mouse cursor every frame, so the player can steer
    /// it while it's active instead of its direction being locked at cast time.</summary>
    private void SteerTowardMouse()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null || Mouse.current == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 direction = (Vector2)worldPosition - (Vector2)transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetVisualLength(float length)
    {
        float scaleX = length / Mathf.Max(0.01f, _naturalWidth);
        transform.localScale = new Vector3(scaleX, _visualThicknessScale, 1f);
    }

    /// <summary>Sweeps a thin cross-section of the beam's thickness forward along its length to find
    /// the first blocking collider. ANY non-player collider on `_targetLayers` blocks -- enemies,
    /// pickups, future trees/terrain -- matching how a physical water jet would actually behave (it
    /// doesn't pass through solid things just because they aren't damageable). Damage/root is applied
    /// separately in `ApplyHitTick` only when the blocker happens to carry an `IDamageable`; a plain
    /// pickup still stops the beam but takes no damage, it just isn't a valid attack target.</summary>
    private Collider2D FindFirstBlocker(out float distance)
    {
        Vector2 origin = transform.position;
        Vector2 direction = transform.right;
        float angle = transform.eulerAngles.z;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, new Vector2(0.05f, _thickness), angle, direction, _maxLength, _targetLayers);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.GetComponentInParent<Player>() != null)
                continue;

            // A non-damageable trigger zone that already surrounds the beam origin (arena camera confiner,
            // tutorial/quest zones) is not an obstacle: without this the beam collapsed to length 0 there.
            if (hit.collider.isTrigger && hit.distance <= 0.001f && hit.collider.GetComponentInParent<IDamageable>() == null)
                continue;

            distance = hit.distance;
            return hit.collider;
        }

        distance = _maxLength;
        return null;
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

    private void ApplyHitTick(Collider2D blocker)
    {
        MonoBehaviour[] candidates = blocker.GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour candidate in candidates)
        {
            if (candidate is not IDamageable target || target.IsDead)
                continue;

            target.TakeDamage(_tickDamage, Vector2.zero, 0f);

            if (candidate is ISlowable slowable)
                slowable.ApplySlow(_rootSpeedMultiplier, _tickInterval + 0.15f);

            var flash = candidate.GetComponent<EnemyHitFlash>();
            if (flash == null)
                flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
            flash.PlayFlash();

            if (_impactVfxPrefab != null)
                Instantiate(_impactVfxPrefab, candidate.transform.position, Quaternion.identity);

            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 boxCenter = transform.position + transform.right * (_maxLength / 2f);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_maxLength, _thickness, 0f));
        Gizmos.matrix = previousMatrix;
    }
}
