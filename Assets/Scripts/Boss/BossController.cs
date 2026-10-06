using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Brain of a boss encounter (D-086): health, phases, the Combo -> Recovery wave loop, damage-taken
/// modifiers and the visual pose. Skill behaviour lives in BossSkills.cs (same partial class); numbers
/// live in <see cref="BossDefinition"/>. Deliberately separate from EnemyUniversal (a Melee/Area/Projectile
/// state machine that has no phases, combos or telegraphs).
/// </summary>
public sealed partial class BossController : MonoBehaviour, IDamageable, ISlowable, IStunnable, IVulnerable
{
    public enum BossState { Dormant, Spawning, Fighting, PhaseTransition, Dead }

    [SerializeField] private BossDefinition _definition;
    [SerializeField] private SpriteRenderer _body;
    [Tooltip("Moved for bobbing/pose; defaults to the body renderer's transform.")]
    [SerializeField] private Transform _visualRoot;
    [Tooltip("Where the Core Resonance ultimate is cast from. Defaults to the position the boss was spawned at.")]
    [SerializeField] private Vector2 _arenaCenter;
    [SerializeField] private bool _useSpawnAsArenaCenter = true;
    [SerializeField] private bool _beginOnStart;

    private float _health;
    private BossState _state = BossState.Dormant;
    private int _phaseIndex;
    private int _phaseComboCounter;

    private bool _runFightLoop = true;
    private bool _recovering;
    private float _recoveryEnd;
    private float _recoveryDamageTaken = 1f;
    private bool _recoveryStunExtended;
    private bool _fistAway;
    private float _vulnerabilityMultiplier = 1f;
    private float _vulnerabilityUntil;
    private float _slowMultiplier = 1f;
    private float _slowUntil;
    private int _activeSkills;

    private bool _hasBounds;
    private Rect _bounds;
    private Transform _effectsRoot;
    private Vector2 _poseOffset;
    private Vector2 _poseScale = Vector2.one;
    private Coroutine _poseRoutine;
    private float _flashUntil;
    private float _alpha = 1f;
    private Player _player;
    private BossClipPlayer _clipPlayer;

    public BossDefinition Definition => _definition;
    public float Health => _health;
    public float MaxHealth => _definition != null ? _definition.maxHealth : 0f;
    public BossState State => _state;
    public int PhaseIndex => _phaseIndex;
    public bool IsRecovering => _recovering;
    public bool IsDead => _state == BossState.Dead;
    public bool IsFistAway => _fistAway;
    public float DamageTakenMultiplier => ComputeDamageTakenMultiplier();
    public BossPhase CurrentPhase => _definition.phases[Mathf.Clamp(_phaseIndex, 0, _definition.phases.Length - 1)];

    public event Action<float, float> HealthChanged;
    public event Action<int> PhaseChanged;
    public event Action Died;
    /// <summary>Raised when a skill starts (telegraph begins): hook for SFX/UI/tests.</summary>
    public event Action<BossSkillId> SkillStarted;
    /// <summary>Raised when the boss starts a Recovery window, with its length in seconds.</summary>
    public event Action<float> RecoveryStarted;

    private void Awake()
    {
        if (_body == null)
            _body = GetComponentInChildren<SpriteRenderer>();
        if (_visualRoot == null && _body != null)
            _visualRoot = _body.transform;
        _clipPlayer = GetComponentInChildren<BossClipPlayer>();

        _effectsRoot = new GameObject("BossEffects_" + name).transform;
        if (_useSpawnAsArenaCenter)
            _arenaCenter = transform.position;
    }

    private void Start()
    {
        if (_beginOnStart && _definition != null)
            BeginEncounter();
    }

    private void OnDestroy()
    {
        StopWaterLoops();
        EndWindFight();
        if (_effectsRoot != null)
            Destroy(_effectsRoot.gameObject);
    }

    /// <summary>Walkable area of the arena (world rect). Skills keep their lines/targets inside it and the
    /// boss never drifts out of it.</summary>
    public void SetArenaBounds(Rect bounds)
    {
        _bounds = bounds;
        _hasBounds = true;
    }

