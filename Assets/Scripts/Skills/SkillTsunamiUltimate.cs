using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Water Skill 4 "Tam Trung Tsunami" ultimate coordinator (D-079). Wraps the existing three-wave fan
/// (`SkillTsunamiWave`, D-074) with the cast and finish effects that make it an ultimate:
///   1. Gather  - animated water ring under the caster, water orbs orbit then shoot forward, the screen dims
///                blue and the camera trembles; animated ripples run along the fan as a warning.
///   2. Waves   - each wave starts with a spray burst at the caster's feet, leaves a fading wet trail, and the
///                last wave carries a water dragon on its crest and shakes the camera harder. Enemies a wave
///                hits get a "wet" status (slowed, with an animated droplet icon).
///   3. Finale  - where the last wave ends it bursts into a big water explosion + shockwave (flash, hit-stop,
///                slow motion, AoE damage), then a flood zone slows enemies and rain falls over the area.
/// Wave size and damage stay exactly as D-074; this script only adds effects and the finale.</summary>
public class SkillTsunamiUltimate : MonoBehaviour
{
    [Header("Waves (same behaviour as D-074)")]
    [SerializeField] private SkillTsunamiWave _wavePrefab;
    [SerializeField] private int _waveCount = 3;
    [SerializeField] private float _waveInterval = 0.9f;
    [SerializeField] private float _waveIntervalGrowth = 0.25f;
    [SerializeField] private float _damageGrowthPerWave = 0.35f;

    [Header("Animated art")]
    [SerializeField] private Sprite[] _gatherRingFrames;
    [SerializeField] private Sprite[] _orbFrames;
    [SerializeField] private Sprite[] _rippleFrames;
    [SerializeField] private Sprite[] _sprayFrames;
    [SerializeField] private Sprite[] _dragonFrames;
    [SerializeField] private Sprite[] _puddleFrames;
    [SerializeField] private Sprite[] _rainSplashFrames;
    [SerializeField] private Sprite[] _shockwaveFrames;
    [SerializeField] private GameObject _explosionVfxPrefab;
    [SerializeField] private SkillStatusLoop _wetIndicatorPrefab;
    [SerializeField] private float _effectFrameRate = 10f;

    [Header("Gather")]
    [SerializeField] private float _gatherDuration = 1f;
    [SerializeField] private Color _dimTint = new Color(0.03f, 0.12f, 0.3f, 1f);
    [SerializeField] private float _dimAlpha = 0.3f;
    [SerializeField] private float _rippleInterval = 0.22f;
    [SerializeField] private float _rippleSpeed = 10f;

    [Header("Wet debuff")]
    [SerializeField, Range(0f, 1f)] private float _wetSlow = 0.75f;
    [SerializeField] private float _wetDuration = 3f;

    [Header("Finale")]
    [SerializeField] private float _explosionRadius = 2.2f;
    [SerializeField] private float _explosionDamage = 40f;
    [SerializeField] private float _explosionKnockback = 8f;
    [SerializeField] private float _floodDuration = 4f;
    [SerializeField, Range(0f, 1f)] private float _floodSlow = 0.5f;
    [SerializeField] private float _rainDuration = 3f;
    [SerializeField] private float _rainRadius = 3.5f;
    [SerializeField] private LayerMask _targetLayers = ~0;

    private Player _caster;
    private Vector2 _direction;
    private float _range;
    private float _fanHalfTan;
    private float _fanAngle;
    private Vector2 _finalePosition;
    private bool _finaleReady;
    private bool _finaleDone;
    private readonly List<GameObject> _persistent = new List<GameObject>();
    private readonly Dictionary<MonoBehaviour, SkillStatusLoop> _wetIndicators = new Dictionary<MonoBehaviour, SkillStatusLoop>();
    private static Sprite _whiteSprite;

    /// <summary>Starts the full sequence. `direction` and `range` are the confirmed aim; `fanAngleDegrees` the cone.</summary>
    public void Launch(Player caster, Vector2 direction, float range, float fanAngleDegrees)
    {
        _caster = caster;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _range = range;
        _fanAngle = fanAngleDegrees;
        _fanHalfTan = Mathf.Tan(Mathf.Clamp(fanAngleDegrees, 1f, 170f) * 0.5f * Mathf.Deg2Rad);
        StartCoroutine(Run());
    }

