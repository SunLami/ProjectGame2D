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

    /// <summary>An animated sprite object (frames from Resources/VFX/Skills/Water) with an optional parent, scale and tint.</summary>
    private static GameObject MakeAnimated(string frameSet, Vector2 position, float scale, float fps, int order, Transform parent = null,
        bool loop = true, Color? tint = null)
    {
        Sprite[] frames = BossVfx.Frames("Water/" + frameSet);
        var go = new GameObject("Boss_" + frameSet);
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        if (tint.HasValue)
            renderer.color = tint.Value;
        if (frames != null && frames.Length > 0)
            go.AddComponent<SkillFrameAnimator>().Play(frames, fps, loop, Random.value * frames.Length / fps);
        return go;
    }

    // ---------------------------------------------------------------- 1. Claw Clamp

    private IEnumerator ClawClampSkill()
    {
        // D-106: the crab runs the player down and cuts several times, one fan telegraph per cut (dash or sidestep each one)
        int cuts = Mathf.Max(1, BossDefinition.PerPhase(_definition.clampCutsByPhase, _phaseIndex, 3));
        float arc = _definition.clampArcDegrees;
        float radius = _definition.clampRadius;
        bool previousHit = false;
        for (int i = 0; i < cuts && _state != BossState.Dead; i++)
        {
            yield return ChasePlayerUntil(radius * 0.55f, 1.2f, _definition.clampChaseSpeed);

            Vector2 origin = transform.position;
            float angle = AngleOf(PredictedPlayerPosition(0.15f) - origin);
            float windup = Mathf.Max(0.5f, Tele(_definition.clampWindup) * 0.65f);
            ShowFan(origin, angle, arc, radius, windup);
            WaterSfx(SfxIds.BosswClawWindup, origin);
            PlayClip(BossClipId.Snap, windup);
            yield return new WaitForSeconds(windup);

            ContinueClip();
            SkillScreenFX.Shake(0.12f, 0.2f);
            WaterSfx(SfxIds.BosswClawSnap, origin);
            bool hit = StrikeCone(origin, angle, arc, radius, _definition.clampDamage * _definition.clampCutDamage, _definition.clampKnockback * 0.3f);
            TrackObject(BossVfx.Spawn("Water/ClawSlash_Hit", origin + DirectionOf(angle) * (radius * 0.6f), 1.5f, 16f, false, 0.7f, 9));
            if (hit && previousHit)
                HoldPlayer(_definition.clampRootSeconds);
            previousHit = hit;
            yield return new WaitForSeconds(0.22f);
        }

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Runs at the player until within `reach` (or `maxSeconds`), playing the walk animation.</summary>
    private IEnumerator ChasePlayerUntil(float reach, float maxSeconds, float speed)
    {
        for (float t = 0f; t < maxSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            Vector2 toPlayer = PlayerPosition() - (Vector2)transform.position;
            if (toPlayer.magnitude <= reach)
                break;

            SetMoving(true);
            SteerSmooth(toPlayer, speed, 22f);
            yield return null;
        }

        _moveVelocity = Vector2.zero;
        SetMoving(false);
    }

    private bool _chasing;
    private Vector2 _moveVelocity;

    /// <summary>Eased movement (acceleration / deceleration instead of an instant start and stop) toward `direction`.</summary>
    private void SteerSmooth(Vector2 direction, float speed, float acceleration)
    {
        Vector2 desired = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector2.zero;
        _moveVelocity = Vector2.MoveTowards(_moveVelocity, desired, acceleration * Time.deltaTime);
        if (_moveVelocity.sqrMagnitude > 0.0004f)
            transform.position = ClampInside((Vector2)transform.position + _moveVelocity * Time.deltaTime, 3f);
    }

    /// <summary>Between skills the crab keeps walking at the player (faster in shallow water) instead of standing still. It starts
    /// walking when the player is farther than 4.4 and stops within 3.2 (hysteresis: no flicker at the boundary) and eases in and out.</summary>
    private IEnumerator WaterChaseLoop()
    {
        while (_state != BossState.Dead)
        {
            bool free = _state == BossState.Fighting && _activeSkills == 0 && !_recovering && !_burrowed && !PlayerIsGone();
            if (free)
            {
                Vector2 toPlayer = PlayerPosition() - (Vector2)transform.position;
                float distance = toPlayer.magnitude;
                if (!_chasing && distance > 4.4f)
                {
                    _chasing = true;
                    SetMoving(true);
                }
                else if (_chasing && distance < 3.2f)
                {
                    _chasing = false;
                }

                SteerSmooth(_chasing ? toPlayer : Vector2.zero, CurrentMoveSpeed, _chasing ? 9f : 14f);
                if (!_chasing && _moveVelocity.sqrMagnitude < 0.01f)
                    SetMoving(false);
            }
            else
            {
                if (_chasing)
                    SetMoving(false);
                _chasing = false;
                _moveVelocity = Vector2.zero;
            }

            yield return null;
        }
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

    /// <summary>Returns true when the strike connected (inside the cone and not dashing through it).</summary>
    private bool StrikeCone(Vector2 origin, float centerAngle, float arc, float radius, float multiplier, float knockback)
    {
        Player player = GetPlayer();
        if (player == null || player.IsDead)
            return false;

        Vector2 to = (Vector2)player.transform.position + Vector2.up * 0.3f - origin;
        if (to.magnitude <= radius + 0.3f && Mathf.Abs(Mathf.DeltaAngle(AngleOf(to), centerAngle)) <= arc * 0.5f)
        {
            bool connects = !player.IsDashInvulnerable;
            HurtPlayer(player, multiplier, origin, knockback);
            return connects;
        }

        return false;
    }

    /// <summary>Claw Clamp: both claws landed, so the player is held for a moment (ripple under the feet shows it).</summary>
    private void HoldPlayer(float seconds)
    {
        Player player = GetPlayer();
        if (player == null || player.IsDead || seconds <= 0f)
            return;

        player.ApplyRoot(seconds);
        WaterSfx(SfxIds.BosswClawHold, player.transform.position);
        TrackObject(BossVfx.Spawn("Water/WaterPuddle_Ripple", (Vector2)player.transform.position + Vector2.down * 0.2f, 1.2f, 12f, true, seconds, 6));
    }

    // ---------------------------------------------------------------- 2. Bubble Trap

    private IEnumerator BubbleTrapSkill()
    {
        // D-106: three waves of bubbles flying out of the crab in a full circle: few, more, then many.
        int[][] waves =
        {
            _definition.bubbleRingWave1ByPhase, _definition.bubbleRingWave2ByPhase, _definition.bubbleRingWave3ByPhase,
        };
        float fallbackAngle = Random.Range(0f, 360f);
        for (int w = 0; w < waves.Length && _state != BossState.Dead; w++)
        {
            int count = Mathf.Max(4, BossDefinition.PerPhase(waves[w], _phaseIndex, 8 + 6 * w));
            PlayClip(BossClipId.Spit, 0.45f);
            WaterSfx(SfxIds.BosswBubbleSpit, transform.position);

            // each wave is turned by half a step so the next ring fills the gaps of the previous one
            float step = 360f / count;
            float offset = fallbackAngle + (w % 2 == 0 ? 0f : step * 0.5f) + Random.Range(-0.1f, 0.1f) * step;
            Vector2 center = (Vector2)transform.position + Vector2.up * 0.5f;

            // telegraph: one red lane per bubble (its flight path) so the gaps to slip through are readable before they fly
            float lead = Mathf.Max(0.55f, Tele(0.7f));
            const float laneLength = 12f;
            for (int i = 0; i < count; i++)
            {
                Vector2 laneDirection = DirectionOf(offset + step * i);
                Track(BossTelegraph.Line(center + laneDirection * 2.2f, laneDirection, laneLength, 0.28f, lead, true));
            }

            yield return new WaitForSeconds(lead);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = DirectionOf(offset + step * i);
                BossBubble.Spawn(center + direction * 2.2f, direction, _definition, this, Damage(_definition.bubbleDamage * 0.7f),
                    _effectsRoot, _definition.bubbleRingScale);
            }

            SkillScreenFX.Shake(0.08f + 0.03f * w, 0.2f);
            ContinueClip();
            if (w < waves.Length - 1)
                yield return new WaitForSeconds(_definition.bubbleWaveInterval);
        }

        yield return new WaitForSeconds(0.5f);
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
            _moundLoop?.Stop(0.15f);
            _moundLoop = null;
        }
    }

    private SfxLoopHandle _moundLoop;
    private SfxLoopHandle _pullLoop;

    /// <summary>Stops the looping Water-boss sounds (also called when the boss dies or is destroyed mid-skill).</summary>
    private void StopWaterLoops()
    {
        _moundLoop?.Stop(0.15f);
        _moundLoop = null;
        _pullLoop?.Stop(0.15f);
        _pullLoop = null;
        _owlLoop?.Stop(0.15f);
        _owlLoop = null;
    }

    private IEnumerator SandAmbushRoutine()
    {
        // sink into the sand (invulnerable)
        WaterSfx(SfxIds.BosswBurrowDive, transform.position);
        PlayClip(BossClipId.Burrow, _definition.ambushSinkSeconds);
        TrackObject(BossVfx.Spawn("Water/SandBurst_Hit", transform.position, 1.1f, 14f, false, 0.8f, 8));
        yield return new WaitForSeconds(_definition.ambushSinkSeconds);
        _burrowed = true;
        SetBodyVisible(false);
        ContinueClip();

        // the sand mound chases the player; in shallow water the same mound reads as a ripple
        GameObject mound = TrackObject(MakeAnimated("SandMound_Move", transform.position, 0.9f, 10f, 2));
        var moundRenderer = mound.GetComponent<SpriteRenderer>();
        if (UsesWaterSfx)
            _moundLoop = SoundFXManager.StartLoop(SfxIds.BosswMoundLoop, 0.8f, 1f, 0.2f);
        float splashTimer = 0f;
        for (float t = 0f; t < _definition.ambushChaseSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            Vector2 toPlayer = PlayerPosition() - (Vector2)transform.position;
            Vector2 next = (Vector2)transform.position + toPlayer.normalized * (_definition.ambushChaseSpeed * Time.deltaTime);
            transform.position = ClampInside(next, 1f);
            mound.transform.position = transform.position;
            TideController tide = TideController.Instance;
            bool wet = tide != null && tide.IsShallow(transform.position);
            moundRenderer.color = wet ? new Color(0.6f, 0.85f, 1f, 0.85f) : Color.white;
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
        _moundLoop?.Stop(0.1f);
        _moundLoop = null;
        _burrowed = false;
        SetBodyVisible(true);
        WaterSfx(SfxIds.BosswEmerge, spot);
        PlayClip(BossClipId.Emerge);
        StrikeCircle(spot, _definition.ambushRadius, _definition.ambushDamage, _definition.ambushKnockback, new HashSet<Player>());
        TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", spot, _definition.ambushRadius * 0.8f, 14f, false, 1f, 8));
        TrackObject(BossVfx.Spawn("Water/SandBurst_Hit", spot, _definition.ambushRadius * 2f / 3.67f, 14f, false, 0.9f, 7));
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
            Track(BossTelegraph.Line(new Vector2((segment.x + segment.y) * 0.5f, yTop), Vector2.down, height, segment.y - segment.x, warn, true));

        WaterSfx(SfxIds.BosswWaveWarning, transform.position);
        PlayClip(BossClipId.Slam, warn);
        yield return new WaitForSeconds(warn);
        ContinueClip();
        SkillScreenFX.Shake(0.15f, 0.4f);
        WaterSfx(SfxIds.BosswWaveRush);

        var bars = new List<Transform>();
        foreach (Vector2 segment in segments)
        {
            // D-103+: an animated wall of generated wave tiles (leading foam edge on the hit line, water trailing behind it)
            GameObject bar = TrackObject(new GameObject("TidalWave"));
            bar.transform.position = new Vector3(segment.x, yTop, 0f);
            float width = segment.y - segment.x;
            const float tile = 3.4f;
            int tiles = Mathf.Max(1, Mathf.CeilToInt(width / tile));
            float step = width / tiles;
            for (int i = 0; i < tiles; i++)
            {
                GameObject piece = MakeAnimated("TidalWall_Flow", new Vector2(segment.x + step * (i + 0.5f), yTop + 1.5f), step / 3.67f * 1.05f, 9f, -90, bar.transform);
                piece.transform.localPosition = new Vector3(step * (i + 0.5f), 1.5f, 0f);
            }

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
                        WaterSfx(SfxIds.BosswWaveHit, point);
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

    private void PullPlayer(Player player, Rigidbody2D body, Vector2 center, float radius, float speed)
    {
        if (player == null || player.IsDead || body == null)
            return;

        Vector2 toCenter = center - (Vector2)player.transform.position;
        float distance = toCenter.magnitude;
        if (distance < radius && distance > 0.3f)
            body.position += toCenter.normalized * (speed * Time.deltaTime);
    }

    private IEnumerator WhirlBurst(Vector2 center, float radius, Player player)
    {
        WaterSfx(SfxIds.BosswWaveHit, center);
        SkillScreenFX.Shake(0.25f, 0.5f);
        GameObject ring = TrackObject(MakeAnimated("WaterGatherRing_Swirl", center, 0.3f, 14f, -87));
        Sprite[] ringFrames = BossVfx.Frames("Water/WaterGatherRing_Swirl");
        float ringSize = ringFrames != null && ringFrames.Length > 0 ? ringFrames[0].bounds.size.x : 3.67f;
        bool hit = false;
        const float seconds = 0.8f;
        for (float t = 0f; t < seconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            float r = Mathf.Lerp(1.5f, radius + 1.5f, t / seconds);
            if (ring != null)
                ring.transform.localScale = Vector3.one * (r * 2f / ringSize);
            if (!hit && player != null && !player.IsDead)
            {
                float distance = Vector2.Distance((Vector2)player.transform.position + Vector2.up * 0.3f, center);
                if (Mathf.Abs(distance - r) <= 0.9f && !player.IsDashInvulnerable)
                {
                    hit = true;
                    HurtPlayer(player, _definition.whirlJetDamage, center, 6f);
                }
            }

            yield return null;
        }

        if (ring != null)
            Destroy(ring);
    }

    private IEnumerator WhirlpoolSkill()
    {
        yield return DriftTo(_arenaCenter, 6f, 2f);
        Vector2 center = transform.position;
        float warn = Tele(_definition.whirlWarnSeconds);
        float radius = _definition.whirlRadius;

        // D-107: the danger zones are shown FIRST (nothing hides them): the red disc is the pull zone, the outer ring is how far
        // the jets will reach. The whirlpool art only opens once the warning is over.
        warn = Mathf.Max(1.4f, warn);
        Track(BossTelegraph.Circle(center, radius, warn));
        float jetReach = Mathf.Max(radius + 0.5f, _definition.whirlJetLength);
        Track(BossTelegraph.Circle(center, jetReach, warn));
        WaterSfx(SfxIds.BosswWhirlCharge, center);
        PlayClip(BossClipId.Whirl, warn);
        SkillScreenFX.Dim(0.2f, 0.5f);
        SkillScreenFX.Shake(0.1f, warn);
        yield return new WaitForSeconds(warn);

        GameObject whirl = TrackObject(BossVfx.Spawn("Water/WaterWhirlpool_Idle", center, 1f, 10f, true,
            _definition.whirlPullSeconds + _definition.whirlJetSeconds * 2f + 3f, -91));
        Sprite[] frames = BossVfx.Frames("Water/WaterWhirlpool_Idle");
        if (whirl != null && frames != null && frames.Length > 0)
            whirl.transform.localScale = Vector3.one * (radius * 2f / Mathf.Max(0.5f, frames[0].bounds.size.x));

        // pull
        if (UsesWaterSfx)
            _pullLoop = SoundFXManager.StartLoop(SfxIds.BosswWhirlLoop, 0.9f, 1f, 0.3f);
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

        // D-107: the jets are the signature of the move. Lanes are shown first (so the sweep is readable), the jets grow out of the
        // crab, spin up, REVERSE half way (a dodge that works for the first half walks into the second), keep dragging the player in
        // and hit them toward the eye; a final water burst rolls outwards when they die down.
        _pullLoop?.Stop(0.3f);
        _pullLoop = null;
        int jets = Mathf.Max(1, _definition.whirlJetCount + BossDefinition.PerPhase(_definition.whirlExtraJetsByPhase, _phaseIndex, 0));
        float baseAngle = Random.Range(0f, 360f);
        float telegraph = Mathf.Max(0.6f, Tele(_definition.whirlJetTelegraphSeconds));
        for (int i = 0; i < jets; i++)
            Track(BossTelegraph.Line(center, DirectionOf(baseAngle + 360f / jets * i), _definition.whirlJetLength, _definition.whirlJetWidth, telegraph));
        WaterSfx(SfxIds.BosswWhirlCharge, center);
        for (float t = 0f; t < telegraph && _state != BossState.Dead; t += Time.deltaTime)
        {
            PullPlayer(player, body, center, radius, _definition.whirlPullSpeed * _definition.whirlPullDuringJets);
            yield return null;
        }

        WaterSfx(SfxIds.BosswWhirlJet, center);
        var bars = new List<Transform>();
        for (int i = 0; i < jets; i++)
        {
            GameObject bar = TrackObject(new GameObject("WaterJet"));
            bar.transform.position = center;
            // a generated water tentacle (suckers + foam crest): body tiles along the lane and a tip at the far end
            float scale = _definition.whirlJetWidth / 1.4f;
            float pitch = 78f / 48f * scale;
            float tipWidth = 98f / 48f * scale;
            int bodyTiles = Mathf.Max(1, Mathf.FloorToInt((_definition.whirlJetLength - tipWidth) / pitch));
            for (int k = 0; k < bodyTiles; k++)
            {
                GameObject piece = MakeAnimated("TentacleBody_Flow", center, scale, 10f, -88, bar.transform);
                piece.transform.localPosition = new Vector3(pitch * (k + 0.5f), 0f, 0f);
            }

            GameObject tip = MakeAnimated("TentacleTip_Flow", center, scale, 6f, -87, bar.transform);
            tip.transform.localPosition = new Vector3(pitch * bodyTiles + tipWidth * 0.5f, 0f, 0f);

            bars.Add(bar.transform);
        }

        var hitJets = new HashSet<int>();
        float angle = baseAngle;
        float duration = Mathf.Max(2.6f, _definition.whirlJetSeconds * 1.6f);
        float maxSpin = _definition.whirlJetSpinDegreesPerSecond * 1.5f;
        const float growSeconds = 0.5f;
        bool reversed = false;
        float direction = Random.value < 0.5f ? 1f : -1f;
        SkillScreenFX.Shake(0.15f, 0.4f);
        for (float t = 0f; t < duration && _state != BossState.Dead; t += Time.deltaTime)
        {
            if (!reversed && t >= duration * 0.5f)
            {
                reversed = true;
                direction = -direction;
                hitJets.Clear(); // the second half can hit again
                SkillScreenFX.Shake(0.12f, 0.3f);
                WaterSfx(SfxIds.BosswWhirlJet, center);
            }

            float half = reversed ? t - duration * 0.5f : t;
            float spin = Mathf.Lerp(maxSpin * 0.25f, maxSpin, Mathf.Clamp01(half / (duration * 0.45f)));
            angle += direction * spin * Time.deltaTime;
            float lengthNow = _definition.whirlJetLength * Mathf.Clamp01(t / growSeconds);
            PullPlayer(player, body, center, radius, _definition.whirlPullSpeed * _definition.whirlPullDuringJets);
            for (int i = 0; i < jets; i++)
            {
                float jetAngle = angle + 360f / jets * i;
                if (bars[i] != null)
                {
                    bars[i].rotation = Quaternion.Euler(0f, 0f, jetAngle);
                    bars[i].localScale = new Vector3(Mathf.Max(0.05f, lengthNow / _definition.whirlJetLength), 1f, 1f);
                }

                if (player != null && !player.IsDead && !hitJets.Contains(i))
                {
                    Vector2 local = (Vector2)player.transform.position + Vector2.up * 0.3f - center;
                    Vector2 axis = DirectionOf(jetAngle);
                    float along = Vector2.Dot(local, axis);
                    float across = Mathf.Abs(local.x * axis.y - local.y * axis.x);
                    if (along >= 0f && along <= lengthNow && across <= _definition.whirlJetWidth * 0.5f + 0.3f)
                    {
                        hitJets.Add(i);
                        // knock the player INWARD, toward the eye of the whirlpool
                        Vector2 away = (Vector2)player.transform.position - center;
                        HurtPlayer(player, _definition.whirlJetDamage, (Vector2)player.transform.position + away, 5f);
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

        // final burst: a ring of water rolls out of the eye (dash through it or outrun it)
        if (_state != BossState.Dead)
            yield return WhirlBurst(center, radius, player);

        SkillScreenFX.Dim(0f, 0.6f);
        ContinueClip();
        if (whirl != null)
            Destroy(whirl);
    }
}
