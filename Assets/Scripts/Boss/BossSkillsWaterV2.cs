using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Water crab kit v2 (D-113, WaterBossCombatPlan.md section 11): the new skills (Shell Roll, Totem Crossfire, Summon Crabs) and the
/// building blocks the reworked skills share (lunge, bubble bursts, shock ring, mini vortices). Every number lives in BossDefinition.</summary>
public sealed partial class BossController
{
    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float length = ab.sqrMagnitude;
        float t = length < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, ab) / length);
        return Vector2.Distance(point, a + ab * t);
    }

    private Vector2 PlayerCentre()
    {
        Player player = GetPlayer();
        return player != null ? (Vector2)player.transform.position + Vector2.up * 0.3f : PlayerPosition();
    }

    /// <summary>A short burst of speed toward `target` (claw lunge): the crab commits to where the player WAS when it started.</summary>
    private IEnumerator LungeToward(Vector2 target, float speed, float seconds)
    {
        Vector2 direction = target - (Vector2)transform.position;
        if (direction.sqrMagnitude < 0.01f)
            yield break;
        direction.Normalize();
        SetMoving(true);
        for (float t = 0f; t < seconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            transform.position = ClampInside((Vector2)transform.position + direction * (speed * Time.deltaTime), 3f);
            yield return null;
        }

        SetMoving(false);
    }

    /// <summary>An expanding ring of water: hurts the player once when the ring front passes through them.</summary>
    private IEnumerator ShockRing(Vector2 center, float maxRadius, float damage, float seconds)
    {
        WaterSfx(SfxIds.BosswWaveHit, center);
        GameObject ring = TrackObject(MakeAnimated("WaterGatherRing_Swirl", center, 0.3f, 14f, -87));
        Sprite[] ringFrames = BossVfx.Frames("Water/WaterGatherRing_Swirl");
        float ringSize = ringFrames != null && ringFrames.Length > 0 ? ringFrames[0].bounds.size.x : 3.67f;
        Player player = GetPlayer();
        bool hit = false;
        for (float t = 0f; t < seconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            float r = Mathf.Lerp(1.2f, maxRadius, t / seconds);
            if (ring != null)
                ring.transform.localScale = Vector3.one * (r * 2f / ringSize);
            if (!hit && player != null && !player.IsDead)
            {
                float distance = Vector2.Distance((Vector2)player.transform.position + Vector2.up * 0.3f, center);
                if (Mathf.Abs(distance - r) <= 0.8f && !player.IsDashInvulnerable)
                {
                    hit = true;
                    HurtPlayer(player, damage, center, 6f);
                }
            }

            yield return null;
        }

        if (ring != null)
            Destroy(ring);
    }

    // ---------------------------------------------------------------- bubble bursts (Bubble Trap, part 2)

    /// <summary>Stationary bubbles settle around the player, swell, and pop into a cross of water jets (the cross of every other bubble is
    /// turned 45 degrees). A bubble that pops on the player roots them for a moment. Run it next to the bubble rings.</summary>
    private IEnumerator BubbleBurstField()
    {
        int count = Mathf.Max(1, BossDefinition.PerPhase(_definition.burstBubbleCountByPhase, _phaseIndex, 3));
        float radius = _definition.burstRadius;
        float fuse = Mathf.Max(1.2f, Tele(_definition.burstFuseSeconds));
        const float jetWarn = 0.8f;
        var spots = new List<Vector2>();
        for (int i = 0; i < count; i++)
        {
            Vector2 spot = PredictedPlayerPosition(0.4f);
            for (int attempt = 0; attempt < 12; attempt++)
            {
                spot = i == 0 ? PredictedPlayerPosition(0.5f) : ClampInside(PlayerPosition() + Random.insideUnitCircle * 9f, 2f);
                bool clear = true;
                foreach (Vector2 other in spots)
                    clear &= Vector2.Distance(other, spot) > radius * 2f;
                if (clear)
                    break;
            }

            spots.Add(spot);
        }

        Sprite[] frames = BossVfx.Frames("Water/WaterOrb_Wobble");
        float frameSize = frames != null && frames.Length > 0 ? Mathf.Max(0.5f, frames[0].bounds.size.x) : 3.67f;
        var orbs = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            Track(BossTelegraph.Circle(spots[i], radius, fuse));
            GameObject orb = TrackObject(MakeAnimated("WaterOrb_Wobble", spots[i], 0.2f, 8f, 9));
            orbs.Add(orb);
        }

        WaterSfx(SfxIds.BosswBubbleSpit, transform.position);
        for (float t = 0f; t < fuse - jetWarn && _state != BossState.Dead; t += Time.deltaTime)
        {
            float swell = Mathf.Lerp(0.3f, 1f, t / Mathf.Max(0.01f, fuse - jetWarn)) * radius * 0.9f * 2f / frameSize;
            foreach (GameObject orb in orbs)
            {
                if (orb != null)
                    orb.transform.localScale = Vector3.one * swell;
            }

            yield return null;
        }

        // the jets are shown just before the pop: a cross per bubble
        for (int i = 0; i < count; i++)
        {
            float baseAngle = i % 2 == 0 ? 0f : 45f;
            for (int k = 0; k < 4; k++)
                Track(BossTelegraph.Line(spots[i], DirectionOf(baseAngle + 90f * k), _definition.burstJetLength, _definition.burstJetWidth, jetWarn, true));
        }

        yield return new WaitForSeconds(jetWarn);

        Player player = GetPlayer();
        bool hitCircle = false;
        bool hitJet = false;
        for (int i = 0; i < count; i++)
        {
            Vector2 spot = spots[i];
            WaterSfx(SfxIds.BosswBubblePop, spot);
            TrackObject(BossVfx.Spawn("Water/WaterDropletProjectile_Impact", spot, 1.8f, 14f, false, 0.8f, 10));
            float baseAngle = i % 2 == 0 ? 0f : 45f;
            for (int k = 0; k < 4; k++)
            {
                Vector2 direction = DirectionOf(baseAngle + 90f * k);
                for (int step = 1; step <= 3; step++)
                    TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", spot + direction * (_definition.burstJetLength * step / 3.2f), 1.1f, 16f, false, 0.7f, 9));
            }

            if (player != null && !player.IsDead && !player.IsDashInvulnerable)
            {
                Vector2 point = PlayerCentre();
                if (!hitCircle && Vector2.Distance(point, spot) <= radius)
                {
                    hitCircle = true;
                    HurtPlayer(player, _definition.burstDamage, spot, 4f);
                    player.ApplyRoot(_definition.burstRootSeconds);
                }
                else if (!hitJet)
                {
                    for (int k = 0; k < 4 && !hitJet; k++)
                    {
                        Vector2 end = spot + DirectionOf(baseAngle + 90f * k) * _definition.burstJetLength;
                        if (DistanceToSegment(point, spot, end) <= _definition.burstJetWidth * 0.5f + 0.35f)
                        {
                            hitJet = true;
                            HurtPlayer(player, _definition.burstJetDamage, spot, 5f);
                        }
                    }
                }
            }
        }

        SkillScreenFX.Shake(0.14f, 0.25f);
        foreach (GameObject orb in orbs)
        {
            if (orb != null)
                Destroy(orb);
        }
    }

    // ---------------------------------------------------------------- 6. Shell Roll

    private IEnumerator ShellRollSkill()
    {
        int legs = Mathf.Max(1, BossDefinition.PerPhase(_definition.rollLegsByPhase, _phaseIndex, 3));
        float radius = _definition.rollHitRadius;
        for (int leg = 0; leg < legs && _state != BossState.Dead; leg++)
        {
            Vector2 origin = transform.position;
            // the first roll leads the player, the next ones are re-aimed at wherever the player dodged to
            Vector2 aim = (leg == 0 ? PredictedPlayerPosition(0.35f) : PlayerPosition()) - origin;
            if (aim.sqrMagnitude < 0.25f)
                aim = Vector2.down;
            Vector2 direction = aim.normalized;
            float length = Mathf.Clamp(aim.magnitude + 9f, 12f, 26f);
            float windup = Mathf.Max(0.45f, Tele(_definition.rollWindup) * (leg == 0 ? 1f : 0.75f));
            Track(BossTelegraph.Line(origin, direction, length, radius * 1.8f, windup, true));
            WaterSfx(SfxIds.BosswClawWindup, origin);
            PlayClip(BossClipId.Whirl, windup);
            yield return new WaitForSeconds(windup);

            ContinueClip();
            WaterSfx(SfxIds.BosswClawSnap, origin);
            Player player = GetPlayer();
            bool hit = false;
            float travelled = 0f;
            float dust = 0f;
            while (travelled < length && _state != BossState.Dead)
            {
                float step = _definition.rollSpeed * Time.deltaTime;
                Vector2 current = transform.position;
                Vector2 next = ClampInside(current + direction * step, 2f);
                transform.position = next;
                travelled += step;
                dust -= Time.deltaTime;
                if (dust <= 0f)
                {
                    dust = 0.12f;
                    TrackObject(BossVfx.Spawn("Water/SandBurst_Hit", next, 0.7f, 14f, false, 0.6f, 5));
                }

                if (!hit && player != null && !player.IsDead && Vector2.Distance(PlayerCentre(), next) <= radius + 0.3f)
                {
                    hit = true;
                    HurtPlayer(player, _definition.rollDamage, next, 7f);
                }

                if ((next - current).magnitude < step * 0.3f)
                    break; // hit the arena wall

                yield return null;
            }

            SkillScreenFX.Shake(0.16f, 0.25f);
            TrackObject(BossVfx.Spawn("Water/SandBurst_Hit", transform.position, 1.3f, 14f, false, 0.8f, 8));
            yield return new WaitForSeconds(0.15f);
        }

        // dizzy after the roll: the punish window the player earns by surviving it
        if (_state != BossState.Dead)
        {
            ApplyVulnerability(_definition.rollDizzyDamageTaken, _definition.rollDizzySeconds);
            PlayClip(BossClipId.Recovery, 0.4f);
            yield return new WaitForSeconds(_definition.rollDizzySeconds);
            ContinueClip();
        }
    }

    // ---------------------------------------------------------------- 7. Totem Crossfire

    private List<Vector2> TotemEyes()
    {
        var eyes = new List<Vector2>();
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (candidate.name == "EyePoint" && candidate.parent != null && candidate.parent.name == "CoralTotem")
                eyes.Add(candidate.position);
        }

        if (eyes.Count == 0)
        {
            Rect arena = ArenaRect();
            eyes.Add(new Vector2(arena.xMin + 5f, arena.yMax - 4f));
            eyes.Add(new Vector2(arena.xMax - 5f, arena.yMax - 4f));
            eyes.Add(new Vector2(arena.xMin + 5f, arena.yMin + 4f));
            eyes.Add(new Vector2(arena.xMax - 5f, arena.yMin + 4f));
        }

        return eyes;
    }

    private IEnumerator TotemCrossfireSkill()
    {
        List<Vector2> eyes = TotemEyes();
        int count = eyes.Count;
        float length = _definition.totemBeamLength;
        float width = _definition.totemBeamWidth;
        float warn = Mathf.Max(0.8f, Tele(_definition.totemBeamWarnSeconds));
        var angles = new float[count];
        var spins = new float[count];
        for (int i = 0; i < count; i++)
        {
            spins[i] = (i % 2 == 0 ? 1f : -1f) * _definition.totemBeamSpinDegrees;
            // start behind the player's position (against the spin) so the sweep crosses where they stand: keep moving or dash through
            angles[i] = AngleOf(PlayerPosition() - eyes[i]) - Mathf.Sign(spins[i]) * Random.Range(20f, 50f);
            Track(BossTelegraph.Line(eyes[i], DirectionOf(angles[i]), length, width, warn));
            TrackObject(BossVfx.Spawn("Water/WaterRainSplash_Hit", eyes[i], 1.2f, 14f, false, warn, 12));
        }

        WaterSfx(SfxIds.BosswWhirlCharge, transform.position);
        PlayClip(BossClipId.Resonance, warn);
        yield return new WaitForSeconds(warn);
        ContinueClip();

        var beams = new List<Transform>();
        var glows = new List<Transform>();
        for (int i = 0; i < count; i++)
        {
            GameObject beam = TrackObject(MakeBar("TotemBeam", new Color(0.35f, 0.85f, 1f, 0.9f), 12));
            beam.transform.position = eyes[i];
            beams.Add(beam.transform);
            GameObject glow = TrackObject(MakeBar("TotemBeamGlow", new Color(0.7f, 0.95f, 1f, 0.3f), 11));
            glow.transform.position = eyes[i];
            glows.Add(glow.transform);
        }

        WaterSfx(SfxIds.BosswWhirlJet, transform.position);
        Player player = GetPlayer();
        var cooldown = new float[count];
        float seconds = _definition.totemBeamSeconds;
        for (float t = 0f; t < seconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            float fadeIn = Mathf.Clamp01(t / 0.25f);
            for (int i = 0; i < count; i++)
            {
                angles[i] += spins[i] * Time.deltaTime;
                Quaternion rotation = Quaternion.Euler(0f, 0f, angles[i]);
                beams[i].rotation = rotation;
                beams[i].localScale = new Vector3(length, width * 0.6f * fadeIn, 1f);
                glows[i].rotation = rotation;
                glows[i].localScale = new Vector3(length, width * 1.6f * fadeIn, 1f);
                cooldown[i] -= Time.deltaTime;
                if (player != null && !player.IsDead && cooldown[i] <= 0f && fadeIn > 0.9f)
                {
                    Vector2 end = eyes[i] + DirectionOf(angles[i]) * length;
                    if (DistanceToSegment(PlayerCentre(), eyes[i], end) <= width * 0.5f + 0.35f)
                    {
                        cooldown[i] = 0.6f;
                        HurtPlayer(player, _definition.totemBeamDamage, eyes[i], 4f);
                    }
                }
            }

            yield return null;
        }

        foreach (Transform beam in beams)
        {
            if (beam != null)
                Destroy(beam.gameObject);
        }

        foreach (Transform glow in glows)
        {
            if (glow != null)
                Destroy(glow.gameObject);
        }
    }

    // ---------------------------------------------------------------- 8. Summon Crabs

    private IEnumerator SummonCrabsSkill()
    {
        int count = Mathf.Max(1, BossDefinition.PerPhase(_definition.crabCountByPhase, _phaseIndex, 4));
        PlayClip(BossClipId.Spit, 0.6f);
        WaterSfx(SfxIds.BosswBubbleSpit, transform.position);
        yield return new WaitForSeconds(0.6f);
        ContinueClip();

        float scale = _body != null ? Mathf.Max(0.08f, _body.transform.lossyScale.x * 0.17f) : 0.4f;
        var crabs = new List<BossMinionCrab>();
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            Vector2 spot = ClampInside((Vector2)transform.position + DirectionOf(offset + 360f / count * i) * 3f, 1f);
            TrackObject(BossVfx.Spawn("Water/SandBurst_Hit", spot, 0.9f, 14f, false, 0.6f, 8));
            crabs.Add(BossMinionCrab.Spawn(spot, _definition, _body != null ? _body.sprite : null, scale, _effectsRoot, Damage(_definition.crabBlastDamage)));
        }

        float guard = Time.time + _definition.crabLifeSeconds + _definition.crabFuseSeconds + 3f;
        bool anyAlive = true;
        while (anyAlive && Time.time < guard && _state != BossState.Dead)
        {
            anyAlive = false;
            foreach (BossMinionCrab crab in crabs)
                anyAlive |= crab != null && !crab.Finished;
            yield return null;
        }

        bool allKilled = true;
        foreach (BossMinionCrab crab in crabs)
            allKilled &= crab != null && crab.Killed;
        if (allKilled && _state != BossState.Dead)
        {
            // every little crab was cut down before it blew up: the mother crab is left exposed
            ApplyVulnerability(_definition.crabWeakMultiplier, _definition.crabWeakSeconds);
            DamageNumber.Spawn((Vector2)transform.position + Vector2.up * 2.6f, "WEAK!", new Color(1f, 0.82f, 0.25f), 1.3f);
            SkillScreenFX.Flash(new Color(1f, 0.85f, 0.3f), 0.1f, 0.25f);
        }

        yield return new WaitForSeconds(0.3f);
    }

    // ---------------------------------------------------------------- Whirlpool: mini vortices

    private IEnumerator MiniWhirls(Vector2 center, float radius)
    {
        int count = Mathf.Max(0, BossDefinition.PerPhase(_definition.whirlMiniCountByPhase, _phaseIndex, 2));
        if (count == 0)
            yield break;

        float r = _definition.whirlMiniRadius;
        var spots = new List<Vector2>();
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f);
            spots.Add(ClampInside(center + DirectionOf(angle) * Random.Range(radius * 0.55f, radius + 3f), 2f));
            Track(BossTelegraph.Circle(spots[i], r, 0.9f));
        }

        yield return new WaitForSeconds(0.9f);

        Sprite[] frames = BossVfx.Frames("Water/WaterWhirlpool_Idle");
        float frameSize = frames != null && frames.Length > 0 ? Mathf.Max(0.5f, frames[0].bounds.size.x) : 3.67f;
        var visuals = new List<GameObject>();
        foreach (Vector2 spot in spots)
            visuals.Add(TrackObject(BossVfx.Spawn("Water/WaterWhirlpool_Idle", spot, r * 2f / frameSize, 10f, true, _definition.whirlMiniSeconds + 0.5f, -90)));

        Player player = GetPlayer();
        Rigidbody2D body = BodyOf(player);
        float tick = 0f;
        for (float t = 0f; t < _definition.whirlMiniSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            tick -= Time.deltaTime;
            foreach (Vector2 spot in spots)
            {
                if (player == null || player.IsDead || body == null)
                    break;
                PullPlayer(player, body, spot, r * 1.6f, _definition.whirlMiniPull);
                if (tick <= 0f && Vector2.Distance(PlayerCentre(), spot) <= r)
                {
                    tick = 0.6f;
                    HurtPlayer(player, _definition.whirlMiniDamage, spot, 2f);
                }
            }

            yield return null;
        }

        foreach (GameObject visual in visuals)
        {
            if (visual != null)
                Destroy(visual);
        }
    }
}