    private Vector2 ClampInside(Vector2 point, float margin = 0f)
    {
        if (_hasBounds)
        {
            point = new Vector2(
                Mathf.Clamp(point.x, _bounds.xMin + margin, _bounds.xMax - margin),
                Mathf.Clamp(point.y, _bounds.yMin + margin, _bounds.yMax - margin));
        }

        // D-110: arenas that are not a rectangle (Wind's sky platform) also give an outline the boss stays inside
        if (_polygon != null)
            point = ArenaPolygon.Constrain(_polygon, point, Mathf.Max(0.5f, margin));

        // D-106: walk around solid arena objects (teleport pillar, totems) instead of through them
        return BossObstacle.PushOut(point, 1.3f);
    }

    private Vector2[] _polygon;

    /// <summary>Optional outline of a non-rectangular arena (D-110); the rectangle from <see cref="SetArenaBounds"/> still applies.</summary>
    public void SetArenaPolygon(Vector2[] polygon)
    {
        _polygon = polygon != null && polygon.Length >= 3 ? polygon : null;
    }

    /// <summary>How far a ray from `origin` along `direction` travels before leaving the arena (capped).</summary>
    private float DistanceToBounds(Vector2 origin, Vector2 direction, float maxLength)
    {
        if (!_hasBounds)
            return maxLength;

        float tx = direction.x > 0.0001f ? (_bounds.xMax - origin.x) / direction.x
            : direction.x < -0.0001f ? (_bounds.xMin - origin.x) / direction.x : float.PositiveInfinity;
        float ty = direction.y > 0.0001f ? (_bounds.yMax - origin.y) / direction.y
            : direction.y < -0.0001f ? (_bounds.yMin - origin.y) / direction.y : float.PositiveInfinity;
        return Mathf.Max(0f, Mathf.Min(maxLength, Mathf.Min(tx, ty)));
    }

    /// <summary>Used when the boss is created from code (spawner, tests).</summary>
    public void Initialize(BossDefinition definition)
    {
        _definition = definition;
        BossTelegraph.SetElement(CombatFeedback.ElementOf(definition.bossId));
        _health = definition.maxHealth;
    }

    /// <summary>Fades the boss in (invulnerable), then starts the fight loop.</summary>
    public void BeginEncounter(bool runFightLoop = true)
    {
        _runFightLoop = runFightLoop;
        if (_state != BossState.Dormant || _definition == null || _definition.phases.Length == 0)
            return;

        if (_health <= 0f)
            _health = _definition.maxHealth;

        HealthChanged?.Invoke(_health, _definition.maxHealth);
        StartCoroutine(EncounterRoutine());
    }

