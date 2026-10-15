using UnityEngine;

// The "effect owner" FarmingSystem.md's quick-bar contract calls for: subscribes to
// QuickBarManager.ActiveItemUseRequested, applies the item's effect to PlayerStat and owns the
// atomic Inventory consumption. Added onto the same GameObject as QuickBarManager in its own
// Awake (see QuickBarManager.Awake), so there is no subscribe-order race with its singleton.
public sealed class ConsumableUseHandler : MonoBehaviour
{
    private void Awake()
    {
        QuickBarManager.Instance.ActiveItemUseRequested += HandleUseRequested;
    }

    private void OnDestroy()
    {
        if (QuickBarManager.Instance != null)
            QuickBarManager.Instance.ActiveItemUseRequested -= HandleUseRequested;
    }

    private void HandleUseRequested(ItemSO item)
    {
        if (item == null || PlayerStat.Instance == null || InventoryManager.Instance == null)
            return;

        if (!TryApplyEffect(item))
            return;

        InventoryManager.Instance.RemoveItem(item, 1);
    }

    // Returns false (and consumes nothing) when the item has no wired effect yet, or the effect
    // would be a no-op (e.g. healing at full health) -- a potion is never spent for nothing.
    private bool TryApplyEffect(ItemSO item)
    {
        PlayerStat stat = PlayerStat.Instance;
        switch (item.consumableEffect)
        {
            case ConsumableEffectType.Heal:
                if (stat.Health >= stat.MaxHealth) return false;
                stat.Heal(item.effectAmount);
                return true;

            case ConsumableEffectType.RestoreStamina:
                if (stat.Stamina >= stat.MaxStamina) return false;
                stat.RestoreStamina(item.effectAmount);
                return true;

            case ConsumableEffectType.RestoreMana:
                if (stat.Mana >= stat.MaxMana) return false;
                stat.RestoreMana(item.effectAmount);
                return true;

            default:
                return false;
        }
    }
}
