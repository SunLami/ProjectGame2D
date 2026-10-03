using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Wind Skill 3 "Gale Fan" (D-080): a narrow cone in front of the caster blows three times in a row.
/// Every pulse deals damage to the enemies inside the cone and shoves them far away from the caster
/// (a short, fast `IPullable` push toward a point beyond them, so the distance does not depend on
/// enemy mass or on the Hit-stagger state). An enemy that is shoved into a solid, non-damageable
/// collider -- a wall, a tree, or the Earth Skill 2 arena rim (layer `SkillWall`) -- gets
/// slammed: extra damage, a short stun and an impact burst, once per enemy per cast.
///
/// The slam is predicted up front with a circle cast along the push direction (distance = push speed *
/// push duration), then applied when the enemy would reach the obstacle, so it also works against
/// obstacles that the physics push would otherwise just stop at silently.
/// Targets are de-duplicated per pulse on the IDamageable component, never on transform.root.</summary>
public class SkillGustFan : MonoBehaviour
{
    [Header("Art (all animated)")]
    [SerializeField] private Sprite[] _gustFrames;
    [SerializeField] private Sprite[] _puffFrames;
    [SerializeField] private GameObject _slamVfxPrefab;
    [SerializeField] private SkillStunIndicator _stunIndicatorPrefab;
    [SerializeField] private float _effectFrameRate = 12f;

    [Header("Pulses")]
    [SerializeField] private int _pulseCount = 3;
    [SerializeField] private float _pulseInterval = 0.35f;
    [SerializeField] private float _pulseDamage = 8f;
    [Tooltip("Speed of the visible gust wave travelling out through the cone.")]
    [SerializeField] private float _gustSpeed = 14f;

    [Header("Push")]
    [SerializeField] private float _pushSpeed = 10f;
    [SerializeField] private float _pushDuration = 0.25f;
    [SerializeField] private float _pushCastRadius = 0.3f;

    [Header("Wall slam")]
    [SerializeField] private float _slamDamage = 18f;
    [SerializeField] private float _slamStun = 0.8f;
    [SerializeField] private float _slamShake = 0.1f;

    [SerializeField] private LayerMask _targetLayers = ~0;

    private Vector2 _origin;
    private Vector2 _direction;
    private float _range;
    private float _halfAngleRad;
    private float _halfTan;
    private readonly HashSet<MonoBehaviour> _slammed = new HashSet<MonoBehaviour>();
    private readonly Dictionary<MonoBehaviour, SkillStunIndicator> _stunIndicators = new Dictionary<MonoBehaviour, SkillStunIndicator>();
    private int _pulsesDone;

    /// <summary>Starts the three pulses. `direction` is the confirmed aim, `fanAngleDegrees` the full cone angle.</summary>
    public void Launch(Vector2 origin, Vector2 direction, float range, float fanAngleDegrees)
    {
        _origin = origin;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _range = range;
        _halfAngleRad = Mathf.Clamp(fanAngleDegrees, 1f, 170f) * 0.5f * Mathf.Deg2Rad;
        _halfTan = Mathf.Tan(_halfAngleRad);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        for (int i = 0; i < _pulseCount; i++)
        {
            Pulse(i);
            _pulsesDone++;
            if (i < _pulseCount - 1)
                yield return new WaitForSeconds(_pulseInterval);
        }

        // Stay alive until the last visible gust, pushes and slams have finished.
        yield return new WaitForSeconds(_range / Mathf.Max(0.1f, _gustSpeed) + _pushDuration + 0.4f);
        Destroy(gameObject);
    }

    private void Pulse(int index)
    {
        SkillScreenFX.Shake(0.05f + 0.03f * index, 0.2f);
        StartCoroutine(GustVisualRoutine());
        SpawnStreakPuffs();
        ApplyPush();
    }

    // ------------------------------------------------------------------ gameplay

    private void ApplyPush()
    {
        var processed = new HashSet<MonoBehaviour>();
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(_origin + _direction * (_range * 0.5f), _range * 0.5f / Mathf.Cos(_halfAngleRad), _targetLayers))
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
                if (!InsideCone(position))
                    break;

                Vector2 away = (position - _origin).normalized;
                target.TakeDamage(_pulseDamage, Vector2.zero, 0f);

                if (candidate is IPullable pullable)
                {
                    // Pull toward a point far beyond the enemy = a hard shove along `away`.
                    pullable.ApplyPull(position + away * 20f, _pushSpeed, _pushDuration);
                    PredictSlam(candidate, overlap, position, away);
                }