    private IEnumerator EncounterRoutine()
    {
        _state = BossState.Spawning;
        float fade = Mathf.Max(0.01f, _definition.spawnFadeSeconds);
        Vector2 rise = new Vector2(0f, -1.2f);
        for (float t = 0f; t < fade; t += Time.deltaTime)
        {
            float k = t / fade;
            _alpha = k;
            _poseOffset = rise * (1f - k);
            if (UnityEngine.Random.value < 0.25f)
                BossVfx.Spawn(_definition.impactVfx, (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 2.2f,
                    0.6f, 14f, false, 0.8f, 7);
            yield return null;
        }

        _alpha = 1f;
        _poseOffset = Vector2.zero;
        BossSfx(SfxIds.BossAwaken, SfxIds.BosswAwaken, transform.position);
        SkillScreenFX.Shake(0.15f, 0.4f);
        if (TideController.Instance != null)
            TideController.Instance.BeginFight(_definition);
        BeginWindFight();
        if (_runFightLoop)
            yield return FightRoutine();
        else
            _state = BossState.Fighting;
    }

    /// <summary>Runs one skill immediately (tests and tools). Respects the same telegraph/damage rules.</summary>
    public Coroutine CastSkill(BossSkillId id) => StartCoroutine(TrackedSkill(id));

    private void Update()
    {
        if (_visualRoot == null)
            return;

        float bob = _state == BossState.Dead ? 0f : Mathf.Sin(Time.time * 1.6f) * 0.18f;
        _visualRoot.localPosition = (Vector3)(_poseOffset + new Vector2(0f, bob));
        _visualRoot.localScale = new Vector3(_poseScale.x, _poseScale.y, 1f);

        if (_body == null)
            return;

        Color tint = _recovering ? new Color(0.78f, 0.8f, 0.9f) : Color.white;
        if (Time.time < _flashUntil)
            tint = Color.Lerp(tint, new Color(1f, 0.55f, 0.55f), 0.8f);
        tint.a = _alpha;
        _body.color = tint;
    }

    // ---------------------------------------------------------------- fight loop

    private IEnumerator FightRoutine()
    {
        _state = BossState.Fighting;
        if (UsesWaterSfx)
            StartCoroutine(WaterChaseLoop());
        while (_state != BossState.Dead)
        {
            while (PlayerIsGone())
                yield return null;

            yield return PhaseTransitionIfNeeded();

            BossPhase phase = CurrentPhase;
            if (phase.combos == null || phase.combos.Length == 0)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            BossCombo combo = phase.combos[_phaseComboCounter % phase.combos.Length];
            yield return DriftTowardPlayer(1.2f, 7f);
            yield return RunCombo(combo, phase);
            if (_state == BossState.Dead)
                yield break;

            _phaseComboCounter++;

            float recoverySeconds = phase.recoverySeconds;
            float damageTaken = _definition.recoveryDamageTaken;
            if (phase.ultimateEveryCombos > 0 && _phaseComboCounter % phase.ultimateEveryCombos == 0)
            {
                yield return TrackedSkill(_definition.ultimateSkill);
                if (_state == BossState.Dead)
                    yield break;

                recoverySeconds = _definition.ultimateRecoverySeconds;
                damageTaken = _definition.ultimateRecoveryDamageTaken;
            }

            yield return RecoveryRoutine(recoverySeconds, damageTaken);
        }
    }

    private IEnumerator RunCombo(BossCombo combo, BossPhase phase)
    {
        BossComboStep[] steps = combo.steps;
        for (int i = 0; i < steps.Length && _state != BossState.Dead; i++)
        {
            if (PhaseIndexNow() > _phaseIndex)
                break;

            if (steps[i].parallelWithNext && i < steps.Length - 1)
            {
                StartCoroutine(TrackedSkill(steps[i].skill));
                continue;
            }

            yield return TrackedSkill(steps[i].skill);
            if (i < steps.Length - 1)
                yield return new WaitForSeconds(phase.gapBetweenSkills);
        }

        while (_activeSkills > 0 && _state != BossState.Dead)
            yield return null;
    }

    private IEnumerator TrackedSkill(BossSkillId id)
    {
        _activeSkills++;
        SkillStarted?.Invoke(id);
        ShowCastAura();
        try
        {
            yield return SkillRoutine(id);
        }
        finally
        {
            _activeSkills--;
        }
    }

    /// <summary>A short charge-up glow on the boss while a skill starts (element aura, Pixellab art).</summary>
    private void ShowCastAura()
    {
        Sprite[] frames = BossVfx.Frames(CombatFeedback.AuraName(CombatFeedback.ElementOf(_definition.bossId)));
        if (frames == null || frames.Length == 0)
            return;

        var aura = new GameObject("CastAura");
        aura.transform.SetParent(transform, false);
        aura.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        aura.transform.localScale = Vector3.one * 3.2f;
        var renderer = aura.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 11;
        renderer.color = new Color(1f, 1f, 1f, 0.9f);
        aura.AddComponent<SkillFrameAnimator>().Play(frames, 16f, true);
        aura.AddComponent<FadeAndDestroy>().Begin(1.1f, 0.35f);
    }

    private IEnumerator RecoveryRoutine(float seconds, float damageTaken)
    {
        _recovering = true;
        _recoveryDamageTaken = damageTaken;
        _recoveryStunExtended = false;
        _recoveryEnd = Time.time + seconds;
        RecoveryStarted?.Invoke(seconds);
        BossSfx(SfxIds.BossRecovery, SfxIds.BosswRecovery, transform.position);
        SetPose(new Vector2(0f, -0.5f), new Vector2(1.04f, 0.93f), 0.25f);
        PlayClip(BossClipId.Recovery, 0.7f);

        while (Time.time < _recoveryEnd && _state != BossState.Dead)
            yield return null;

        _recovering = false;
        ContinueClip();
        SetPose(Vector2.zero, Vector2.one, 0.25f);
    }

    private int PhaseIndexNow() => _definition.PhaseIndexForHealthFraction(_health / _definition.maxHealth);

    private IEnumerator PhaseTransitionIfNeeded()
    {
        int next = PhaseIndexNow();
        if (next <= _phaseIndex)
            yield break;

        _state = BossState.PhaseTransition;
        _phaseIndex = next;
        _phaseComboCounter = 0;
        if (TideController.Instance != null)
            TideController.Instance.SetPhase(_phaseIndex);
        if (UsesWindOwl && ArenaWind.Instance != null)
            ArenaWind.Instance.SetPhase(_phaseIndex);
        PhaseChanged?.Invoke(_phaseIndex);
        BossSfx(SfxIds.BossPhaseRoar, SfxIds.BosswPhaseRoar, transform.position);

        SetPose(new Vector2(0f, 0.6f), new Vector2(1.08f, 1.08f), 0.3f);
        PlayClip(BossClipId.Resonance, 0.6f);
        SkillScreenFX.Shake(0.35f, _definition.phaseTransitionSeconds);
        SkillScreenFX.Flash(UsesWindOwl ? new Color(0.75f, 0.92f, 1f) : new Color(0.5f, 1f, 0.6f), 0.35f, 0.8f);
        BossVfx.Spawn(_definition.phaseVfx, transform.position, 2.4f, 12f, true, _definition.phaseTransitionSeconds, -90);
        if (UsesWindOwl)
            StartCoroutine(WindScreechPush());
        yield return new WaitForSeconds(_definition.phaseTransitionSeconds);

        ContinueClip();
        SetPose(Vector2.zero, Vector2.one, 0.3f);
        _state = BossState.Fighting;
    }

    private bool PlayerIsGone()
    {
        Player player = GetPlayer();
        return player == null || player.IsDead;
    }

    private Player GetPlayer()
    {
        if (_player == null)
            _player = FindAnyObjectByType<Player>();
        return _player;
    }

    // ---------------------------------------------------------------- damage

    private float ComputeDamageTakenMultiplier()
    {
        float multiplier = 1f;
        if (_recovering)
            multiplier *= _recoveryDamageTaken;
        if (_fistAway)
            multiplier *= _definition.fistAwayDamageTaken;
        if (Time.time < _vulnerabilityUntil)
            multiplier *= _vulnerabilityMultiplier;
        return multiplier;
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        if (_state == BossState.Dead || damage <= 0f || _burrowed || _airborne)
            return;

        // Untouchable while it is appearing or roaring into the next phase.
        if (_state == BossState.Dormant || _state == BossState.Spawning || _state == BossState.PhaseTransition)
            return;

        float multiplier = ComputeDamageTakenMultiplier();
        _health = Mathf.Max(0f, _health - damage * multiplier);
        _flashUntil = Time.time + 0.1f;
        CombatFeedback.BossHit(transform.position, CombatFeedback.ElementOf(_definition.bossId), damage * multiplier, multiplier > 1.01f);
        BossSfx(SfxIds.BossHit, SfxIds.BosswHit, transform.position);
        HealthChanged?.Invoke(_health, _definition.maxHealth);

        if (_health <= 0f)
        {
            // Stop every running skill first, then play the death sequence on a clean slate.
            StopAllCoroutines();
            StartCoroutine(DeathRoutine());
        }
    }

    public void ApplyStun(float duration)
    {
        // Only the recovery window can be extended, and only once per recovery (plan section 3).
        if (!_recovering || _recoveryStunExtended || duration <= 0f)
            return;

        _recoveryStunExtended = true;
        _recoveryEnd += Mathf.Min(duration, 1f);
    }

    public void ApplySlow(float speedMultiplier, float duration)
    {
        // Slow only works at half strength on the boss.
        float effective = Mathf.Lerp(1f, Mathf.Clamp01(speedMultiplier), 0.5f);
        if (Time.time >= _slowUntil || effective < _slowMultiplier)
            _slowMultiplier = effective;
        _slowUntil = Mathf.Max(_slowUntil, Time.time + duration);
    }

    public void ApplyVulnerability(float damageTakenMultiplier, float duration)
    {
        if (Time.time >= _vulnerabilityUntil || damageTakenMultiplier > _vulnerabilityMultiplier)
            _vulnerabilityMultiplier = damageTakenMultiplier;
        _vulnerabilityUntil = Mathf.Max(_vulnerabilityUntil, Time.time + duration);
    }

    private float CurrentMoveSpeed => _definition.moveSpeed * (Time.time < _slowUntil ? _slowMultiplier : 1f)
        * TideSpeedMultiplier();

    private IEnumerator DeathRoutine()
    {
        _state = BossState.Dead;
        _recovering = false;
        _fistAway = false;
        _burrowed = false;
        _airborne = false;
        SetBodyVisible(true);
        if (TideController.Instance != null)
            TideController.Instance.EndFight();
        EndWindFight();
        if (UsesWindOwl)
            SkillScreenFX.Dim(0f, 0.6f);
        foreach (Transform child in _effectsRoot)
            Destroy(child.gameObject);

        StopWaterLoops();
        BossSfx(SfxIds.BossDeath, SfxIds.BosswDeath, transform.position);
        PlayClip(UsesWindOwl ? BossClipId.Death : BossClipId.Recovery, 0.5f);
        SkillScreenFX.HitStopAndSlowMo(0.08f, 0.35f, 0.9f);
        SkillScreenFX.Shake(0.4f, 1.2f);
        SetPose(new Vector2(0f, -0.4f), new Vector2(1.05f, 0.9f), 0.4f);

        float fade = 1.6f;
        for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
        {
            _alpha = 1f - t / fade;
            if (UnityEngine.Random.value < 0.3f)
                BossVfx.Spawn(_definition.impactVfx, (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 2.6f,
                    0.8f, 14f, false, 0.8f, 7);
            yield return null;
        }

        BossSfx(SfxIds.BossVictory, SfxIds.BossVictory);
        Died?.Invoke();
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------- pose / movement helpers

    private void PlayClip(BossClipId id, float windupSeconds = 0f)
    {
        if (_clipPlayer != null)
            _clipPlayer.Play(id, windupSeconds);
    }

    private void ContinueClip()
    {
        if (_clipPlayer != null)
            _clipPlayer.Continue();
    }

    private void SetMoving(bool moving)
    {
        if (_clipPlayer != null)
            _clipPlayer.SetMoving(moving);
    }

    private void SetPose(Vector2 offset, Vector2 scale, float seconds)
    {
        if (_poseRoutine != null)
            StopCoroutine(_poseRoutine);
        _poseRoutine = StartCoroutine(PoseRoutine(offset, scale, seconds));
    }

    private IEnumerator PoseRoutine(Vector2 offset, Vector2 scale, float seconds)
    {
        Vector2 startOffset = _poseOffset;
        Vector2 startScale = _poseScale;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, seconds));
            _poseOffset = Vector2.Lerp(startOffset, offset, k);
            _poseScale = Vector2.Lerp(startScale, scale, k);
            yield return null;
        }

        _poseOffset = offset;
        _poseScale = scale;
        _poseRoutine = null;
    }

