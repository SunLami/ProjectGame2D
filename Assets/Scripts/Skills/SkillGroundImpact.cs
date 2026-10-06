using System.Collections.Generic;
using UnityEngine;

/// <summary>Ground-target water vortex zone for the "Vòng tròn mục tiêu" skill shape
/// (SkillVfxPipeline.md §7.1 khuôn #2): a looping vortex that persists for `_duration` seconds.
/// Enemies inside `_outerRadius` are slowed and take small periodic damage; enemies closer to the
/// center, inside `_innerRadius`, take larger periodic damage instead. Each tick also pulls affected
/// enemies toward the vortex center (suction) rather than knocking them away. Spawned by the skill's
/// confirm callback at the chosen ground position (PlayerGroundTargetCast).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillGroundImpact : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 10f;
    [SerializeField] private float _duration = 8f;

    [Header("Outer Vortex Zone (slow + small DoT)")]
    [SerializeField] private float _outerRadius = 2f;
    [SerializeField] private float _outerTickDamage = 2f;
    [SerializeField, Range(0f, 1f)] private float _outerSlowMultiplier = 0.5f;

    [Header("Inner Zone (bigger DoT, near the vortex center)")]
    [SerializeField] private float _innerRadius = 0.93f;
    [SerializeField] private float _innerTickDamage = 6f;

    [Header("Tick Timing / Suction")]
    [SerializeField] private float _tickInterval = 0.5f;
    [Tooltip("Movement speed enemies are pulled toward the vortex center at, instead of a knockback push.")]
    [SerializeField] private float _pullSpeed = 2.5f;
    [SerializeField] private LayerMask _targetLayers = ~0;

    private float _frameTimer;
    private int _frameIndex;
    private float _tickTimer;
    private float _elapsed;

    private void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        if (_frames is { Length: > 0 } && _renderer != null)
            _renderer.sprite = _frames[0];
        SoundFXManager.PlaySfxAt(SfxIds.SkillWaterS2Splash, transform.position);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        TickFrames();
        TickZoneDamage();
    }

    private void TickFrames()
    {
        if (_frames is not { Length: > 0 })
            return;

        _frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(0.01f, _frameRate);
        if (_frameTimer < frameDuration)
            return;

        _frameTimer -= frameDuration;
        _frameIndex = (_frameIndex + 1) % _frames.Length;

        if (_renderer != null)
            _renderer.sprite = _frames[_frameIndex];
    }

    private void TickZoneDamage()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer < _tickInterval)
            return;

        _tickTimer -= _tickInterval;
        ApplyZoneTick();
    }

    private void ApplyZoneTick()
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, _outerRadius, _targetLayers);
        // Dedupe by the IDamageable component, not by collider (an enemy can expose several
        // Collider2D in its children) and not by transform.root (enemies often share a parent, which
        // would collapse a whole group into one hit).
        var processedRoots = new HashSet<MonoBehaviour>();

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap.GetComponentInParent<Player>() != null)
                continue;

            MonoBehaviour[] candidates = overlap.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour candidate in candidates)
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!processedRoots.Add(candidate))
                    break;

                float distance = Vector2.Distance(candidate.transform.position, transform.position);
                bool nearCenter = distance <= _innerRadius;
                float damage = nearCenter ? _innerTickDamage : _outerTickDamage;

                target.TakeDamage(damage, Vector2.zero, 0f);

                if (candidate is ISlowable slowable)
                    slowable.ApplySlow(_outerSlowMultiplier, _tickInterval + 0.15f);

                // Suction: continuously drag the enemy toward the vortex center instead of knocking
                // it away. Re-applied every tick while they remain in the outer radius.
                if (candidate is IPullable pullable)
                    pullable.ApplyPull(transform.position, _pullSpeed, _tickInterval + 0.15f);

                var flash = candidate.GetComponent<EnemyHitFlash>();
                if (flash == null)
                    flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
                flash.PlayFlash();
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _outerRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _innerRadius);
    }
}