                var flash = candidate.GetComponent<EnemyHitFlash>();
                if (flash == null)
                    flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
                flash.PlayFlash();
                break;
            }
        }
    }

    private bool InsideCone(Vector2 position)
    {
        Vector2 offset = position - _origin;
        float forward = Vector2.Dot(offset, _direction);
        if (forward <= 0f || forward > _range + 0.5f)
            return false;

        float sideways = Mathf.Abs(offset.x * -_direction.y + offset.y * _direction.x);
        return sideways <= forward * _halfTan + 0.4f; // 0.4 = rough enemy half-width so edge hits count
    }

    /// <summary>Casts along the push direction; if a solid non-damageable collider is within reach the slam is
    /// scheduled for the moment the enemy would arrive there.</summary>
    private void PredictSlam(MonoBehaviour candidate, Collider2D ownCollider, Vector2 position, Vector2 away)
    {
        if (_slammed.Contains(candidate))
            return;

        float reach = _pushSpeed * _pushDuration;
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(position, _pushCastRadius, away, reach + 0.3f, _targetLayers))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || collider == ownCollider
                || collider.GetComponentInParent<Player>() != null || HasDamageable(collider))
                continue;

            _slammed.Add(candidate);
            float delay = Mathf.Max(0.02f, hit.distance / Mathf.Max(0.1f, _pushSpeed));
            StartCoroutine(SlamRoutine(candidate, hit.point, delay));
            return; // nearest solid is enough; CircleCastAll order is by distance
        }
    }

    private static bool HasDamageable(Collider2D collider)
    {
        foreach (MonoBehaviour behaviour in collider.GetComponentsInParent<MonoBehaviour>())
        {
            if (behaviour is IDamageable)
                return true;
        }

        return false;
    }

    private IEnumerator SlamRoutine(MonoBehaviour candidate, Vector2 point, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (candidate == null || candidate is not IDamageable target || target.IsDead)
            yield break;

        target.TakeDamage(_slamDamage, Vector2.zero, 0f);

        if (candidate is IStunnable stunnable)
        {
            stunnable.ApplyStun(_slamStun);
            ShowStunIndicator(candidate);
        }

        if (_slamVfxPrefab != null)
            Instantiate(_slamVfxPrefab, point, Quaternion.identity);

        SkillScreenFX.Shake(_slamShake, 0.2f);
    }

    private void ShowStunIndicator(MonoBehaviour target)
    {
        if (_stunIndicatorPrefab == null)
            return;

        if (_stunIndicators.TryGetValue(target, out SkillStunIndicator existing) && existing != null)
        {
            existing.Show(target.transform, _slamStun);
            return;
        }

        SkillStunIndicator indicator = Instantiate(_stunIndicatorPrefab);
        indicator.Show(target.transform, _slamStun);
        _stunIndicators[target] = indicator;
    }

    // ------------------------------------------------------------------ visuals

    /// <summary>One animated gust arc runs out through the cone, widening with the cone and fading near the end.</summary>
    private IEnumerator GustVisualRoutine()
    {
        if (_gustFrames is not { Length: > 0 })
            yield break;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        SpriteRenderer gust = CreateAnimated("Gust", _gustFrames, _origin, 0.3f, 8);
        gust.sortingLayerName = "Player";
        gust.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        float spriteHeight = _gustFrames[0].bounds.size.y;

        float distance = 0.6f;
        while (distance < _range && gust != null)
        {
            distance += _gustSpeed * Time.deltaTime;
            float chord = Mathf.Max(0.9f, 2f * distance * _halfTan + 0.6f);
            float scale = chord / spriteHeight;
            gust.transform.localScale = new Vector3(scale, scale, 1f);
            gust.transform.position = _origin + _direction * distance;
            SetAlpha(gust, Mathf.Clamp01((_range - distance) / 1.5f + 0.15f));
            yield return null;
        }

        if (gust != null)
            Destroy(gust.gameObject);
    }

    private void SpawnStreakPuffs()
    {
        if (_puffFrames is not { Length: > 0 })
            return;

        for (int i = 0; i < 6; i++)
        {
            float distance = Random.Range(0.8f, _range);
            float side = Random.Range(-1f, 1f) * distance * _halfTan;
            Vector2 perpendicular = new Vector2(-_direction.y, _direction.x);
            Vector2 position = _origin + _direction * distance + perpendicular * side;
            StartCoroutine(PuffRoutine(position));
        }
    }

    private IEnumerator PuffRoutine(Vector2 position)
    {
        SpriteRenderer puff = CreateAnimated("GustPuff", _puffFrames, position, 0.22f * Random.Range(0.8f, 1.3f), 7, false);
        puff.sortingLayerName = "Player";
        puff.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        float duration = _puffFrames.Length / _effectFrameRate;
        float t = 0f;
        while (t < duration && puff != null)
        {
            t += Time.deltaTime;
            puff.transform.position += (Vector3)(_direction * (4f * Time.deltaTime)); // drifts downwind
            yield return null;
        }

        if (puff != null)
            Destroy(puff.gameObject);
    }

    private SpriteRenderer CreateAnimated(string name, Sprite[] frames, Vector2 position, float scale, int sortingOrder, bool loop = true)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOrder;
        if (frames.Length > 1)
            go.AddComponent<SkillFrameAnimator>().Play(frames, _effectFrameRate, loop, loop ? Random.value : 0f);
        return sr;
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }
}
