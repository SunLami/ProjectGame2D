using System;

/// <summary>Transient state per actor; definitions and V13 saves are never mutated.</summary>
public sealed class ConsumableBuffState
{
    private sealed class Buff
    {
        public float remaining, cap, regeneration, staminaRegeneration;
        public void Set(ItemSO item)
        {
            remaining = item.buffDurationSeconds;
            cap = item.effectAmount;
            regeneration = item.buffRegenerationFraction;
            staminaRegeneration = item.buffStaminaRegenerationBonus;
        }
        public bool Tick(float seconds)
        {
            bool active = remaining > 0f;
            remaining = Math.Max(0f, remaining - seconds);
            return active && remaining == 0f;
        }
    }
    private readonly Buff _health = new(), _mana = new(), _stamina = new();
    public float HealthRemaining => _health.remaining;
    public float ManaRemaining => _mana.remaining;
    public float StaminaRemaining => _stamina.remaining;
    public float HealthCapBonus => _health.remaining > 0f ? _health.cap : 0f;
    public float ManaCapBonus => _mana.remaining > 0f ? _mana.cap : 0f;
    public float StaminaCapBonus => _stamina.remaining > 0f ? _stamina.cap : 0f;
    public float HealthRegenerationFraction => _health.remaining > 0f ? _health.regeneration : 0f;
    public float ManaRegenerationFraction => _mana.remaining > 0f ? _mana.regeneration : 0f;
    public float StaminaRegenerationBonus => _stamina.remaining > 0f ? _stamina.staminaRegeneration : 0f;
    public void Refresh(ItemSO item)
    {
        switch (item.consumableEffect)
        {
            case ConsumableEffectType.HealthFood: _health.Set(item); break;
            case ConsumableEffectType.ManaDrink: _mana.Set(item); break;
            case ConsumableEffectType.StaminaDrink: _stamina.Set(item); break;
        }
    }
    public bool Tick(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
        // Do not short-circuit: all groups must advance.
        bool health = _health.Tick(seconds), mana = _mana.Tick(seconds), stamina = _stamina.Tick(seconds);
        return health || mana || stamina;
    }
}
