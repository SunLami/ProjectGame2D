using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The six Wind-owl skills (WindBossCombatPlan.md section 4, D-111). Same rules as Earth/Water: a telegraph first, then damage
/// through Player.TakeDamage (dash i-frames respected). Pushes that slam the player into the platform edge or an obstacle add an
/// impact hit (<see cref="BossDefinition.windImpactDamage"/>); there is no falling off the platform (D-111). Own partial file so
/// the other bosses stay untouched.</summary>
public sealed partial class BossController
{
    private bool _airborne;
    private static readonly RaycastHit2D[] ImpactHits = new RaycastHit2D[6];

    private readonly List<WindHazard> _castHazards = new List<WindHazard>();

    /// <summary>Holds the owl in its cast until the projectiles of this skill are gone (or `maxSeconds`), so Recovery never starts while its own attack is still flying.</summary>
    private IEnumerator WaitCastHazards(float maxSeconds)
    {
        for (float t = 0f; t < maxSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            _castHazards.RemoveAll(h => h == null);
            if (_castHazards.Count == 0)
                break;
            yield return null;
        }

        _castHazards.Clear();
    }

    private bool UsesWindOwl => _definition != null && _definition.bossId == "boss.wind_owl";

    private static Sprite[] WindFrames(string set) => BossVfx.Frames("Wind/" + set);

    private static float FitScale(Sprite[] frames, float worldWidth)
    {
        return frames != null && frames.Length > 0 && frames[0] != null ? worldWidth / Mathf.Max(0.1f, frames[0].bounds.size.x) : 1f;
    }

    private void StopClip()
    {
        if (_clipPlayer != null)
            _clipPlayer.Stop();
    }

    private void BeginWindFight()
    {
        if (UsesWindOwl && ArenaWind.Instance != null)
            ArenaWind.Instance.BeginFight(_definition);
    }

    private void EndWindFight()
    {
        if (UsesWindOwl && ArenaWind.Instance != null)
            ArenaWind.Instance.EndFight();
    }

    // ---------------------------------------------------------------- shared: pushes with edge impact

    private static bool BlockedByWorld(Rigidbody2D body, Vector2 direction, float distance)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        int count = body.Cast(direction, filter, ImpactHits, distance + 0.02f);
        for (int i = 0; i < count; i++)
        {
            Collider2D other = ImpactHits[i].collider;
            if (other != null && other.GetComponentInParent<Player>() == null && other.GetComponentInParent<BossController>() == null)
                return true;
        }

