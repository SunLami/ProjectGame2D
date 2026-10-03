using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The five Earth-golem skills (EarthBossCombatPlan.md section 4). Every attack shows a
/// <see cref="BossTelegraph"/> first, then applies damage through Player.TakeDamage, which already
/// honours the dash invulnerability frames (D-085).</summary>
public sealed partial class BossController
{
    private IEnumerator SkillRoutine(BossSkillId id)
    {
        switch (id)
        {
            case BossSkillId.Slam: return SlamSkill(false);
            case BossSkillId.SlamDouble: return SlamSkill(true);
            case BossSkillId.StoneRain: return StoneRainSkill();
            case BossSkillId.SpikeLanes: return SpikeLanesSkill();
            case BossSkillId.RocketFist: return RocketFistSkill();
            case BossSkillId.CoreResonance: return CoreResonanceSkill();
            case BossSkillId.ClawClamp: return ClawClampSkill();
            case BossSkillId.BubbleTrap: return BubbleTrapSkill();
            case BossSkillId.SandAmbush: return SandAmbushSkill();
            case BossSkillId.TidalWave: return TidalWaveSkill();
            case BossSkillId.Whirlpool: return WhirlpoolSkill();
            default: return null;
        }
    }

    // ---------------------------------------------------------------- shared helpers

    private float Damage(float multiplier) => _definition.baseDamage * multiplier * _definition.difficultyDamageScale;

    private Vector2 PlayerPosition()
    {
        Player player = GetPlayer();
        return player != null ? (Vector2)player.transform.position : (Vector2)transform.position + Vector2.down * 6f;
    }

    /// <summary>Where the player will roughly be in `seconds` if they keep their current velocity.</summary>
    private Vector2 PredictedPlayerPosition(float seconds)
    {
        Player player = GetPlayer();
        if (player == null)
            return PlayerPosition();

        Vector2 velocity = Vector2.zero;
        if (player.TryGetComponent(out Rigidbody2D body))
            velocity = body.linearVelocity;

        Vector2 offset = velocity * seconds;
        if (offset.magnitude > 6f)
            offset = offset.normalized * 6f;
        return ClampInside((Vector2)player.transform.position + offset, 1f);
    }

    private void HurtPlayer(Player player, float multiplier, Vector2 from, float knockback)
    {
        Vector2 direction = ((Vector2)player.transform.position - from);
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.down;
        player.TakeDamage(Damage(multiplier), direction.normalized, knockback);
    }

