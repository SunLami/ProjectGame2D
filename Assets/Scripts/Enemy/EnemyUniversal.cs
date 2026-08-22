using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public sealed class EnemyUniversal : MonoBehaviour, IDamageable
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

    [Header("Stats")]
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

    public float Health => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsDead => _state == State.Dead;
    public event Action<float, float> HealthChanged;
    public event Action ReturnedHome;

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_animator == null) _animator = GetComponent<Animator>();
        _currentHealth = _maxHealth;
        InitializeAttacks();
        InitializeHitboxes();
    }

    private void Start()
    {
        _home = transform.position;
        ResolvePlayer();
        EnterState(State.Idle);
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void Update()
    {
        if (_state == State.Dead) return;
        _desiredVelocity = Vector2.zero;

        switch (_state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.ReturnHome: UpdateReturnHome(); break;
        }
    }

    private void FixedUpdate()
    {
        if (_state != State.Hurt && _rigidbody != null)
            _rigidbody.linearVelocity = _state == State.Dead
                ? Vector2.zero
                : _launchRoutine != null ? _launchVelocity : _desiredVelocity;
    }

    public void TakeDamage(float damage, Vector2 direction = default, float knockbackForce = 0f)
    {
        if (_state == State.Dead || damage <= 0f) return;
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
        if (_currentHealth <= 0f) { EnterState(State.Dead); return; }

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
            _patrolTarget = _home + UnityEngine.Random.insideUnitCircle * _patrolRadius;
            EnterState(State.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        if (CanSeePlayer()) { EnterState(State.Chase); return; }
        MoveTowards(_patrolTarget, false);
        if (IsNear(_patrolTarget, 0.1f)) EnterState(State.Idle);
    }

    private void UpdateChase()
    {
        if (!HasLivingPlayer() || !IsNear(_home, _chaseRange))
        {
            EnterState(State.ReturnHome);
            return;
        }

        AttackProfile attack = SelectReadyAttack();
        if (attack != null) { BeginAttack(attack); return; }
        MoveTowards(_playerTransform.position, true);
    }

    private void UpdateReturnHome()
    {
        MoveTowards(_home, true);
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

    private AttackProfile SelectReadyAttack()
    {
        if (_attacks == null) return null;
        foreach (AttackProfile attack in _attacks)
        {
            if (attack != null && IsNear(_playerTransform.position, attack.activationRange)
                && Time.time - attack.lastUsedTime >= attack.cooldown)
                return attack;
        }
        return null;
    }

    private void BeginAttack(AttackProfile attack)
    {
        _activeAttack = attack;
        Face(_playerTransform.position - transform.position);
        EnterState(State.Attack);
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
        hitbox?.Open(_activeAttack.damage, _activeAttack.knockbackForce);
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
            hitbox.Open(_activeAttack.damage, _activeAttack.knockbackForce);
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
            player.TakeDamage(attack.damage, direction, attack.knockbackForce);
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

        Transform origin = _activeAttack.projectileOrigin != null ? _activeAttack.projectileOrigin : transform;
        Vector2 offset = _activeAttack.projectileSpawnOffset;
        if (_activeAttack.rotateProjectileOffsetWithDirection)
            offset = RotateFromDown(offset, _lastDirection);

        Vector2 spawnPosition = (Vector2)origin.position + offset;
        UniversalEnemyProjectile projectile = Instantiate(
            _activeAttack.projectilePrefab,
            spawnPosition,
            Quaternion.identity);
        projectile.Launch(_lastDirection, _activeAttack.projectileSpeed, _activeAttack.damage, _activeAttack.knockbackForce);
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
            StopLaunch();
            _activeAttack = null;
        }
        _state = state;
        _stateEnteredAt = Time.time;
        _desiredVelocity = Vector2.zero;
        _animator.SetBool(IsWalking, state is State.Patrol or State.Chase or State.ReturnHome);
        _animator.SetBool(IsRunning, state is State.Chase or State.ReturnHome);

        if (state == State.Hurt) _animator.SetTrigger(IsHit);
        if (state != State.Dead) return;

        GrantExperience();
        if (!string.IsNullOrEmpty(_enemyId))
            QuestDomainEvents.RaiseEnemyKilled(_enemyId, _areaId);

        if (_hurtRoutine != null)
        {
            StopCoroutine(_hurtRoutine);
            _hurtRoutine = null;
        }

        _animator.SetBool(IsDeadHash, true);
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.simulated = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        Destroy(gameObject, _deathLifetime);
    }

    private void GrantExperience()
    {
        if (_experienceGranted || _experienceReward <= 0)
            return;

        _experienceGranted = true;
        PlayerStat.Instance?.AddExperience(_experienceReward);
    }

    private void MoveTowards(Vector2 target, bool running)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        Face(direction);
        float speed = running ? _chaseSpeed : _patrolSpeed;
        _desiredVelocity = direction * speed;
        _animator.SetBool(IsWalking, true);
        _animator.SetBool(IsRunning, running);
    }

    private void Face(Vector2 direction)
    {
        _lastDirection = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
        _animator.SetFloat(InputX, direction.x);
        _animator.SetFloat(InputY, direction.y);
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
    }

    private bool HasLivingPlayer() => _playerTransform != null && (_playerComponent == null || !_playerComponent.IsDead);
    private bool CanSeePlayer() { if (_playerTransform == null) ResolvePlayer(); return HasLivingPlayer() && IsNear(_playerTransform.position, _detectionRange); }
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
