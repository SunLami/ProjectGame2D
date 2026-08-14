using UnityEngine;

public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce);
}
