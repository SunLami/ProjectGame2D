using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The five Water-crab skills (WaterBossCombatPlan.md section 4, D-101). Same rules as the Earth skills: a
/// <see cref="BossTelegraph"/> first, then damage through Player.TakeDamage (dash i-frames respected). Kept in its own
/// partial file so the Earth skills stay untouched.</summary>
public sealed partial class BossController
{
    private bool _burrowed;

    private float TideSpeedMultiplier()
    {
        TideController tide = TideController.Instance;
        if (tide == null || !tide.Active || _definition == null || !tide.IsShallow(transform.position))
            return 1f;
        return _definition.tideCrabSpeedMultiplier;
    }

    /// <summary>Hides/shows the crab while it is burrowed (body + shadow; the shadow keeps its own sprite).</summary>
    private void SetBodyVisible(bool visible)
    {
        if (_body != null)
            _body.enabled = visible;
        Transform shadow = transform.Find("Shadow");
        if (shadow != null && shadow.TryGetComponent(out SpriteRenderer shadowRenderer))
            shadowRenderer.enabled = visible;
    }

    private static Rigidbody2D BodyOf(Player player) => player != null ? player.GetComponent<Rigidbody2D>() : null;

    private Rect ArenaRect() => _hasBounds ? _bounds : new Rect(-31.8f, -16f, 63.6f, 31.7f);

    private static GameObject MakeBar(string name, Color color, int order)
    {
        var go = new GameObject(name);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = BossTelegraph.SquareSprite; // 1 unit long, pivot left-centre
        renderer.color = color;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        return go;
    }

    // ---------------------------------------------------------------- 1. Claw Clamp

    private IEnumerator ClawClampSkill()
    {
        float windup = Tele(_definition.clampWindup);
        Vector2 origin = transform.position;
        float baseAngle = AngleOf(PlayerPosition() - origin);
        float rotate = _definition.clampSecondRotateDegrees * (Random.value < 0.5f ? 1f : -1f);
        float arc = _definition.clampArcDegrees;
        float radius = _definition.clampRadius;

        ShowFan(origin, baseAngle, arc, radius, windup);
        ShowFan(origin, baseAngle + rotate, arc, radius, windup + _definition.clampSecondDelay);
        PlayClip(BossClipId.Snap, windup);
        yield return new WaitForSeconds(windup);

        ContinueClip();
        SkillScreenFX.Shake(0.12f, 0.2f);
        StrikeCone(origin, baseAngle, arc, radius, _definition.clampDamage, _definition.clampKnockback);
        TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", origin + DirectionOf(baseAngle) * (radius * 0.6f), 1.6f, 14f, false, 0.8f, 8));
        yield return new WaitForSeconds(_definition.clampSecondDelay);

        SkillScreenFX.Shake(0.14f, 0.25f);
        StrikeCone(origin, baseAngle + rotate, arc, radius, _definition.clampDamage, _definition.clampKnockback);
        TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", origin + DirectionOf(baseAngle + rotate) * (radius * 0.6f), 1.6f, 14f, false, 0.8f, 8));
        yield return new WaitForSeconds(0.45f);
    }

    /// <summary>Fan-shaped danger zone drawn as overlapping wide bars.</summary>
    private void ShowFan(Vector2 origin, float centerAngle, float arc, float radius, float duration)
    {
        const int bars = 5;
        float width = Mathf.Max(1.4f, radius * Mathf.Tan(arc / (bars * 2f) * Mathf.Deg2Rad) * 2.4f);
        for (int i = 0; i < bars; i++)
        {
            float t = bars == 1 ? 0.5f : (float)i / (bars - 1);
            float angle = centerAngle + Mathf.Lerp(-arc * 0.5f + arc / (bars * 2f), arc * 0.5f - arc / (bars * 2f), t);
            Track(BossTelegraph.Line(origin, DirectionOf(angle), radius, width, duration));
        }
    }

