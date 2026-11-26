using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public sealed class EnemyUniversal : MonoBehaviour, IDamageable, ISlowable, IPullable, IVulnerable, IStunnable, IAirborne
{
    public enum State { Idle, Patrol, Chase, Attack, Hurt, Dead, ReturnHome }
    public enum AttackType { Melee, Area, Projectile }
    public enum AreaMode { LocalHitboxes, Radius }

    [Serializable]
    public sealed class AttackProfile
    {
        public string name = "Attack";
        public AttackType type = AttackType.Melee;
        [Min(0f)] public float activationRange = 1.2f;
        [Min(0f)] public float damage = 10f;
        [Min(0f)] public float knockbackForce = 2.5f;
        [Min(0f)] public float cooldown = 1f;
        public string animatorTrigger = "Attack";

        // Melee
        public bool launchForward;
        [Min(0f)] public float launchDelay;
        [Min(0f)] public float launchDistance = 0.5f;
        [Min(0.01f)] public float launchDuration = 0.15f;

        public UniversalEnemyAttackHitbox meleeHitboxDown;
        public UniversalEnemyAttackHitbox meleeHitboxLeft;
        public UniversalEnemyAttackHitbox meleeHitboxRight;
        public UniversalEnemyAttackHitbox meleeHitboxUp;

        // Local area
        public UniversalEnemyAttackHitbox[] hitboxes;

        // Area
        public AreaMode areaMode = AreaMode.LocalHitboxes;
        [Min(0f)] public float areaRadius = 1.5f;
        public LayerMask areaTargetLayers = ~0;

        // Projectile
        public UniversalEnemyProjectile projectilePrefab;
        public Transform projectileOrigin;
        public Vector2 projectileSpawnOffset;
        public bool rotateProjectileOffsetWithDirection = true;
        [Min(0f)] public float projectileSpeed = 5f;

        [NonSerialized] public float lastUsedTime = float.NegativeInfinity;
        [NonSerialized] public int animatorTriggerHash;
        /// <summary>Directional melee hitbox rects (Down, Left, Right, Up) relative to the enemy
        /// pivot in world units; used to attack only when the swing can actually connect.</summary>
        [NonSerialized] public Rect[] meleeReach;
        [NonSerialized] public bool hasMeleeReach;

        public void InitializeRuntime()
        {
            lastUsedTime = float.NegativeInfinity;
            animatorTriggerHash = Animator.StringToHash(animatorTrigger);
        }
    }

    [Header("References")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _player;

    [Header("Quest Identity")]
    [Tooltip("Stable enemyId for Kill objectives (e.g. 'enemy.slime.green'). Empty means this " +
        "instance never satisfies a Kill objective.")]
    [SerializeField] private string _enemyId;
    [Tooltip("Optional stable areaId this instance counts as being in for Kill objectives that " +
        "require a specific area. Empty matches an objective with no area requirement.")]
    [SerializeField] private string _areaId;

    [SerializeField] private string _displayName;

    [Header("Stats")]
    [Tooltip("Inclusive minimum and maximum encounter level.")]
    [SerializeField] private Vector2Int _levelRange = Vector2Int.one;
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [SerializeField, Min(0f)] private float _patrolSpeed = 1f;
    [SerializeField, Min(0f)] private float _chaseSpeed = 2f;
    [SerializeField, Min(0f)] private float _hurtDuration = 0.3f;
    [SerializeField, Min(0f)] private float _deathLifetime = 3f;
    [SerializeField, Min(0)] private int _experienceReward = 25;

    [Header("AI")]
    [SerializeField, Min(0f)] private float _detectionRange = 4f;
    [SerializeField, Min(0f)] private float _chaseRange = 8f;
    [SerializeField, Min(0f)] private float _patrolRadius = 3f;
    [SerializeField, Min(0f)] private float _idleDuration = 2f;
    [Tooltip("Detection and continued chase need an unobstructed line to the player (D-109).")]
    [SerializeField] private bool _requireLineOfSight = true;
    [Tooltip("Within this distance the enemy notices the player even without line of sight " +
        "(hearing / close awareness), so a thin tree trunk cannot hide the player point-blank.")]
    [SerializeField, Min(0f)] private float _awarenessRadius = 2.5f;
    [Tooltip("Seconds the enemy keeps hunting the last seen player position after losing sight.")]
    [SerializeField, Min(0f)] private float _loseSightMemory = 3f;
    [Tooltip("How fast velocity ramps toward the steering target (units/s²). Higher = snappier.")]
    [SerializeField, Min(1f)] private float _acceleration = 14f;
    [SerializeField] private AttackProfile[] _attacks;

    private static readonly int InputX = Animator.StringToHash("InputX");
    private static readonly int InputY = Animator.StringToHash("InputY");
    private static readonly int LastInputX = Animator.StringToHash("LastInputX");
    private static readonly int LastInputY = Animator.StringToHash("LastInputY");
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int IsHit = Animator.StringToHash("isHit");
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    private State _state;
    private Vector2 _home;
    private Vector2 _patrolTarget;
    private Vector2 _desiredVelocity;
    private Vector2 _lastDirection = Vector2.down;
    private float _stateEnteredAt;
    private AttackProfile _activeAttack;
    private Player _playerComponent;
    private Transform _playerTransform;
    private Coroutine _hurtRoutine;
    private Coroutine _launchRoutine;
    private Vector2 _launchVelocity;
    private float _currentHealth;
    private bool _experienceGranted;
    private PathFollower _navigator;
    private FacingFilter _facing;
    private Vector2 _lastSeenPlayer;
    private float _lastSeenPlayerAt = float.NegativeInfinity;
    private bool _separationRegistered;
    private bool _started;
    private Collider2D _playerBody;

    private const float MaxPatrolDuration = 8f;
    /// <summary>Safety net: an attack state that outlives this (e.g. the animator could not enter the
    /// attack clip, so FinishAttackAnimation never fired) is finished from code.</summary>
    private const float AttackWatchdogSeconds = 2.5f;

    public float Health => _currentHealth;
    public float MaxHealth => _maxHealth * EncounterLevel.HealthMultiplier(Level);
    public int Level { get; private set; } = 1;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? gameObject.name : _displayName;
    private float AttackDamageMultiplier => EncounterLevel.DamageMultiplier(Level);
    public bool IsDead => _state == State.Dead;
    public event Action<float, float> HealthChanged;
    public event Action ReturnedHome;

    /// <summary>Fires exactly once on death, before the delayed corpse Destroy(). BossDefeatTracker
    /// (Assets/Scripts/World/) subscribes to capture defeated state before this GameObject is gone.</summary>
    public event Action Died;

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_animator == null) _animator = GetComponent<Animator>();
        Level = EncounterLevel.Roll(_levelRange);
        _currentHealth = MaxHealth;
        _navigator = new PathFollower(_rigidbody);
        _facing = new FacingFilter(Vector2.down);
        InitializeAttacks();
        InitializeHitboxes();
        InitializeMeleeReach();
    }

    private void Start()
    {
        _home = transform.position;
        ResolvePlayer();
        EnterState(State.Idle);
        HealthChanged?.Invoke(_currentHealth, MaxHealth);
        _started = true;
        RegisterSeparation();
    }

    private void OnEnable()
    {
        if (_started && !_separationRegistered)
            RegisterSeparation();
    }

    private void OnDisable()
    {
        CrowdSeparation.Unregister(_rigidbody);
        _separationRegistered = false;
    }

    private void RegisterSeparation()
    {
        if (_rigidbody == null || _state == State.Dead)
            return;
        CrowdSeparation.Register(_rigidbody, _navigator.Footprint.HalfExtents.x);
        _separationRegistered = true;
    }

    private void Update()
    {
        if (_state == State.Dead) return;
        _desiredVelocity = Vector2.zero;

        // Stun freezes AI and movement entirely; checked before the pull so a stunned enemy is not dragged.
        if (IsStunned)
            return;

        if (TryApplyPullMovement())
            return;

        switch (_state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.ReturnHome: UpdateReturnHome(); break;
            case State.Attack:
                if (Time.time - _stateEnteredAt > AttackWatchdogSeconds)
                    FinishAttackAnimation();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null)
            return;

        // An active pull (e.g. a vortex's suction) takes priority over the brief Hurt state that
        // TakeDamage enters on every tick -- otherwise the per-tick damage would repeatedly block
        // velocity sync here and the pull would stutter instead of reading as continuous.
        bool isPulling = Time.time < _pullUntil;
        if (!isPulling && _state == State.Hurt)
            return;

        if (_state == State.Dead)
            _rigidbody.linearVelocity = Vector2.zero;
        else if (_launchRoutine != null || isPulling)
            _rigidbody.linearVelocity = _launchRoutine != null ? _launchVelocity : _desiredVelocity;
        else
            // Ramp toward the steering velocity: no instant snaps when turning or stopping.
            _rigidbody.linearVelocity = Vector2.MoveTowards(_rigidbody.linearVelocity, _desiredVelocity,
                _acceleration * Time.fixedDeltaTime);
    }

    /// <summary>Restore-only: removes an already-defeated boss silently on scene load -- no
    /// TakeDamage/state machine, no HealthChanged/Died/EnemyKilled event, no death animation, no
    /// duplicate experience/kill credit. Called by BossDefeatTracker.RestoreState.</summary>
    public void RestoreDefeated()
    {
        if (_state == State.Dead)
            return;

        _state = State.Dead;
        _currentHealth = 0f;
        gameObject.SetActive(false);
    }

    public void ApplyAirborne(float duration, float height)
    {
        if (IsDead)
            return;

        // Airborne = unable to act for the duration (stun) + lifted visuals drawn by EnemyAirborneVisual.
        ApplyStun(duration);
        EnemyAirborneVisual visual = GetComponent<EnemyAirborneVisual>();
        if (visual == null)
            visual = gameObject.AddComponent<EnemyAirborneVisual>();
        visual.Begin(duration, height);
    }

    private float _stunUntil;

    public bool IsStunned => Time.time < _stunUntil;

    public void ApplyStun(float duration)
    {
        if (IsDead)
            return;

        _stunUntil = Mathf.Max(_stunUntil, Time.time + Mathf.Max(0f, duration));
        SoundFXManager.PlaySfxAt(SfxIds.CombatStunApply, transform.position);
    }

    private float _vulnerableMultiplier = 1f;
    private float _vulnerableUntil;

    public void ApplyVulnerability(float damageTakenMultiplier, float duration)
    {
        if (IsDead)
            return;

        float until = Time.time + Mathf.Max(0f, duration);
        if (Time.time >= _vulnerableUntil || damageTakenMultiplier >= _vulnerableMultiplier)
            _vulnerableMultiplier = Mathf.Max(1f, damageTakenMultiplier);
        _vulnerableUntil = Mathf.Max(_vulnerableUntil, until);
    }

    private float CurrentDamageTakenMultiplier => Time.time < _vulnerableUntil ? _vulnerableMultiplier : 1f;

    private float _slowMultiplier = 1f;
    private float _slowUntil;

    public void ApplySlow(float speedMultiplier, float duration)
    {
        if (IsDead)
            return;

        float until = Time.time + Mathf.Max(0f, duration);
        if (until >= _slowUntil)
        {
            _slowMultiplier = Mathf.Clamp01(speedMultiplier);
            _slowUntil = until;
        }
    }

    private float CurrentSlowMultiplier => Time.time < _slowUntil ? _slowMultiplier : 1f;

    private Vector2 _pullTarget;
    private float _pullSpeed;
    private float _pullUntil;

    public void ApplyPull(Vector2 targetPosition, float speed, float duration)
    {
        if (IsDead)
            return;

        _pullTarget = targetPosition;
        _pullSpeed = speed;
        _pullUntil = Time.time + Mathf.Max(0f, duration);
    }

    /// <summary>While an active pull hasn't expired, overrides normal AI movement for this frame so
    /// the enemy visibly travels toward the pull source (e.g. a vortex center) instead of its usual
    /// idle/patrol/chase behavior.</summary>
    private bool TryApplyPullMovement()
    {
        if (Time.time >= _pullUntil)
            return false;

        Vector2 toTarget = _pullTarget - (Vector2)transform.position;
        if (toTarget.sqrMagnitude <= 0.0025f)
        {
            _desiredVelocity = Vector2.zero;
            return true;
        }

        Vector2 direction = toTarget.normalized;
        Face(direction);
        _desiredVelocity = direction * _pullSpeed;
        _animator.SetBool(IsWalking, true);
        _animator.SetBool(IsRunning, false);
        return true;
    }

    public void TakeDamage(float damage, Vector2 direction = default, float knockbackForce = 0f)
    {
        if (_state == State.Dead || damage <= 0f) return;
        damage *= CurrentDamageTakenMultiplier;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        HealthChanged?.Invoke(_currentHealth, MaxHealth);
        if (_currentHealth <= 0f) { EnterState(State.Dead); return; }

        EnemySfx.Hurt(this);
        EnterState(State.Hurt);
        if (_rigidbody != null && direction != Vector2.zero && knockbackForce > 0f)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.AddForce(direction.normalized * knockbackForce, ForceMode2D.Impulse);
        }
        if (_hurtRoutine != null)
            StopCoroutine(_hurtRoutine);
        _hurtRoutine = StartCoroutine(FinishHurtAfterDelay());
    }

    private void UpdateIdle()
    {
        if (CanSeePlayer()) { EnterState(State.Chase); return; }
        if (Time.time - _stateEnteredAt >= _idleDuration)
        {
            _patrolTarget = PickPatrolTarget();
            EnterState(State.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        if (CanSeePlayer()) { EnterState(State.Chase); return; }
        // Unreachable or blocked patrol point: rest and pick another instead of pushing into it.
        if (!MoveTowards(_patrolTarget, false) || Time.time - _stateEnteredAt > MaxPatrolDuration)
        {
            EnterState(State.Idle);
            return;
        }
        if (IsNear(_patrolTarget, 0.1f)) EnterState(State.Idle);
    }

    private void UpdateChase()
    {
        if (!HasLivingPlayer() || !IsNear(_home, _chaseRange))
        {
            EnterState(State.ReturnHome);
            return;
        }

        bool visible = HasLineOfSightToPlayer();
        if (visible)
        {
            _lastSeenPlayer = _playerTransform.position;
            _lastSeenPlayerAt = Time.time;
        }
        else if (Time.time - _lastSeenPlayerAt > _loseSightMemory)
        {
            // Lost the player behind cover for too long: give up and go home.
            EnterState(State.ReturnHome);
            return;
        }

        if (visible)
        {
            AttackProfile attack = SelectReadyAttack(out Vector2 attackFacing);
            if (attack != null) { BeginAttack(attack, attackFacing); return; }

            // Melee with known hitboxes: walk to a spot where the swing actually overlaps the
            // player's body, then hold there while the attack cools down.
            AttackProfile melee = PrimaryMeleeWithReach();
            if (melee != null && IsNear(PlayerAimPoint(), melee.activationRange + 2f))
            {
                if (WouldMeleeHit(melee, CurrentPosition, out Vector2 facing))
                {
                    HoldFacing(facing);
                    return;
                }

                if (TryGetAttackSlot(melee, out Vector2 slot))
                {
                    if (IsNear(slot, 0.08f))
                    {
                        HoldFacing(FacingFilter.ToCardinal(PlayerAimPoint() - CurrentPosition));
                        return;
                    }
                    if (MoveTowards(slot, true))
                        return;
                }
            }
            else if (IsHoldingAtAttackRange())
            {
                // Other attack types: hold at range facing the player instead of shoving into them.
                HoldFacing((Vector2)_playerTransform.position - CurrentPosition);
                return;
            }
        }

        // Not visible: hunt the last seen position, then wait there until memory runs out.
        Vector2 target = visible ? (Vector2)_playerTransform.position : _lastSeenPlayer;
        if (!visible && IsNear(target, 0.4f))
        {
            _desiredVelocity = Vector2.zero;
            _animator.SetBool(IsWalking, false);
            _animator.SetBool(IsRunning, false);
            return;
        }

        if (!MoveTowards(target, true))
            EnterState(State.ReturnHome);
    }

    private Vector2 CurrentPosition => _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;

    private void HoldFacing(Vector2 direction)
    {
        _desiredVelocity = Vector2.zero;
        Face(direction);
        _animator.SetBool(IsWalking, false);
        _animator.SetBool(IsRunning, false);
    }

    private AttackProfile PrimaryMeleeWithReach()
    {
        if (_attacks == null) return null;
        foreach (AttackProfile attack in _attacks)
            if (attack != null && attack.type == AttackType.Melee && attack.hasMeleeReach)
                return attack;
        return null;
    }

    private static int ReachIndex(Vector2 cardinal)
        => cardinal.x < 0f ? 1 : cardinal.x > 0f ? 2 : cardinal.y > 0f ? 3 : 0;

    /// <summary>True if the directional hitbox (toward the player) would overlap the player's body
    /// collider with the enemy pivot at <paramref name="pivot"/>.</summary>
    private bool WouldMeleeHit(AttackProfile attack, Vector2 pivot, out Vector2 facing)
    {
        facing = _lastDirection;
        if (!attack.hasMeleeReach || !HasLivingPlayer())
            return false;

        Bounds body = PlayerBodyBounds();
        Rect target = new(body.min.x - 0.05f, body.min.y - 0.05f, body.size.x + 0.1f, body.size.y + 0.1f);
        Vector2 toPlayer = (Vector2)body.center - pivot;
        Vector2 primary = FacingFilter.ToCardinal(toPlayer);
        Vector2 secondary = Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y)
            ? new Vector2(0f, toPlayer.y >= 0f ? 1f : -1f)
            : new Vector2(toPlayer.x >= 0f ? 1f : -1f, 0f);
        foreach (Vector2 candidate in new[] { primary, secondary })
        {
            Rect reach = attack.meleeReach[ReachIndex(candidate)];
            reach.position += pivot;
            if (reach.width > 0f && reach.Overlaps(target))
            {
                facing = candidate;
                return true;
            }
        }
        return false;
    }

    /// <summary>Pivot position that centres the hitbox facing the player on the player's body,
    /// choosing the nearest reachable of the four sides.</summary>
    private bool TryGetAttackSlot(AttackProfile attack, out Vector2 slot)
    {
        slot = default;
        Vector2 bodyCenter = PlayerBodyBounds().center;
        Vector2 position = CurrentPosition;
        PathGridBuilder grid = PathGridBuilder.Active;
        float best = float.MaxValue;
        Vector2[] sides = { Vector2.down, Vector2.left, Vector2.right, Vector2.up };
        foreach (Vector2 side in sides)
        {
            Rect reach = attack.meleeReach[ReachIndex(side)];
            if (reach.width <= 0f)
                continue;
            Vector2 candidate = bodyCenter - reach.center;
            if (grid != null && !grid.IsWalkable(candidate, _navigator.Footprint))
                continue;
            float cost = (candidate - position).sqrMagnitude;
            if (cost < best)
            {
                best = cost;
                slot = candidate;
            }
        }
        return best < float.MaxValue;
    }

    private Bounds PlayerBodyBounds()
    {
        if (_playerBody != null && _playerBody.enabled)
            return _playerBody.bounds;
        return new Bounds(_playerTransform.position, new Vector3(0.5f, 0.3f, 0f));
    }

    private void InitializeMeleeReach()
    {
        if (_attacks == null) return;
        foreach (AttackProfile attack in _attacks)
        {
            if (attack == null || attack.type != AttackType.Melee) continue;
            attack.meleeReach = new Rect[4];
            bool any = false;
            any |= TryGetLocalReach(attack.meleeHitboxDown, out attack.meleeReach[0]);
            any |= TryGetLocalReach(attack.meleeHitboxLeft, out attack.meleeReach[1]);
            any |= TryGetLocalReach(attack.meleeHitboxRight, out attack.meleeReach[2]);
            any |= TryGetLocalReach(attack.meleeHitboxUp, out attack.meleeReach[3]);
            attack.hasMeleeReach = any;
        }
    }

    /// <summary>Hitbox shape rect relative to this enemy's pivot (works while the collider is
    /// disabled, since it reads the shape rather than runtime bounds).</summary>
    private bool TryGetLocalReach(UniversalEnemyAttackHitbox hitbox, out Rect rect)
    {
        rect = default;
        if (hitbox == null || !hitbox.TryGetComponent(out Collider2D collider))
            return false;

        Vector2 min, max;
        switch (collider)
        {
            case BoxCollider2D box:
                min = box.offset - box.size * 0.5f; max = box.offset + box.size * 0.5f; break;
            case CircleCollider2D circle:
                min = circle.offset - Vector2.one * circle.radius; max = circle.offset + Vector2.one * circle.radius; break;
            case CapsuleCollider2D capsule:
                min = capsule.offset - capsule.size * 0.5f; max = capsule.offset + capsule.size * 0.5f; break;
            case PolygonCollider2D polygon when polygon.points.Length > 0:
                min = max = polygon.points[0];
                foreach (Vector2 point in polygon.points) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
                min += polygon.offset; max += polygon.offset; break;
            default:
                return false;
        }

        Vector2 a = (Vector2)collider.transform.TransformPoint(min) - (Vector2)transform.position;
        Vector2 b = (Vector2)collider.transform.TransformPoint(max) - (Vector2)transform.position;
        rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        return true;
    }

    private bool IsHoldingAtAttackRange()
    {
        if (_attacks == null) return false;
        foreach (AttackProfile attack in _attacks)
        {
            if (attack != null && IsNear(_playerTransform.position, attack.activationRange * 0.9f)
                && HasAttackLineOfSight(attack))
                return true;
        }
        return false;
    }

    private bool HasLineOfSightToPlayer()
    {
        if (!HasLivingPlayer()) return false;
        PathGridBuilder grid = PathGridBuilder.Active;
        if (!_requireLineOfSight || grid == null) return true;
        if (IsNear(PlayerAimPoint(), _awarenessRadius)) return true;

        // Two parallel rays half a body apart: only obstacles wider than a trunk hide the player.
        Vector2 eye = EyePosition();
        Vector2 aim = PlayerAimPoint();
        Vector2 side = Vector2.Perpendicular((aim - eye).normalized) * 0.3f;
        return grid.HasLineOfSight(eye + side, aim + side) || grid.HasLineOfSight(eye - side, aim - side);
    }

    private Vector2 EyePosition()
        => _rigidbody != null ? _rigidbody.position + _navigator.Footprint.Offset : (Vector2)transform.position;

    private Vector2 PlayerAimPoint()
        => _playerBody != null && _playerBody.enabled ? (Vector2)_playerBody.bounds.center : (Vector2)_playerTransform.position;

    private void UpdateReturnHome()
    {
        if (!MoveTowards(_home, true))
        {
            // Home unreachable from here (e.g. pushed across water): settle at the current spot.
            _home = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            EnterState(State.Idle);
            return;
        }
        if (IsNear(_home, 0.1f))
        {
            if (_rigidbody != null)
                _rigidbody.position = _home;
            else
                transform.position = _home;

            ReturnedHome?.Invoke();
            EnterState(State.Idle);
        }
    }

    private AttackProfile SelectReadyAttack(out Vector2 facing)
    {
        facing = (Vector2)_playerTransform.position - CurrentPosition;
        if (_attacks == null) return null;
        foreach (AttackProfile attack in _attacks)
        {
            if (attack == null || Time.time - attack.lastUsedTime < attack.cooldown
                || !IsNear(_playerTransform.position, attack.activationRange + (attack.hasMeleeReach ? 1f : 0f)))
                continue;

            // Melee with known hitboxes: only swing when the swing will actually connect.
            if (attack.type == AttackType.Melee && attack.hasMeleeReach)
            {
                if (WouldMeleeHit(attack, CurrentPosition, out Vector2 meleeFacing))
                {
                    facing = meleeFacing;
                    return attack;
                }
                continue;
            }

            if (IsNear(_playerTransform.position, attack.activationRange) && HasAttackLineOfSight(attack))
                return attack;
        }
        return null;
    }

    /// <summary>Melee/projectile attacks need a clear line to the player so enemies walk around a
    /// tree instead of swinging through it (D-109). Area attacks and scenes without a grid skip this.</summary>
    private bool HasAttackLineOfSight(AttackProfile attack)
    {
        PathGridBuilder grid = PathGridBuilder.Active;
        if (grid == null || attack.type == AttackType.Area)
            return true;

        return grid.HasLineOfSight(EyePosition(), PlayerAimPoint());
    }

    private void BeginAttack(AttackProfile attack, Vector2 facing)
    {
        _activeAttack = attack;
        Face(facing, true);
        EnterState(State.Attack);
        EnemySfx.Attack(this);
        _animator.SetTrigger(attack.animatorTriggerHash);
        if (attack.type == AttackType.Melee)
            BeginLaunch(attack);
    }

    private void BeginLaunch(AttackProfile attack)
    {
        if (!attack.launchForward || attack.launchDistance <= 0f)
            return;

        StopLaunch();
        _launchRoutine = StartCoroutine(LaunchForward(
            attack.launchDelay,
            attack.launchDistance,
            attack.launchDuration));
    }

    private IEnumerator LaunchForward(float delay, float distance, float duration)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (_state != State.Attack)
        {
            _launchRoutine = null;
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        Vector2 launchDirection = GetLaunchDirectionToPlayer();
        _launchVelocity = launchDirection * (distance / duration);
        yield return new WaitForSeconds(duration);

        _launchRoutine = null;
        _launchVelocity = Vector2.zero;
        if (_rigidbody != null && _state == State.Attack)
            _rigidbody.linearVelocity = Vector2.zero;
    }

    private Vector2 GetLaunchDirectionToPlayer()
    {
        if (_playerTransform != null)
        {
            Vector2 direction = (Vector2)_playerTransform.position - (Vector2)transform.position;
            if (direction.sqrMagnitude > Mathf.Epsilon)
                return direction.normalized;
        }

        return _lastDirection == Vector2.zero ? Vector2.down : _lastDirection.normalized;
    }

    private void StopLaunch()
    {
        if (_launchRoutine != null)
        {
            StopCoroutine(_launchRoutine);
            _launchRoutine = null;
        }

        _launchVelocity = Vector2.zero;
    }

    // Animation Events. A clip may call these repeatedly for multi-hit attacks.
    public void OpenAttackWindow()
    {
        if (_state != State.Attack || _activeAttack == null) return;

        switch (_activeAttack.type)
        {
            case AttackType.Melee:
                OpenDirectionalMeleeHitbox();
                break;
            case AttackType.Area:
                if (_activeAttack.areaMode == AreaMode.Radius)
                    DamagePlayersInArea(_activeAttack);
                else
                    OpenConfiguredHitboxes();
                break;
        }
    }

    private void OpenDirectionalMeleeHitbox()
    {
        UniversalEnemyAttackHitbox hitbox = GetDirectionalMeleeHitbox(_activeAttack, _lastDirection);
        hitbox?.Open(_activeAttack.damage * AttackDamageMultiplier, _activeAttack.knockbackForce);
    }

    private static UniversalEnemyAttackHitbox GetDirectionalMeleeHitbox(
        AttackProfile attack,
        Vector2 direction)
    {
        if (attack == null) return null;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x >= 0f ? attack.meleeHitboxRight : attack.meleeHitboxLeft;

        return direction.y > 0f ? attack.meleeHitboxUp : attack.meleeHitboxDown;
    }

    private void OpenConfiguredHitboxes()
    {
        if (_activeAttack?.hitboxes == null) return;
        foreach (UniversalEnemyAttackHitbox hitbox in _activeAttack.hitboxes)
        {
            if (hitbox == null) continue;
            hitbox.Open(_activeAttack.damage * AttackDamageMultiplier, _activeAttack.knockbackForce);
        }
    }

    private void DamagePlayersInArea(AttackProfile attack)
    {
        if (attack.areaRadius <= 0f) return;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(
            transform.position,
            attack.areaRadius,
            attack.areaTargetLayers);
        HashSet<Player> damagedPlayers = new();

        foreach (Collider2D overlap in overlaps)
        {
            Player player = overlap != null ? overlap.GetComponentInParent<Player>() : null;
            if (player == null || player.IsDead || !damagedPlayers.Add(player)) continue;

            Vector2 direction = (player.transform.position - transform.position).normalized;
            player.TakeDamage(attack.damage * AttackDamageMultiplier, direction, attack.knockbackForce);
        }
    }

    // Compatibility with the existing Slime attack clips.
    public void ActivateAttackHitbox()
    {
        OpenAttackWindow();
    }

    public void CloseAttackWindow()
    {
        if (_activeAttack == null) return;

        CloseDirectionalMeleeHitboxes(_activeAttack);
        if (_activeAttack.hitboxes == null) return;
        foreach (UniversalEnemyAttackHitbox hitbox in _activeAttack.hitboxes)
            hitbox?.Close();
    }

    // Animation Event compatibility shared with Player attack clips.
    public void CloseAttackHitbox()
    {
        CloseAttackWindow();
    }

    private static void CloseDirectionalMeleeHitboxes(AttackProfile attack)
    {
        attack.meleeHitboxDown?.Close();
        attack.meleeHitboxLeft?.Close();
        attack.meleeHitboxRight?.Close();
        attack.meleeHitboxUp?.Close();
    }

    public void FireProjectile()
    {
        if (_state != State.Attack || _activeAttack?.type != AttackType.Projectile
            || _activeAttack.projectilePrefab == null) return;

        EnemySfx.ProjectileLaunch(this);
        Transform origin = _activeAttack.projectileOrigin != null ? _activeAttack.projectileOrigin : transform;
        Vector2 offset = _activeAttack.projectileSpawnOffset;
        if (_activeAttack.rotateProjectileOffsetWithDirection)
            offset = RotateFromDown(offset, _lastDirection);

        Vector2 spawnPosition = (Vector2)origin.position + offset;
        UniversalEnemyProjectile projectile = Instantiate(
            _activeAttack.projectilePrefab,
            spawnPosition,
            Quaternion.identity);
        projectile.Launch(_lastDirection, _activeAttack.projectileSpeed, _activeAttack.damage * AttackDamageMultiplier, _activeAttack.knockbackForce);
    }

    private static Vector2 RotateFromDown(Vector2 value, Vector2 direction)
    {
        if (direction == Vector2.zero) direction = Vector2.down;
        float angle = Vector2.SignedAngle(Vector2.down, direction);
        return (Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)value);
    }

    public void FinishAttackAnimation()
    {
        if (_state != State.Attack) return;
        CloseAttackWindow();
        _activeAttack.lastUsedTime = Time.time;
        _activeAttack = null;
        EnterState(State.Chase);
    }

    private IEnumerator FinishHurtAfterDelay()
    {
        yield return new WaitForSeconds(_hurtDuration);
        _hurtRoutine = null;
        if (_state != State.Hurt) yield break;
        _rigidbody.linearVelocity = Vector2.zero;
        EnterState(HasLivingPlayer() ? State.Chase : State.Idle);
    }

    private void EnterState(State state)
    {
        CloseAttackWindow();
        if (_state == State.Attack && state != State.Attack)
        {
            // Drop an unconsumed attack trigger so a stale one cannot replay the swing later.
            if (_activeAttack != null)
                _animator.ResetTrigger(_activeAttack.animatorTriggerHash);
            StopLaunch();
            _activeAttack = null;
        }
        _state = state;
        _stateEnteredAt = Time.time;
        _navigator?.Reset();
        if (state == State.Chase && HasLivingPlayer())
        {
            // Entering a chase (spotted, or hit from behind cover): remember where the player is.
            _lastSeenPlayer = _playerTransform.position;
            _lastSeenPlayerAt = Time.time;
        }
        _desiredVelocity = Vector2.zero;
        _animator.SetBool(IsWalking, state is State.Patrol or State.Chase or State.ReturnHome);
        _animator.SetBool(IsRunning, state is State.Chase or State.ReturnHome);

        if (state == State.Hurt) _animator.SetTrigger(IsHit);
        if (state != State.Dead) return;

        EnemySfx.Death(this);
        GrantExperience();
        if (!string.IsNullOrEmpty(_enemyId))
            QuestDomainEvents.RaiseEnemyKilled(_enemyId, _areaId);
        Died?.Invoke();

        if (_hurtRoutine != null)
        {
            StopCoroutine(_hurtRoutine);
            _hurtRoutine = null;
        }

        _animator.SetBool(IsDeadHash, true);
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.simulated = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        CrowdSeparation.Unregister(_rigidbody);
        Destroy(gameObject, _deathLifetime);
    }

    private void GrantExperience()
    {
        if (_experienceGranted || _experienceReward <= 0)
            return;

        _experienceGranted = true;
        PlayerStat.Instance?.AddExperience(_experienceReward);
    }

    /// <summary>Steers toward <paramref name="target"/> around obstacles (D-109). Returns false when
    /// the navigator reports the target unreachable; the caller decides the fallback state.</summary>
    private bool MoveTowards(Vector2 target, bool running)
    {
        Vector2 position = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
        PathFollower.Status status = _navigator.Steer(position, target, out Vector2 direction);
        if (status == PathFollower.Status.NoPath)
        {
            _desiredVelocity = Vector2.zero;
            return false;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            _desiredVelocity = Vector2.zero;
            return true;
        }

        if (status != PathFollower.Status.Legacy)
            direction = CrowdSeparation.Apply(_rigidbody, _navigator.Footprint.HalfExtents.x, direction);
        Face(direction);
        float speed = running ? _chaseSpeed : _patrolSpeed;
        _desiredVelocity = direction * speed * CurrentSlowMultiplier;
        _animator.SetBool(IsWalking, true);
        _animator.SetBool(IsRunning, running);
        return true;
    }

    private Vector2 PickPatrolTarget()
    {
        PathGridBuilder grid = PathGridBuilder.Active;
        Vector2 candidate = _home + UnityEngine.Random.insideUnitCircle * _patrolRadius;
        if (grid == null)
            return candidate;

        for (int attempt = 0; attempt < 6 && !grid.IsWalkable(candidate, _navigator.Footprint); attempt++)
            candidate = _home + UnityEngine.Random.insideUnitCircle * _patrolRadius;
        return candidate;
    }

    /// <summary>Turns the sprite with hysteresis (see <see cref="FacingFilter"/>); pass
    /// <paramref name="immediate"/> for attack wind-ups so the swing aims at the player.</summary>
    private void Face(Vector2 direction, bool immediate = false)
    {
        _lastDirection = _facing != null
            ? _facing.Update(direction, Time.time, immediate)
            : FacingFilter.ToCardinal(direction);
        _animator.SetFloat(InputX, _lastDirection.x);
        _animator.SetFloat(InputY, _lastDirection.y);
        _animator.SetFloat(LastInputX, _lastDirection.x);
        _animator.SetFloat(LastInputY, _lastDirection.y);
    }

    private void InitializeHitboxes()
    {
        if (_attacks == null) return;
        foreach (AttackProfile attack in _attacks)
        {
            if (attack == null) continue;

            attack.meleeHitboxDown?.Initialize(this);
            attack.meleeHitboxLeft?.Initialize(this);
            attack.meleeHitboxRight?.Initialize(this);
            attack.meleeHitboxUp?.Initialize(this);

            if (attack.hitboxes != null)
                foreach (UniversalEnemyAttackHitbox hitbox in attack.hitboxes)
                    hitbox?.Initialize(this);
        }
    }

    private void InitializeAttacks()
    {
        if (_attacks == null) return;
        foreach (AttackProfile attack in _attacks)
            attack?.InitializeRuntime();
    }

    private void ResolvePlayer()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player");
        _playerComponent = _player != null ? _player.GetComponent<Player>() : null;
        _playerTransform = _player != null ? _player.transform : null;
        _playerBody = null;
        if (_player != null)
            foreach (Collider2D collider in _player.GetComponents<Collider2D>())
                if (!collider.isTrigger) { _playerBody = collider; break; }
    }

    private bool HasLivingPlayer() => _playerTransform != null && (_playerComponent == null || !_playerComponent.IsDead);
    private bool CanSeePlayer()
    {
        if (_playerTransform == null) ResolvePlayer();
        return HasLivingPlayer() && IsNear(_playerTransform.position, _detectionRange) && HasLineOfSightToPlayer();
    }
    private bool IsNear(Vector2 target, float range) => ((Vector2)transform.position - target).sqrMagnitude <= range * range;

    private void OnValidate()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
        if (_animator == null)
            _animator = GetComponent<Animator>();

        _maxHealth = Mathf.Max(1f, _maxHealth);
        _patrolSpeed = Mathf.Max(0f, _patrolSpeed);
        _chaseSpeed = Mathf.Max(0f, _chaseSpeed);
        _hurtDuration = Mathf.Max(0f, _hurtDuration);
        _deathLifetime = Mathf.Max(0f, _deathLifetime);
        _detectionRange = Mathf.Max(0f, _detectionRange);
        _chaseRange = Mathf.Max(_detectionRange, _chaseRange);
        _patrolRadius = Mathf.Max(0f, _patrolRadius);
        _idleDuration = Mathf.Max(0f, _idleDuration);

        if (_attacks == null) return;
        foreach (AttackProfile attack in _attacks)
        {
            if (attack == null) continue;
            attack.activationRange = Mathf.Max(0f, attack.activationRange);
            attack.damage = Mathf.Max(0f, attack.damage);
            attack.knockbackForce = Mathf.Max(0f, attack.knockbackForce);
            attack.cooldown = Mathf.Max(0f, attack.cooldown);
            attack.launchDelay = Mathf.Max(0f, attack.launchDelay);
            attack.launchDistance = Mathf.Max(0f, attack.launchDistance);
            attack.launchDuration = Mathf.Max(0.01f, attack.launchDuration);
            attack.areaRadius = Mathf.Max(0f, attack.areaRadius);
            attack.projectileSpeed = Mathf.Max(0f, attack.projectileSpeed);
            if (string.IsNullOrWhiteSpace(attack.animatorTrigger))
                attack.animatorTrigger = "Attack";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 home = Application.isPlaying ? (Vector3)_home : transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(home, _patrolRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(home, _chaseRange);

        if (_attacks == null)
            return;

        Gizmos.color = Color.red;
        foreach (AttackProfile attack in _attacks)
        {
            if (attack == null) continue;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attack.activationRange);

            if (attack.type == AttackType.Area && attack.areaMode == AreaMode.Radius)
            {
                Gizmos.color = new Color(1f, 0.35f, 0f);
                Gizmos.DrawWireSphere(transform.position, attack.areaRadius);
            }

            if (attack.type == AttackType.Projectile)
            {
                Transform origin = attack.projectileOrigin != null ? attack.projectileOrigin : transform;
                Vector2 direction = Application.isPlaying ? _lastDirection : Vector2.down;
                Vector2 offset = attack.rotateProjectileOffsetWithDirection
                    ? RotateFromDown(attack.projectileSpawnOffset, direction)
                    : attack.projectileSpawnOffset;
                Vector3 spawnPosition = origin.position + (Vector3)offset;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(spawnPosition, 0.1f);
                Gizmos.DrawLine(spawnPosition, spawnPosition + (Vector3)direction);
            }
        }
    }
}
