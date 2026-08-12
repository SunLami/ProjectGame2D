using System.Collections;
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

    [Header("Animation Timing")]
    [SerializeField, Min(0.01f)] private float _attackDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float _hitDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float _deathDuration = 3f;

    [Header("Attack Hitbox")]
    [SerializeField] private EnemyAttackHitbox _attackHitbox;
    [Tooltip("Bật để hitbox xoay và đổi vị trí theo hướng Enemy. Tắt để giữ nguyên transform đã thiết lập trong prefab.")]
    [SerializeField] private bool _rotateHitboxWithEnemyDirection = true;
    [SerializeField, Min(0f)] private float _attackHitboxOffset = 0.8f;
    [SerializeField, Min(0.02f)] private float _attackHitboxActiveDuration = 0.1f;

    [Header("State")]
    [FormerlySerializedAs("currentState"), SerializeField] private EnemyState _currentState;
    [FormerlySerializedAs("initialPosition"), SerializeField] private Vector2 _initialPosition;
    [FormerlySerializedAs("idleTime"), SerializeField, Min(0f)] private float _idleTime = 2f;

    private Player _playerScript;
    private Vector2 _currentPatrolTarget;
    private Vector2 _desiredVelocity;
    private float _stateEnterTime;
    private float _lastAttackEndTime = float.NegativeInfinity;
    private Collider2D[] _colliders;
    private Coroutine _stateRoutine;
    private Vector2 _lastDirection = Vector2.down;
    private bool _hasEnteredState;

    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsHitHash = Animator.StringToHash("isHit");
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

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
        EnsureAttackHitbox();
    }

    private void Start()
    {
        _initialPosition = transform.position;
        ResolvePlayer();
        SetDirectionParameters(Vector2.zero, false);
        SetLocomotionParameters(false, false);
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
        }
    }

    private void EnterState(EnemyState newState)
    {
        if (_hasEnteredState && _currentState == newState && newState != EnemyState.Hit)
            return;

        StopStateRoutine();
        _hasEnteredState = true;
        _currentState = newState;
        _stateEnterTime = Time.time;
        _desiredVelocity = Vector2.zero;

        switch (newState)
        {
            case EnemyState.Idling:
                SetLocomotionParameters(false, false);
                break;
            case EnemyState.Patrolling:
                _currentPatrolTarget = _initialPosition + Random.insideUnitCircle * _patrolDistance;
                break;
            case EnemyState.Chasing:
                break;
            case EnemyState.Returning:
                break;
            case EnemyState.Attacking:
                SetLocomotionParameters(false, false);
                FacePlayer();
                _animator?.SetTrigger(AttackHash);
                _stateRoutine = StartCoroutine(AttackRoutine());
                break;
            case EnemyState.Hit:
                SetLocomotionParameters(false, false);
                _animator?.SetTrigger(IsHitHash);
                _stateRoutine = StartCoroutine(HitRoutine());
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
        MoveTowards(_currentPatrolTarget, false);
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

        MoveTowards(_player.transform.position, true);
    }

    private void UpdateReturning()
    {
        MoveTowards(_initialPosition, false);
        if (IsNear(_initialPosition, 0.1f))
            EnterState(EnemyState.Idling);
    }

    private IEnumerator AttackRoutine()
    {
        float duration = _attackDuration;
        float hitDelay = duration * _attackHitNormalizedTime;

        if (hitDelay > 0f)
            yield return new WaitForSeconds(hitDelay);

        float remainingDuration = duration - hitDelay;
        float activeDuration = Mathf.Min(_attackHitboxActiveDuration, remainingDuration);
        _attackHitbox.Configure(_lastDirection, _attackHitboxOffset, _rotateHitboxWithEnemyDirection);
        _attackHitbox.BeginAttack();

        if (activeDuration > 0f)
            yield return new WaitForSeconds(activeDuration);

        _attackHitbox.EndAttack();

        float recoveryDuration = remainingDuration - activeDuration;
        if (recoveryDuration > 0f)
            yield return new WaitForSeconds(recoveryDuration);

        _stateRoutine = null;
        _lastAttackEndTime = Time.time;
        EvaluateNextState();
    }

    private IEnumerator HitRoutine()
    {
        yield return new WaitForSeconds(_hitDuration);

        _stateRoutine = null;
        EvaluateNextState();
    }

    private void StopStateRoutine()
    {
        _attackHitbox?.EndAttack();

        if (_stateRoutine == null)
            return;

        StopCoroutine(_stateRoutine);
        _stateRoutine = null;
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

    private void MoveTowards(Vector2 target, bool isRunning)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        _desiredVelocity = direction * _moveSpeed;
        SetDirectionParameters(direction, true);
        SetLocomotionParameters(true, isRunning);
    }

    private void FacePlayer()
    {
        if (HasLivingPlayer())
            SetDirectionParameters(_player.transform.position - transform.position, true);
    }

    private void SetLocomotionParameters(bool isWalking, bool isRunning)
    {
        if (_animator == null)
            return;

        _animator.SetBool(IsWalkingHash, isWalking);
        _animator.SetBool(IsRunningHash, isWalking && isRunning);
        if (!isWalking)
        {
            _animator.SetFloat(InputXHash, 0f);
            _animator.SetFloat(InputYHash, 0f);
        }
    }

    private void SetDirectionParameters(Vector2 direction, bool updateLastDirection)
    {
        if (_animator == null)
            return;

        Vector2 cardinal = ToCardinalDirection(direction);
        if (updateLastDirection && cardinal != Vector2.zero)
            _lastDirection = cardinal;

        _animator.SetFloat(InputXHash, cardinal.x);
        _animator.SetFloat(InputYHash, cardinal.y);
        _animator.SetFloat(LastInputXHash, _lastDirection.x);
        _animator.SetFloat(LastInputYHash, _lastDirection.y);
    }

    private static Vector2 ToCardinalDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Vector2.zero;

        return Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
    }

    public void DamagePlayerFromHitbox(Player player)
    {
        if (IsDead || _currentState != EnemyState.Attacking || player == null || player.IsDead)
            return;

        Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
        player.TakeDamage(_attackDamage, knockbackDirection, _attackKnockbackForce);
    }

    private void EnsureAttackHitbox()
    {
        if (_attackHitbox == null)
            _attackHitbox = GetComponentInChildren<EnemyAttackHitbox>(true);

        if (_attackHitbox == null)
        {
            GameObject hitboxObject = new("AttackHitbox");
            hitboxObject.transform.SetParent(transform, false);
            hitboxObject.AddComponent<PolygonCollider2D>();
            _attackHitbox = hitboxObject.AddComponent<EnemyAttackHitbox>();
        }

        _attackHitbox.Initialize(this);
        _attackHitbox.Configure(_lastDirection, _attackHitboxOffset, _rotateHitboxWithEnemyDirection);
    }

    private void Die()
    {
        _desiredVelocity = Vector2.zero;
        _attackHitbox?.EndAttack();
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
        }

        foreach (Collider2D enemyCollider in _colliders)
            enemyCollider.enabled = false;

        SetLocomotionParameters(false, false);
        _animator?.SetBool(IsDeadHash, true);
        Destroy(gameObject, _deathDuration);
    }

    private bool IsNear(Vector2 target, float distance)
    {
        return ((Vector2)transform.position - target).sqrMagnitude <= distance * distance;
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
        _attackDuration = Mathf.Max(0.01f, _attackDuration);
        _hitDuration = Mathf.Max(0.01f, _hitDuration);
        _deathDuration = Mathf.Max(0.01f, _deathDuration);
        _attackHitboxOffset = Mathf.Max(0f, _attackHitboxOffset);
        _attackHitboxActiveDuration = Mathf.Max(0.02f, _attackHitboxActiveDuration);
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
