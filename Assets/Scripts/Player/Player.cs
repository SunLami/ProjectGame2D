using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(PlayerStat))]
public class Player : MonoBehaviour, IDamageable
{
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsHitHash = Animator.StringToHash("isHit");
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    private Rigidbody2D _rigidbody;
    private Animator _animator;
    private PlayerStat _stats;
    private Camera _mainCamera;
    private Vector2 _moveInput;
    private Vector2 _lastFacingDirection = Vector2.down;

    [Header("Attack Hitbox")]
    [SerializeField] private PlayerAttackHitbox _attackHitbox;
    [SerializeField] private SpriteRenderer _attackFxRenderer;
    [SerializeField, Min(0f)] private float _attackHitboxOffset = 0.6f;
    [SerializeField, Min(0f)] private float _attackKnockbackForce = 2.5f;
    private Vector2 _facingDirection = Vector2.down;

    [SerializeField, Min(0f)] private float _moveSpeed = 2f;
    [SerializeField, Min(1f)] private float _sprintMultiplier = 2f;
    [SerializeField] private bool _isMoving;
    [SerializeField] private bool _isAttacking;
    [SerializeField] private bool _isRunning;
    [SerializeField] private bool _isHit;
    [SerializeField] private bool _isDead;

    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private int _weaponSortingOrderRight = 1;
    [SerializeField] private int _weaponSortingOrderLeft = -1;
    [SerializeField] private int _weaponSortingOrderVertical = 0;

    private bool _deathPending;

    public float MoveSpeed
    {
        get => _moveSpeed;
        set => _moveSpeed = Mathf.Max(0f, value);
    }

    public bool IsMoving => _isMoving;
    public bool IsAttacking => _isAttacking;
    public bool IsRunning => _isRunning;
    public bool IsHit => _isHit;
    public bool IsDead => _isDead;

    private float CurrentMoveSpeed => _isRunning
        ? _moveSpeed * _sprintMultiplier
        : _moveSpeed;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _stats = GetComponent<PlayerStat>();
        _mainCamera = Camera.main;
        if (_attackFxRenderer == null)
            _attackFxRenderer = transform.Find("AttackFX")?.GetComponent<SpriteRenderer>();
        EnsureAttackHitbox();

        if (_weaponRenderer == null)
        {
            Transform weaponTransform = transform.Find("Weapon");
            if (weaponTransform != null)
                _weaponRenderer = weaponTransform.GetComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        UpdateWeaponSortingOrder();
    }

    private void UpdateWeaponSortingOrder()
    {
        if (_weaponRenderer == null)
            return;

        int targetOrder;
        if (_facingDirection.x > 0f)
            targetOrder = _weaponSortingOrderRight;
        else if (_facingDirection.x < 0f)
            targetOrder = _weaponSortingOrderLeft;
        else
            targetOrder = _weaponSortingOrderVertical;

        if (_weaponRenderer.sortingOrder != targetOrder)
            _weaponRenderer.sortingOrder = targetOrder;
    }

    private void FixedUpdate()
    {
        if (_isHit || _isDead)
            return;

        _rigidbody.linearVelocity = _moveInput * CurrentMoveSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isDead)
            return;

        _moveInput = context.ReadValue<Vector2>();
        _animator.SetFloat(InputXHash, _moveInput.x);
        _animator.SetFloat(InputYHash, _moveInput.y);

        SetMoving(!context.canceled && _moveInput != Vector2.zero);

        if (_isMoving && !_isAttacking && !_isHit)
            SetFacingDirection(_moveInput);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (_isDead)
            return;

