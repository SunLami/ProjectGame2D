using System.Collections.Generic;
using UnityEngine;

/// <summary>Earth Skill 3 "Rock Spike Field" (D-077): a field of rock spikes erupts and rushes
/// forward from the caster, filling the whole aimed cone (fan). Spikes are laid on arcs at growing
/// distance from the origin, evenly spaced across the cone angle, and each arc erupts a moment after
/// the one before it (delay = distance / spread speed) so the field visibly surges outward.
/// Every enemy a spike touches takes damage once per cast (AoE: dedupe is keyed on the IDamageable
/// component, not transform.root) and is slowed. Spikes whose straight line from the caster is
/// blocked by a solid non-damageable collider (walls/trees) are not spawned, so the field does not
/// grow through obstacles -- same blocking rule as SkillBeam / SkillTsunamiWave.</summary>
public class SkillSpikeCone : MonoBehaviour
{
    [SerializeField] private Sprite[] _pillarFrames;
    [SerializeField] private GameObject _impactVfxPrefab;
    [SerializeField] private float _riseFrameRate = 14f;
    [SerializeField] private float _holdTime = 0.6f;
    [SerializeField] private float _spikeScale = 0.25f;

    [Header("Layout")]
    [Tooltip("Distance from the caster to the first arc of spikes.")]
    [SerializeField] private float _startDistance = 0.8f;
    [SerializeField] private float _arcSpacing = 0.55f;
    [Tooltip("Spacing between neighbouring spikes along an arc; lower = denser field.")]
    [SerializeField] private float _spikeSpacing = 0.5f;
    [Tooltip("How fast the eruption rushes outward (units/second).")]
    [SerializeField] private float _spreadSpeed = 9f;

    [Header("Damage")]
    [SerializeField] private float _hitRadius = 0.45f;
    [SerializeField] private float _damage = 12f;
    [SerializeField] private float _knockbackForce = 0f;
    [Header("Control (damage + stun + root, D-077)")]
    [Tooltip("Stun length. Shorter than the root, so the enemy is first stunned, then still held in the cage.")]
    [SerializeField] private float _stunDuration = 1.2f;
    [Tooltip("How long the cage holds the enemy in place (movement multiplier 0), then they can move again.")]
    [SerializeField] private float _rootDuration = 2f;
    [SerializeField] private SkillSpikeCage _cagePrefab;
    [SerializeField] private SkillStunIndicator _stunIndicatorPrefab;
    [SerializeField] private LayerMask _targetLayers = ~0;
    [SerializeField] private string _ignoredWallLayerName = "SkillWall";

    private readonly List<Spike> _spikes = new List<Spike>();
    private readonly HashSet<MonoBehaviour> _alreadyHit = new HashSet<MonoBehaviour>();
    private readonly Dictionary<MonoBehaviour, SkillStunIndicator> _stunIndicators = new Dictionary<MonoBehaviour, SkillStunIndicator>();
    private Vector2 _origin;
    private float _damageMultiplier = 1f;
    private float _elapsed;

    private class Spike
    {
        public SpriteRenderer Renderer;
        public Vector2 Position;
        public float StartTime;
        public bool Hit;
    }

