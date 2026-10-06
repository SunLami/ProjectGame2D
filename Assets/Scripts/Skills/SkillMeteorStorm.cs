using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Earth Skill 4 "Meteor Judgement" ultimate (D-078), aimed with the ground-target circle.
/// Timeline (all phases driven from here):
///   A. Gather   - rune circle under the caster, rocks orbit the caster, screen dims, camera trembles.
///   B. Warning  - a glowing rune ring marks the target circle; every meteor shows a growing ground shadow
///                 before it lands so the danger zone is readable.
///   C. Rain     - ~20 meteors fall into the circle. Normal ones deal AoE damage + knockback and leave a
///                 crater, a ring of small spikes and rolling rock shards. Some are crystal meteors (stun
///                 instead of heavy damage). Two impacts close together in time and space chain into a bigger
///                 explosion.
///   D. Finale   - a giant shadow darkens the whole circle, then one huge meteor lands in the centre:
///                 hit-stop + slow motion, flash, big shake, large AoE damage/knockback/stun.
///   E. Aftermath- craters smoulder (embers) and the cracked ground slows and weakens (IVulnerable) enemies
///                 for a few seconds, which also feeds combos with Earth Skill 2.
/// Enemy dedupe in every blast is keyed on the IDamageable component, not transform.root.</summary>
public class SkillMeteorStorm : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Sprite[] _meteorFrames;
    [Tooltip("Smouldering crater loop (ember cracks pulsing, smoke wisps).")]
    [SerializeField] private Sprite[] _craterFrames;
    [Tooltip("Glowing rune circle loop, used under the caster and as the target ring.")]
    [SerializeField] private Sprite[] _runeFrames;
    [Tooltip("Hovering rock loop, used for the orbiting rocks and the rolling shards.")]
    [SerializeField] private Sprite[] _rockFrames;
    [Tooltip("Expanding dust/debris ring played once for the finale shockwave.")]
    [SerializeField] private Sprite[] _shockwaveFrames;
    [SerializeField] private float _effectFrameRate = 10f;
    [SerializeField] private Sprite[] _spikeFrames;
    [SerializeField] private GameObject _impactVfxPrefab;
    [SerializeField] private SkillStunIndicator _stunIndicatorPrefab;

    [Header("Circle")]
    [SerializeField] private float _radius = 3.5f;
    [Tooltip("Radius of the finale blast (matches the inner indicator circle).")]
    [SerializeField] private float _finaleRadius = 2.2f;

    [Header("Timeline (seconds)")]
    [SerializeField] private float _gatherDuration = 1f;
    [SerializeField] private float _rainDuration = 4f;
    [SerializeField] private int _meteorCount = 20;
    [Tooltip("Ground shadow is shown this long before a normal meteor lands.")]
    [SerializeField] private float _warningLead = 0.9f;
    [SerializeField] private float _fallTime = 0.55f;
    [SerializeField] private float _finaleDelay = 1.2f;
    [SerializeField] private float _finaleWarningLead = 1.8f;
    [SerializeField] private float _finaleFallTime = 0.9f;
    [SerializeField] private float _aftermathDuration = 5f;
    [SerializeField] private float _fallHeight = 7f;

    [Header("Normal meteors")]
    [SerializeField] private float _meteorScale = 0.55f;
    [SerializeField] private float _blastRadius = 1f;
    [SerializeField] private float _damage = 18f;
    [SerializeField] private float _knockback = 5f;
    [SerializeField, Range(0f, 1f)] private float _crystalChance = 0.2f;
    [SerializeField] private float _crystalDamage = 8f;
    [SerializeField] private float _crystalStun = 1.2f;
    [SerializeField] private Color _crystalTint = new Color(1f, 0.92f, 0.45f, 1f);
    [SerializeField] private float _minImpactSpacing = 0.9f;

    [Header("Chain explosion")]
    [SerializeField] private float _chainRadius = 1.4f;
    [SerializeField] private float _chainWindow = 0.8f;
    [SerializeField] private float _chainBlastRadius = 1.7f;
    [SerializeField] private float _chainDamage = 12f;

    [Header("Finale")]
    [SerializeField] private float _finaleScale = 1.7f;
    [SerializeField] private float _finaleDamage = 70f;
    [SerializeField] private float _finaleKnockback = 9f;
    [SerializeField] private float _finaleStun = 1.5f;

    [Header("Craters / spikes / shards")]
    [SerializeField] private float _craterScale = 0.4f;
    [SerializeField] private int _spikesPerCrater = 6;
    [SerializeField] private float _spikeScale = 0.12f;
    [SerializeField] private float _spikeHold = 1.2f;
    [SerializeField] private float _spikeSlow = 0.5f;
    [SerializeField] private int _shardsPerImpact = 3;

    [Header("Aftermath zone")]
    [SerializeField, Range(0f, 1f)] private float _aftermathSlow = 0.6f;
    [SerializeField] private float _aftermathVulnerable = 1.3f;

    [SerializeField] private LayerMask _targetLayers = ~0;

    private Player _caster;
    private Vector2 _center;
    private SpriteRenderer _targetRing;
    private readonly List<GameObject> _persistent = new List<GameObject>();
    private readonly List<SpriteRenderer> _craters = new List<SpriteRenderer>();
    private readonly List<ImpactRecord> _recentImpacts = new List<ImpactRecord>();
    private readonly List<Vector2> _plannedImpacts = new List<Vector2>();
    private readonly Dictionary<MonoBehaviour, SkillStunIndicator> _stunIndicators = new Dictionary<MonoBehaviour, SkillStunIndicator>();
    private static Sprite _whiteSprite;
    private static Sprite _shadowSprite;

    private struct ImpactRecord
    {
        public Vector2 Position;
        public float Time;
    }

    /// <summary>Starts the whole sequence centred on `center`, with `caster` as the gathering point.</summary>
    public void Launch(Vector2 center, Player caster)
    {
        _center = center;
        _caster = caster;
        transform.position = center;
        StartCoroutine(Run());
    }

    // ------------------------------------------------------------------ timeline

    private IEnumerator Run()
    {
        // A. Gather + B. warning ring
        SoundFXManager.PlaySfxAt(SfxIds.SkillEarthS4Charge, _center);
        StartCoroutine(GatherRoutine());
        SkillScreenFX.Dim(0.35f, _gatherDuration);
        SkillScreenFX.Shake(0.04f, _gatherDuration);

        yield return new WaitForSeconds(_gatherDuration * 0.3f);
        CreateTargetRing();

        yield return new WaitForSeconds(_gatherDuration * 0.7f);

        // C. rain: schedule every meteor against the rain window.
        float rainEndImpact = _warningLead + _rainDuration;
        for (int i = 0; i < _meteorCount; i++)
        {
            float impactDelay = _warningLead + _rainDuration * (i + Random.value * 0.8f) / _meteorCount;
            Vector2 position = PickImpactPosition();
            bool crystal = Random.value < _crystalChance;
            StartCoroutine(MeteorRoutine(position, impactDelay, _warningLead, _fallTime, _meteorScale, crystal, false));
        }

        // D. finale lands after the rain's last meteor.
        float finaleImpactDelay = rainEndImpact + _finaleDelay;
        StartCoroutine(MeteorRoutine(_center, finaleImpactDelay, _finaleWarningLead, _finaleFallTime, _finaleScale, false, true));

        yield return new WaitForSeconds(finaleImpactDelay);

        // E. aftermath
        SoundFXManager.PlaySfxAt(SfxIds.SkillEarthS4Aftermath, _center);
        float elapsed = 0f;
        float tick = 0f;
        SkillScreenFX.Dim(0f, 1.5f);
        while (elapsed < _aftermathDuration)
        {
            elapsed += Time.deltaTime;
            tick += Time.deltaTime;
            if (tick >= 0.4f)
            {
                tick -= 0.4f;
                ApplyAftermathZone();
            }

            SpawnEmber();
            yield return null;
        }

        yield return FadeOutAndCleanup();
    }

    private IEnumerator FadeOutAndCleanup()
    {
        const float fade = 0.8f;
        float t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            float alpha = 1f - t / fade;
            foreach (SpriteRenderer crater in _craters)
            {
                if (crater != null)
                    SetAlpha(crater, alpha);
            }

            if (_targetRing != null)
                SetAlpha(_targetRing, alpha * 0.8f);
            yield return null;
        }

        foreach (GameObject go in _persistent)
        {
            if (go != null)
                Destroy(go);
        }

        Destroy(gameObject);
    }

    private Vector2 PickImpactPosition()
    {
        // Random point in the circle, kept apart from earlier meteors so impacts spread over the whole area.
        Vector2 best = _center;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector2 candidate = _center + Random.insideUnitCircle * (_radius * 0.92f);
            bool farEnough = true;
            foreach (Vector2 other in _plannedImpacts)
            {
                if ((other - candidate).sqrMagnitude < _minImpactSpacing * _minImpactSpacing)
                {
                    farEnough = false;
                    break;
                }
            }

            best = candidate;
            if (farEnough)
                break;
        }

        _plannedImpacts.Add(best);
        return best;
    }

    // ------------------------------------------------------------------ A. gather

    private IEnumerator GatherRoutine()
    {
        if (_caster == null)
            yield break;

        // Rune circle on the ground under the caster.
        SpriteRenderer rune = CreateAnimatedSprite("CastRune", _runeFrames, _caster.transform.position, 0.65f, -95);
        _persistent.Add(rune.gameObject);

        // Rocks orbiting the caster's body centre.
        const int rockCount = 6;
        var rocks = new SpriteRenderer[rockCount];
        SpriteRenderer casterRenderer = _caster.GetComponentInChildren<SpriteRenderer>();
        for (int i = 0; i < rockCount; i++)
        {
            rocks[i] = CreateAnimatedSprite("OrbitRock_" + i, _rockFrames, _caster.SkillOrigin, 0.11f, 0);
            if (casterRenderer != null)
                rocks[i].sortingLayerID = casterRenderer.sortingLayerID;
            _persistent.Add(rocks[i].gameObject);
        }

        float t = 0f;
        while (t < _gatherDuration && _caster != null)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / _gatherDuration);
            SetAlpha(rune, Mathf.Clamp01(progress * 3f));
            rune.transform.position = _caster.transform.position;
            rune.transform.Rotate(0f, 0f, -70f * Time.deltaTime);

            // Ring centred on the hips so the back half stays waist-high behind the body, not at face level.
            Vector2 origin = (Vector2)_caster.transform.position + new Vector2(0f, 0.25f);
            float rise = Mathf.SmoothStep(0f, 0.3f, progress);
            int order = casterRenderer != null ? casterRenderer.sortingOrder : 0;
            for (int i = 0; i < rockCount; i++)
            {
                float angle = (i / (float)rockCount) * Mathf.PI * 2f + t * (2f + progress * 4f);
                float sin = Mathf.Sin(angle);
                rocks[i].transform.position = new Vector3(
                    origin.x + Mathf.Cos(angle) * 0.9f,
                    origin.y + sin * 0.22f + rise + Mathf.Sin(t * 5f + i) * 0.03f,
                    0f);
                rocks[i].transform.Rotate(0f, 0f, 120f * Time.deltaTime);
                rocks[i].sortingOrder = order + (sin > 0f ? -1 : 1);
                SetAlpha(rocks[i], Mathf.Clamp01(progress * 4f));
            }

            yield return null;
        }

        // Rocks shoot up into the sky (they "become" the meteors), the cast rune fades.
        float launch = 0f;
        while (launch < 0.35f)
        {
            launch += Time.deltaTime;
            float k = launch / 0.35f;
            for (int i = 0; i < rockCount; i++)
            {
                if (rocks[i] == null)
                    continue;
                rocks[i].transform.position += Vector3.up * (12f * Time.deltaTime);
                SetAlpha(rocks[i], 1f - k);
            }

            if (rune != null)
                SetAlpha(rune, 1f - k);
            yield return null;
        }
    }

    private void CreateTargetRing()
    {
        // Rune circle sized to the target circle; slow pulse and spin, tinted hot orange.
        float runeWidth = _runeFrames is { Length: > 0 } ? _runeFrames[0].bounds.size.x : 3.67f;
        float scale = _radius * 2f / runeWidth;
        _targetRing = CreateAnimatedSprite("TargetRing", _runeFrames, _center, scale, -97);
        _targetRing.color = new Color(1f, 0.6f, 0.25f, 0f);
        _persistent.Add(_targetRing.gameObject);
        StartCoroutine(TargetRingRoutine());
    }

    private IEnumerator TargetRingRoutine()
    {
        float t = 0f;
        while (_targetRing != null)
        {
            t += Time.deltaTime;
            _targetRing.transform.Rotate(0f, 0f, 25f * Time.deltaTime);
            float pulse = 0.6f + 0.2f * Mathf.Sin(t * 6f);
            SetAlpha(_targetRing, Mathf.Min(t / 0.5f, 1f) * pulse);
            yield return null;
        }
    }

    // ------------------------------------------------------------------ C./D. meteors

    private IEnumerator MeteorRoutine(Vector2 position, float impactDelay, float lead, float fall, float scale, bool crystal, bool giant)
    {
        float startDelay = Mathf.Max(0f, impactDelay - lead);
        yield return new WaitForSeconds(startDelay);

        // Growing ground shadow (giant one covers the whole circle).
        float shadowSize = giant ? _radius * 2.1f : _blastRadius * 1.6f;
        SpriteRenderer shadow = CreateSprite(giant ? "GiantShadow" : "MeteorShadow", GetShadowSprite(), position, shadowSize, -80);
        shadow.color = new Color(0f, 0f, 0f, 0f);
        shadow.transform.localScale = Vector3.one * shadowSize * 0.3f;

        float fallStart = lead - fall;
        SpriteRenderer meteor = null;
        float t = 0f;
        int frame = 0;
        float frameTimer = 0f;

        while (t < lead)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / lead);
            float shadowAlpha = giant ? Mathf.Lerp(0f, 0.6f, k) : Mathf.Lerp(0.1f, 0.45f, k);
            shadow.color = new Color(0f, 0f, 0f, shadowAlpha);
            shadow.transform.localScale = Vector3.one * Mathf.Lerp(shadowSize * 0.3f, shadowSize, k);

            if (t >= fallStart)
            {
                if (meteor == null)
                {
                    Sprite first = _meteorFrames is { Length: > 0 } ? _meteorFrames[0] : null;
                    meteor = CreateSprite("Meteor", first, position + Vector2.up * _fallHeight, scale, 0);
                    SoundFXManager.PlaySfxAt(SfxIds.SkillEarthS4MeteorFall, position);
                    meteor.sortingLayerName = "Player";
                    meteor.sortingOrder = 100;
                    if (crystal)
                        meteor.color = _crystalTint;
                }

                float fallProgress = Mathf.Clamp01((t - fallStart) / fall);
                float eased = fallProgress * fallProgress; // accelerates like a real fall
                meteor.transform.position = (Vector3)(position + Vector2.up * (_fallHeight * (1f - eased)));

                if (_meteorFrames is { Length: > 1 })
                {
                    frameTimer += Time.deltaTime;
                    if (frameTimer >= 0.06f)
                    {
                        frameTimer = 0f;
                        frame = (frame + 1) % _meteorFrames.Length;
                        meteor.sprite = _meteorFrames[frame];
                    }
                }
            }

            yield return null;
        }

        if (meteor != null)
            Destroy(meteor.gameObject);
        Destroy(shadow.gameObject);

        if (giant)
            ResolveFinale(position);
        else
            ResolveImpact(position, crystal);
    }

    private void ResolveImpact(Vector2 position, bool crystal)
    {
        var hit = new HashSet<MonoBehaviour>();
        float damage = crystal ? _crystalDamage : _damage;
        ApplyBlast(position, _blastRadius, damage, _knockback, crystal ? _crystalStun : 0f, hit);

        SoundFXManager.PlaySfxAt(SfxIds.SkillEarthS4MeteorImpact, position);
        SpawnImpactVfx(position, crystal ? 0.9f : 1.2f);
        AddCrater(position, _craterScale * (crystal ? 0.85f : 1f));
        SpawnShards(position);
        StartCoroutine(SpikeRingRoutine(position));
        SkillScreenFX.Shake(0.08f, 0.2f);

        // Chain explosion: another impact very close in space and time sets off a bigger blast between them.
        float now = Time.time;
        for (int i = _recentImpacts.Count - 1; i >= 0; i--)
        {
            if (now - _recentImpacts[i].Time > _chainWindow)
            {
                _recentImpacts.RemoveAt(i);
                continue;
            }

            if ((_recentImpacts[i].Position - position).sqrMagnitude <= _chainRadius * _chainRadius)
            {
                Vector2 mid = (_recentImpacts[i].Position + position) * 0.5f;
                ApplyBlast(mid, _chainBlastRadius, _chainDamage, _knockback * 0.8f, 0f, hit);
                SpawnImpactVfx(mid, 2f);
                SkillScreenFX.Shake(0.18f, 0.3f);
                _recentImpacts.RemoveAt(i);
                _recentImpacts.Add(new ImpactRecord { Position = position, Time = now - _chainWindow }); // no re-chain
                return;
            }
        }

        _recentImpacts.Add(new ImpactRecord { Position = position, Time = now });
    }

    private void ResolveFinale(Vector2 position)
    {
        var hit = new HashSet<MonoBehaviour>();
        ApplyBlast(position, _finaleRadius, _finaleDamage, _finaleKnockback, _finaleStun, hit);

        SoundFXManager.PlaySfxAt(SfxIds.SkillEarthS4Finale, position);
        SoundFXManager.PlaySfx(SfxIds.SkillScreenSlam);
        SpawnImpactVfx(position, 3f);
        AddCrater(position, _craterScale * 2.4f);
        StartCoroutine(ShockwaveRoutine(position));
        StartCoroutine(SpikeRingRoutine(position, 1.6f, 12));
        for (int i = 0; i < 3; i++)
            SpawnShards(position);

        SkillScreenFX.Flash(new Color(1f, 0.85f, 0.55f), 0.85f, 0.6f);
        SkillScreenFX.Shake(0.5f, 0.7f);
        SkillScreenFX.HitStopAndSlowMo(0.06f, 0.3f, 0.35f);
    }

    // ------------------------------------------------------------------ damage

    private void ApplyBlast(Vector2 position, float radius, float damage, float knockback, float stunSeconds, HashSet<MonoBehaviour> dedupe)
    {
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(position, radius, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!dedupe.Add(candidate))
                    break;

                Vector2 away = ((Vector2)candidate.transform.position - position).normalized;
                target.TakeDamage(damage, away, knockback);

                if (stunSeconds > 0f && candidate is IStunnable stunnable)
                {
                    stunnable.ApplyStun(stunSeconds);
                    ShowStunIndicator(candidate, stunSeconds);
                }

                var flash = candidate.GetComponent<EnemyHitFlash>();
                if (flash == null)
                    flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
                flash.PlayFlash();
                break;
            }
        }
    }

    private void ShowStunIndicator(MonoBehaviour target, float seconds)
    {
        if (_stunIndicatorPrefab == null)
            return;

        if (_stunIndicators.TryGetValue(target, out SkillStunIndicator existing) && existing != null)
        {
            existing.Show(target.transform, seconds);
            return;
        }

        SkillStunIndicator indicator = Instantiate(_stunIndicatorPrefab);
        indicator.Show(target.transform, seconds);
        _stunIndicators[target] = indicator;
    }

    private void ApplyAftermathZone()
    {
        var processed = new HashSet<MonoBehaviour>();
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(_center, _radius, _targetLayers))
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
                    slowable.ApplySlow(_aftermathSlow, 0.55f);
                if (candidate is IVulnerable vulnerable)
                    vulnerable.ApplyVulnerability(_aftermathVulnerable, 0.85f);
                break;
            }
        }
    }

    // ------------------------------------------------------------------ visuals

    private void SpawnImpactVfx(Vector2 position, float scaleMultiplier)
    {
        if (_impactVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(_impactVfxPrefab, position, Quaternion.identity);
        vfx.transform.localScale *= scaleMultiplier;
    }

    private void AddCrater(Vector2 position, float scale)
    {
        SpriteRenderer crater = CreateAnimatedSprite("Crater", _craterFrames, position, scale, -90 + _craters.Count % 8, false, 10f);
        crater.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        _craters.Add(crater);
        _persistent.Add(crater.gameObject);
    }

    private void SpawnShards(Vector2 position)
    {
        for (int i = 0; i < _shardsPerImpact; i++)
            StartCoroutine(ShardRoutine(position));
    }

    private IEnumerator ShardRoutine(Vector2 origin)
    {
        SpriteRenderer shard = CreateAnimatedSprite("Shard", _rockFrames, origin, 0.07f, -70);
        Vector2 direction = Random.insideUnitCircle.normalized;
        float distance = Random.Range(0.8f, 1.6f);
        float duration = Random.Range(0.5f, 0.8f);
        float spin = Random.Range(-540f, 540f);

        float t = 0f;
        while (t < duration && shard != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            // Rolls outward, decelerating, with a small hop at the start.
            float outward = 1f - (1f - k) * (1f - k);
            float hop = Mathf.Sin(k * Mathf.PI) * 0.25f;
            shard.transform.position = (Vector3)(origin + direction * (distance * outward)) + Vector3.up * hop;
            shard.transform.Rotate(0f, 0f, spin * Time.deltaTime);
            SetAlpha(shard, k < 0.7f ? 1f : (1f - k) / 0.3f);
            yield return null;
        }

        if (shard != null)
            Destroy(shard.gameObject);
    }

    private IEnumerator SpikeRingRoutine(Vector2 center, float radiusScale = 1f, int countOverride = 0)
    {
        if (_spikeFrames is not { Length: > 0 })
            yield break;

        int count = countOverride > 0 ? countOverride : _spikesPerCrater;
        float ringRadius = _blastRadius * 0.85f * radiusScale;
        var spikes = new SpriteRenderer[count];
        float[] delays = new float[count];

        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
            Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
            float scale = _spikeScale * Random.Range(0.9f, 1.15f) * (radiusScale > 1f ? 1.6f : 1f);
            SpriteRenderer sr = CreateSprite("CraterSpike", null, position, scale, 0);
            sr.transform.localScale = new Vector3(Random.value < 0.5f ? -scale : scale, scale, 1f);
            sr.sortingLayerName = "Default";
            spikes[i] = sr;
            delays[i] = i * 0.03f;
        }

        // The spikes also snare enemies caught beside the crater, once, as they erupt.
        ApplyRingSnare(center, ringRadius + 0.5f);

        float rise = _spikeFrames.Length / 14f;
        float total = rise * 2f + _spikeHold;
        float t = 0f;
        while (t < total)
        {
            t += Time.deltaTime;
            for (int i = 0; i < count; i++)
            {
                if (spikes[i] == null)
                    continue;

                float local = t - delays[i];
                if (local < 0f)
                    continue;

                int frame = local < rise
                    ? Mathf.FloorToInt(local * 14f)
                    : local < rise + _spikeHold ? _spikeFrames.Length - 1 : Mathf.FloorToInt((total - local) * 14f);
                spikes[i].sprite = _spikeFrames[Mathf.Clamp(frame, 0, _spikeFrames.Length - 1)];
            }

            yield return null;
        }

        foreach (SpriteRenderer spike in spikes)
        {
            if (spike != null)
                Destroy(spike.gameObject);
        }
    }

    private void ApplyRingSnare(Vector2 center, float radius)
    {
        var processed = new HashSet<MonoBehaviour>();
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(center, radius, _targetLayers))
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
                    slowable.ApplySlow(_spikeSlow, 1.5f);
                break;
            }
        }
    }

    private IEnumerator ShockwaveRoutine(Vector2 center)
    {
        // Expanding ring made from a scaled, hollow banded circle: grows to the finale radius while fading.
        bool animated = _shockwaveFrames is { Length: > 1 };
        float duration = animated ? _shockwaveFrames.Length / _effectFrameRate : 0.6f;
        // Animated frames carry the ring growth themselves; the scale only has to reach the blast radius.
        float frameWidth = animated ? _shockwaveFrames[0].bounds.size.x : 1f;
        float finalScale = _finaleRadius * 2.6f / frameWidth;
        SpriteRenderer ring = animated
            ? CreateAnimatedSprite("Shockwave", _shockwaveFrames, center, finalScale, -60, false)
            : CreateSprite("Shockwave", GetRingSprite(), center, 0.5f, -60);
        float t = 0f;
        while (t < duration && ring != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            if (!animated)
            {
                float eased = 1f - (1f - k) * (1f - k);
                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, _finaleRadius * 2.6f, eased);
                ring.color = new Color(1f, 0.85f, 0.55f, 1f - k);
            }
            else if (k > 0.75f)
            {
                SetAlpha(ring, (1f - k) / 0.25f);
            }

            yield return null;
        }

        if (ring != null)
            Destroy(ring.gameObject);
    }

    private void SpawnEmber()
    {
        if (Random.value > 0.35f || _craters.Count == 0)
            return;

        SpriteRenderer crater = _craters[Random.Range(0, _craters.Count)];
        if (crater == null)
            return;

        Vector2 start = (Vector2)crater.transform.position + Random.insideUnitCircle * 0.4f;
        StartCoroutine(EmberRoutine(start));
    }

    private IEnumerator EmberRoutine(Vector2 start)
    {
        SpriteRenderer ember = CreateSprite("Ember", GetWhiteSprite(), start, 0.07f, 60);
        ember.sortingLayerName = "Player";
        ember.color = new Color(1f, Random.Range(0.45f, 0.75f), 0.2f, 1f);
        float duration = Random.Range(0.8f, 1.5f);
        float drift = Random.Range(-0.3f, 0.3f);
        float t = 0f;
        while (t < duration && ember != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            ember.transform.position = (Vector3)start + new Vector3(drift * k, 1.2f * k, 0f);
            SetAlpha(ember, 1f - k);
            yield return null;
        }

        if (ember != null)
            Destroy(ember.gameObject);
    }

    // ------------------------------------------------------------------ helpers

    private SpriteRenderer CreateSprite(string name, Sprite sprite, Vector2 position, float scale, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    /// <summary>Creates a sprite object that plays `frames` (looping by default) instead of a static sprite.</summary>
    private SpriteRenderer CreateAnimatedSprite(string name, Sprite[] frames, Vector2 position, float scale, int sortingOrder, bool loop = true, float frameRate = 0f)
    {
        SpriteRenderer sr = CreateSprite(name, frames is { Length: > 0 } ? frames[0] : null, position, scale, sortingOrder);
        if (frames is { Length: > 1 })
        {
            // Random start offset so a ring of identical rocks does not animate in lockstep.
            sr.gameObject.AddComponent<SkillFrameAnimator>().Play(frames, frameRate > 0f ? frameRate : _effectFrameRate, loop, loop ? Random.value : 0f);
        }

        return sr;
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite == null)
            _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _whiteSprite;
    }

    /// <summary>Banded (not smooth) dark ellipse, 1 unit wide at scale 1, to match the chunky pixel style.</summary>
    private static Sprite GetShadowSprite()
    {
        if (_shadowSprite == null)
            _shadowSprite = BuildDiscSprite(0f);
        return _shadowSprite;
    }

    private static Sprite _ringSprite;

    private static Sprite GetRingSprite()
    {
        if (_ringSprite == null)
            _ringSprite = BuildDiscSprite(0.82f);
        return _ringSprite;
    }

    /// <summary>`innerCut` 0 = filled 3-band disc; otherwise a thin hollow ring starting at that fraction.</summary>
    private static Sprite BuildDiscSprite(float innerCut)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha;
                if (innerCut <= 0f)
                    alpha = d < 0.5f ? 1f : d < 0.75f ? 0.6f : d < 1f ? 0.3f : 0f;
                else
                    alpha = d >= innerCut && d < 1f ? 1f : 0f;

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
