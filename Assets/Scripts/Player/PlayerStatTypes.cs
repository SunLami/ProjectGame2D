using System;

public enum PlayerDamageOutcome
{
    Ignored,
    Dodged,
    Damaged,
    Killed
}

public readonly struct PlayerDamageResult
{
    public PlayerDamageResult(PlayerDamageOutcome outcome, float damageTaken)
    {
        Outcome = outcome;
        DamageTaken = damageTaken;
    }

    public PlayerDamageOutcome Outcome { get; }
    public float DamageTaken { get; }
}

[Serializable]
public struct PlayerStatModifiers
{
    public float maxHealth;
    public float attackDamage;
    public float defense;
    public float moveSpeed;
    public float sprintMultiplier;
    public float criticalChance;
    public float criticalMultiplier;
    public float damageReduction;
    public float healthRegeneration;
    public float dodgeChance;

    public static PlayerStatModifiers operator +(PlayerStatModifiers a, PlayerStatModifiers b)
    {
        return new PlayerStatModifiers
        {
            maxHealth = a.maxHealth + b.maxHealth,
            attackDamage = a.attackDamage + b.attackDamage,
            defense = a.defense + b.defense,
            moveSpeed = a.moveSpeed + b.moveSpeed,
            sprintMultiplier = a.sprintMultiplier + b.sprintMultiplier,
            criticalChance = a.criticalChance + b.criticalChance,
            criticalMultiplier = a.criticalMultiplier + b.criticalMultiplier,
            damageReduction = a.damageReduction + b.damageReduction,
            healthRegeneration = a.healthRegeneration + b.healthRegeneration,
            dodgeChance = a.dodgeChance + b.dodgeChance
        };
    }
}
