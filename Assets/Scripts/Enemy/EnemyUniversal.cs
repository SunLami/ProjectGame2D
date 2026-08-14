using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public sealed class EnemyUniversal : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Hurt, Dead, ReturnHome }
    public enum AttackType { Melee, Area, Projectile, Custom }

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
        public UniversalEnemyAttackHitbox[] hitboxes;
        public UniversalEnemyProjectile projectilePrefab;
        public Transform projectileOrigin;
        [Min(0f)] public float projectileSpeed = 5f;

        [NonSerialized] public float lastUsedTime = float.NegativeInfinity;
    }

    [Header("References")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _player;

    [Header("Stats")]
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [SerializeField, Min(0f)] private float _health = 100f;
    [SerializeField, Min(0f)] private float _patrolSpeed = 1f;
    [SerializeField, Min(0f)] private float _chaseSpeed = 2f;
    [SerializeField, Min(0f)] private float _hurtDuration = 0.3f;
    [SerializeField, Min(0f)] private float _deathLifetime = 3f;

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
    private static readonly int IsDead = Animator.StringToHash("isDead");

    private State _state;
    private Vector2 _home;
    private Vector2 _patrolTarget;
    private Vector2 _desiredVelocity;
    private Vector2 _lastDirection = Vector2.down;
    private float _stateEnteredAt;
    private AttackProfile _activeAttack;
    private Player _playerComponent;

    public State CurrentState => _state;
    public float Health => _health;
    public bool IsDeadNow => _state == State.Dead;

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_animator == null) _animator = GetComponent<Animator>();
        _health = Mathf.Clamp(_health, 0f, _maxHealth);
        InitializeHitboxes();
    }

    private void Start()
    {
        _home = transform.position;
        ResolvePlayer();
        EnterState(_health <= 0f ? State.Dead : State.Idle);
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
            _rigidbody.linearVelocity = _state == State.Dead ? Vector2.zero : _desiredVelocity;
    }

    public void TakeDamage(float damage, Vector2 direction = default, float knockbackForce = 0f)
    {
        if (_state == State.Dead || damage <= 0f) return;
        _health = Mathf.Max(0f, _health - damage);
        if (_health <= 0f) { EnterState(State.Dead); return; }

        EnterState(State.Hurt);
        if (_rigidbody != null && direction != Vector2.zero && knockbackForce > 0f)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.AddForce(direction.normalized * knockbackForce, ForceMode2D.Impulse);
        }
        StartCoroutine(FinishHurtAfterDelay());
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
        MoveTowards(_player.transform.position, true);
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

            EnterState(State.Idle);
        }
    }

    private AttackProfile SelectReadyAttack()
    {
        if (_attacks == null) return null;
        foreach (AttackProfile attack in _attacks)
        {
            if (attack != null && IsNear(_player.transform.position, attack.activationRange)
                && Time.time - attack.lastUsedTime >= attack.cooldown)
                return attack;
        }
        return null;
    }

    private void BeginAttack(AttackProfile attack)
    {
        _activeAttack = attack;
        Face(_player.transform.position - transform.position);
        EnterState(State.Attack);
        _animator.SetTrigger(Animator.StringToHash(attack.animatorTrigger));
    }

    // Animation Events. A clip may call these repeatedly for multi-hit attacks.
    public void OpenAttackWindow()
    {
        if (_state != State.Attack || _activeAttack == null) return;
        if (_activeAttack.hitboxes == null) return;
        foreach (UniversalEnemyAttackHitbox hitbox in _activeAttack.hitboxes)
            hitbox?.Open(_activeAttack.damage, _activeAttack.knockbackForce);
    }

    // Compatibility with the existing Slime attack clips.
    public void ActivateAttackHitbox()
    {
        OpenAttackWindow();
    }

    public void CloseAttackWindow()
    {
        if (_activeAttack?.hitboxes == null) return;
        foreach (UniversalEnemyAttackHitbox hitbox in _activeAttack.hitboxes) hitbox?.Close();
    }

    public void FireProjectile()
    {
        if (_state != State.Attack || _activeAttack?.projectilePrefab == null) return;
        Transform origin = _activeAttack.projectileOrigin != null ? _activeAttack.projectileOrigin : transform;
        UniversalEnemyProjectile projectile = Instantiate(_activeAttack.projectilePrefab, origin.position, Quaternion.identity);
        projectile.Launch(_lastDirection, _activeAttack.projectileSpeed, _activeAttack.damage, _activeAttack.knockbackForce);
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
        if (_state != State.Hurt) yield break;
        _rigidbody.linearVelocity = Vector2.zero;
        EnterState(HasLivingPlayer() ? State.Chase : State.Idle);
    }

    private void EnterState(State state)
    {
        CloseAttackWindow();
        if (_state == State.Attack && state != State.Attack)
            _activeAttack = null;
        _state = state;
        _stateEnteredAt = Time.time;
        _desiredVelocity = Vector2.zero;
        _animator.SetBool(IsWalking, state is State.Patrol or State.Chase or State.ReturnHome);
        _animator.SetBool(IsRunning, state is State.Chase or State.ReturnHome);

        if (state == State.Hurt) _animator.SetTrigger(IsHit);
        if (state != State.Dead) return;

        _animator.SetBool(IsDead, true);
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.simulated = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        Destroy(gameObject, _deathLifetime);
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
            if (attack?.hitboxes != null)
                foreach (UniversalEnemyAttackHitbox hitbox in attack.hitboxes) hitbox?.Initialize(this);
    }

    private void ResolvePlayer()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player");
        _playerComponent = _player != null ? _player.GetComponent<Player>() : null;
    }

    private bool HasLivingPlayer() => _player != null && (_playerComponent == null || !_playerComponent.IsDead);
    private bool CanSeePlayer() { if (_player == null) ResolvePlayer(); return HasLivingPlayer() && IsNear(_player.transform.position, _detectionRange); }
    private bool IsNear(Vector2 target, float range) => ((Vector2)transform.position - target).sqrMagnitude <= range * range;

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
            if (attack != null)
                Gizmos.DrawWireSphere(transform.position, attack.activationRange);
        }
    }
}
