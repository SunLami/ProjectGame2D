using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class MannequinHurtbox : MonoBehaviour, IDamageable
{
    [SerializeField] private MannequinHitReaction _hitReaction;

    [Tooltip("Quest Kill objective target id (e.g. \"enemy.manequin.training\"). Left empty, this "
        + "mannequin never counts toward a Kill objective -- purely cosmetic training feedback.")]
    [SerializeField] private string _enemyId;
    [SerializeField] private string _areaId;

    [Header("Health")]
    [SerializeField, Min(1f)] private float _maxHealth = 30f;
    [Tooltip("Seconds after death before this dummy resets its health and reappears.")]
    [SerializeField, Min(0f)] private float _respawnDelay = 10f;

    private CapsuleCollider2D _hurtboxCollider;
    private SpriteRenderer _bodyRenderer;
    private Collider2D _bodyCollider;
    private float _health;
    private bool _isDead;
    private Coroutine _respawnRoutine;

    /// <summary>(currentHealth, maxHealth) -- for a world-space health bar, mirroring
    /// EnemyUniversal.HealthChanged's shape so the same presentation pattern applies here.</summary>
    public event Action<float, float> HealthChanged;

    /// <summary>Fired when this dummy resets back to full health and becomes hittable again --
    /// mirrors EnemyUniversal.ReturnedHome as the health bar's "hide me" signal.</summary>
    public event Action Respawned;

    public bool IsDead => _isDead;
    public float Health => _health;
    public float MaxHealth => _maxHealth;

    private void Awake()
    {
        CacheReferences();
        _health = _maxHealth;
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        // Knockback is intentionally ignored -- the training mannequin never moves, dead or alive.
        if (_isDead || damage <= 0f)
            return;

        _hitReaction?.PlayHitFeedback();

        _health = Mathf.Max(0f, _health - damage);
        HealthChanged?.Invoke(_health, _maxHealth);

        if (_health <= 0f)
            Die();
    }

    private void Die()
    {
        _isDead = true;

        // Quest tracking only, never a player reward -- this is a training dummy, not a real
        // enemy, so its death must never grant XP/loot. RaiseEnemyKilled only progresses a
        // Kill objective's counter; the quest's own reward (if any) is granted once at turn-in,
        // not per kill, and nothing here calls PlayerStat.AddExperience.
        if (!string.IsNullOrEmpty(_enemyId))
            QuestDomainEvents.RaiseEnemyKilled(_enemyId, _areaId);

        if (_bodyRenderer != null)
            _bodyRenderer.enabled = false;
        if (_bodyCollider != null)
            _bodyCollider.enabled = false;
        _hurtboxCollider.enabled = false;

        if (_respawnRoutine != null)
            StopCoroutine(_respawnRoutine);
        _respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        _respawnRoutine = null;
        _isDead = false;
        _health = _maxHealth;

        if (_bodyRenderer != null)
            _bodyRenderer.enabled = true;
        if (_bodyCollider != null)
            _bodyCollider.enabled = true;
        _hurtboxCollider.enabled = true;

        HealthChanged?.Invoke(_health, _maxHealth);
        Respawned?.Invoke();
    }

    private void CacheReferences()
    {
        if (_hurtboxCollider == null)
            _hurtboxCollider = GetComponent<CapsuleCollider2D>();
        if (_hitReaction == null)
            _hitReaction = GetComponentInParent<MannequinHitReaction>();
        if (_bodyRenderer == null)
            _bodyRenderer = GetComponentInParent<SpriteRenderer>();
        if (_bodyCollider == null)
        {
            foreach (Collider2D candidate in GetComponentsInParent<Collider2D>())
            {
                if (candidate != _hurtboxCollider)
                {
                    _bodyCollider = candidate;
                    break;
                }
            }
        }
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

    private void OnDisable()
    {
        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }
    }
}
