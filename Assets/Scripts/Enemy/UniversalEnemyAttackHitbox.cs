using System.Collections.Generic;
using UnityEngine;

public sealed class UniversalEnemyAttackHitbox : MonoBehaviour
{
    private readonly HashSet<Player> _hits = new();
    private EnemyUniversal _owner;
    private Collider2D _collider;
    private float _damage;
    private float _knockback;

    public void Initialize(EnemyUniversal owner)
    {
        _owner = owner;
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        Close();
    }

    public void Open(float damage, float knockback)
    {
        _damage = damage;
        _knockback = knockback;
        _hits.Clear();
        _collider.enabled = true;
    }

    public void Close() { if (_collider != null) _collider.enabled = false; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (_owner == null || player == null || player.IsDead || !_hits.Add(player)) return;
        Vector2 direction = (player.transform.position - _owner.transform.position).normalized;
        player.TakeDamage(_damage, direction, _knockback);
    }
}