    private void StrikeCone(Vector2 origin, float centerAngle, float arc, float radius, float multiplier, float knockback)
    {
        Player player = GetPlayer();
        if (player == null || player.IsDead)
            return;

        Vector2 to = (Vector2)player.transform.position + Vector2.up * 0.3f - origin;
        if (to.magnitude <= radius + 0.3f && Mathf.Abs(Mathf.DeltaAngle(AngleOf(to), centerAngle)) <= arc * 0.5f)
            HurtPlayer(player, multiplier, origin, knockback);
    }

    // ---------------------------------------------------------------- 2. Bubble Trap

    private IEnumerator BubbleTrapSkill()
    {
        int count = BossDefinition.PerPhase(_definition.bubbleCountByPhase, _phaseIndex, 3);
        PlayClip(BossClipId.Spit, 0.5f);
        yield return new WaitForSeconds(0.55f);

        for (int i = 0; i < count && _state != BossState.Dead; i++)
        {
            Vector2 target = PredictedPlayerPosition(_definition.bubblePredictSeconds + i * 0.25f);
            if (i >= 2)
                target += Random.insideUnitCircle * 3f;
            Vector2 from = (Vector2)transform.position + Vector2.up * 0.5f;
            Vector2 direction = target - from;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.down;
            BossBubble.Spawn(from, direction, _definition, this, Damage(_definition.bubbleDamage), _effectsRoot);
            yield return new WaitForSeconds(_definition.bubbleSpawnInterval);
        }

        ContinueClip();
        yield return new WaitForSeconds(0.4f);
    }

    // ---------------------------------------------------------------- 3. Sand Ambush

    private IEnumerator SandAmbushSkill()
    {
        try
        {
            yield return SandAmbushRoutine();
        }
        finally
        {
            _burrowed = false;
            SetBodyVisible(true);
        }
    }

    private IEnumerator SandAmbushRoutine()
    {
        // sink into the sand (invulnerable)
        PlayClip(BossClipId.Burrow, _definition.ambushSinkSeconds);
        TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", transform.position, 2f, 14f, false, 0.8f, 8));
        yield return new WaitForSeconds(_definition.ambushSinkSeconds);
        _burrowed = true;
        SetBodyVisible(false);
        ContinueClip();

        // the sand mound chases the player; in shallow water the same mound reads as a ripple
        GameObject mound = TrackObject(MakeBar("SandMound", Color.white, 2));
        var moundRenderer = mound.GetComponent<SpriteRenderer>();
        moundRenderer.sprite = BossTelegraph.CircleSprite;
        mound.transform.localScale = new Vector3(2.6f, 1.3f, 1f);
        float splashTimer = 0f;
        for (float t = 0f; t < _definition.ambushChaseSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            Vector2 toPlayer = PlayerPosition() - (Vector2)transform.position;
            Vector2 next = (Vector2)transform.position + toPlayer.normalized * (_definition.ambushChaseSpeed * Time.deltaTime);
            transform.position = ClampInside(next, 1f);
            mound.transform.position = transform.position;
            TideController tide = TideController.Instance;
            bool wet = tide != null && tide.IsShallow(transform.position);
            moundRenderer.color = wet ? new Color(0.45f, 0.8f, 0.9f, 0.55f) : new Color(0.5f, 0.4f, 0.27f, 0.9f);
            splashTimer -= Time.deltaTime;
            if (splashTimer <= 0f)
            {
                splashTimer = 0.22f;
                TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", transform.position, 0.9f, 16f, false, 0.6f, 8));
            }

            yield return null;
        }

        // lock the spot, warn, then burst out
        Vector2 spot = transform.position;
        float warn = Tele(_definition.ambushWarnSeconds);
        Track(BossTelegraph.Circle(spot, _definition.ambushRadius, warn));
        yield return new WaitForSeconds(warn);

        if (mound != null)
            Destroy(mound);
        _burrowed = false;
        SetBodyVisible(true);
        PlayClip(BossClipId.Emerge);
        StrikeCircle(spot, _definition.ambushRadius, _definition.ambushDamage, _definition.ambushKnockback, new HashSet<Player>());
        TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", spot, _definition.ambushRadius * 0.8f, 14f, false, 1f, 8));
        TrackObject(BossVfx.Spawn("RockProjectile_Impact", spot, 2f, 14f, false, 1f, 7));
        SkillScreenFX.Shake(0.22f, 0.3f);
        yield return new WaitForSeconds(_definition.ambushStandSeconds);
    }

