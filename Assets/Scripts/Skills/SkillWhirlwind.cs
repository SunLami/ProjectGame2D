using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Wind Skill 2 "Whirlwind" (D-080): inside the chosen ground circle a tornado drifts slowly on a
/// small orbit for `_duration` seconds. Any enemy that touches its core is flung into the air
/// (`IAirborne`) for `_liftDuration`, cannot act while up, and takes fall damage plus a short stun when
/// it lands (a dust puff and the stun stars mark the landing). Enemies in the rest of the circle are
/// nudged gently away from the tornado. Each enemy can be lifted again only after `_liftCooldown`.
///
/// Targets are de-duplicated on the IDamageable component, never on transform.root. The tornado
/// is not blocked by anything: it is an area effect, not a projectile.</summary>
public class SkillWhirlwind : MonoBehaviour
{
    [Header("Art (all animated)")]
    [SerializeField] private Sprite[] _tornadoFrames;
    [SerializeField] private Sprite[] _ringFrames;
    [SerializeField] private Sprite[] _puffFrames;
    [SerializeField] private GameObject _landingVfxPrefab;
    [SerializeField] private SkillStunIndicator _stunIndicatorPrefab;
    [SerializeField] private float _effectFrameRate = 10f;
    [SerializeField] private float _tornadoScale = 0.55f;

    [Header("Area")]
    [Tooltip("Whole circle: enemies inside (outside the core) are nudged away. Matches the aim indicator's outer circle.")]
    [SerializeField] private float _radius = 2f;
    [Tooltip("Radius around the tornado that actually lifts enemies.")]
    [SerializeField] private float _coreRadius = 0.9f;
    [SerializeField] private float _duration = 6f;
    [Tooltip("The tornado drifts on a circle of this radius around the centre.")]
    [SerializeField] private float _driftRadius = 0.7f;
    [SerializeField] private float _driftSpeed = 0.9f;

    [Header("Lift")]
    [SerializeField] private float _liftHeight = 1f;
    [SerializeField] private float _liftDuration = 1.2f;
    [SerializeField] private float _liftCooldown = 2.5f;
    [SerializeField] private float _landingDamage = 14f;
    [SerializeField] private float _landingStun = 0.6f;

    [Header("Outer suction")]
    [Tooltip("Speed enemies inside the circle (outside the core) are drawn toward the tornado at.")]
    [SerializeField] private float _pullSpeed = 2.6f;
    [SerializeField] private float _pullDuration = 0.3f;

    [SerializeField] private LayerMask _targetLayers = ~0;

    private SpriteRenderer _tornado;
    private SpriteRenderer _ring;
    private float _elapsed;
    private float _tickTimer;
    private float _puffTimer;
    private float _driftAngle;
    private bool _ending;
    private readonly Dictionary<MonoBehaviour, float> _liftReadyAt = new Dictionary<MonoBehaviour, float>();
    private readonly Dictionary<MonoBehaviour, SkillStunIndicator> _stunIndicators = new Dictionary<MonoBehaviour, SkillStunIndicator>();

    private Vector2 Center => transform.position;
    private Vector2 TornadoPosition => Center + new Vector2(Mathf.Cos(_driftAngle), Mathf.Sin(_driftAngle)) * _driftRadius;

    private void Start()
    {
        float ringWidth = _ringFrames is { Length: > 0 } ? _ringFrames[0].bounds.size.x : 3.67f;
        _ring = CreateSprite("WhirlwindRing", _ringFrames, Center, _radius * 2f / ringWidth, -95);
        _tornado = CreateSprite("Tornado", _tornadoFrames, TornadoPosition, _tornadoScale, 3);
        _driftAngle = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        _driftAngle += _driftSpeed * Time.deltaTime;

        if (_tornado != null)
        {
            _tornado.transform.position = TornadoPosition;
            SetAlpha(_tornado, Mathf.Clamp01(Mathf.Min(_elapsed / 0.4f, (_duration - _elapsed) / 0.5f)));
        }

        if (_ring != null)
        {
            _ring.transform.Rotate(0f, 0f, -45f * Time.deltaTime);
            SetAlpha(_ring, 0.85f * Mathf.Clamp01(Mathf.Min(_elapsed / 0.4f, (_duration - _elapsed) / 0.5f)));
        }

        if (!_ending && _elapsed >= _duration)
        {
            // Stop affecting new targets but stay alive until every lifted enemy has landed.
            _ending = true;
            if (_tornado != null)
                Destroy(_tornado.gameObject);
            if (_ring != null)
                Destroy(_ring.gameObject);
            Destroy(gameObject, _liftDuration + 0.3f);
        }

        if (_ending)
            return;

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= 0.15f)
        {
            _tickTimer = 0f;
            ApplyAreaEffects();
        }