    /// <summary>Hurts the player once (per `alreadyHit` set) if inside the circle.</summary>
    private void StrikeCircle(Vector2 center, float radius, float multiplier, float knockback, HashSet<Player> alreadyHit)
    {
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(center, radius))
        {
            Player player = overlap.GetComponentInParent<Player>();
            if (player == null || !alreadyHit.Add(player))
                continue;

            HurtPlayer(player, multiplier, center, knockback);
        }
    }

    private static float AngleOf(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    private static Vector2 DirectionOf(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    // ---------------------------------------------------------------- 1. Crushing Fist

    private IEnumerator SlamSkill(bool doubleSlam)
    {
        yield return SlamOnce(Tele(_definition.slamWindup));
        if (doubleSlam && _state != BossState.Dead)
        {
            yield return new WaitForSeconds(_definition.slamDoubleDelay);
            yield return SlamOnce(Tele(_definition.slamDoubleWindup));
        }
    }

    private IEnumerator SlamOnce(float windup)
    {
        Vector2 target = PlayerPosition();
        BossTelegraphVisual telegraph = Track(BossTelegraph.Circle(target, _definition.slamRadius, windup));
        telegraph.Follow(() =>
        {
            target = PlayerPosition();
            return target;
        });

        SetPose(new Vector2(0f, 0.9f), new Vector2(0.97f, 1.06f), windup * 0.8f);
        PlayClip(BossClipId.Slam, windup);
        yield return new WaitForSeconds(Mathf.Min(_definition.slamLockSeconds, windup * 0.5f));
        telegraph.Lock();
        yield return new WaitForSeconds(Mathf.Max(0f, windup - _definition.slamLockSeconds));

        ContinueClip();
        SetPose(new Vector2(0f, -0.3f), new Vector2(1.06f, 0.94f), 0.08f);
        StrikeCircle(target, _definition.slamRadius, _definition.slamDamage, _definition.slamKnockback, new HashSet<Player>());
        TrackObject(BossVfx.Spawn("Shockwave_Expand", target, _definition.slamRadius / 1.8f, 14f, false, 1.2f, 5));
        TrackObject(BossVfx.Spawn("RockProjectile_Impact", target, 1.8f, 14f, false, 1f, 7));
        SkillScreenFX.Shake(0.22f, 0.3f);
        yield return new WaitForSeconds(0.25f);
        SetPose(Vector2.zero, Vector2.one, 0.3f);
    }

    // ---------------------------------------------------------------- 2. Stone Rain

    private IEnumerator StoneRainSkill()
    {
        int count = BossDefinition.PerPhase(_definition.rainCountByPhase, _phaseIndex, 6);
        float lead = Tele(_definition.rainLead);
        var centers = new List<Vector2>();
        Vector2 playerPosition = PlayerPosition();

        SetPose(new Vector2(0f, 0.8f), new Vector2(0.98f, 1.05f), 0.4f);
        PlayClip(BossClipId.Raise, 0.5f);
        for (int i = 0; i < count && _state != BossState.Dead; i++)
        {
            Vector2 position = i < 2
                ? PredictedPlayerPosition(_definition.rainPredictSeconds * (i == 0 ? 1f : 1.6f))
                : PickRainPosition(playerPosition, centers);
            centers.Add(position);
            StartCoroutine(RainDrop(position, lead));
            yield return new WaitForSeconds(_definition.rainSpacing);
        }

        yield return new WaitForSeconds(lead + 0.35f);
        ContinueClip();
        SetPose(Vector2.zero, Vector2.one, 0.3f);
    }

    private Vector2 PickRainPosition(Vector2 around, List<Vector2> existing)
    {
        Vector2 best = around;
        for (int attempt = 0; attempt < 14; attempt++)
        {
            Vector2 candidate = ClampInside(around + Random.insideUnitCircle * _definition.rainSpreadRadius, 1f);
            bool clear = true;
            foreach (Vector2 other in existing)
            {
                if (Vector2.Distance(other, candidate) < _definition.rainMinSpacing)
                {
                    clear = false;
                    break;
                }
            }

            best = candidate;
            if (clear)
                return candidate;
        }

        return best;
    }

    private IEnumerator RainDrop(Vector2 position, float lead)
    {
        BossTelegraphVisual telegraph = Track(BossTelegraph.Circle(position, _definition.rainRadius, lead));
        float fallTime = Mathf.Min(0.4f, lead * 0.5f);
        yield return new WaitForSeconds(lead - fallTime);

        GameObject rock = TrackObject(BossVfx.Spawn("RockChunk_Hover", position + Vector2.up * 9f, 0.9f, 10f, true, 0f, 8));
        for (float t = 0f; t < fallTime; t += Time.deltaTime)
        {
            if (rock != null)
                rock.transform.position = Vector2.Lerp(position + Vector2.up * 9f, position, (t / fallTime) * (t / fallTime));
            yield return null;
        }

        if (rock != null)
            Destroy(rock);

        StrikeCircle(position, _definition.rainRadius, _definition.rainDamage, _definition.rainKnockback, new HashSet<Player>());
        TrackObject(BossVfx.Spawn("RockProjectile_Impact", position, 1.3f, 14f, false, 1f, 7));
        TrackObject(BossVfx.Spawn("MeteorCrater_Smolder", position, 0.7f, 10f, false, 2.5f, -80));
        SkillScreenFX.Shake(0.08f, 0.15f);
    }

    // ---------------------------------------------------------------- 3. Spike Lanes

    private IEnumerator SpikeLanesSkill()
    {
        int count = BossDefinition.PerPhase(_definition.laneCountByPhase, _phaseIndex, 3);
        Vector2 origin = transform.position;
        float baseAngle = AngleOf(PlayerPosition() - origin);
        float duration = Tele(_definition.laneTelegraph);

        var directions = new List<Vector2>();
        var lengths = new List<float>();
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * _definition.laneStepDegrees;
            Vector2 direction = DirectionOf(baseAngle + offset);
            directions.Add(direction);
            float length = DistanceToBounds(origin, direction, _definition.laneLength);
            lengths.Add(length);
            Track(BossTelegraph.Line(origin, direction, length, _definition.laneWidth, duration));
        }

        SetPose(new Vector2(0f, 0.7f), new Vector2(0.98f, 1.05f), duration * 0.8f);
        PlayClip(BossClipId.Stomp, duration);
        yield return new WaitForSeconds(duration);

        ContinueClip();
        SetPose(new Vector2(0f, -0.3f), new Vector2(1.05f, 0.95f), 0.08f);
        SkillScreenFX.Shake(0.18f, 0.5f);
        var alreadyHit = new HashSet<Player>();
        float longest = 0f;
        for (int i = 0; i < directions.Count; i++)
        {
            StartCoroutine(LaneEruption(origin, directions[i], lengths[i], alreadyHit));
            longest = Mathf.Max(longest, lengths[i]);
        }

        yield return new WaitForSeconds(longest / _definition.laneSpreadSpeed + 0.6f);
        SetPose(Vector2.zero, Vector2.one, 0.3f);
    }

    private IEnumerator LaneEruption(Vector2 origin, Vector2 direction, float length, HashSet<Player> alreadyHit)
    {
        const float segment = 1.5f;
        float interval = segment / _definition.laneSpreadSpeed;
        for (float distance = 2f; distance <= length; distance += segment)
        {
            Vector2 point = origin + direction * distance;
            TrackObject(BossVfx.Spawn("RockSpike_Rise", point, 0.95f, 14f, false, 1.4f, 6));
            StrikeCircle(point, _definition.laneWidth * 0.6f, _definition.laneDamage, _definition.laneKnockback, alreadyHit);
            StartCoroutine(SpikeLinger(point, alreadyHit));
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>A risen spike stays dangerous while it is visible (1.4 s): walking into it still hurts, but only
    /// once per cast (the shared `alreadyHit` set), so a lane never hits twice.</summary>
    private IEnumerator SpikeLinger(Vector2 point, HashSet<Player> alreadyHit)
    {
        const float visibleSeconds = 1.2f;
        for (float t = 0f; t < visibleSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            StrikeCircle(point, _definition.laneWidth * 0.6f, _definition.laneDamage, _definition.laneKnockback, alreadyHit);
            yield return null;
        }
    }

    // ---------------------------------------------------------------- 4. Rocket Fist

    private IEnumerator RocketFistSkill()
    {
        Vector2 origin = transform.position;
        Vector2 direction = (PredictedPlayerPosition(0.3f) - origin).normalized;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.down;

        float windup = Tele(_definition.fistWindup);
        float lineLength = Mathf.Max(4f, DistanceToBounds(origin, direction, _definition.fistLineLength));
        Track(BossTelegraph.Line(origin, direction, lineLength, _definition.fistLineWidth, windup));
        SetPose(-direction * 0.5f + Vector2.up * 0.3f, new Vector2(1.02f, 1.02f), windup * 0.9f);
        PlayClip(BossClipId.Fist, windup);
        yield return new WaitForSeconds(windup);
        ContinueClip();

        // The fist leaves the body.
        var fist = TrackObject(new GameObject("BossRocketFist"));
        fist.transform.position = origin;
        fist.transform.rotation = Quaternion.Euler(0f, 0f, AngleOf(direction));
        // D-096: a stone fist sprite (RockFist_Fly, 96x64 at PPU 32 = 3 units long; scale 1.4 = 4.2 units) instead of a plain rock; falls back to the old rock.
        Sprite[] fistFrames = BossVfx.Frames("RockFist_Fly");
        bool stoneFist = fistFrames != null && fistFrames.Length > 0;
        fist.transform.localScale = Vector3.one * (stoneFist ? 1.4f : 1.2f);
        var renderer = fist.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 8;
        fist.AddComponent<SkillFrameAnimator>().Play(stoneFist ? fistFrames : BossVfx.Frames("RockProjectile_Fly"), 14f, true);

        _fistAway = true;
        SetPose(direction * 0.6f, new Vector2(1.03f, 0.98f), 0.1f);
        SkillScreenFX.Shake(0.15f, 0.2f);

        var hitOut = new HashSet<Player>();
        Vector2 end = origin + direction * lineLength;
        while (fist != null && Vector2.Distance(fist.transform.position, end) > 0.2f)
        {
            fist.transform.position = Vector2.MoveTowards(fist.transform.position, end, _definition.fistSpeed * Time.deltaTime);
            StrikeCircle(fist.transform.position, _definition.fistLineWidth * 0.55f, _definition.fistDamage, _definition.fistKnockback, hitOut);
            yield return null;
        }

        SetPose(Vector2.zero, Vector2.one, 0.3f);
        yield return new WaitForSeconds(_definition.fistHoverSeconds);

        var hitBack = new HashSet<Player>();
        while (fist != null && Vector2.Distance(fist.transform.position, transform.position) > 0.6f)
        {
            Vector2 toBody = (Vector2)transform.position - (Vector2)fist.transform.position;
            fist.transform.rotation = Quaternion.Euler(0f, 0f, AngleOf(toBody));
            fist.transform.position = Vector2.MoveTowards(fist.transform.position, transform.position, _definition.fistReturnSpeed * Time.deltaTime);
            StrikeCircle(fist.transform.position, _definition.fistLineWidth * 0.55f, _definition.fistReturnDamage, _definition.fistKnockback * 0.6f, hitBack);
            yield return null;
        }

        _fistAway = false;
        if (fist != null)
            Destroy(fist);
        TrackObject(BossVfx.Spawn("RockProjectile_Impact", transform.position, 1.4f, 14f, false, 1f, 8));
    }

    // ---------------------------------------------------------------- 5. Core Resonance (ultimate)

    private IEnumerator CoreResonanceSkill()
    {
        yield return DriftTo(_arenaCenter, 6f, 2f);
        Vector2 center = transform.position;

        float charge = Tele(_definition.resonanceChargeSeconds);

        SetPose(new Vector2(0f, 1f), new Vector2(1.08f, 1.08f), charge);
        PlayClip(BossClipId.Resonance, charge);
        GameObject runes = TrackObject(BossVfx.Spawn("EarthRune_Pulse", center, 2.6f, 12f, true,
            charge + BossDefinition.PerPhase(_definition.resonanceBurstVolleysByPhase, _phaseIndex, 4) * _definition.resonanceRingInterval + 1f, -90));
        SkillScreenFX.Dim(0.25f, 0.5f);
        SkillScreenFX.Shake(0.12f, charge);
        yield return new WaitForSeconds(charge);

        // D-095: volleys of rocks flying out 360 degrees (the old expanding rings live on in ResonanceRing, unused).
        int volleys = BossDefinition.PerPhase(_definition.resonanceBurstVolleysByPhase, _phaseIndex, 4);
        int bullets = Mathf.Max(8, BossDefinition.PerPhase(_definition.resonanceBurstBulletsByPhase, _phaseIndex, 20));
        float step = 360f / bullets;
        float baseAngle = Random.Range(0f, 360f);
        for (int i = 0; i < volleys && _state != BossState.Dead; i++)
        {
            float offset = baseAngle + (i % 2 == 0 ? 0f : step * 0.5f) + Random.Range(-step * 0.08f, step * 0.08f);
            StartCoroutine(RockBurstVolley(center, offset, bullets));
            yield return new WaitForSeconds(_definition.resonanceRingInterval);
        }

        SkillScreenFX.Dim(0f, 0.8f);
        // The last volley is still flying; give it time to clear the near area before the boss recovers.
        yield return new WaitForSeconds(1.2f);
        ContinueClip();
        SetPose(Vector2.zero, Vector2.one, 0.3f);
        if (runes != null)
            Destroy(runes);
    }

    /// <summary>One volley: red spokes first, then `count` rocks fly out evenly around `center`. Each rock that
    /// reaches the player hurts once per volley (shared set) and disappears; rocks vanish outside the arena.</summary>
    private IEnumerator RockBurstVolley(Vector2 center, float offsetDegrees, int count)
    {
        float step = 360f / count;
        float lead = Mathf.Max(0.1f, _definition.resonanceBurstTelegraphSeconds);
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = DirectionOf(offsetDegrees + step * i);
            Track(BossTelegraph.Line(center, direction, 7f, 0.22f, lead));
        }

        yield return new WaitForSeconds(lead);
        if (_state == BossState.Dead)
            yield break;

        SkillScreenFX.Shake(0.1f, 0.2f);
        Sprite[] frames = BossVfx.Frames("RockProjectile_Fly");
        var rocks = new Transform[count];
        var directions = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            directions[i] = DirectionOf(offsetDegrees + step * i);
            var rock = TrackObject(new GameObject("BossRockBullet"));
            rock.transform.position = center + directions[i] * 1.5f;
            rock.transform.rotation = Quaternion.Euler(0f, 0f, AngleOf(directions[i]));
            rock.transform.localScale = Vector3.one * 0.6f; // visual diameter ~1.3 units, close to the hit size
            var renderer = rock.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 8;
            rock.AddComponent<SkillFrameAnimator>().Play(frames, 14f, true);
            rocks[i] = rock.transform;
        }

        var hurt = new HashSet<Player>();
        Player player = GetPlayer();
        float hitRadius = _definition.resonanceBurstHitRadius + 0.3f;
        Rect outer = _hasBounds ? new Rect(_bounds.xMin - 4f, _bounds.yMin - 4f, _bounds.width + 8f, _bounds.height + 8f) : new Rect(-60f, -60f, 120f, 120f);
        int alive = count;
        while (alive > 0 && _state != BossState.Dead)
        {
            Vector2 playerPoint = player != null ? (Vector2)player.transform.position + Vector2.up * 0.4f : Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                if (rocks[i] == null)
                    continue;

                Vector2 position = (Vector2)rocks[i].position + directions[i] * (_definition.resonanceBurstSpeed * Time.deltaTime);
                rocks[i].position = position;
                bool gone = !outer.Contains(position);
                if (!gone && player != null && !player.IsDead && Vector2.Distance(position, playerPoint) <= hitRadius)
                {
                    if (hurt.Add(player))
                        HurtPlayer(player, _definition.resonanceBurstDamage, position, _definition.resonanceKnockback);
                    gone = true;
                    TrackObject(BossVfx.Spawn("RockProjectile_Impact", position, 0.8f, 14f, false, 0.6f, 7));
                }

                if (gone)
                {
                    Destroy(rocks[i].gameObject);
                    rocks[i] = null;
                    alive--;
                }
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (rocks[i] != null)
                Destroy(rocks[i].gameObject);
        }
    }

    private IEnumerator ResonanceRing(Vector2 center, float gapAngle, float gapDegrees, float lead)
    {
        yield return new WaitForSeconds(lead);

        var ring = TrackObject(new GameObject("BossResonanceRing"));
        var line = ring.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = new Color(1f, 0.62f, 0.25f, 0.95f);
        line.sortingLayerName = "Default";
        line.sortingOrder = -60;
        line.widthMultiplier = _definition.resonanceRingThickness;
        const int points = 96;
        line.positionCount = points;

        float half = gapDegrees * 0.5f;
        float radius = 1.5f;
        Player player = GetPlayer();
        bool hit = false;
        SkillScreenFX.Shake(0.1f, 0.25f);

        while (radius < _definition.resonanceMaxRadius && _state != BossState.Dead)
        {
            radius += _definition.resonanceRingSpeed * Time.deltaTime;
            float start = gapAngle + half;
            float span = 360f - gapDegrees;
            for (int i = 0; i < points; i++)
                line.SetPosition(i, center + DirectionOf(start + span * i / (points - 1)) * radius);

            if (!hit && player != null && !player.IsDead)
            {
                Vector2 toPlayer = (Vector2)player.transform.position - center;
                float distance = toPlayer.magnitude;
                float inGap = Mathf.Abs(Mathf.DeltaAngle(AngleOf(toPlayer), gapAngle));
                bool onRing = Mathf.Abs(distance - radius) <= _definition.resonanceRingThickness * 0.5f + 0.2f;
                if (onRing && inGap > half)
                {
                    hit = true;
                    HurtPlayer(player, _definition.resonanceDamage, center, _definition.resonanceKnockback);
                }
            }

            yield return null;
        }

        Destroy(ring);
    }
}
