using System;
using UnityEngine;

/// <summary>Ordinary scene actor: wander/flee or chase using definition data. Respawns on scene reload.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class WildlifeActor : MonoBehaviour, IDamageable
{
    [SerializeField] private WildlifeDefinition _definition;
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
        if (player != null && !player.IsDead && Vector2.Distance(position, player.transform.position) < 4f)
        {
            direction = ((Vector2)player.transform.position - position).normalized;
            if (!_definition.Aggressive) direction = -direction;
            if (_definition.Aggressive && Vector2.Distance(position, player.transform.position) < 0.85f && Time.time >= _nextDamage)
            {
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
        _body.linearVelocity = GameStateManager.AllowsGameplayInput ? direction * _definition.Speed : Vector2.zero;
    }
    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        if (IsDead || damage <= 0f || float.IsNaN(damage)) return;
        _health = Mathf.Max(0f, _health - damage);
        if (!IsDead) return;
        _body.linearVelocity = Vector2.zero;
        Died?.Invoke();
        if (_definition != null) QuestDomainEvents.RaiseEnemyKilled(_definition.AnimalId, string.Empty);
        Destroy(gameObject);
    }
}