        return false;
    }

    private void ImpactHit(Player player, Vector2 direction)
    {
        HurtPlayer(player, _definition.windImpactDamage, (Vector2)player.transform.position - direction, 0f);
        SkillScreenFX.Shake(0.18f, 0.25f);
        TrackObject(BossVfx.Spawn("Wind/WindImpact_Burst", (Vector2)player.transform.position + direction * 0.5f, 0.5f, 16f, false, 0.6f, 9));
    }

    /// <summary>Shoves the player `distance` along `direction` over `seconds`; slamming into the edge or an obstacle hurts.</summary>
    private IEnumerator KnockWithImpact(Player player, Vector2 direction, float distance, float seconds)
    {
        Rigidbody2D body = BodyOf(player);
        if (body == null || player == null || player.IsDead)
            yield break;

        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
        float speed = distance / Mathf.Max(0.05f, seconds);
        for (float t = 0f; t < seconds && !player.IsDead; t += Time.deltaTime)
        {
            float step = speed * Time.deltaTime;
            if (BlockedByWorld(body, direction, step))
            {
                ImpactHit(player, direction);
                yield break;
            }

            body.position += direction * step;
            yield return null;
        }
    }

    /// <summary>Two bursts of wind at the wing tips when a cast is released.</summary>
    private void WingBursts(Vector2 origin)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            TrackObject(BossVfx.Spawn("Wind/WindImpact_Burst", origin + new Vector2(side * 3.2f, 0.9f), 0.8f, 18f, false, 0.6f, 9));
            TrackObject(BossVfx.Spawn("Wind/WindGustPuff_Dissipate", origin + new Vector2(side * 3.6f, 0.4f), 0.5f, 14f, false, 0.7f, 8));
        }
    }

    private bool PlayerInCircle(Vector2 center, float radius)
    {
        Player player = GetPlayer();
        return player != null && !player.IsDead && Vector2.Distance((Vector2)player.transform.position + Vector2.up * 0.3f, center) <= radius + 0.3f;
    }

    // ---------------------------------------------------------------- 1. Feather Volley

    private IEnumerator FeatherVolleySkill()
    {
        int count = Mathf.Max(3, BossDefinition.PerPhase(_definition.featherCountByPhase, _phaseIndex, 9));
        bool second = BossDefinition.PerPhase(_definition.featherSecondFanByPhase, _phaseIndex, false);
        bool spiral = BossDefinition.PerPhase(_definition.featherSpiralByPhase, _phaseIndex, false);
        float windup = Tele(_definition.featherWindup);
        float fan = _definition.featherFanDegrees;
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.4f;
        float baseAngle = AngleOf(PredictedPlayerPosition(0.3f) - origin);

        // telegraph: a fan of lanes (a ring for the phase 3 spiral)
        if (spiral)
            Track(BossTelegraph.Circle(origin, 4.5f, windup));
        else
        {
            ShowFan(origin, baseAngle, fan, 16f, windup);
            if (second)
                ShowFan(origin, baseAngle + 28f, fan * 0.55f, 16f, windup);
        }

        SetPose(new Vector2(0f, 0.5f), new Vector2(1.04f, 1.04f), windup * 0.8f);
        OwlSfx(SfxIds.BossowlVolleyWindup, origin);
        PlayClip(BossClipId.Volley, windup);
        yield return new WaitForSeconds(windup);

        ContinueClip();
        SkillScreenFX.Shake(0.1f, 0.2f);
        OwlSfx(SfxIds.BossowlVolleyFling, origin);
        WingBursts(origin);
        Sprite[] frames = WindFrames("WindFeather_Flutter");
        if (spiral)
        {
            for (int i = 0; i < count && _state != BossState.Dead; i++)
            {
                FireFeather(frames, origin, baseAngle + i * 27f, 0.85f);
                yield return new WaitForSeconds(0.07f);
            }
        }
        else
        {
            int primary = second ? Mathf.CeilToInt(count * 0.6f) : count;
            for (int i = 0; i < primary; i++)
                FireFeather(frames, origin, baseAngle + Mathf.Lerp(-fan * 0.5f, fan * 0.5f, primary == 1 ? 0.5f : (float)i / (primary - 1)), 1f);
            if (second)
            {
                int extra = count - primary;
                for (int i = 0; i < extra; i++)
                    FireFeather(frames, origin, baseAngle + 28f + Mathf.Lerp(-fan * 0.275f, fan * 0.275f, extra == 1 ? 0.5f : (float)i / (extra - 1)), 0.9f);
            }
        }

        yield return WaitCastHazards(1.4f);
        SetPose(Vector2.zero, Vector2.one, 0.3f);
    }

    private void FireFeather(Sprite[] frames, Vector2 origin, float angle, float speedScale)
    {
        Vector2 velocity = DirectionOf(angle) * (_definition.featherSpeed * speedScale);
        float homing = _definition.featherHomingDegrees;
        Rect arena = ArenaRect();
        WindHazard feather = WindHazard.Spawn(frames, origin + DirectionOf(angle) * 1.8f, 0.42f, 14f, 9, _definition.featherHitRadius,
            _definition.featherLifeSeconds,
            (age, position) =>
            {
                if (age < 1f && homing > 0f)
                {
                    Vector2 to = PlayerPosition() - position;
                    float turn = Mathf.Clamp(Mathf.DeltaAngle(AngleOf(velocity), AngleOf(to)), -homing * Time.deltaTime, homing * Time.deltaTime);
                    velocity = Quaternion.Euler(0f, 0f, turn) * velocity;
                }

                Vector2 next = position + velocity * Time.deltaTime;
                return arena.Contains(next) || age < 0.5f ? next : position;
            },
            (player, position) =>
            {
                OwlSfx(SfxIds.BossowlFeatherHit, position);
                HurtPlayer(player, _definition.featherDamage, position, 3f);
            }, true, true, null, 0.8f,
            new WindHazard.Look
            {
                popSeconds = 0.12f, fadeSeconds = 0.3f, wobbleAmplitude = 0.22f, wobbleHertz = 4.5f,
                trail = true, trailColor = new Color(0.9f, 1f, 0.96f, 0.55f), trailWidth = 0.2f, trailSeconds = 0.35f,
            });
        TrackObject(feather.gameObject);
        _castHazards.Add(feather);
    }

    // ---------------------------------------------------------------- 2. Talon Dive

    private IEnumerator TalonDiveSkill()
    {
        int strikes = Mathf.Max(1, BossDefinition.PerPhase(_definition.diveTargetsByPhase, _phaseIndex, 1));
        float perch = BossDefinition.PerPhase(_definition.divePerchByPhase, _phaseIndex, 2f);

        // fly off (untouchable, out of the picture)
        OwlSfx(SfxIds.BossowlTakeoff, transform.position);
        PlayClip(BossClipId.TakeOff);
        SetPose(new Vector2(0f, 1.2f), new Vector2(1.12f, 1.12f), _definition.diveTakeoffSeconds);
        yield return new WaitForSeconds(_definition.diveTakeoffSeconds);
        _airborne = true;
        SetBodyVisible(false);
        SetPose(Vector2.zero, Vector2.one, 0.01f);

        for (int i = 0; i < strikes && _state != BossState.Dead; i++)
        {
            float shadowSeconds = Mathf.Max(0.8f, Tele(_definition.diveShadowSeconds));
            Vector2 target = PredictedPlayerPosition(0.2f);
            OwlSfx(SfxIds.BossowlDiveWarning, target);
            BossTelegraphVisual telegraph = Track(BossTelegraph.Circle(target, _definition.diveRadius, shadowSeconds));
            telegraph.Follow(() =>
            {
                target = PredictedPlayerPosition(0.15f);
                return target;
            });

            // the owl's shadow grows over the target
            var shadow = new GameObject("OwlShadow");
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = BossTelegraph.CircleSprite;
            shadowRenderer.sortingLayerName = "Default";
            shadowRenderer.sortingOrder = -60;
            TrackObject(shadow);

            float lock0 = Mathf.Min(_definition.diveLockSeconds, shadowSeconds * 0.5f);
            bool locked = false;
            for (float t = 0f; t < shadowSeconds && _state != BossState.Dead; t += Time.deltaTime)
            {
                if (!locked && t >= shadowSeconds - lock0)
                {
                    locked = true;
                    telegraph.Lock();
                }

                float k = t / shadowSeconds;
                shadow.transform.position = target;
                shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, _definition.diveRadius * 1.9f, k * k);
                shadowRenderer.color = new Color(0f, 0f, 0.05f, Mathf.Lerp(0.1f, 0.5f, k));
                yield return null;
            }

            if (shadow != null)
                Destroy(shadow);

            // dive: appear on the target, big, and slam down
            transform.position = ClampInside(target, 1.5f);
            _airborne = false;
            SetBodyVisible(true);
            SetPose(new Vector2(0f, 2.2f), new Vector2(1.6f, 1.6f), 0.01f);
            PlayClip(BossClipId.Dive);
            SetPose(Vector2.zero, Vector2.one, 0.16f);
            yield return new WaitForSeconds(0.16f);

            SkillScreenFX.Shake(0.3f, 0.35f);
            OwlSfx(SfxIds.BossowlDiveHit, target);
            TrackObject(BossVfx.Spawn("Wind/WindShockwave_Expand", target, _definition.diveRadius / 1.7f, 14f, false, 1.1f, 6));
            TrackObject(BossVfx.Spawn("Wind/WindImpact_Burst", target, 1.5f, 14f, false, 0.8f, 8));
            Player player = GetPlayer();
            if (player != null && !player.IsDead && PlayerInCircle(target, _definition.diveRadius) && !player.IsDashInvulnerable)
            {
                Vector2 away = (Vector2)player.transform.position - target;
                HurtPlayer(player, _definition.diveDamage, target, 0f);
                StartCoroutine(KnockWithImpact(player, away, _definition.diveKnockback, 0.25f));
            }

            if (i < strikes - 1)
            {
                // straight back up for the next strike
                yield return new WaitForSeconds(0.35f);
                OwlSfx(SfxIds.BossowlTakeoff, transform.position);
                PlayClip(BossClipId.TakeOff);
                yield return new WaitForSeconds(0.45f);
                _airborne = true;
                SetBodyVisible(false);
            }
        }

        // perched and winded: the counter-attack window
        ApplyVulnerability(_definition.recoveryDamageTaken, perch);
        PlayClip(BossClipId.Recovery, 0.4f);
        SetPose(new Vector2(0f, -0.4f), new Vector2(1.03f, 0.94f), 0.25f);
        yield return new WaitForSeconds(perch);
        ContinueClip();
        SetPose(Vector2.zero, Vector2.one, 0.3f);
    }

    // ---------------------------------------------------------------- 3. Cyclones

    private IEnumerator CyclonesSkill()
    {
        int count = Mathf.Max(1, BossDefinition.PerPhase(_definition.cycloneCountByPhase, _phaseIndex, 2));
        float warn = Mathf.Max(0.6f, Tele(_definition.cycloneWarnSeconds));
        var points = new List<Vector2>();
        Vector2 player = PlayerPosition();
        for (int attempt = 0; attempt < 40 && points.Count < count; attempt++)
        {
            Vector2 candidate = ClampInside(player + Random.insideUnitCircle.normalized * Random.Range(5f, 10f), 2f);
            bool clear = Vector2.Distance(candidate, player) > 3.5f;
            foreach (Vector2 other in points)
                clear &= Vector2.Distance(other, candidate) > 5f;
            if (clear)
                points.Add(candidate);
        }

        foreach (Vector2 point in points)
            Track(BossTelegraph.Circle(point, 1.6f, warn));

        SetPose(new Vector2(0f, 0.4f), new Vector2(1.03f, 1.03f), warn * 0.6f);
        OwlSfx(SfxIds.BossowlCycloneCast, transform.position);
        PlayClip(BossClipId.Flap);
        yield return new WaitForSeconds(warn * 0.5f);
        PlayClip(BossClipId.Flap);
        yield return new WaitForSeconds(warn * 0.5f);

        Sprite[] frames = WindFrames("WindTornado_Spin");
        float scale = FitScale(frames, 2.6f);
        foreach (Vector2 point in points)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float retarget = 0f;
            WindHazard cyclone = WindHazard.Spawn(frames, point, scale, 16f, 8, _definition.cycloneHitRadius, _definition.cycloneLifeSeconds,
                (age, position) =>
                {
                    retarget -= Time.deltaTime;
                    if (retarget <= 0f)
                    {
                        retarget = Random.Range(0.8f, 1.5f);
                        Vector2 toPlayer = (PlayerPosition() - position).normalized;
                        direction = Vector2.Lerp(Random.insideUnitCircle.normalized, toPlayer, Mathf.Clamp01(0.3f + age * 0.08f)).normalized;
                    }

                    return ClampInside(position + direction * (_definition.cycloneSpeed * Time.deltaTime), 1f);
                },
                (hitPlayer, position) =>
                {
                    OwlSfx(SfxIds.BossowlCycloneHit, position);
                    HurtPlayer(hitPlayer, _definition.cycloneDamage, position, 6f);
                    if (!hitPlayer.IsDashInvulnerable)
                        hitPlayer.ApplyRoot(_definition.cycloneStunSeconds);
                    TrackObject(BossVfx.Spawn("Wind/WindImpact_Burst", position, 0.7f, 16f, false, 0.6f, 9));
                }, false, false, null, 1.6f,
                new WindHazard.Look
                {
                    popSeconds = 0.4f, fadeSeconds = 0.7f, breathe = 0.06f, shadow = true, shadowScale = 2.4f, orbitLeaves = true,
                    wobbleAmplitude = 0.12f, wobbleHertz = 1.1f,
                });
            TrackObject(cyclone.gameObject);
            TrackObject(BossVfx.Spawn("Wind/WindImpact_Burst", point, 0.7f, 16f, false, 0.6f, 9));
        }

        ContinueClip();
        if (UsesWindOwl && points.Count > 0)
        {
            _owlLoop?.Stop(0.1f);
            _owlLoop = SoundFXManager.StartLoop(SfxIds.BossowlCycloneLoop, 0.6f, 1f, 0.3f);
            StartCoroutine(StopOwlLoopAfter(_definition.cycloneLifeSeconds));
        }

        yield return new WaitForSeconds(0.8f);
        SetPose(Vector2.zero, Vector2.one, 0.3f);
    }

    private SfxLoopHandle _owlLoop;

    private IEnumerator StopOwlLoopAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _owlLoop?.Stop(0.5f);
        _owlLoop = null;
    }

    // ---------------------------------------------------------------- 4. Gale Wall

    private IEnumerator GaleWallSkill()
    {
        Rect arena = ArenaRect();
        bool fromLeft = Random.value < 0.5f;
        float direction = fromLeft ? 1f : -1f;
        float startX = fromLeft ? arena.xMin - 2f : arena.xMax + 2f;
        float endX = fromLeft ? arena.xMax + 2f : arena.xMin - 2f;
        float warn = Mathf.Max(1.2f, Tele(_definition.wallWarnSeconds));
        float travel = Mathf.Abs(endX - startX) / _definition.wallSpeed;
        float height = arena.height + 2f;

        // the danger lane at the start edge, and a green shadow behind every obstacle (the wind totems) where the wall cannot reach
        Track(BossTelegraph.Line(new Vector2(startX + direction * _definition.wallThickness * 0.5f, arena.yMin - 1f), Vector2.up, height,
            _definition.wallThickness * 1.6f, warn, true));
        var pockets = new List<BossObstacle>(BossObstacle.All);
        foreach (BossObstacle obstacle in pockets)
        {
            if (obstacle == null)
                continue;

            var pocket = new GameObject("GalePocket");
            pocket.transform.position = obstacle.Center;
            pocket.transform.localScale = new Vector3(direction * _definition.wallShelterLength, _definition.wallShelterHalfWidth * 2f, 1f);
            var pocketRenderer = pocket.AddComponent<SpriteRenderer>();
            pocketRenderer.sprite = BossTelegraph.SquareSprite;
            pocketRenderer.color = new Color(BossTelegraph.Safe.r, BossTelegraph.Safe.g, BossTelegraph.Safe.b, 0.26f);
            pocketRenderer.sortingLayerName = "Default";
            pocketRenderer.sortingOrder = -55;
            TrackObject(pocket);
            Destroy(pocket, warn + travel + 0.5f);
        }

        SetPose(new Vector2(0f, 0.3f), new Vector2(1.04f, 1.0f), warn * 0.8f);
        OwlSfx(SfxIds.BossowlWallWarning, transform.position);
        PlayClip(BossClipId.Sweep, warn);
        yield return new WaitForSeconds(warn);
        ContinueClip();
        OwlSfx(SfxIds.BossowlWallRush, new Vector2(startX, arena.center.y));

        // the wall: a column of gust sprites sweeping across, shoving the player along
        Sprite[] frames = WindFrames("WindGustPuff_Dissipate");
        float tile = 3.2f;
        float tileScale = FitScale(frames, tile * 1.5f);
        int tiles = Mathf.CeilToInt(height / tile) + 1;
        var wall = new GameObject("GaleWall");
        TrackObject(wall);
        for (int i = 0; i < tiles; i++)
        {
            var piece = new GameObject("GaleTile");
            piece.transform.SetParent(wall.transform, false);
            piece.transform.localPosition = new Vector3(0f, (arena.yMin - 1f) + tile * (i + 0.5f), 0f);
            piece.transform.localScale = new Vector3(tileScale * direction, tileScale, 1f);
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 10;
            renderer.color = new Color(1f, 1f, 1f, 0.92f);
            if (frames != null && frames.Length > 0)
                piece.AddComponent<SkillFrameAnimator>().Play(frames, 14f, true, Random.value);
        }

        // a second, larger and fainter layer trailing behind the wall, and leaves/dust swept along by it
        for (int i = 0; i < tiles; i++)
        {
            var piece = new GameObject("GaleTileBack");
            piece.transform.SetParent(wall.transform, false);
            piece.transform.localPosition = new Vector3(-direction * 1.4f, (arena.yMin - 1f) + tile * (i + 0.5f) + 0.9f, 0f);
            piece.transform.localScale = new Vector3(tileScale * 1.35f * direction, tileScale * 1.35f, 1f);
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 9;
            renderer.color = new Color(1f, 1f, 1f, 0.42f);
            if (frames != null && frames.Length > 0)
                piece.AddComponent<SkillFrameAnimator>().Play(frames, 9f, true, Random.value);
        }

        BuildWallParticles(wall.transform, direction, height, arena.yMin - 1f);
        SkillScreenFX.Shake(0.12f, travel);
        bool damaged = false;
        bool impacted = false;
        Player player = GetPlayer();
        Rigidbody2D body = BodyOf(player);
        float x = startX;
        while (x * direction < endX * direction && _state != BossState.Dead)
        {
            x += direction * _definition.wallSpeed * Time.deltaTime;
            wall.transform.position = new Vector3(x, 0f, 0f);
            if (player != null && body != null && !player.IsDead)
            {
                Vector2 position = player.transform.position;
                bool inside = Mathf.Abs(position.x - x) <= _definition.wallThickness * 0.5f + 0.4f && position.y >= arena.yMin - 1f && position.y <= arena.yMax + 1f;
                if (inside && !IsShelteredFromWall(position, direction, pockets))
                {
                    Vector2 push = new Vector2(direction, 0f);
                    float step = _definition.wallPushSpeed * Time.deltaTime;
                    if (!damaged && !player.IsDashInvulnerable)
                    {
                        damaged = true;
                        OwlSfx(SfxIds.BossowlWallHit, position);
                        HurtPlayer(player, _definition.wallDamage, position - push, 0f);
                    }

                    if (BlockedByWorld(body, push, step))
                    {
                        if (!impacted && !player.IsDashInvulnerable)
                        {
                            impacted = true;
                            ImpactHit(player, push);
                        }
                    }
                    else if (!player.IsDashInvulnerable)
                    {
                        body.position += push * step;
                    }
                }
            }

            yield return null;
        }

        if (wall != null)
            Destroy(wall);
        SetPose(Vector2.zero, Vector2.one, 0.3f);
    }

    private void BuildWallParticles(Transform wall, float direction, float height, float bottom)
    {
        var go = new GameObject("GaleLeaves");
        go.transform.SetParent(wall, false);
        go.transform.localPosition = new Vector3(0f, bottom + height * 0.5f, 0f);
        var particles = go.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.98f, 1f, 0.98f, 0.95f), new Color(0.55f, 0.82f, 0.42f, 0.95f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 220;
        var emission = particles.emission;
        emission.rateOverTime = 90f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.4f, height, 1f);
        shape.rotation = new Vector3(0f, 0f, direction > 0f ? 0f : 180f);
        var spin = particles.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 11;
    }

    private bool IsShelteredFromWall(Vector2 position, float direction, List<BossObstacle> obstacles)
    {
        foreach (BossObstacle obstacle in obstacles)
        {
            if (obstacle == null)
                continue;

            Vector2 delta = position - obstacle.Center;
            float behind = delta.x * direction;
            if (behind > 0f && behind < _definition.wallShelterLength && Mathf.Abs(delta.y) < _definition.wallShelterHalfWidth)
                return true;
        }

        return false;
    }

    // ---------------------------------------------------------------- 5. Crescent Blades

    private IEnumerator CrescentBladesSkill()
    {
        int count = Mathf.Max(1, BossDefinition.PerPhase(_definition.crescentCountByPhase, _phaseIndex, 2));
        float warn = Mathf.Max(0.8f, Tele(_definition.crescentWarnSeconds));
        Rect arena = ArenaRect();
        Vector2 centre = arena.center;
        var paths = new List<Vector2[]>();
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) + i * 360f / count;
            Vector2 start = ClampInside(centre + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * arena.width * 0.5f, Mathf.Sin(angle * Mathf.Deg2Rad) * arena.height * 0.5f), 1.5f);
            Vector2 end = ClampInside(PredictedPlayerPosition(0.5f) + Random.insideUnitCircle * 2.5f, 1f);
            Vector2 mid = (start + end) * 0.5f;
            Vector2 side = new Vector2(-(end - start).y, (end - start).x).normalized * (Random.value < 0.5f ? -1f : 1f) * Random.Range(5f, 9f);
            var points = new Vector2[28];
            for (int k = 0; k < points.Length; k++)
            {
                float t = k / (float)(points.Length - 1);
                points[k] = Bezier(start, mid + side, end, t);
            }

            paths.Add(points);
            BuildArcTelegraph(points, warn);
        }

        SetPose(new Vector2(0f, 0.3f), new Vector2(1.03f, 1.03f), warn * 0.8f);
        OwlSfx(SfxIds.BossowlSlashWindup, transform.position);
        PlayClip(BossClipId.Slash, warn);
        yield return new WaitForSeconds(warn);
        ContinueClip();
        OwlSfx(SfxIds.BossowlBladeCut, transform.position);
        SkillScreenFX.Shake(0.12f, 0.25f);
        WingBursts((Vector2)transform.position + Vector2.up * 0.4f);

        Sprite[] frames = WindFrames("WindBlade_Spin");
        float scale = FitScale(frames, 2.2f);
        foreach (Vector2[] points in paths)
        {
            float length = 0f;
            for (int k = 1; k < points.Length; k++)
                length += Vector2.Distance(points[k - 1], points[k]);
            Vector2[] path = points;
            float total = length;
            WindHazard blade = WindHazard.Spawn(frames, path[0], scale, 18f, 9, _definition.crescentHitRadius, 2f * total / _definition.crescentSpeed + 0.1f,
                (age, position) =>
                {
                    float s = age * _definition.crescentSpeed / Mathf.Max(0.1f, total); // 0..1 out, 1..2 back
                    float u = s <= 1f ? s : 2f - s;
                    return PointOnPath(path, Mathf.Clamp01(u));
                },
                (hitPlayer, position) =>
                {
                    OwlSfx(SfxIds.BossowlBladeHit, position);
                    HurtPlayer(hitPlayer, _definition.crescentDamage, position, 4f);
                }, false, false, null, 0.9f,
                new WindHazard.Look
                {
                    popSeconds = 0.15f, fadeSeconds = 0.25f, breathe = 0.05f,
                    trail = true, trailColor = new Color(0.72f, 0.95f, 1f, 0.6f), trailWidth = 0.85f, trailSeconds = 0.4f,
                });
            TrackObject(blade.gameObject);
            _castHazards.Add(blade);
        }

        yield return WaitCastHazards(3.5f);
        SetPose(Vector2.zero, Vector2.one, 0.3f);
    }

    private static Vector2 Bezier(Vector2 a, Vector2 control, Vector2 b, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * control + t * t * b;
    }

    private static Vector2 PointOnPath(Vector2[] points, float u)
    {
        float scaled = u * (points.Length - 1);
        int index = Mathf.Min(points.Length - 2, Mathf.FloorToInt(scaled));
        return Vector2.Lerp(points[index], points[index + 1], scaled - index);
    }

    private void BuildArcTelegraph(Vector2[] points, float duration)
    {
        var go = new GameObject("CrescentTelegraph");
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(BossTelegraph.Red.r, BossTelegraph.Red.g, BossTelegraph.Red.b, 0.35f);
        line.endColor = new Color(BossTelegraph.Red.r, BossTelegraph.Red.g, BossTelegraph.Red.b, 0.8f);
        line.startWidth = _definition.crescentHitRadius * 1.6f;
        line.endWidth = _definition.crescentHitRadius * 1.6f;
        line.sortingLayerName = "Default";
        line.sortingOrder = 11;
        line.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
            line.SetPosition(i, points[i]);
        TrackObject(go);
        Destroy(go, duration);
    }

    // ---------------------------------------------------------------- 6. Sky Storm (ultimate)

    private IEnumerator SkyStormSkill()
    {
        Vector2 centre = _arenaCenter;
        // the owl rises and circles above the platform (untouchable)
        OwlSfx(SfxIds.BossowlStormCharge, transform.position);
        PlayClip(BossClipId.Circle);
        SetPose(new Vector2(0f, 1.0f), new Vector2(1.12f, 1.12f), 0.8f);
        _airborne = true;
        SkillScreenFX.Dim(_definition.stormDimAlpha, 1.2f, new Color(0.08f, 0.1f, 0.28f));
        SkillScreenFX.Shake(0.15f, 1f);

        // the whole platform breaks away from the edge inwards; only a circle around the middle (the eye) stays safe and it keeps shrinking
        float[] radii = _definition.stormEyeRadii != null && _definition.stormEyeRadii.Length > 0
            ? _definition.stormEyeRadii : new[] { 17f, 13f, 9.5f, 6.5f };
        float eye = float.PositiveInfinity;
        var overlays = new List<GameObject>();
        GameObject mask = MakePlatformMask();
        float angle = AngleOf((Vector2)transform.position - centre);
        float nextTick = 0f;
        for (int i = 0; i < radii.Length && _state != BossState.Dead; i++)
        {
            float next = radii[i];
            GameObject ring = MakeEyeOverlay(centre, next);
            overlays.Add(ring);

            // flicker (warning), then break
            float warn = Mathf.Max(0.8f, Tele(_definition.stormTierWarnSeconds));
            OwlSfx(SfxIds.BossowlFloorWarn, centre);
            for (float t = 0f; t < warn && _state != BossState.Dead; t += Time.deltaTime)
            {
                float flicker = 0.16f + 0.3f * (0.5f + 0.5f * Mathf.Sin(t * 22f));
                SetOverlayColor(ring, new Color(BossTelegraph.Red.r, BossTelegraph.Red.g, BossTelegraph.Red.b, flicker));
                angle = StormCircle(centre, angle);
                StormTick(centre, eye, ref nextTick);
                yield return null;
            }

            SetOverlayColor(ring, new Color(0.05f, 0.04f, 0.12f, 0.72f));
            eye = next;
            OwlSfx(SfxIds.BossowlFloorBreak, centre);
            SkillScreenFX.Shake(0.25f, 0.35f);
            for (int k = 0; k < 6; k++)
            {
                float around = (k / 6f + Random.value * 0.1f) * Mathf.PI * 2f;
                Vector2 spot = centre + new Vector2(Mathf.Cos(around), Mathf.Sin(around) * 0.62f) * next;
                TrackObject(BossVfx.Spawn("Wind/WindImpact_Burst", spot, 1.1f, 16f, false, 0.7f, 9));
            }
        }

        // the broken floor stays dangerous a little longer
        for (float t = 0f; t < _definition.stormBrokenSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            angle = StormCircle(centre, angle);
            StormTick(centre, eye, ref nextTick);
            yield return null;
        }

        // calm: the floor comes back, the owl drops to the middle exhausted (the Recovery window follows)
        SkillScreenFX.Dim(0f, 0.8f);
        foreach (GameObject overlay in overlays)
        {
            if (overlay != null)
                Destroy(overlay);
        }

        if (mask != null)
            Destroy(mask);
        StopClip();
        _airborne = false;
        transform.position = ClampInside(centre, 1.5f);
        OwlSfx(SfxIds.BossowlStormEnd, transform.position);
        SetPose(Vector2.zero, Vector2.one, 0.3f);
        TrackObject(BossVfx.Spawn("Wind/WindShockwave_Expand", transform.position, 3f, 14f, false, 1f, 6));
        SkillScreenFX.Shake(0.2f, 0.3f);
        yield return new WaitForSeconds(0.3f);
    }

    private float StormCircle(Vector2 centre, float angle)
    {
        angle += 70f * Time.deltaTime;
        Vector2 target = centre + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * 8f, Mathf.Sin(angle * Mathf.Deg2Rad) * 4.5f);
        transform.position = Vector2.MoveTowards(transform.position, target, 18f * Time.deltaTime);
        return angle;
    }

    private static readonly Dictionary<int, Sprite> EyeSprites = new Dictionary<int, Sprite>();
    private const float EyeOuterRadius = 46f;

    /// <summary>A sprite that is solid outside `innerFraction` of its radius and clear inside (the broken floor around the eye).</summary>
    private static Sprite EyeSprite(float innerFraction)
    {
        int key = Mathf.RoundToInt(innerFraction * 1000f);
        if (EyeSprites.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                byte alpha = d >= innerFraction && d <= 1f ? (byte)255 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        EyeSprites[key] = sprite;
        return sprite;
    }

    /// <summary>The floor beyond `eyeRadius` from `centre`, as a flat overlay clipped to the platform by a sprite mask.</summary>
    private GameObject MakeEyeOverlay(Vector2 centre, float eyeRadius)
    {
        var go = new GameObject("StormRing");
        go.transform.position = new Vector3(centre.x, centre.y, 0f);
        go.transform.localScale = Vector3.one * (EyeOuterRadius * 2f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = EyeSprite(Mathf.Clamp01(eyeRadius / EyeOuterRadius));
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = -58;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        TrackObject(go);
        return go;
    }

    /// <summary>A sprite mask in the shape of the platform (its own cut-out sprite), so the storm overlay never spills over the clouds.</summary>
    private GameObject MakePlatformMask()
    {
        GameObject platform = GameObject.Find("Arena_Platform");
        if (platform == null || !platform.TryGetComponent(out SpriteRenderer source))
            return null;

        var go = new GameObject("StormMask");
        go.transform.position = platform.transform.position;
        var mask = go.AddComponent<SpriteMask>();
        mask.sprite = source.sprite;
        mask.alphaCutoff = 0.3f;
        TrackObject(go);
        return go;
    }

    private static void SetOverlayColor(GameObject overlay, Color color)
    {
        if (overlay != null && overlay.TryGetComponent(out SpriteRenderer renderer))
            renderer.color = color;
    }

    /// <summary>Floor outside the eye: damage over time and a gale that shoves the player back toward the eye.</summary>
    private void StormTick(Vector2 centre, float eyeRadius, ref float nextTick)
    {
        Player player = GetPlayer();
        if (player == null || player.IsDead || float.IsPositiveInfinity(eyeRadius))
            return;

        Vector2 position = player.transform.position;
        Vector2 toEye = centre - position;
        if (toEye.magnitude <= eyeRadius)
            return;

        Rigidbody2D body = BodyOf(player);
        Vector2 direction = toEye.normalized;
        if (body != null && !BlockedByWorld(body, direction, _definition.stormPushSpeed * Time.deltaTime))
            body.position += direction * (_definition.stormPushSpeed * Time.deltaTime);
        if (Time.time >= nextTick)
        {
            nextTick = Time.time + _definition.stormTickInterval;
            HurtPlayer(player, _definition.stormTickDamage, position - direction, 0f);
        }
    }

    // ---------------------------------------------------------------- phase change: the screech

    private IEnumerator WindScreechPush()
    {
        Player player = GetPlayer();
        if (player == null || player.IsDead)
            yield break;

        yield return new WaitForSeconds(0.45f);
        Vector2 away = (Vector2)player.transform.position - (Vector2)transform.position;
        if (away.magnitude < 14f && !player.IsDashInvulnerable)
            yield return KnockWithImpact(player, away, 6f, 0.45f);
    }
}
