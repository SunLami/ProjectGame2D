using System.Collections.Generic;
using UnityEngine;

/// <summary>Retains all rolled loot in the world while inventory is full; retries near the player.</summary>
public sealed class DroppedLootPickup : MonoBehaviour
{
    private List<InventoryItemGrant> _grants;
    private float _nextAttempt;
    public void Initialize(List<InventoryItemGrant> grants) => _grants = grants;
    private void Update()
    {
        if (_grants == null || Time.time < _nextAttempt || PlayerStat.Instance == null
            || PlayerStat.Instance.IsDead || !GameStateManager.AllowsGameplayInput) return;
        _nextAttempt = Time.time + 0.3f;
        if (Vector2.Distance(transform.position, PlayerStat.Instance.transform.position) > 1.5f) return;
        if (InventoryManager.Instance != null && InventoryManager.Instance.TryAddBatch(_grants))
        {
            _grants = null;
            Destroy(gameObject);
        }
    }
}
