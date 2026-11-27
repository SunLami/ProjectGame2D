using UnityEngine;

/// <summary>Picks the enemy-family SFX (slime / goblin / ent / generic) from the enemy's GameObject name, because most enemy
/// prefabs have no id configured. Positional (quieter when far from the player).</summary>
public static class EnemySfx
{
    private enum Family { Generic, Slime, Goblin, Ent }

    private static Family FamilyOf(Component enemy)
    {
        string n = enemy != null ? enemy.name.ToLowerInvariant() : string.Empty;
        if (n.Contains("slime")) return Family.Slime;
        if (n.Contains("goblin")) return Family.Goblin;
        if (n.StartsWith("ent")) return Family.Ent; // "Ent", "Ent (2)"; not Contains: would match "parent", "serpent"…
        return Family.Generic;
    }

    public static void Hurt(Component enemy)
    {
        string id = FamilyOf(enemy) switch
        {
            Family.Slime => SfxIds.EnemySlimeHurt,
            Family.Goblin => SfxIds.EnemyGoblinHurt,
            Family.Ent => SfxIds.EnemyEntHurt,
            _ => SfxIds.CombatEnemyHit,
        };
        SoundFXManager.PlaySfxAt(id, enemy.transform.position);
    }

    public static void Death(Component enemy)
    {
        string id = FamilyOf(enemy) switch
        {
            Family.Slime => SfxIds.EnemySlimeDeath,
            Family.Goblin => SfxIds.EnemyGoblinDeath,
            Family.Ent => SfxIds.EnemyEntDeath,
            _ => SfxIds.CombatEnemyDeath,
        };
        SoundFXManager.PlaySfxAt(id, enemy.transform.position);
    }

    public static void Attack(Component enemy)
    {
        string id = FamilyOf(enemy) switch
        {
            Family.Slime => SfxIds.EnemySlimeAttack,
            Family.Goblin => SfxIds.EnemyGoblinAttack,
            Family.Ent => SfxIds.EnemyEntAttack,
            _ => null,
        };
        if (id != null)
            SoundFXManager.PlaySfxAt(id, enemy.transform.position);
    }

    /// <summary>The enemy spots the player (enters a chase). Generic enemies stay silent.</summary>
    public static void Alert(Component enemy) => PlayIfAny(enemy, FamilyOf(enemy) switch
    {
        Family.Slime => SfxIds.EnemySlimeAlert,
        Family.Goblin => SfxIds.EnemyGoblinAlert,
        Family.Ent => SfxIds.EnemyEntAlert,
        _ => null,
    });

    /// <summary>Rare idle voice while idling / patrolling near the player.</summary>
    public static void Idle(Component enemy) => PlayIfAny(enemy, FamilyOf(enemy) switch
    {
        Family.Slime => SfxIds.EnemySlimeIdle,
        Family.Goblin => SfxIds.EnemyGoblinIdle,
        Family.Ent => SfxIds.EnemyEntIdle,
        _ => null,
    });

    private static void PlayIfAny(Component enemy, string id)
    {
        if (id != null && enemy != null)
            SoundFXManager.PlaySfxAt(id, enemy.transform.position);
    }

    public static void ProjectileLaunch(Component enemy) => SoundFXManager.PlaySfxAt(SfxIds.CombatProjectileLaunch, enemy.transform.position);
}