    private IEnumerator DriftTowardPlayer(float maxSeconds, float stopDistance)
    {
        Player player = GetPlayer();
        for (float t = 0f; t < maxSeconds && player != null && _state != BossState.Dead; t += Time.deltaTime)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            if (toPlayer.magnitude <= stopDistance)
                break;

            SetMoving(true);
            Vector2 next = (Vector2)transform.position + toPlayer.normalized * (CurrentMoveSpeed * Time.deltaTime);
            transform.position = ClampInside(next, 3f);
            yield return null;
        }

        SetMoving(false);
    }

    private IEnumerator DriftTo(Vector2 target, float speed, float maxSeconds)
    {
        for (float t = 0f; t < maxSeconds && _state != BossState.Dead; t += Time.deltaTime)
        {
            Vector2 delta = target - (Vector2)transform.position;
            if (delta.magnitude < 0.1f)
                break;

            SetMoving(true);
            Vector2 next = (Vector2)transform.position + delta.normalized * Mathf.Min(delta.magnitude, speed * Time.deltaTime);
            transform.position = ClampInside(next, 3f);
            yield return null;
        }

        SetMoving(false);
    }

    /// <summary>Telegraph length for a base time, scaled by the phase and never below its minimum.</summary>
    private float Tele(float baseSeconds)
    {
        BossPhase phase = CurrentPhase;
        return Mathf.Max(phase.telegraphMinSeconds, baseSeconds * phase.telegraphScale);
    }

    private T Track<T>(T visual) where T : Component
    {
        visual.transform.SetParent(_effectsRoot, true);
        return visual;
    }

    private GameObject TrackObject(GameObject go)
    {
        if (go != null)
            go.transform.SetParent(_effectsRoot, true);
        return go;
    }
}
