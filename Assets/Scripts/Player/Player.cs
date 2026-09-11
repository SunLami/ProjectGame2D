using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(PlayerStat))]
public partial class Player : MonoBehaviour, IDamageable
{
    private static readonly int IsHitHash = Animator.StringToHash("isHit");
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    private Rigidbody2D _rigidbody;
    private Animator _animator;
    private PlayerStat _stats;
    private Camera _mainCamera;

    [Header("Runtime State")]
    [SerializeField] private bool _isMoving;
    [SerializeField] private bool _isAttacking;
    [SerializeField] private bool _isRunning;
    [SerializeField] private bool _isHit;
    [SerializeField] private bool _isDead;

    private bool _deathPending;

    public bool IsMoving => _isMoving;
    public bool IsAttacking => _isAttacking;
    public bool IsRunning => _isRunning;
    public bool IsHit => _isHit;
    public bool IsDead => _isDead;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _stats = GetComponent<PlayerStat>();
        _mainCamera = Camera.main;

        CacheCombatReferences();
        CacheVisualReferences();
        BindSharedManagers();
    }

    // EquipmentManager/SoundFXManager are shared Bootstrap singletons (one instance for the whole
    // game), so they can't hold a fixed Inspector reference to any one scene's Player -- each
    // scene's own Player pushes its own visuals/foot position in here instead.
    private void BindSharedManagers()
    {
        if (EquipmentManager.Instance != null)
        {
            var body = transform.Find("Body")?.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>();
            var head = transform.Find("Head")?.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>();
            var sword = transform.Find("Weapon")?.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>();
            EquipmentManager.Instance.BindPlayerVisuals(body, head, sword);
        }

        if (SoundFXManager.Instance != null)
            SoundFXManager.Instance.BindPlayerFootPos(transform.Find("FootPos"));
    }

    private void Update()
    {
        _stats.TickRegeneration(Time.deltaTime);
        bool isActivelySprinting = _isRunning && _isMoving && GameStateManager.AllowsGameplayInput;
        _stats.TickStamina(isActivelySprinting, Time.deltaTime);

        if (_isRunning && !_stats.HasStamina)
            SetRunning(false);
    }

    public void TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce)
    {
        if (_isDead || _deathPending || damageAmount <= 0f)
            return;

        PlayerDamageResult result = _stats.ReceiveDamage(damageAmount);
        if (result.Outcome is PlayerDamageOutcome.Ignored or PlayerDamageOutcome.Dodged)
            return;

        _deathPending = result.Outcome == PlayerDamageOutcome.Killed;
        _isHit = true;
        _isAttacking = false;
        DisableAttackHitbox();
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        _animator.SetTrigger(IsHitHash);
    }

    public void FinishHit()
    {
        _isHit = false;
        if (_deathPending)
            Die();
    }

    private void Die()
    {
        _deathPending = false;
        _isDead = true;
        _isHit = false;
        _isAttacking = false;
        DisableAttackHitbox();
        StopMovement();

        _animator.ResetTrigger(IsHitHash);
        _animator.ResetTrigger(AttackHash);
        _animator.SetBool(IsDeadHash, true);
    }
}