/// <summary>A small crab the Water boss summons (Summon Crabs, D-113). Runs at the player and, after a short blinking fuse, blows up for damage;
/// cutting it down first (any player attack) removes the threat. Two colliders like the bubbles: a trigger for attacks and a solid one that
/// excludes every layer.</summary>
public sealed class BossMinionCrab : MonoBehaviour, IDamageable
{
    private float _speed;
    private float _health;
    private float _fuse;
    private float _fuseLeft = -1f;
    private float _life;
    private float _radius;
    private float _damage;
    private float _baseScale;
    private SpriteRenderer _renderer;
    private Player _player;
    private float _seed;

    public bool Killed { get; private set; }
    public bool Exploded { get; private set; }
    public bool Finished => Killed || Exploded;
    public bool IsDead => Finished;

    public static BossMinionCrab Spawn(Vector2 position, BossDefinition definition, Sprite sprite, float scale, Transform parent, float damage)
    {
        var go = new GameObject("BossMinionCrab");
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 8;
        renderer.sprite = sprite;
        renderer.color = new Color(0.8f, 1f, 1f, 1f);
        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.9f / Mathf.Max(0.01f, scale);
        var solid = go.AddComponent<CircleCollider2D>();
        solid.radius = trigger.radius;
        solid.excludeLayers = ~0;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        var crab = go.AddComponent<BossMinionCrab>();
        crab._renderer = renderer;
        crab._speed = definition.crabSpeed;
        crab._health = definition.crabHealth;
        crab._fuse = definition.crabFuseSeconds;
        crab._life = definition.crabLifeSeconds;
        crab._radius = definition.crabBlastRadius;
        crab._damage = damage;
        crab._baseScale = scale;
        crab._seed = Random.value * 10f;
        return crab;
    }