        if (context.started)
            SetRunning(true);
        else if (context.canceled)
            SetRunning(false);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started || _isAttacking || _isHit || _isDead)
            return;

        _isAttacking = true;

        if (_isRunning && _moveInput != Vector2.zero)
            SetFacingDirection(_moveInput);
        else
            UpdateDirectionToMouse();

        _animator.SetTrigger(AttackHash);
    }

    public void TakeDamage(float damageAmount, Vector2 knockbackDirection, float knockbackForce)
    {
        if (_isDead || _deathPending || damageAmount <= 0f)
            return;

        _deathPending = _stats.TakeDamage(damageAmount);

        _isHit = true;
        _isAttacking = false;
        DisableAttackHitbox();
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        _animator.SetTrigger(IsHitHash);
    }

    public void FinishAttack()
    {
        DisableAttackHitbox();
        _isAttacking = false;
    }

    // Called by Animation Events to close the damage window independently
    // from the visual end of the attack animation.
    public void CloseAttackHitbox()
    {
        DisableAttackHitbox();
    }

    // Called by Animation Events on all Player attack clips.
    public void ActivatePlayerAttackHitbox()
    {
        if (!_isAttacking || _isHit || _isDead)
            return;

        _attackHitbox.Configure(_attackFxRenderer, _lastFacingDirection, _attackHitboxOffset);
        _attackHitbox.BeginAttack();
    }

    public void DamageEnemyFromHitbox(Enemy enemy)
    {
        if (!_isAttacking || _isHit || _isDead || enemy == null || enemy.IsDead)
            return;

        Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
        enemy.TakeDamage(_stats.AtkDmg, knockbackDirection, _attackKnockbackForce);
    }

    public void DamageEnemyFromHitbox(EnemyUniversal enemy)
    {
        if (!_isAttacking || _isHit || _isDead || enemy == null || enemy.IsDeadNow)
            return;

        Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
        enemy.TakeDamage(_stats.AtkDmg, knockbackDirection, _attackKnockbackForce);
    }

    private void DisableAttackHitbox()
    {
        _attackHitbox?.EndAttack();
    }

    private void EnsureAttackHitbox()
    {
        if (_attackHitbox == null)
            _attackHitbox = GetComponentInChildren<PlayerAttackHitbox>(true);

        if (_attackHitbox == null)
        {
            GameObject hitboxObject = new("AttackHitbox");
            hitboxObject.transform.SetParent(transform, false);
            hitboxObject.AddComponent<PolygonCollider2D>();
            _attackHitbox = hitboxObject.AddComponent<PlayerAttackHitbox>();
        }

        _attackHitbox.Initialize(this);
        _attackHitbox.Configure(_attackFxRenderer, _lastFacingDirection, _attackHitboxOffset);
    }

    public void FinishHit()
    {
        _isHit = false;

        if (_deathPending)
            Die();
    }

    private void UpdateDirectionToMouse()
    {
        if (_mainCamera == null || Pointer.current == null)
            return;

        Vector2 mouseScreenPosition = Pointer.current.position.ReadValue();
        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 attackDirection = (mouseWorldPosition - transform.position).normalized;

        if (attackDirection != Vector2.zero)
            SetFacingDirection(attackDirection);
    }

    private void SetMoving(bool value)
    {
        _isMoving = value;
        _animator.SetBool(IsMovingHash, value);
    }

    private void SetRunning(bool value)
    {
        _isRunning = value;
        _animator.SetBool(IsRunningHash, value);
    }

    private void SetFacingDirection(Vector2 direction)
    {
        _lastFacingDirection = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
        _facingDirection = direction;
        _animator.SetFloat(LastInputXHash, direction.x);
        _animator.SetFloat(LastInputYHash, direction.y);
    }

    private void Die()
    {
        _deathPending = false;
        _isDead = true;
        _isHit = false;
        _isAttacking = false;
        DisableAttackHitbox();
        _moveInput = Vector2.zero;

        SetMoving(false);
        SetRunning(false);

        _rigidbody.linearVelocity = Vector2.zero;
        _animator.ResetTrigger(IsHitHash);
        _animator.ResetTrigger(AttackHash);
        _animator.SetBool(IsDeadHash, true);
    }
}