    /// <summary>`direction` is the aim direction, `fanAngleDegrees` the full cone angle and `range` how far
    /// the field reaches. Call once, right after instantiating.</summary>
    public void Launch(Vector2 origin, Vector2 direction, float range, float fanAngleDegrees, float damageMultiplier = 1f)
    {
        _origin = origin;
        _damageMultiplier = damageMultiplier;
        transform.position = origin;

        // The arena wall must not stop this skill: strip its layer from the blocking mask.
        int wallLayer = LayerMask.NameToLayer(_ignoredWallLayerName);
        int blockMask = wallLayer >= 0 ? (int)_targetLayers & ~(1 << wallLayer) : (int)_targetLayers;

        float centerAngle = Mathf.Atan2(direction.y, direction.x);
        float halfAngle = Mathf.Max(1f, fanAngleDegrees) * 0.5f * Mathf.Deg2Rad;

        int arcIndex = 0;
        for (float distance = _startDistance; distance <= range + 0.001f; distance += _arcSpacing, arcIndex++)
        {
            float arcLength = distance * halfAngle * 2f;
            int count = Mathf.Max(2, Mathf.RoundToInt(arcLength / Mathf.Max(0.05f, _spikeSpacing)) + 1);
            // Alternate arcs by half a step so spikes interleave rather than line up in spokes.
            float step = halfAngle * 2f / (count - 1);
            float offset = (arcIndex % 2 == 0) ? 0f : step * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float angle = centerAngle - halfAngle + offset + i * step;
                if (angle > centerAngle + halfAngle + 0.0001f)
                    continue; // the half-step shift pushed this one past the cone edge

                Vector2 position = _origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if (IsBlocked(position, blockMask))
                    continue;

                AddSpike(position, distance / Mathf.Max(0.1f, _spreadSpeed));
            }
        }
    }

    private bool IsBlocked(Vector2 position, int blockMask)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(_origin, position, blockMask))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || collider.GetComponentInParent<Player>() != null)
                continue;

            if (HasDamageable(collider))
                continue;

            return true;
        }

        return false;
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

    private void AddSpike(Vector2 position, float delay)
    {
        var go = new GameObject("Spike");
        go.transform.SetParent(transform, true);
        go.transform.position = position;
        float scale = _spikeScale * Random.Range(0.9f, 1.15f);
        go.transform.localScale = new Vector3(Random.value < 0.5f ? -scale : scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 0;

        _spikes.Add(new Spike { Renderer = sr, Position = position, StartTime = delay });
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_pillarFrames is not { Length: > 0 })
        {
            Destroy(gameObject);
            return;
        }

        float riseDuration = _pillarFrames.Length / Mathf.Max(0.01f, _riseFrameRate);
        float hitTime = riseDuration * 0.5f;
        float totalLife = riseDuration * 2f + _holdTime;
        bool anyAlive = false;

        foreach (Spike spike in _spikes)
        {
            float t = _elapsed - spike.StartTime;
            if (t >= totalLife)
            {
                spike.Renderer.sprite = null;
                continue;
            }

            anyAlive = true;
            if (t < 0f)
                continue; // waiting for the eruption to reach this arc

            int frame;
            if (t < riseDuration)
                frame = Mathf.FloorToInt(t * _riseFrameRate);
            else if (t < riseDuration + _holdTime)
                frame = _pillarFrames.Length - 1;
            else
                frame = Mathf.FloorToInt((totalLife - t) * _riseFrameRate);

            spike.Renderer.sprite = _pillarFrames[Mathf.Clamp(frame, 0, _pillarFrames.Length - 1)];

            if (!spike.Hit && t >= hitTime)
            {
                spike.Hit = true;
                ApplySpikeHit(spike.Position);
            }
        }

        if (!anyAlive)
            Destroy(gameObject);
    }

    private void ShowStunIndicator(MonoBehaviour target)
    {
        if (_stunIndicatorPrefab == null)
            return;

        SkillStunIndicator indicator = Instantiate(_stunIndicatorPrefab);
        indicator.Show(target.transform, _stunDuration);
        _stunIndicators[target] = indicator;
    }

    private void ApplySpikeHit(Vector2 position)
    {
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(position, _hitRadius, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!_alreadyHit.Add(candidate))
                    break;

                Vector2 away = ((Vector2)candidate.transform.position - _origin).normalized;
                target.TakeDamage(_damage * _damageMultiplier, away, _knockbackForce);

                // Root: movement multiplier 0 for the cage duration; stun (no AI/attacks) only for the first part.
                if (candidate is ISlowable slowable)
                    slowable.ApplySlow(0f, _rootDuration);

                if (candidate is IStunnable stunnable)
                {
                    stunnable.ApplyStun(_stunDuration);
                    ShowStunIndicator(candidate);
                }

                if (_cagePrefab != null)
                    Instantiate(_cagePrefab).Show(candidate.transform, _rootDuration);

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
}
