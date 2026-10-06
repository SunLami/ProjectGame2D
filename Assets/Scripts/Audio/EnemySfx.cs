using UnityEngine;

/// <summary>Picks the enemy-family SFX (slime / goblin / generic) from the enemy's GameObject name, because most enemy
/// prefabs have no id configured. Positional (quieter when far from the player).</summary>
public static class EnemySfx
{
    private enum Family { Generic, Slime, Goblin }

    private static Family FamilyOf(Component enemy)
    {
        string n = enemy != null ? enemy.name.ToLowerInvariant() : string.Empty;
        if (n.Contains("slime")) return Family.Slime;
        if (n.Contains("goblin")) return Family.Goblin;
        return Family.Generic;
    }

    public static void Hurt(Component enemy)
    {
        string id = FamilyOf(enemy) switch
        {
            Family.Slime => SfxIds.EnemySlimeHurt,
            Family.Goblin => SfxIds.EnemyGoblinHurt,
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
            _ => SfxIds.CombatEnemyDeath,
        };
        SoundFXManager.PlaySfxAt(id, enemy.transform.position);
    }

    public static void Attack(Component enemy)
    {
        Family family = FamilyOf(enemy);
        if (family == Family.Generic)
            return;
        SoundFXManager.PlaySfxAt(family == Family.Slime ? SfxIds.EnemySlimeAttack : SfxIds.EnemyGoblinAttack, enemy.transform.position);
    }

    public static void ProjectileLaunch(Component enemy) => SoundFXManager.PlaySfxAt(SfxIds.CombatProjectileLaunch, enemy.transform.position);
}
