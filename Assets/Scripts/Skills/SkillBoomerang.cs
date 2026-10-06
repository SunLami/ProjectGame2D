using System.Collections.Generic;
using UnityEngine;

/// <summary>Wind Skill 1 "Wind Blade" (D-080): a spinning crescent of wind that flies out along the aimed
/// direction, turns around at full range (or immediately when it meets a solid obstacle) and flies
/// back to the caster, who catches it. Each pass hits every enemy once -- at most two hits per enemy
/// per cast -- and the return pass tugs the enemies it touches gently toward the caster.
///
/// Targets are found with a per-frame overlap (not triggers) and de-duplicated per pass on the
/// IDamageable component, never on transform.root, so groups of enemies sharing a parent are all hit.
/// Solid non-damageable colliders on the way out (walls, trees) make the blade turn back, in line with
/// SkillBeam / SkillTsunamiWave; the Earth arena's `SkillWall` layer does not stop it.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillBoomerang : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 14f;
    [Tooltip("Degrees per second the sprite additionally spins, so the crescent reads as rotating even between animation frames.")]
    [SerializeField] private float _spinSpeed = 540f;

    [Header("Flight")]
    [SerializeField] private float _outSpeed = 10f;
    [SerializeField] private float _returnSpeed = 13f;
    [SerializeField] private float _catchDistance = 0.6f;
    [Tooltip("Safety net: the blade disappears after this many seconds even if it never reaches the caster.")]
    [SerializeField] private float _maxLifetime = 8f;

    [Header("Damage")]
    [SerializeField] private float _outDamage = 14f;
    [SerializeField] private float _returnDamage = 10f;
    [SerializeField] private float _outKnockback = 3f;
    [Tooltip("Speed enemies hit on the way back are tugged toward the caster at.")]
    [SerializeField] private float _returnPullSpeed = 3f;
    [SerializeField] private float _returnPullDuration = 0.35f;
    [SerializeField] private float _hitRadius = 0.55f;
    [SerializeField] private float _obstacleRadius = 0.3f;
    [SerializeField] private LayerMask _targetLayers = ~0;
    [SerializeField] private string _ignoredWallLayerName = "SkillWall";

    [Header("Effects")]
    [SerializeField] private GameObject _impactVfxPrefab;
    [SerializeField] private Sprite[] _trailFrames;
    [SerializeField] private float _trailInterval = 0.07f;
    [SerializeField] private float _trailScale = 0.22f;
    [SerializeField] private float _trailFrameRate = 12f;

    private enum Phase { Outbound, Returning }

    private Player _owner;
    private Vector2 _direction;
    private float _range;
    private float _travelled;
    private Phase _phase = Phase.Outbound;
    private float _lifetime;
    private float _trailTimer;
    private float _frameTimer;
    private int _frameIndex;
    private int _blockMask;
    private readonly HashSet<MonoBehaviour> _hitThisPass = new HashSet<MonoBehaviour>();

    private void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        _renderer.sortingLayerName = "Player";
        _renderer.sortingOrder = 5;
        if (_frames is { Length: > 0 })
            _renderer.sprite = _frames[0];
    }

    /// <summary>Starts the blade at the caster's skill origin flying along `direction` for `range` units before returning.</summary>
    public void Launch(Player owner, Vector2 direction, float range)
    {
        _owner = owner;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _range = Mathf.Max(0.5f, range);

        int wallLayer = LayerMask.NameToLayer(_ignoredWallLayerName);
        _blockMask = wallLayer >= 0 ? (int)_targetLayers & ~(1 << wallLayer) : (int)_targetLayers;

        if (owner != null)
            transform.position = owner.SkillOrigin;

        SoundFXManager.PlaySfx(SfxIds.SkillWindS1Throw);
        _loop = SoundFXManager.StartLoop(SfxIds.SkillWindS1Loop, 0.8f, 1f, 0.1f);
    }

    private SfxLoopHandle _loop;

    private void OnDestroy() => _loop?.Stop(0.1f);

    private void Update()
    {
        _lifetime += Time.deltaTime;
        if (_owner == null || _lifetime >= _maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
        TickFrames();
        TickTrail();

        if (_phase == Phase.Outbound)
            MoveOutbound();
        else if (MoveReturning())
            return;

        ApplyHits();
    }

    private void MoveOutbound()
    {
        float step = _outSpeed * Time.deltaTime;
        transform.position += (Vector3)(_direction * step);
        _travelled += step;

        if (_travelled >= _range || HitsObstacle())
            TurnAround();
    }

    /// <summary>Returns true when the blade was caught and destroyed this frame.</summary>
    private bool MoveReturning()
    {
        Vector2 target = _owner.SkillOrigin;
        Vector2 toOwner = target - (Vector2)transform.position;
        float step = _returnSpeed * Time.deltaTime;

        if (toOwner.magnitude <= Mathf.Max(_catchDistance, step))
        {
            Catch();
            return true;
        }

        transform.position += (Vector3)(toOwner.normalized * step);
        return false;
    }

    private void TurnAround()
    {
        _phase = Phase.Returning;
        _hitThisPass.Clear(); // enemies hit on the way out can be hit again on the way back
    }

    private void Catch()
    {
        SoundFXManager.PlaySfx(SfxIds.SkillWindS1Catch);
        if (_impactVfxPrefab != null)
        {
            GameObject vfx = Instantiate(_impactVfxPrefab, _owner.SkillOrigin, Quaternion.identity);
            vfx.transform.localScale *= 0.6f;
        }

        Destroy(gameObject);
    }

    private bool HitsObstacle()
    {
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, _obstacleRadius, _blockMask))
        {
            if (collider.isTrigger || collider.GetComponentInParent<Player>() != null)
                continue;

            bool damageable = false;
            foreach (MonoBehaviour behaviour in collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IDamageable)
                {
                    damageable = true;
                    break;
                }
            }

            if (!damageable)
                return true;
        }

        return false;
    }

    private void ApplyHits()
    {
        bool returning = _phase == Phase.Returning;
        float damage = returning ? _returnDamage : _outDamage;

        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(transform.position, _hitRadius, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!_hitThisPass.Add(candidate))
                    break;

                Vector2 knockDirection = returning ? Vector2.zero : _direction;
                target.TakeDamage(damage, knockDirection, returning ? 0f : _outKnockback);

                if (returning && candidate is IPullable pullable)
                    pullable.ApplyPull(_owner.SkillOrigin, _returnPullSpeed, _returnPullDuration);

                var flash = candidate.GetComponent<EnemyHitFlash>();
                if (flash == null)
                    flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
                flash.PlayFlash();

                if (_impactVfxPrefab != null)
                    Instantiate(_impactVfxPrefab, candidate.transform.position, Quaternion.identity);

                break;
            }
        }
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

    private void TickTrail()
    {
        if (_trailFrames is not { Length: > 0 })
            return;

        _trailTimer += Time.deltaTime;
        if (_trailTimer < _trailInterval)
            return;

        _trailTimer = 0f;
        var go = new GameObject("WindTrail");
        go.transform.position = transform.position;
        go.transform.localScale = new Vector3(_trailScale, _trailScale, 1f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _trailFrames[0];
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 4;
        go.AddComponent<SkillFrameAnimator>().Play(_trailFrames, _trailFrameRate, false);

        Destroy(go, _trailFrames.Length / Mathf.Max(0.01f, _trailFrameRate));
    }
}
