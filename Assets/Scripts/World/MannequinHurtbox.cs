using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class MannequinHurtbox : MonoBehaviour, IDamageable
{
    [SerializeField] private MannequinHitReaction _hitReaction;

    private CapsuleCollider2D _hurtboxCollider;

    public bool IsDead => false;

    private void Awake()
    {
        CacheReferences();
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        // The training mannequin only displays feedback; damage and knockback are intentionally ignored.
        _hitReaction?.PlayHitFeedback();
    }

    private void CacheReferences()
    {
        if (_hurtboxCollider == null)
            _hurtboxCollider = GetComponent<CapsuleCollider2D>();
        if (_hitReaction == null)
            _hitReaction = GetComponentInParent<MannequinHitReaction>();
    }

    private void Reset()
    {
        CacheReferences();
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        CacheReferences();
        EnsureTriggerCollider();
    }

    private void EnsureTriggerCollider()
    {
        if (_hurtboxCollider != null)
            _hurtboxCollider.isTrigger = true;
    }
}
