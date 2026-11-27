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
    private AnimatorUpdateMode _standingAnimationMode;
    private float _deathAnimationDeadline;

    [Header("Hurt feedback")]
    [Tooltip("Seconds after a hit during which further damage is ignored (the sprite blinks). 0 = off. Stops multi-hit boss moves from stacking in a few frames.")]
    [SerializeField, Min(0f)] private float _hurtInvulnerabilitySeconds = 0.45f;
    private float _hurtInvulnerableUntil;

    /// <summary>Time.time of the last hit that actually damaged the player (boss skills compare it to know whether their hit landed).</summary>
    public float LastHurtTime { get; private set; } = -100f;

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
        CacheSkillFxReferences();
        CacheShieldFxReferences();
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
        if (_stats.IsDead)
        {
            if (!_isDead && GameStateManager.AllowsGameplayInput
                && (!_deathPending || Time.unscaledTime >= _deathAnimationDeadline))
                Die();
            return;
        }

        _stats.TickRegeneration(Time.deltaTime);
        bool isActivelySprinting = _isRunning && _isMoving && GameStateManager.AllowsGameplayInput;
        _stats.TickStamina(isActivelySprinting, Time.deltaTime);

        if (_isRunning && !_stats.HasStamina)
            SetRunning(false);

        TickSkillAim();
        TickGroundTargetAim();
        TickSkillVisualRestore();
        TickDashInputBinding();
        TickShieldBlock();
    }

    public void TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce)
    {
        if (_isDead || _deathPending || damageAmount <= 0f || IsDashInvulnerable || Time.time < _hurtInvulnerableUntil)
            return;

        PlayerDamageResult result = _stats.ReceiveDamage(damageAmount);
        if (result.Outcome == PlayerDamageOutcome.Blocked)
        {
            SoundFXManager.PlaySfx(SfxIds.CombatShieldBlock); // the shield absorbs one hit, then breaks
            if (!_stats.IsBlocking) StopBlocking();
            return; // No hurt animation, knockback, hurt sound or hurt invulnerability on a blocked hit.
        }
        if (result.Outcome == PlayerDamageOutcome.Dodged)
            SoundFXManager.PlaySfx(SfxIds.CombatPlayerDodge);
        if (result.Outcome is PlayerDamageOutcome.Ignored or PlayerDamageOutcome.Dodged)
            return;

        SoundFXManager.PlaySfx(SfxIds.CombatPlayerHurt);
        LastHurtTime = Time.time;
        _hurtInvulnerableUntil = Time.time + _hurtInvulnerabilitySeconds;
        CombatFeedback.PlayerHurt(this, result.DamageTaken, _hurtInvulnerabilitySeconds);
        _deathPending = result.Outcome == PlayerDamageOutcome.Killed;
        if (_deathPending)
            _deathAnimationDeadline = Time.unscaledTime + 0.75f;
        _isHit = true;
        _isAttacking = false;
        DisableAttackHitbox();
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        _animator.SetTrigger(IsHitHash);
    }

    /// <summary>Instantly moves the player, clearing physics velocity so momentum from before the
    /// warp doesn't carry over. Used by DevPanelController's Teleport action.</summary>
    public void WarpTo(Vector3 position)
    {
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.position = position;
        transform.position = position;
    }

    public void FinishHit()
    {
        _isHit = false;
        if (_deathPending)
            Die();
    }

    private void Die()
    {
        if (_isDead)
            return;

        _deathPending = false;
        _isDead = true;
        _isHit = false;
        _isAttacking = false;
        if (_isBlocking)
            StopBlocking();
        DisableAttackHitbox();
        CancelAimSkill();
        CancelGroundTargetSkill();
        CancelDash();
        StopMovement();
        SoundFXManager.PlaySfx(SfxIds.CombatPlayerDeath);

        _animator.ResetTrigger(IsHitHash);
        _animator.ResetTrigger(AttackHash);
        _animator.SetBool(IsDeadHash, true);
        _standingAnimationMode = _animator.updateMode;
        _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        GameStateManager.Instance?.ReplaceState(GameState.PlayerDead);
        PlayerDeathPopupUI.Show(this);
    }

    internal void Revive()
    {
        if (!_isDead)
            return;

        _isDead = false;
        _stats.RestoreHealth(-1f);
        _stats.RestoreStamina(_stats.MaxStamina);
        _stats.RestoreMana(_stats.MaxMana);
        _hurtInvulnerableUntil = Time.time + 1.5f;
        _animator.SetBool(IsDeadHash, false);
        _animator.Play("Idle", 0, 0f);
        _animator.updateMode = _standingAnimationMode;
        GameSessionManager.Instance?.MarkDirty();
        GameStateManager.Instance?.ResetToPlaying();
    }
}