    // ---------------------------------------------------------------- 4. Tidal Wave

    private IEnumerator TidalWaveSkill()
    {
        Rect arena = ArenaRect();
        float yTop = arena.yMax + 4f;
        float yEnd = arena.yMin - 2f;
        float height = yTop - yEnd;
        float warn = Tele(_definition.waveWarnSeconds);
        int gapCount = BossDefinition.PerPhase(_definition.waveGapCountByPhase, _phaseIndex, 2);
        float gapWidth = BossDefinition.PerPhase(_definition.waveGapWidthByPhase, _phaseIndex, 4.5f);

        var segments = BuildWaveSegments(arena, gapCount, gapWidth);
        foreach (Vector2 segment in segments)
            Track(BossTelegraph.Line(new Vector2((segment.x + segment.y) * 0.5f, yTop), Vector2.down, height, segment.y - segment.x, warn));

        PlayClip(BossClipId.Snap, warn);
        yield return new WaitForSeconds(warn);
        ContinueClip();
        SkillScreenFX.Shake(0.15f, 0.4f);

        var bars = new List<Transform>();
        foreach (Vector2 segment in segments)
        {
            GameObject bar = TrackObject(MakeBar("TidalWave", new Color(0.3f, 0.75f, 0.88f, 0.85f), -90));
            bar.transform.position = new Vector3(segment.x, yTop, 0f);
            bar.transform.localScale = new Vector3(segment.y - segment.x, _definition.waveThickness, 1f);
            GameObject foam = MakeBar("Foam", new Color(0.95f, 1f, 1f, 0.9f), -89);
            foam.transform.SetParent(bar.transform, false);
            foam.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            foam.transform.localScale = new Vector3(1f, 0.3f, 1f);
            bars.Add(bar.transform);
        }

        bool hit = false;
        float front = yTop;
        Player player = GetPlayer();
        while (front > yEnd && _state != BossState.Dead)
        {
            front -= _definition.waveSpeed * Time.deltaTime;
            foreach (Transform bar in bars)
            {
                if (bar != null)
                    bar.position = new Vector3(bar.position.x, front, 0f);
            }

            if (!hit && player != null && !player.IsDead)
            {
                Vector2 point = player.transform.position;
                foreach (Vector2 segment in segments)
                {
                    if (point.x >= segment.x && point.x <= segment.y && Mathf.Abs(point.y + 0.3f - front) <= _definition.waveThickness * 0.5f + 0.3f)
                    {
                        hit = true;
                        HurtPlayer(player, _definition.waveDamage, new Vector2(point.x, front + 2f), 4f);
                        Rigidbody2D body = BodyOf(player);
                        if (body != null)
                            body.position += Vector2.down * _definition.wavePushDistance;
                        TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", point, 2f, 14f, false, 0.8f, 9));
                        break;
                    }
                }
            }

            yield return null;
        }

        foreach (Transform bar in bars)
        {
            if (bar != null)
                Destroy(bar.gameObject);
        }
    }

