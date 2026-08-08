using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyState
    {
        Hit,
        Idling,
        Patroling,
        Chasing,
        Returning,
        Attacking,
        Dead
    }

    [Header("References")]
    public SpriteRenderer enemySprite;
    public Animator animator;
    public Rigidbody2D rb;
    public GameObject player;

    [Header("Animation Clips")]
    public AnimationClip idleAnimation;
    public AnimationClip walkAnimation;
    public AnimationClip runAnimation;
    public AnimationClip attackAnimation;
    public AnimationClip hitAnimation;
    public AnimationClip deadAnimation;

    [Header("Basic Stats")]
    public float maxHealth = 100f;
    public float health = 100f;
    public float moveSpeed = 2f;

    [Header("Detection Ranges")]
    [Tooltip("Khoảng cách phát hiện người chơi")]
    [Range(1f, 10f)] public float detectionRange = 4f;
    [Tooltip("Khoảng cách tối đa đuổi theo người chơi")]
    [Range(1f, 10f)] public float chaseRange = 8f;
    [Tooltip("Vùng tuần tra, quái sẽ di chuyển ngẫu nhiên trong vùng này")]
    [Range(1f, 10f)] public float patrolDistance = 3f;

    [Header("Attack Setup")]
    [Tooltip("Tầm kích hoạt tấn công")]
    [Range(0.5f, 5f)] public float attackRange = 1.2f;
    public float attackDamage = 10f;
    [Tooltip("Thời gian nghỉ giữa các lần tấn công")]
    public float attackCooldown = 1.5f;

    [Header("Current State")]
    public EnemyState currentState;
    public Vector2 initialPosition;
    [Tooltip("Thời gian nghỉ giữa các lần tuần tra")]
    public float idleTime = 2f;

    private float lastIdleTime;
    private float stateEnterTime;
    private float lastAttackTime;
    private Vector2 currentPatrolTarget;
    private Player playerScript;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        enemySprite = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        initialPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerScript = player.GetComponent<Player>();
        }

        ChangeState(EnemyState.Idling);
    }

    void Update()
    {
        if (currentState == EnemyState.Dead) return;

        switch (currentState)
        {
            case EnemyState.Idling:
                Idle();
                break;
            case EnemyState.Patroling:
                Patrol();
                break;
            case EnemyState.Chasing:
                Chase();
                break;
            case EnemyState.Returning:
                Returning();
                break;
            case EnemyState.Attacking:
                Attack();
                break;
            case EnemyState.Hit:
                Hit();
                break;
        }
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState == newState && newState != EnemyState.Hit) return;

        currentState = newState;
        stateEnterTime = Time.time;

        rb.linearVelocity = Vector2.zero;

        switch (currentState)
        {
            case EnemyState.Idling:
                if (idleAnimation) animator.Play(idleAnimation.name);
                lastIdleTime = Time.time;
                break;

            case EnemyState.Patroling:
                if (walkAnimation) animator.Play(walkAnimation.name);
                // Tạo điểm tuần tra mới 1 lần duy nhất
                currentPatrolTarget = initialPosition + Random.insideUnitCircle * patrolDistance;
                break;

            case EnemyState.Chasing:
                if (runAnimation) animator.Play(runAnimation.name);
                else if (walkAnimation) animator.Play(walkAnimation.name);
                break;

            case EnemyState.Returning:
                if (walkAnimation) animator.Play(walkAnimation.name);
                break;

            case EnemyState.Attacking:
                if (attackAnimation) animator.Play(attackAnimation.name);
                lastAttackTime = Time.time;
                break;

            case EnemyState.Hit:
                if (hitAnimation) animator.Play(hitAnimation.name);
                break;

            case EnemyState.Dead:
                if (deadAnimation) animator.Play(deadAnimation.name);
                Destroy(gameObject, 3f);
                break;
        }
    }

    void Idle()
    {
        if (Time.time - lastIdleTime > idleTime)
        {
            ChangeState(EnemyState.Patroling);
            return;
        }

        LookForPlayer();
    }

    void Patrol()
    {
        MoveTowardsTarget(currentPatrolTarget);

        if (Vector2.Distance(transform.position, currentPatrolTarget) < 0.1f)
        {
            ChangeState(EnemyState.Idling);
            return;
        }

        LookForPlayer();
    }

    void Chase()
    {
        if (player == null || (playerScript != null && playerScript.IsDead))
        {
            ChangeState(EnemyState.Returning);
            return;
        }

        float distanceToHome = Vector2.Distance(transform.position, initialPosition);
        float distanceToPlayer = Vector2.Distance(player.transform.position, transform.position);

        if (distanceToHome > chaseRange)
        {
            ChangeState(EnemyState.Returning);
            return;
        }

        if (distanceToPlayer <= attackRange && Time.time - lastAttackTime > attackCooldown)
        {
            ChangeState(EnemyState.Attacking);
            return;
        }

        MoveTowardsTarget(player.transform.position);
    }

    void Attack()
    {
        float duration = attackAnimation != null ? attackAnimation.length : 0.5f;
        if (Time.time - stateEnterTime >= duration)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    void Returning()
    {
        MoveTowardsTarget(initialPosition);

        if (Vector2.Distance(transform.position, initialPosition) < 0.1f)
        {
            ChangeState(EnemyState.Patroling);
        }
    }

    void Hit()
    {
        if (health <= 0)
        {
            ChangeState(EnemyState.Dead);
            return;
        }

        float duration = hitAnimation != null ? hitAnimation.length : 0.3f;
        if (Time.time - stateEnterTime >= duration)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    void LookForPlayer()
    {
        if (player == null || (playerScript != null && playerScript.IsDead)) return;

        float distanceToPlayer = Vector2.Distance(player.transform.position, transform.position);
        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    void MoveTowardsTarget(Vector2 target)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // Tự động lật mặt Sprite theo hướng đi
        if (direction.x != 0 && enemySprite != null)
        {
            enemySprite.flipX = direction.x < 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("HitBox_Player"))
        {
            PlayerStat playerStat = player != null ? player.GetComponent<PlayerStat>() : null;
            float playerDamage = playerStat != null ? playerStat.AtkDmg : 10f;

            health -= playerDamage;
            health = Mathf.Clamp(health, 0f, maxHealth);

            ChangeState(EnemyState.Hit);
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 basePos = Application.isPlaying ? (Vector3)initialPosition : transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(basePos, patrolDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(basePos, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}