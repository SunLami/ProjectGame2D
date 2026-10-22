using UnityEngine;

// Validates effect and quantity before consumption. Cooldown is session-only.
public sealed class ConsumableUseHandler : MonoBehaviour
{
    public const float PotionCooldownSeconds = 10f;
    private float _potionReadyAt;
    private bool _using;
    public float PotionCooldownRemaining => Mathf.Max(0f, _potionReadyAt - Time.time);
    private void Awake() => QuickBarManager.Instance.ActiveItemUseRequested += HandleUseRequested;
    private void OnDestroy()
    {
        if (QuickBarManager.Instance != null)
            QuickBarManager.Instance.ActiveItemUseRequested -= HandleUseRequested;
    }
    private void HandleUseRequested(ItemSO item)
    {
        if (!TryUse(item, Time.time) && IsPotion(item) && PotionCooldownRemaining > 0f)
            InventoryActionFeedbackUI.Show($"Potion ready in {Mathf.CeilToInt(PotionCooldownRemaining)}s.");
    }
    internal bool TryUse(ItemSO item, float now)
    {
        if (_using || float.IsNaN(now) || float.IsInfinity(now)) return false;
        _using = true;
        try { return TryUseCore(item, now); }
        finally { _using = false; }
    }
    private bool TryUseCore(ItemSO item, float now)
    {
        PlayerStat stat = PlayerStat.Instance;
        InventoryManager inventory = InventoryManager.Instance;
        if (!GameStateManager.AllowsGameplayInput || stat == null || stat.IsDead || inventory == null
            || item == null || !item.usedItems || item.quickBarUseMode != QuickBarUseMode.Active
            || !inventory.HasItem(item, 1) || !PositiveFinite(item.effectAmount)
            || (IsPotion(item) && now < _potionReadyAt)) return false;
        bool buff = item.consumableEffect is ConsumableEffectType.HealthFood
            or ConsumableEffectType.ManaDrink or ConsumableEffectType.StaminaDrink;
        if (buff && (!PositiveFinite(item.buffDurationSeconds) || item.effectAmount > 1f
            || !Fraction(item.buffRegenerationFraction) || !Fraction(item.buffStaminaRegenerationBonus))) return false;
        switch (item.consumableEffect)
        {
            case ConsumableEffectType.Heal:
            case ConsumableEffectType.HealPercent:
                if (stat.Health >= stat.MaxHealth) return false;
                break;
            case ConsumableEffectType.RestoreMana:
            case ConsumableEffectType.RestoreManaPercent:
                if (stat.Mana >= stat.MaxMana) return false;
                break;
            case ConsumableEffectType.RestoreStamina:
                if (stat.Stamina >= stat.MaxStamina) return false;
                break;
            default:
                if (!buff) return false;
                break;
        }
        if (IsPotion(item) && item.effectAmount > 1f) return false;
        if (!inventory.RemoveItem(item, 1)) return false;
        switch (item.consumableEffect)
        {
            case ConsumableEffectType.Heal: stat.Heal(item.effectAmount); break;
            case ConsumableEffectType.RestoreStamina: stat.RestoreStamina(item.effectAmount); break;
            case ConsumableEffectType.RestoreMana: stat.RestoreMana(item.effectAmount); break;
            case ConsumableEffectType.HealPercent: stat.Heal(stat.MaxHealth * item.effectAmount); break;
            case ConsumableEffectType.RestoreManaPercent: stat.RestoreMana(stat.MaxMana * item.effectAmount); break;
            default:
                stat.ConsumableBuffs.Refresh(item);
                stat.NotifyConsumableStatsChanged();
                break;
        }
        if (IsPotion(item)) _potionReadyAt = now + PotionCooldownSeconds;
        return true;
    }
    private static bool IsPotion(ItemSO item) => item != null && item.consumableEffect
        is ConsumableEffectType.HealPercent or ConsumableEffectType.RestoreManaPercent;
    private static bool PositiveFinite(float value) => value > 0f && !float.IsInfinity(value) && !float.IsNaN(value);
    private static bool Fraction(float value) => value >= 0f && value <= 1f && !float.IsNaN(value);
}
