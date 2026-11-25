using System;
using UnityEngine;

/// <summary>Ordinary scene actor: wander/flee or chase using definition data. Respawns on scene reload.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class WildlifeActor : MonoBehaviour, IDamageable
{
    [SerializeField] private WildlifeDefinition _definition;
    [SerializeField] private Animator _animator;
    [SerializeField, Min(0f)] private float _deathAnimationDuration;

    private static readonly int Facing = Animator.StringToHash("Facing");
    private static readonly int Motion = Animator.StringToHash("Motion");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");

    private Rigidbody2D _body;
    private Vector2 _home, _wander;
    private float _health, _nextWander, _nextDamage;
    public event Action Died;
    public bool IsDead => _health <= 0f;
    public WildlifeDefinition Definition => _definition;
    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _home = transform.position;
        _wander = _home;
        _health = _definition != null ? _definition.Health : 1f;
    }
    private void FixedUpdate()
    {
        if (IsDead || _definition == null) return;
        Vector2 position = _body.position;
        Vector2 direction = Vector2.zero;
        PlayerStat player = PlayerStat.Instance;
        bool reactingToPlayer = false;
        bool canMove = GameStateManager.AllowsGameplayInput;
        if (player != null && !player.IsDead && Vector2.Distance(position, player.transform.position) < 4f)
        {
            reactingToPlayer = true;
            direction = ((Vector2)player.transform.position - position).normalized;
            if (!_definition.Aggressive) direction = -direction;
            if (_definition.Aggressive && Vector2.Distance(position, player.transform.position) < 0.85f && Time.time >= _nextDamage)
            {
                if (_animator != null && canMove)
                {
                    UpdateFacing(direction);
                    _animator.SetTrigger(Attack);
                }
                player.GetComponent<Player>()?.TakeDamage(_definition.ContactDamage, direction, 0.5f);
                _nextDamage = Time.time + 1.2f;
            }
        }
        else
        {
            if (Time.time >= _nextWander)
            {
                _wander = _home + UnityEngine.Random.insideUnitCircle * 2f;
                _nextWander = Time.time + 3f;
            }
            if (Vector2.Distance(position, _wander) > 0.2f) direction = (_wander - position).normalized;
        }
        if (Vector2.Distance(position, _home) > 8f) direction = (_home - position).normalized;
        _body.linearVelocity = canMove ? direction * _definition.Speed : Vector2.zero;
        if (_animator != null)
        {
            if (_body.linearVelocity.sqrMagnitude > 0.001f)
                UpdateFacing(direction);
            _animator.SetFloat(Motion, _body.linearVelocity.sqrMagnitude <= 0.001f ? 0f : reactingToPlayer ? 2f : 1f);
        }
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        float facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x < 0f ? 2f : 3f)
            : (direction.y < 0f ? 0f : 1f);
        _animator.SetFloat(Facing, facing);
    }
    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        if (IsDead || damage <= 0f || float.IsNaN(damage)) return;
        _health = Mathf.Max(0f, _health - damage);
        if (!IsDead)
        {
            if (_animator != null)
                _animator.SetTrigger(Hurt);
            return;
        }
        _body.linearVelocity = Vector2.zero;
        Died?.Invoke();
        if (_definition != null) QuestDomainEvents.RaiseEnemyKilled(_definition.AnimalId, string.Empty);
        if (_animator != null && _deathAnimationDuration > 0f)
        {
            _animator.SetBool(Dead, true);
            foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>())
                hitbox.enabled = false;
            Destroy(gameObject, _deathAnimationDuration);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