        _puffTimer += Time.deltaTime;
        if (_puffTimer >= 0.22f)
        {
            _puffTimer = 0f;
            SpawnPuff();
        }
    }

    private void ApplyAreaEffects()
    {
        Vector2 tornadoPosition = TornadoPosition;
        var processed = new HashSet<MonoBehaviour>();

        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(Center, _radius + _driftRadius, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!processed.Add(candidate))
                    break;

                Vector2 position = candidate.transform.position;
                float distance = Vector2.Distance(position, tornadoPosition);

                if (distance <= _coreRadius && candidate is IAirborne airborne && CanLift(candidate))
                {
                    _liftReadyAt[candidate] = Time.time + _liftCooldown;
                    airborne.ApplyAirborne(_liftDuration, _liftHeight);
                    StartCoroutine(LandingRoutine(candidate));
                }
                else if (distance > _coreRadius && distance <= _radius && candidate is IPullable pullable && !IsAirborne(candidate))
                {
                    // Suction: everything inside the circle is drawn toward the tornado, so enemies converge
                    // on its core and get flung up. Re-applied every tick, like the Water whirlpool.
                    pullable.ApplyPull(tornadoPosition, _pullSpeed, _pullDuration);
                }

                break;
            }
        }
    }

    private bool CanLift(MonoBehaviour candidate)
    {
        return !_liftReadyAt.TryGetValue(candidate, out float readyAt) || Time.time >= readyAt;
    }

    private bool IsAirborne(MonoBehaviour candidate)
    {
        return _liftReadyAt.TryGetValue(candidate, out float readyAt) && Time.time < readyAt - (_liftCooldown - _liftDuration);
    }

    private IEnumerator LandingRoutine(MonoBehaviour candidate)
    {
        yield return new WaitForSeconds(_liftDuration);

        if (candidate == null || candidate is not IDamageable target || target.IsDead)
            yield break;

        Vector2 position = candidate.transform.position;
        target.TakeDamage(_landingDamage, Vector2.zero, 0f);

        if (candidate is IStunnable stunnable)
        {
            stunnable.ApplyStun(_landingStun);
            ShowStunIndicator(candidate);
        }

        var flash = candidate.GetComponent<EnemyHitFlash>();
        if (flash == null)
            flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
        flash.PlayFlash();

        if (_landingVfxPrefab != null)
            Instantiate(_landingVfxPrefab, position, Quaternion.identity);
    }

    private void ShowStunIndicator(MonoBehaviour target)
    {
        if (_stunIndicatorPrefab == null)
            return;

        if (_stunIndicators.TryGetValue(target, out SkillStunIndicator existing) && existing != null)
        {
            existing.Show(target.transform, _landingStun);
            return;
        }

        SkillStunIndicator indicator = Instantiate(_stunIndicatorPrefab);
        indicator.Show(target.transform, _landingStun);
        _stunIndicators[target] = indicator;
    }

    private void SpawnPuff()
    {
        if (_puffFrames is not { Length: > 0 })
            return;

        Vector2 position = TornadoPosition + Random.insideUnitCircle * 0.5f;
        SpriteRenderer puff = CreateSprite("WhirlPuff", _puffFrames, position, 0.25f * Random.Range(0.8f, 1.3f), 4, false);
        puff.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        Destroy(puff.gameObject, _puffFrames.Length / _effectFrameRate);
    }

    private SpriteRenderer CreateSprite(string name, Sprite[] frames, Vector2 position, float scale, int sortingOrder, bool loop = true)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames is { Length: > 0 } ? frames[0] : null;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOrder;
        if (frames is { Length: > 1 })
            go.AddComponent<SkillFrameAnimator>().Play(frames, _effectFrameRate, loop, loop ? Random.value : 0f);
        return sr;
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _radius);
        Gizmos.color = new Color(0.5f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _coreRadius);
    }
}