    // ------------------------------------------------------------------ timeline

    private IEnumerator Run()
    {
        // 1. gather
        SoundFXManager.PlaySfx(SfxIds.SkillWaterS4Charge);
        StartCoroutine(GatherRoutine());
        StartCoroutine(RippleWarningRoutine());
        SkillScreenFX.Dim(_dimAlpha, _gatherDuration, _dimTint);
        SkillScreenFX.Shake(0.04f, _gatherDuration);
        yield return new WaitForSeconds(_gatherDuration);

        // 2. waves
        for (int i = 0; i < _waveCount; i++)
        {
            if (_caster == null)
                break;

            bool last = i == _waveCount - 1;
            Vector2 origin = _caster.SkillOrigin;
            SpawnSpray(origin, 1f + 0.25f * i);
            SkillScreenFX.Shake(last ? 0.3f : 0.1f + 0.05f * i, last ? 0.5f : 0.25f);

            SkillTsunamiWave wave = Instantiate(_wavePrefab, origin, Quaternion.identity);
            SoundFXManager.PlaySfx(SfxIds.SkillWaterS4Wave);
            wave.TargetHit += OnWaveHit;
            if (last)
                wave.Ended += OnLastWaveEnded;
            wave.Launch(origin, _direction, _range, _fanAngle, 1f + _damageGrowthPerWave * i);

            StartCoroutine(TrailRoutine(wave));
            if (last)
                StartCoroutine(DragonRoutine(wave));

            if (!last)
                yield return new WaitForSeconds(_waveInterval + _waveIntervalGrowth * i);
        }

        // 3. finale: wait for the last wave to end (it normally takes ~1s), then run the finish effects.
        float waited = 0f;
        while (!_finaleReady && waited < 6f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (!_finaleReady)
        {
            Cleanup();
            yield break;
        }

        yield return FinaleRoutine(_finalePosition);
        yield return new WaitForSeconds(0.3f);
        Cleanup();
    }

    private void Cleanup()
    {
        foreach (GameObject go in _persistent)
        {
            if (go != null)
                Destroy(go);
        }

        Destroy(gameObject);
    }

    private void OnLastWaveEnded(Vector2 position, bool reachedFullRange)
    {
        _finalePosition = position;
        _finaleReady = true;
    }

    // ------------------------------------------------------------------ 1. gather

    private IEnumerator GatherRoutine()
    {
        if (_caster == null)
            yield break;

        SpriteRenderer ring = CreateAnimated("GatherRing", _gatherRingFrames, _caster.transform.position, 0.6f, -95);
        _persistent.Add(ring.gameObject);

        const int orbCount = 6;
        var orbs = new SpriteRenderer[orbCount];
        SpriteRenderer casterRenderer = _caster.GetComponentInChildren<SpriteRenderer>();
        for (int i = 0; i < orbCount; i++)
        {
            orbs[i] = CreateAnimated("OrbitOrb_" + i, _orbFrames, _caster.SkillOrigin, 0.1f, 0);
            if (casterRenderer != null)
                orbs[i].sortingLayerID = casterRenderer.sortingLayerID;
            _persistent.Add(orbs[i].gameObject);
        }

        float t = 0f;
        while (t < _gatherDuration && _caster != null)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / _gatherDuration);
            SetAlpha(ring, Mathf.Clamp01(progress * 3f));
            ring.transform.position = _caster.transform.position;
            ring.transform.Rotate(0f, 0f, 80f * Time.deltaTime);

            // Ring centred on the hips (not the chest) so the back half stays waist-high behind the body
            // instead of rising to face level.
            Vector2 origin = (Vector2)_caster.transform.position + new Vector2(0f, 0.25f);
            int order = casterRenderer != null ? casterRenderer.sortingOrder : 0;
            for (int i = 0; i < orbCount; i++)
            {
                float angle = i / (float)orbCount * Mathf.PI * 2f + t * (2f + progress * 5f);
                float sin = Mathf.Sin(angle);
                orbs[i].transform.position = new Vector3(
                    origin.x + Mathf.Cos(angle) * 0.85f,
                    origin.y + sin * 0.22f + Mathf.Sin(t * 6f + i) * 0.03f,
                    0f);
                orbs[i].sortingOrder = order + (sin > 0f ? -1 : 1);
                SetAlpha(orbs[i], Mathf.Clamp01(progress * 4f));
            }

            yield return null;
        }

