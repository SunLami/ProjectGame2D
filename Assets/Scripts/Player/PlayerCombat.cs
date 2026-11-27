using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public partial class Player
{
    public static event Action PlayerAttacked;

    internal static void RaiseAttackedForTests() => PlayerAttacked?.Invoke();

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    [Header("Attack Hitbox")]
    [SerializeField] private PlayerAttackHitbox _attackHitbox;
    [SerializeField] private SpriteRenderer _attackFxRenderer;
    [SerializeField, Min(0f)] private float _attackHitboxOffset = 0.6f;
    [SerializeField, Min(0f)] private float _attackKnockbackForce = 2.5f;

    private readonly List<Collider2D> _practiceAimHits = new();
    private MannequinHurtbox _practiceTarget;

    internal static bool IsVillageSafeZone()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return activeScene.name == "MapNhat" ||
            activeScene.name == "Bootstrap" &&
            UnityEngine.SceneManagement.SceneManager.GetSceneByName("MapNhat").isLoaded;
    }

    private MannequinHurtbox FindPracticeTarget()
    {
        if (!AreaZoneRegistry.TryGet("area.training", out Transform yard)
            || yard.gameObject.scene.name != "MapNhat"
            || !yard.TryGetComponent(out Collider2D boundary)
            || !boundary.OverlapPoint(transform.position)
            || Pointer.current == null)
            return null;

        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null)
            return null;

        Vector2 pointer = _mainCamera.ScreenToWorldPoint(Pointer.current.position.ReadValue());
        int count = Physics2D.OverlapPoint(pointer, ContactFilter2D.noFilter, _practiceAimHits);
        for (int i = 0; i < count; i++)
        {
            var dummy = _practiceAimHits[i].GetComponentInParent<MannequinHurtbox>();
            if (dummy != null && dummy.IsVillageTrainingTarget && !dummy.IsDead
                && ((Vector2)(dummy.transform.position - transform.position)).sqrMagnitude <= 2.25f)
                return dummy;
        }

        return null;
    }

    internal bool CanHitWithAttack(IDamageable target) =>
        !IsVillageSafeZone() || _practiceTarget != null && ReferenceEquals(target, _practiceTarget);

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started || _isAttacking || _isHit || _isDead || _isAimingSkill || _isDashing
            || !GameStateManager.AllowsGameplayInput
            || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()
            || IsPointerOverGameplayHud()
            || GameCursorManager.Instance != null
                && GameCursorManager.Instance.IsPointerOverNonCombatInteraction)
            return;
        bool inVillage = IsVillageSafeZone();
        _practiceTarget = inVillage ? FindPracticeTarget() : null;
        if (inVillage && _practiceTarget == null)
            return;

        if (!_stats.TryConsumeAttackStamina())
        {
            SoundFXManager.PlaySfx(SfxIds.PlayerExhausted);
            return;
        }

        _isAttacking = true;
        if (_practiceTarget == null && _isRunning && _moveInput != Vector2.zero)
            SetFacingDirection(_moveInput);
        else
            UpdateDirectionToMouse();

        _animator.SetTrigger(AttackHash);
        PlayerAttacked?.Invoke();
    }

    public void FinishAttack()
    {
        DisableAttackHitbox();
        _isAttacking = false;
        _practiceTarget = null;
        _isCastingSkill = false;
    }

    public void CloseAttackHitbox() => DisableAttackHitbox();

    public void ActivatePlayerAttackHitbox()
    {
        if (!_isAttacking || _isHit || _isDead || _isCastingSkill)
            return;

        SoundFXManager.PlaySfx(HasWeaponEquipped() ? SfxIds.CombatPlayerAttack : SfxIds.CombatUnarmedSwing);
        _attackHitbox.Configure(_attackFxRenderer, _lastFacingDirection, _attackHitboxOffset);
        _attackHitbox.BeginAttack();
    }

    public void DamageTargetFromHitbox(IDamageable target, Transform targetTransform)
    {
        if (!_isAttacking || _isHit || _isDead || target == null || target.IsDead || !CanHitWithAttack(target))
            return;

        Vector2 direction = targetTransform != null
            ? ((Vector2)targetTransform.position - (Vector2)transform.position).normalized
            : _lastFacingDirection;
        float damage = _stats.RollOutgoingDamage(out bool isCritical);
        target.TakeDamage(damage, direction, _attackKnockbackForce);
        SoundFXManager.PlaySfx(AttackHitSfx(target, isCritical));
    }

    private static bool HasWeaponEquipped() =>
        EquipmentManager.Instance != null && EquipmentManager.Instance.GetEquipped(EquipSlot.Weapon) != null;

    /// <summary>Sword cuts and bare fists sound different; the village training dummy is wood, so it always gets a
    /// wooden knock.</summary>
    private static string AttackHitSfx(IDamageable target, bool isCritical)
    {
        if (target is MannequinHurtbox)
            return SfxIds.CombatDummyHit;
        if (HasWeaponEquipped())
            return isCritical ? SfxIds.CombatHitCrit : SfxIds.CombatHitImpact;
        return isCritical ? SfxIds.CombatUnarmedCrit : SfxIds.CombatUnarmedHit;
    }

    private void UpdateDirectionToMouse()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null || Pointer.current == null)
            return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 direction = (worldPosition - transform.position).normalized;
        if (direction != Vector2.zero)
            SetFacingDirection(direction);
    }

    private static bool IsPointerOverGameplayHud()
    {
        if (Pointer.current == null)
            return false;

        Vector2 screenPosition = Pointer.current.position.ReadValue();
        return ContainsScreenPoint("PlayerHUD", screenPosition)
            || ContainsScreenPoint("BottomHUD", screenPosition)
            || ContainsScreenPoint("Minimap", screenPosition);
    }

    private static bool ContainsScreenPoint(string objectName, Vector2 screenPosition)
    {
        GameObject hudObject = GameObject.Find(objectName);
        if (hudObject == null || !hudObject.TryGetComponent(out RectTransform rect))
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

    private void DisableAttackHitbox() => _attackHitbox?.EndAttack();

    private void CacheCombatReferences()
    {
        if (_attackFxRenderer == null)
            _attackFxRenderer = transform.Find("AttackFX")?.GetComponent<SpriteRenderer>();

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
}
