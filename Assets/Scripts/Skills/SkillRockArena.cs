using System.Collections.Generic;
using UnityEngine;

/// <summary>Earth Skill 2 "Rock Arena" (D-076): a ring of rock pillars erupts around the chosen ground
/// circle and stays for `_duration` seconds. Enemies inside are physically trapped by the ring (a
/// loop EdgeCollider2D on its own `SkillWall` layer so beams/projectiles/waves, which don't mask that
/// layer, pass over it and the Player is excluded per-collider), slowed, and given a defense-down
/// debuff (`IVulnerable`) so follow-up skills hit harder. The ring does no damage by itself.
///
/// Pillars are separate sprites (pivot at their base) so Y-sort puts back pillars behind enemies and
/// front pillars in front of them; the floor is a flat decal drawn behind everything on the layer.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillRockArena : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _floorRenderer;
    [SerializeField] private Sprite[] _pillarFrames;
    [SerializeField] private float _duration = 8f;

    [Header("Ring")]
    [Tooltip("Radius of the wall centerline. Aim indicator should show _radius +/- half the wall thickness.")]
    [SerializeField] private float _radius = 1.8f;
    [SerializeField] private int _pillarCount = 16;
    [SerializeField] private float _pillarScale = 0.18f;
    [SerializeField] private float _wallThickness = 0.4f;
    [SerializeField] private float _riseFrameRate = 14f;
    [Tooltip("Delay between neighbouring pillars starting to rise, so the ring erupts around instead of all at once.")]
    [SerializeField] private float _riseStagger = 0.04f;
    [SerializeField] private string _wallLayerName = "SkillWall";
    [SerializeField] private int _pillarSortingOrder = 0;
    [SerializeField] private int _floorSortingOrder = -100;
    [Tooltip("Pixellab appear animation (small patch -> full arena), played once. Empty = scale/twist fallback.")]
    [SerializeField] private Sprite[] _floorAppearFrames;
    [Tooltip("Seconds for the floor to spread out to full size.")]
    [SerializeField] private float _floorAppearTime = 0.6f;
    [SerializeField, Range(0.05f, 1f)] private float _floorStartScale = 1f;
    [Tooltip("Degrees the floor is rotated at the start of its appear animation.")]
    [SerializeField] private float _floorStartTwist = 0f;

    [Header("Debuffs (applied to enemies inside)")]
    [SerializeField, Range(0f, 1f)] private float _slowMultiplier = 0.5f;
    [Tooltip("Damage taken multiplier while debuffed (1.3 = +30%). Combo hook for other skills.")]
    [SerializeField] private float _vulnerableMultiplier = 1.3f;
    [SerializeField] private float _vulnerableLinger = 1f;
    [SerializeField] private float _tickInterval = 0.3f;
    [SerializeField] private LayerMask _targetLayers = ~0;

    [Header("Inner spikes (erupt after the ring is up; stun + small damage)")]
    [SerializeField] private float _spikeStartDelay = 0.7f;
    [SerializeField] private int _spikeWaves = 2;
    [SerializeField] private float _spikeWaveInterval = 2f;
    [SerializeField] private float _spikeScale = 0.2f;
    [Tooltip("Radius around each spike that stuns/damages on eruption.")]
    [SerializeField] private float _spikeHitRadius = 0.5f;
    [Tooltip("Spikes fill the circle out to this fraction of the wall radius so they never overlap the wall.")]
    [SerializeField, Range(0.3f, 0.9f)] private float _spikePlacementRatio = 0.72f;
    [Tooltip("Distance between concentric spike rings, measured from the circle center.")]
    [SerializeField] private float _spikeRingSpacing = 0.45f;
    [Tooltip("Arc distance between neighbouring spikes on the same ring; lower = denser spike field.")]
    [SerializeField] private float _spikeAngularSpacing = 0.5f;
    [Tooltip("How fast the eruption spreads outward from the center (units/second).")]
    [SerializeField] private float _spikeSpreadSpeed = 4f;
    [SerializeField] private float _spikeHoldTime = 1.2f;
    [SerializeField] private float _spikeDamage = 5f;
    [SerializeField] private float _stunDuration = 1.5f;
    [Tooltip("Dizzy-stars icon shown above stunned enemies (SkillStunIndicator).")]
    [SerializeField] private SkillStunIndicator _stunIndicatorPrefab;

    private readonly List<SpriteRenderer> _pillars = new List<SpriteRenderer>();
    private readonly List<float> _pillarDelays = new List<float>();
    private readonly List<Spike> _spikes = new List<Spike>();
    private readonly Dictionary<MonoBehaviour, SkillStunIndicator> _stunIndicators = new Dictionary<MonoBehaviour, SkillStunIndicator>();
    private int _wavesSpawned;

    private class Spike
    {
        public SpriteRenderer Renderer;
        public Vector2 WorldPosition;
        public float StartTime;
        public bool Hit;
    }
    private float _elapsed;
    private float _tickTimer;

    private void Awake()
    {
        if (_floorRenderer == null)
            _floorRenderer = GetComponent<SpriteRenderer>();

        // The floor art lives on a child so its appear animation can scale/rotate freely without
        // distorting the wall collider and other children that depend on this root's scale.
        SpriteRenderer rootRenderer = _floorRenderer;
        var floorGo = new GameObject("Floor");
        floorGo.transform.SetParent(transform, false);
        _floorRenderer = floorGo.AddComponent<SpriteRenderer>();
        _floorRenderer.sprite = rootRenderer.sprite;
        _floorRenderer.sortingLayerID = rootRenderer.sortingLayerID;
        _floorRenderer.sortingOrder = _floorSortingOrder;
        rootRenderer.enabled = false;
        ApplyFloorAppear(0f);
    }

    private SfxLoopHandle _stunLoop;

    private void Start()
    {
        BuildPillars();
        BuildWall();
        SoundFXManager.PlaySfxAt(SfxIds.SkillEarthS2Rise, transform.position);
        _stunLoop = SoundFXManager.StartLoop(SfxIds.SkillEarthS2StunLoop, 0.7f, 1f, 0.4f);
    }

    private void OnDestroy() => _stunLoop?.Stop(0.4f);

    private void BuildPillars()
    {
        if (_pillarFrames is not { Length: > 0 })
            return;

        for (int i = 0; i < _pillarCount; i++)
        {
            float angle = (i + Random.Range(-0.15f, 0.15f)) / _pillarCount * Mathf.PI * 2f;
            var go = new GameObject("Pillar_" + i);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _radius;

            // Counter the parent's scale so pillar size doesn't depend on how big the floor decal is.
            float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
            float scale = _pillarScale * Random.Range(0.9f, 1.1f) / parentScale;
            go.transform.localScale = new Vector3(Random.value < 0.5f ? -scale : scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = _floorRenderer.sortingLayerID;
            sr.sortingOrder = _pillarSortingOrder;
            sr.sprite = null;

            _pillars.Add(sr);
            _pillarDelays.Add(i * _riseStagger);
        }
    }

    private void BuildWall()
    {
        var wall = new GameObject("Wall");
        wall.transform.SetParent(transform, false);

        int layer = LayerMask.NameToLayer(_wallLayerName);
        if (layer >= 0)
            wall.layer = layer;
        else
            Debug.LogWarning($"SkillRockArena: layer '{_wallLayerName}' missing; wall falls back to Default and will block beams/projectiles.");

        // Edge points live in the parent's local space, which is scaled; divide the world radius out.
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
        float localRadius = _radius / parentScale;

        const int segments = 40;
        var points = new Vector2[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * localRadius;
        }

        var edge = wall.AddComponent<EdgeCollider2D>();
        edge.points = points;
        edge.edgeRadius = _wallThickness * 0.5f / parentScale;

        // The caster must be able to walk through their own wall.
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            foreach (Collider2D playerCollider in player.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(edge, playerCollider, true);
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _duration)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePillarFrames();
        UpdateSpikes();
        UpdateFloorAlpha();

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyDebuffs();
        }
    }

    private void UpdatePillarFrames()
    {
        if (_pillarFrames is not { Length: > 0 })
            return;

        float riseDuration = _pillarFrames.Length / Mathf.Max(0.01f, _riseFrameRate);
        float remaining = _duration - _elapsed;

        for (int i = 0; i < _pillars.Count; i++)
        {
            float t = _elapsed - _pillarDelays[i];
            if (t < 0f)
            {
                _pillars[i].sprite = null;
                continue;
            }

            int frame;
            if (remaining < riseDuration)
                frame = Mathf.FloorToInt(remaining * _riseFrameRate); // collapse: play the rise backwards
            else
                frame = Mathf.FloorToInt(t * _riseFrameRate);

            frame = Mathf.Clamp(frame, 0, _pillarFrames.Length - 1);
            _pillars[i].sprite = _pillarFrames[frame];
        }
    }

    /// <summary>Appear animation. Primary path: the Pixellab-generated frames (`_floorAppearFrames`, small
    /// cracked patch growing into the full arena) played once over `_floorAppearTime`. Fallback when no
    /// frames are assigned: the floor sprite spreads out from a small patch with a slight overshoot
    /// (ease-out-back) while untwisting.</summary>
    private void ApplyFloorAppear(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);

        if (_floorAppearFrames is { Length: > 0 })
        {
            int frame = Mathf.Min(Mathf.FloorToInt(t * _floorAppearFrames.Length), _floorAppearFrames.Length - 1);
            _floorRenderer.sprite = _floorAppearFrames[frame];
            return;
        }

        const float overshoot = 1.70158f;
        float eased = 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
        float scale = Mathf.LerpUnclamped(_floorStartScale, 1f, eased);
        _floorRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        _floorRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(_floorStartTwist, 0f, t));
    }

    private void UpdateFloorAlpha()
    {
        ApplyFloorAppear(_elapsed / Mathf.Max(0.01f, _floorAppearTime));

        const float fade = 0.4f;
        // Frame animation already starts from a small patch, so only fade out at the end; the scale
        // fallback also fades in.
        float fadeIn = _floorAppearFrames is { Length: > 0 } ? _duration : _elapsed;
        float alpha = Mathf.Clamp01(Mathf.Min(fadeIn, _duration - _elapsed) / fade);
        Color c = _floorRenderer.color;
        c.a = alpha;
        _floorRenderer.color = c;
    }

    private void ApplyDebuffs()
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, _radius, _targetLayers);
        // Keyed on the IDamageable component: enemies often share a parent, so transform.root would
        // collapse a whole group into one target.
        var processed = new HashSet<MonoBehaviour>();

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!processed.Add(candidate))
                    break;

                if (candidate is ISlowable slowable)
                    slowable.ApplySlow(_slowMultiplier, _tickInterval + 0.15f);

                if (candidate is IVulnerable vulnerable)
                    vulnerable.ApplyVulnerability(_vulnerableMultiplier, _tickInterval + _vulnerableLinger);


                break;
            }
        }
    }

    private void UpdateSpikes()
    {
        if (_pillarFrames is not { Length: > 0 })
            return;

        // Waves are scheduled against arena time so they always come after the ring has risen.
        while (_wavesSpawned < _spikeWaves && _elapsed >= _spikeStartDelay + _wavesSpawned * _spikeWaveInterval)
        {
            SpawnSpikeWave(_wavesSpawned);
            _wavesSpawned++;
        }

        float riseDuration = _pillarFrames.Length / Mathf.Max(0.01f, _riseFrameRate);
        // Stun lands once the spike is mostly out of the ground, not on frame 0.
        float hitTime = riseDuration * 0.5f;
        float totalLife = riseDuration * 2f + _spikeHoldTime;

        for (int i = _spikes.Count - 1; i >= 0; i--)
        {
            Spike spike = _spikes[i];
            float t = _elapsed - spike.StartTime;
            if (t < 0f)
                continue; // scheduled: waiting for the eruption to reach this ring

            if (t >= totalLife || spike.Renderer == null)
            {
                if (spike.Renderer != null)
                    Destroy(spike.Renderer.gameObject);
                _spikes.RemoveAt(i);
                continue;
            }

            int frame;
            if (t < riseDuration)
                frame = Mathf.FloorToInt(t * _riseFrameRate);
            else if (t < riseDuration + _spikeHoldTime)
                frame = _pillarFrames.Length - 1;
            else
                frame = Mathf.FloorToInt((totalLife - t) * _riseFrameRate);

            spike.Renderer.sprite = _pillarFrames[Mathf.Clamp(frame, 0, _pillarFrames.Length - 1)];

            if (!spike.Hit && t >= hitTime)
            {
                spike.Hit = true;
                ApplySpikeHit(spike.WorldPosition);
            }
        }
    }

    /// <summary>The circle's center is the summoning point: spikes are laid on concentric rings, evenly
    /// spaced through the full 360 degrees of each ring (ring circumference / angular spacing spikes per
    /// ring), and erupt outward from the center like a shockwave (delay = distance / spread speed).
    /// Each later wave is rotated by half a step so it fills the gaps of the previous one.</summary>
    private void SpawnSpikeWave(int waveIndex)
    {
        Vector2 center = transform.position;
        SoundFXManager.PlaySfxAt(SfxIds.SkillEarthS2Spike, center);
        float maxRing = _radius * _spikePlacementRatio;
        // Round the ring count up and re-divide so the outermost ring lands exactly on maxRing
        // (no empty band between the last ring and the wall).
        int ringCount = Mathf.Max(1, Mathf.CeilToInt(maxRing / Mathf.Max(0.05f, _spikeRingSpacing)));
        float ringStep = maxRing / ringCount;

        for (int ring = 0; ring <= ringCount; ring++)
        {
            float ringRadius = ring * ringStep;
            int count = ring == 0
                ? 1
                : Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * ringRadius / Mathf.Max(0.05f, _spikeAngularSpacing)));
            float step = Mathf.PI * 2f / count;
            // Stagger neighbouring rings and waves so spikes interleave instead of lining up in spokes.
            float offset = (ring % 2) * step * 0.5f + (waveIndex % 2) * step * 0.25f;

            for (int i = 0; i < count; i++)
            {
                float angle = offset + i * step;
                Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
                AddSpike(position, _elapsed + ringRadius / Mathf.Max(0.1f, _spikeSpreadSpeed));
            }
        }
    }

    private void AddSpike(Vector2 position, float startTime)
    {
        var go = new GameObject("Spike");
        go.transform.position = position;
        float scale = _spikeScale * Random.Range(0.9f, 1.15f);
        go.transform.localScale = new Vector3(Random.value < 0.5f ? -scale : scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerID = _floorRenderer.sortingLayerID;
        sr.sortingOrder = _pillarSortingOrder;

        _spikes.Add(new Spike { Renderer = sr, WorldPosition = position, StartTime = startTime });
    }

    private void ApplySpikeHit(Vector2 position)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, _spikeHitRadius, _targetLayers);
        var processed = new HashSet<MonoBehaviour>();

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!processed.Add(candidate))
                    break;

                target.TakeDamage(_spikeDamage, Vector2.zero, 0f);

                if (candidate is IStunnable stunnable)
                {
                    stunnable.ApplyStun(_stunDuration);
                    ShowStunIndicator(candidate);
                }

                var flash = candidate.GetComponent<EnemyHitFlash>();
                if (flash == null)
                    flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
                flash.PlayFlash();
                break;
            }
        }
    }

    private void ShowStunIndicator(MonoBehaviour target)
    {
        if (_stunIndicatorPrefab == null)
            return;

        // One indicator per enemy: a second spike extends the existing one instead of stacking stars.
        if (_stunIndicators.TryGetValue(target, out SkillStunIndicator existing) && existing != null)
        {
            existing.Show(target.transform, _stunDuration);
            return;
        }

        SkillStunIndicator indicator = Instantiate(_stunIndicatorPrefab);
        indicator.Show(target.transform, _stunDuration);
        _stunIndicators[target] = indicator;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
