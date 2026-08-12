using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public enum EnemyState
    {
        Hit,
        Idling,
        Patrolling,
        Chasing,
        Returning,
        Attacking,
        Dead
    }

    [Header("References")]
    [FormerlySerializedAs("enemySprite"), SerializeField] private SpriteRenderer _enemySprite;
    [FormerlySerializedAs("animator"), SerializeField] private Animator _animator;
    [FormerlySerializedAs("rb"), SerializeField] private Rigidbody2D _rigidbody;
    [FormerlySerializedAs("player"), SerializeField] private GameObject _player;

    [Header("Animation Clips")]
    [FormerlySerializedAs("idleAnimation"), SerializeField] private AnimationClip _idleAnimation;
    [FormerlySerializedAs("walkAnimation"), SerializeField] private AnimationClip _walkAnimation;
    [FormerlySerializedAs("runAnimation"), SerializeField] private AnimationClip _runAnimation;
    [FormerlySerializedAs("attackAnimation"), SerializeField] private AnimationClip _attackAnimation;
    [FormerlySerializedAs("hitAnimation"), SerializeField] private AnimationClip _hitAnimation;
    [FormerlySerializedAs("deadAnimation"), SerializeField] private AnimationClip _deadAnimation;

    [Header("Basic Stats")]
    [FormerlySerializedAs("maxHealth"), SerializeField, Min(1f)] private float _maxHealth = 100f;
    [FormerlySerializedAs("health"), SerializeField] private float _health = 100f;
    [FormerlySerializedAs("moveSpeed"), SerializeField, Min(0f)] private float _moveSpeed = 2f;

    [Header("Detection Ranges")]
    [FormerlySerializedAs("detectionRange"), SerializeField, Range(1f, 10f)] private float _detectionRange = 4f;
    [FormerlySerializedAs("chaseRange"), SerializeField, Range(1f, 10f)] private float _chaseRange = 8f;
    [FormerlySerializedAs("patrolDistance"), SerializeField, Range(1f, 10f)] private float _patrolDistance = 3f;

    [Header("Attack Setup")]
    [FormerlySerializedAs("attackRange"), SerializeField, Range(0.5f, 5f)] private float _attackRange = 1.2f;
    [FormerlySerializedAs("attackDamage"), SerializeField, Min(0f)] private float _attackDamage = 10f;
    [FormerlySerializedAs("attackCooldown"), SerializeField, Min(0f)] private float _attackCooldown = 1.5f;
    [SerializeField, Range(0f, 1f)] private float _attackHitNormalizedTime = 0.5f;
    [SerializeField, Min(0f)] private float _attackKnockbackForce = 5f;

    [Header("State")]
    [FormerlySerializedAs("currentState"), SerializeField] private EnemyState _currentState;
    [FormerlySerializedAs("initialPosition"), SerializeField] private Vector2 _initialPosition;
    [FormerlySerializedAs("idleTime"), SerializeField, Min(0f)] private float _idleTime = 2f;

    private Player _playerScript;
    private Vector2 _currentPatrolTarget;
    private Vector2 _desiredVelocity;
    private float _stateEnterTime;
    private float _lastAttackEndTime = float.NegativeInfinity;
    private bool _attackDamageApplied;
    private Collider2D[] _colliders;

    public EnemyState CurrentState => _currentState;
    public float Health => _health;
    public bool IsDead => _currentState == EnemyState.Dead;

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_enemySprite == null) _enemySprite = GetComponentInChildren<SpriteRenderer>();

        _colliders = GetComponentsInChildren<Collider2D>();
        _health = Mathf.Clamp(_health, 0f, _maxHealth);
    }

    private void Start()
    {
        _initialPosition = transform.position;
        ResolvePlayer();
        EnterState(_health <= 0f ? EnemyState.Dead : EnemyState.Idling);
    }

    private void Update()
    {
        if (IsDead)
            return;

        _desiredVelocity = Vector2.zero;
        UpdateState();
    }

    private void FixedUpdate()
    {
        if (_rigidbody != null)
            _rigidbody.linearVelocity = IsDead ? Vector2.zero : _desiredVelocity;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        _health = Mathf.Clamp(_health - damage, 0f, _maxHealth);
        EnterState(_health <= 0f ? EnemyState.Dead : EnemyState.Hit);
    }

    private void UpdateState()
    {
        switch (_currentState)
        {
            case EnemyState.Idling: UpdateIdle(); break;
            case EnemyState.Patrolling: UpdatePatrol(); break;
            case EnemyState.Chasing: UpdateChase(); break;
            case EnemyState.Returning: UpdateReturning(); break;
            case EnemyState.Attacking: UpdateAttack(); break;
            case EnemyState.Hit: UpdateHit(); break;
        }
    }

    private void EnterState(EnemyState newState)
    {
        if (_currentState == newState && newState != EnemyState.Hit)
            return;

        _currentState = newState;
        _stateEnterTime = Time.time;
        _desiredVelocity = Vector2.zero;

        switch (newState)
        {
            case EnemyState.Idling:
                PlayAnimation(_idleAnimation);
                break;
            case EnemyState.Patrolling:
                PlayAnimation(_walkAnimation);
                _currentPatrolTarget = _initialPosition + Random.insideUnitCircle * _patrolDistance;
                break;
            case EnemyState.Chasing:
                PlayAnimation(_runAnimation != null ? _runAnimation : _walkAnimation);
                break;
            case EnemyState.Returning:
                PlayAnimation(_walkAnimation);
                break;
            case EnemyState.Attacking:
                _attackDamageApplied = false;
                PlayAnimation(_attackAnimation);
                break;
            case EnemyState.Hit:
                PlayAnimation(_hitAnimation);
                break;
            case EnemyState.Dead:
                Die();
                break;
        }
    }

    private void UpdateIdle()
    {
        if (Time.time - _stateEnterTime >= _idleTime)
        {
            EnterState(EnemyState.Patrolling);
            return;
        }

        LookForPlayer();
    }

    private void UpdatePatrol()
    {
        MoveTowards(_currentPatrolTarget);
        if (IsNear(_currentPatrolTarget, 0.1f))
        {
            EnterState(EnemyState.Idling);
            return;
        }

        LookForPlayer();
    }

    private void UpdateChase()
    {
        if (!HasLivingPlayer())
        {
            EnterState(EnemyState.Returning);
            return;
        }

        if (!IsNear(_initialPosition, _chaseRange))
        {
            EnterState(EnemyState.Returning);
            return;
        }

        if (IsNear(_player.transform.position, _attackRange) && Time.time - _lastAttackEndTime >= _attackCooldown)
        {
            EnterState(EnemyState.Attacking);
            return;
        }

        MoveTowards(_player.transform.position);
    }

    private void UpdateReturning()
    {
        MoveTowards(_initialPosition);
        if (IsNear(_initialPosition, 0.1f))
            EnterState(EnemyState.Idling);
    }

    private void UpdateAttack()
    {
        float duration = GetDuration(_attackAnimation, 0.5f);
        float elapsed = Time.time - _stateEnterTime;

        if (!_attackDamageApplied && elapsed >= duration * _attackHitNormalizedTime)
            ApplyAttackDamage();

        if (elapsed >= duration)
        {
            _lastAttackEndTime = Time.time;
            EvaluateNextState();
        }
    }

    private void UpdateHit()
    {
        if (Time.time - _stateEnterTime >= GetDuration(_hitAnimation, 0.3f))
            EvaluateNextState();
    }

    private void EvaluateNextState()
    {
        if (!HasLivingPlayer() || !IsNear(_initialPosition, _chaseRange))
        {
            EnterState(EnemyState.Returning);
            return;
        }

        EnterState(IsNear(_player.transform.position, _detectionRange)
            ? EnemyState.Chasing
            : EnemyState.Returning);
    }

    private void LookForPlayer()
    {
        if (_player == null)
            ResolvePlayer();

        if (HasLivingPlayer() && IsNear(_player.transform.position, _detectionRange))
            EnterState(EnemyState.Chasing);
    }

    private void ResolvePlayer()
    {
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player");

        _playerScript = _player != null ? _player.GetComponent<Player>() : null;
    }

    private bool HasLivingPlayer()
    {
        return _player != null && (_playerScript == null || !_playerScript.IsDead);
    }

    private void MoveTowards(Vector2 target)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        _desiredVelocity = direction * _moveSpeed;

        if (Mathf.Abs(direction.x) > Mathf.Epsilon && _enemySprite != null)
            _enemySprite.flipX = direction.x < 0f;
    }

    private void ApplyAttackDamage()
    {
        _attackDamageApplied = true;
        if (!HasLivingPlayer() || !IsNear(_player.transform.position, _attackRange))
            return;

        Vector2 knockbackDirection = (_player.transform.position - transform.position).normalized;
        _playerScript?.TakeDamage(_attackDamage, knockbackDirection, _attackKnockbackForce);
    }

    private void Die()
    {
        _desiredVelocity = Vector2.zero;
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
        }

        foreach (Collider2D enemyCollider in _colliders)
            enemyCollider.enabled = false;

        PlayAnimation(_deadAnimation);
        Destroy(gameObject, GetDuration(_deadAnimation, 3f));
    }

    private void PlayAnimation(AnimationClip clip)
    {
        if (_animator != null && clip != null)
            _animator.Play(Animator.StringToHash(clip.name));
    }

    private bool IsNear(Vector2 target, float distance)
    {
        return ((Vector2)transform.position - target).sqrMagnitude <= distance * distance;
    }

    private static float GetDuration(AnimationClip clip, float fallback)
    {
        return clip != null ? clip.length : fallback;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("HitBox_Player"))
            return;

        PlayerStat playerStats = collision.GetComponentInParent<PlayerStat>();
        if (playerStats == null && _player != null)
            playerStats = _player.GetComponent<PlayerStat>();

        TakeDamage(playerStats != null ? playerStats.AtkDmg : 10f);
    }

    private void OnValidate()
    {
        _maxHealth = Mathf.Max(1f, _maxHealth);
        _health = Mathf.Clamp(_health, 0f, _maxHealth);
        _moveSpeed = Mathf.Max(0f, _moveSpeed);
        _attackDamage = Mathf.Max(0f, _attackDamage);
        _attackCooldown = Mathf.Max(0f, _attackCooldown);
        _attackKnockbackForce = Mathf.Max(0f, _attackKnockbackForce);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 home = Application.isPlaying ? (Vector3)_initialPosition : transform.position;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(home, _patrolDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(home, _chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