    private static List<Vector2> BuildWaveSegments(Rect arena, int gapCount, float gapWidth)
    {
        var centers = new List<float>();
        float lo = arena.xMin + gapWidth * 0.5f + 1f;
        float hi = arena.xMax - gapWidth * 0.5f - 1f;
        for (int i = 0; i < gapCount; i++)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float candidate = Random.Range(lo, hi);
                bool clear = true;
                foreach (float other in centers)
                    clear &= Mathf.Abs(other - candidate) >= Mathf.Max(12f, gapWidth * 2.5f);
                if (clear || attempt == 29)
                {
                    centers.Add(candidate);
                    break;
                }
            }
        }

        centers.Sort();
        var segments = new List<Vector2>();
        float start = arena.xMin - 1f;
        foreach (float center in centers)
        {
            float gapStart = center - gapWidth * 0.5f;
            if (gapStart > start + 0.5f)
                segments.Add(new Vector2(start, gapStart));
            start = center + gapWidth * 0.5f;
        }

        if (arena.xMax + 1f > start + 0.5f)
            segments.Add(new Vector2(start, arena.xMax + 1f));
        return segments;
    }

    // ---------------------------------------------------------------- 5. Whirlpool (ultimate)

    private IEnumerator WhirlpoolSkill()
    {
        yield return DriftTo(_arenaCenter, 6f, 2f);
        Vector2 center = transform.position;
        float warn = Tele(_definition.whirlWarnSeconds);
        float radius = _definition.whirlRadius;

        Track(BossTelegraph.Circle(center, radius, warn));
        PlayClip(BossClipId.Whirl, warn);
        GameObject whirl = TrackObject(BossVfx.Spawn("Water/WaterWhirlpool_Idle", center, 1f, 10f, true,
            warn + _definition.whirlPullSeconds + _definition.whirlJetSeconds + 1f, -91));
        Sprite[] frames = BossVfx.Frames("Water/WaterWhirlpool_Idle");
        if (whirl != null && frames != null && frames.Length > 0)
            whirl.transform.localScale = Vector3.one * (radius * 2f / Mathf.Max(0.5f, frames[0].bounds.size.x));
        SkillScreenFX.Dim(0.2f, 0.5f);
        SkillScreenFX.Shake(0.1f, warn);
        yield return new WaitForSeconds(warn);

        // pull
        Player player = GetPlayer();
        Rigidbody2D body = BodyOf(player);
        float tick = 0f;
        for (float t = 0f; t < _definition.whirlPullSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            if (player != null && !player.IsDead && body != null)
            {
                Vector2 toCenter = center - (Vector2)player.transform.position;
                float distance = toCenter.magnitude;
                if (distance < radius && distance > 0.3f)
                    body.position += toCenter.normalized * (_definition.whirlPullSpeed * Time.deltaTime);

                tick -= Time.deltaTime;
                if (distance <= _definition.whirlCoreRadius && tick <= 0f)
                {
                    tick = _definition.whirlTickInterval;
                    HurtPlayer(player, _definition.whirlTickDamage, center, 3f);
                }
            }

            yield return null;
        }

        // rotating jets
        int jets = Mathf.Max(1, _definition.whirlJetCount);
        var bars = new List<Transform>();
        for (int i = 0; i < jets; i++)
        {
            GameObject bar = TrackObject(MakeBar("WaterJet", new Color(0.45f, 0.9f, 1f, 0.8f), -88));
            bar.transform.position = center;
            bar.transform.localScale = new Vector3(_definition.whirlJetLength, _definition.whirlJetWidth, 1f);
            bars.Add(bar.transform);
        }

        var hitJets = new HashSet<int>();
        float angle = Random.Range(0f, 360f);
        SkillScreenFX.Shake(0.15f, 0.4f);
        for (float t = 0f; t < _definition.whirlJetSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            angle += _definition.whirlJetSpinDegreesPerSecond * Time.deltaTime;
            for (int i = 0; i < jets; i++)
            {
                float jetAngle = angle + 360f / jets * i;
                if (bars[i] != null)
                    bars[i].rotation = Quaternion.Euler(0f, 0f, jetAngle);

                if (player != null && !player.IsDead && !hitJets.Contains(i))
                {
                    Vector2 local = (Vector2)player.transform.position + Vector2.up * 0.3f - center;
                    Vector2 axis = DirectionOf(jetAngle);
                    float along = Vector2.Dot(local, axis);
                    float across = Mathf.Abs(local.x * axis.y - local.y * axis.x);
                    if (along >= 0f && along <= _definition.whirlJetLength && across <= _definition.whirlJetWidth * 0.5f + 0.3f)
                    {
                        hitJets.Add(i);
                        HurtPlayer(player, _definition.whirlJetDamage, center, 5f);
                    }
                }
            }

            yield return null;
        }

        foreach (Transform bar in bars)
        {
            if (bar != null)
                Destroy(bar.gameObject);
        }

        SkillScreenFX.Dim(0f, 0.6f);
        ContinueClip();
        if (whirl != null)
            Destroy(whirl);
    }
}
