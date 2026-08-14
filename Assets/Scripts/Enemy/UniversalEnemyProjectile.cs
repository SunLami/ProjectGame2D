using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class UniversalEnemyProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _lifetime = 5f;
    [SerializeField] private bool _destroyOnHit = true;
    private float _damage;
    private float _knockback;

    public void Launch(Vector2 direction, float speed, float damage, float knockback)
    {
        _damage = damage;
        _knockback = knockback;
        GetComponent<Collider2D>().isTrigger = true;
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
        Destroy(gameObject, _lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead) return;
        Vector2 direction = (player.transform.position - transform.position).normalized;
        player.TakeDamage(_damage, direction, _knockback);
        if (_destroyOnHit) Destroy(gameObject);
    }
}