        // Orbs launch forward along the aim direction while the ring fades.
        float launch = 0f;
        while (launch < 0.35f)
        {
            launch += Time.deltaTime;
            float k = launch / 0.35f;
            for (int i = 0; i < orbCount; i++)
            {
                if (orbs[i] == null)
                    continue;
                orbs[i].transform.position += (Vector3)(_direction * (14f * Time.deltaTime));
                SetAlpha(orbs[i], 1f - k);
            }

            if (ring != null)
                SetAlpha(ring, 1f - k);
            yield return null;
        }
    }

    /// <summary>Warning: animated ripple arcs run outward through the fan so the player can read where the waves go.</summary>
    private IEnumerator RippleWarningRoutine()
    {
        float elapsed = 0f;
        float timer = 0f;
        while (elapsed < _gatherDuration + 0.4f && _caster != null)
        {
            if (timer <= 0f)
            {
                timer = _rippleInterval;
                StartCoroutine(RippleRoutine(_caster.SkillOrigin));
            }

            timer -= Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RippleRoutine(Vector2 origin)
    {
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        SpriteRenderer ripple = CreateAnimated("Ripple", _rippleFrames, origin, 0.2f, -60);
        ripple.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        float spriteHeight = _rippleFrames is { Length: > 0 } ? _rippleFrames[0].bounds.size.y : 3f;

        float distance = 0.8f;
        while (distance < _range && ripple != null)
        {
            distance += _rippleSpeed * Time.deltaTime;
            float chord = Mathf.Max(0.8f, 2f * distance * _fanHalfTan * 0.8f);
            float scale = chord / spriteHeight;
            ripple.transform.localScale = new Vector3(scale, scale, 1f);
            ripple.transform.position = origin + _direction * distance;
            float fade = Mathf.Clamp01((_range - distance) / 1.5f);
            SetAlpha(ripple, 0.85f * fade);
            yield return null;
        }

        if (ripple != null)
            Destroy(ripple.gameObject);
    }

    // ------------------------------------------------------------------ 2. waves

    private void SpawnSpray(Vector2 position, float scale)
    {
        StartCoroutine(OneShotRoutine("Spray", _sprayFrames, position, 0.4f * scale, 6));
    }

    /// <summary>Plays `frames` once at `position`, then destroys the object.</summary>
    private IEnumerator OneShotRoutine(string name, Sprite[] frames, Vector2 position, float scale, int sortingOrder, float fps = 0f)
    {
        if (frames is not { Length: > 0 })
            yield break;

        SpriteRenderer sr = CreateAnimated(name, frames, position, scale, sortingOrder, false, fps);
        sr.sortingLayerName = "Default";
        float duration = frames.Length / (fps > 0f ? fps : _effectFrameRate);
        yield return new WaitForSeconds(duration);
        if (sr != null)
            Destroy(sr.gameObject);
    }

    private IEnumerator TrailRoutine(SkillTsunamiWave wave)
    {
        // A fading wet trail across the crest width, dropped every ~0.8 units the wave travels.
        float lastDrop = -1f;
        Vector2 origin = wave.FrontPosition;
        while (wave != null)
        {
            Vector2 front = wave.FrontPosition;
            float travelled = Vector2.Distance(origin, front);
            if (travelled - lastDrop >= 0.8f)
            {
                lastDrop = travelled;
                Vector2 perpendicular = new Vector2(-wave.Direction.y, wave.Direction.x);
                float width = wave.CrestWidth;
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 position = front - wave.Direction * 0.6f + perpendicular * (i * width * 0.3f);
                    StartCoroutine(PuddleRoutine(position, 0.2f, 2.5f));
                }
            }

            yield return null;
        }
    }

    private IEnumerator PuddleRoutine(Vector2 position, float scale, float lifetime)
    {
        SpriteRenderer puddle = CreateAnimated("TrailPuddle", _puddleFrames, position, scale * Random.Range(0.8f, 1.2f), -88);
        float t = 0f;
        while (t < lifetime && puddle != null)
        {
            t += Time.deltaTime;
            float k = t / lifetime;
            SetAlpha(puddle, k < 0.15f ? k / 0.15f : Mathf.Clamp01((1f - k) / 0.5f));
            yield return null;
        }

        if (puddle != null)
            Destroy(puddle.gameObject);
    }

    private IEnumerator DragonRoutine(SkillTsunamiWave wave)
    {
        if (_dragonFrames is not { Length: > 0 })
            yield break;

        float angle = Mathf.Atan2(wave.Direction.y, wave.Direction.x) * Mathf.Rad2Deg;
        SpriteRenderer dragon = CreateAnimated("WaterDragon", _dragonFrames, wave.FrontPosition, 0.45f, -49);
        dragon.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        float t = 0f;
        while (wave != null && dragon != null)
        {
            t += Time.deltaTime;
            // Rises out of the crest over the first moments, then rides the leading edge.
            float grow = Mathf.SmoothStep(0.15f, 1f, Mathf.Clamp01(t / 0.4f));
            // The dragon scales with the crest so it stays prominent as the wave widens (never below 0.4).
            float scale = Mathf.Clamp(wave.CrestWidth * 0.16f, 0.4f, 1f) * grow;
            dragon.transform.localScale = new Vector3(scale, scale, 1f);
            dragon.transform.position = wave.FrontPosition - wave.Direction * 0.5f;
            yield return null;
        }

        if (dragon != null)
            Destroy(dragon.gameObject);
    }

    private void OnWaveHit(MonoBehaviour target)
    {
        if (target is ISlowable slowable)
            slowable.ApplySlow(_wetSlow, _wetDuration);

        if (_wetIndicatorPrefab == null)
            return;

        if (_wetIndicators.TryGetValue(target, out SkillStatusLoop existing) && existing != null)
        {
            existing.Show(target.transform, _wetDuration);
            return;
        }

        SkillStatusLoop indicator = Instantiate(_wetIndicatorPrefab);
        indicator.Show(target.transform, _wetDuration);
        _wetIndicators[target] = indicator;
    }

    // ------------------------------------------------------------------ 3. finale

    private IEnumerator FinaleRoutine(Vector2 position)
    {
        if (_finaleDone)
            yield break;
        _finaleDone = true;

        SoundFXManager.PlaySfxAt(SfxIds.SkillWaterS4Crash, position);
        SoundFXManager.PlaySfx(SfxIds.SkillScreenSlam);
        SkillScreenFX.Dim(0f, 1.2f);
        SkillScreenFX.Flash(new Color(0.7f, 0.95f, 1f), 0.8f, 0.6f);
        SkillScreenFX.Shake(0.5f, 0.7f);
        SkillScreenFX.HitStopAndSlowMo(0.06f, 0.3f, 0.35f);

        if (_explosionVfxPrefab != null)
        {
            GameObject vfx = Instantiate(_explosionVfxPrefab, position, Quaternion.identity);
            vfx.transform.localScale *= 3.5f;
        }

        StartCoroutine(ShockwaveRoutine(position));
        ApplyExplosion(position);

        // Flood zone: a big animated puddle that slows enemies standing in it, with rain falling over the area.
        StartCoroutine(FloodRoutine(position));
        StartCoroutine(RainRoutine(position));

        yield return new WaitForSeconds(Mathf.Max(_floodDuration, _rainDuration) + 0.8f);
    }

    private IEnumerator ShockwaveRoutine(Vector2 center)
    {
        if (_shockwaveFrames is not { Length: > 0 })
            yield break;

        float frameWidth = _shockwaveFrames[0].bounds.size.x;
        float scale = _explosionRadius * 2.6f / frameWidth;
        SpriteRenderer ring = CreateAnimated("WaterShockwave", _shockwaveFrames, center, scale, -60, false);
        ring.color = new Color(0.55f, 0.9f, 1f, 1f); // tint the shared dust ring to water
        float duration = _shockwaveFrames.Length / _effectFrameRate;
        float t = 0f;
        while (t < duration && ring != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            if (k > 0.75f)
                SetAlpha(ring, (1f - k) / 0.25f);
            yield return null;
        }

        if (ring != null)
            Destroy(ring.gameObject);
    }

    private void ApplyExplosion(Vector2 position)
    {
        var hit = new HashSet<MonoBehaviour>();
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(position, _explosionRadius, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!hit.Add(candidate))
                    break;

                Vector2 away = ((Vector2)candidate.transform.position - position).normalized;
                target.TakeDamage(_explosionDamage, away, _explosionKnockback);
                OnWaveHit(candidate); // soaked by the burst too

                var flash = candidate.GetComponent<EnemyHitFlash>();
                if (flash == null)
                    flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
                flash.PlayFlash();
                break;
            }
        }
    }

    private IEnumerator FloodRoutine(Vector2 center)
    {
        float radius = _rainRadius * 0.65f;
        float frameWidth = _puddleFrames is { Length: > 0 } ? _puddleFrames[0].bounds.size.x : 3.67f;
        SpriteRenderer flood = CreateAnimated("FloodZone", _puddleFrames, center, radius * 2f / frameWidth, -86);
        _persistent.Add(flood.gameObject);

        float t = 0f;
        float tick = 0f;
        while (t < _floodDuration && flood != null)
        {
            t += Time.deltaTime;
            tick += Time.deltaTime;
            SetAlpha(flood, Mathf.Clamp01(Mathf.Min(t, _floodDuration - t) / 0.6f));

            if (tick >= 0.4f)
            {
                tick -= 0.4f;
                SlowInArea(center, radius);
            }

            yield return null;
        }

        if (flood != null)
            Destroy(flood.gameObject);
    }

    private void SlowInArea(Vector2 center, float radius)
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
                    slowable.ApplySlow(_floodSlow, 0.55f);
                break;
            }
        }
    }

    private IEnumerator RainRoutine(Vector2 center)
    {
        SfxLoopHandle rainLoop = SoundFXManager.StartLoop(SfxIds.SkillWaterS4AftermathRain, 0.6f, 1f, 0.5f);
        float t = 0f;
        float spawnTimer = 0f;
        while (t < _rainDuration)
        {
            t += Time.deltaTime;
            spawnTimer -= Time.deltaTime;
            while (spawnTimer <= 0f)
            {
                spawnTimer += 1f / 14f;
                Vector2 position = center + Random.insideUnitCircle * _rainRadius;
                StartCoroutine(RaindropRoutine(position));
            }

            yield return null;
        }

        rainLoop.Stop(1f);
    }

    private IEnumerator RaindropRoutine(Vector2 position)
    {
        // A short streak falls to the ground, then the animated splash plays where it lands.
        SpriteRenderer streak = CreateSprite("Raindrop", GetWhiteSprite(), position + Vector2.up * 3f, 1f, 70);
        streak.transform.localScale = new Vector3(0.05f, 0.45f, 1f);
        streak.sortingLayerName = "Player";
        streak.color = new Color(0.7f, 0.92f, 1f, 0.9f);

        const float fall = 0.22f;
        float t = 0f;
        while (t < fall && streak != null)
        {
            t += Time.deltaTime;
            streak.transform.position = Vector3.Lerp(position + Vector2.up * 3f, position, t / fall);
            yield return null;
        }

        if (streak != null)
            Destroy(streak.gameObject);

        yield return OneShotRoutine("RainSplash", _rainSplashFrames, position, 0.16f, -55);
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

    private SpriteRenderer CreateAnimated(string name, Sprite[] frames, Vector2 position, float scale, int sortingOrder, bool loop = true, float frameRate = 0f)
    {
        SpriteRenderer sr = CreateSprite(name, frames is { Length: > 0 } ? frames[0] : null, position, scale, sortingOrder);
        if (frames is { Length: > 1 })
            sr.gameObject.AddComponent<SkillFrameAnimator>().Play(frames, frameRate > 0f ? frameRate : _effectFrameRate, loop, loop ? Random.value : 0f);
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
}