    private void Update()
    {
        if (Finished)
            return;

        if (_player == null)
            _player = FindAnyObjectByType<Player>();
        if (_player == null || _player.IsDead)
            return;

        _life -= Time.deltaTime;
        Vector2 toPlayer = (Vector2)_player.transform.position - (Vector2)transform.position;
        if (_fuseLeft < 0f)
        {
            // scuttle: a small wobble so the little crabs do not slide
            transform.position += (Vector3)(toPlayer.normalized * (_speed * Time.deltaTime));
            float wobble = 1f + 0.08f * Mathf.Sin((Time.time + _seed) * 18f);
            transform.localScale = new Vector3(_baseScale * wobble, _baseScale / wobble, 1f);
            if (toPlayer.magnitude <= 1.3f || _life <= 0f)
                StartFuse();
        }
        else
        {
            _fuseLeft -= Time.deltaTime;
            bool red = Mathf.FloorToInt(Time.time * 14f) % 2 == 0;
            _renderer.color = red ? new Color(1f, 0.3f, 0.25f) : Color.white;
            if (_fuseLeft <= 0f)
                Explode();
        }
    }

    private void StartFuse()
    {
        _fuseLeft = _fuse;
        BossTelegraph.Circle(transform.position, _radius, _fuse);
    }

    private void Explode()
    {
        Exploded = true;
        SoundFXManager.PlaySfxAt(SfxIds.BosswBubblePop, transform.position);
        BossVfx.Spawn("Water/WaterRainSplash_Hit", transform.position, _radius * 0.8f, 14f, false, 0.9f, 10);
        if (_player != null && !_player.IsDead && !_player.IsDashInvulnerable
            && Vector2.Distance((Vector2)_player.transform.position + Vector2.up * 0.3f, transform.position) <= _radius)
            _player.TakeDamage(_damage, ((Vector2)_player.transform.position - (Vector2)transform.position).normalized, 6f);
        Destroy(gameObject);
    }

    /// <summary>Called by the player's attacks.</summary>
    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        if (Finished)
            return;

        _health -= damage;
        BossVfx.Spawn("Water/WaterDropletProjectile_Impact", transform.position, 0.8f, 14f, false, 0.6f, 10);
        if (_health > 0f)
            return;

        Killed = true;
        SoundFXManager.PlaySfxAt(SfxIds.BosswBubblePop, transform.position);
        BossVfx.Spawn("Water/SandBurst_Hit", transform.position, 1f, 14f, false, 0.7f, 10);
        Destroy(gameObject);
    }
}
