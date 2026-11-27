/// <summary>Hurt / death / attack SFX of MapDuy wildlife, chosen from <see cref="WildlifeDefinition.AnimalId"/>
/// (animal.deer / animal.boar / animal.rabbit). Unknown animals fall back to the generic enemy sounds. Positional.</summary>
public static class WildlifeSfx
{
    private static string Animal(string animalId) =>
        string.IsNullOrEmpty(animalId) ? string.Empty : animalId.Substring(animalId.LastIndexOf('.') + 1);

    public static void Hurt(UnityEngine.Component actor, string animalId) => Play(actor, Animal(animalId) switch
    {
        "deer" => SfxIds.WildlifeDeerHurt,
        "boar" => SfxIds.WildlifeBoarHurt,
        "rabbit" => SfxIds.WildlifeRabbitHurt,
        _ => SfxIds.CombatEnemyHit,
    });

    public static void Death(UnityEngine.Component actor, string animalId) => Play(actor, Animal(animalId) switch
    {
        "deer" => SfxIds.WildlifeDeerDeath,
        "boar" => SfxIds.WildlifeBoarDeath,
        "rabbit" => SfxIds.WildlifeRabbitDeath,
        _ => SfxIds.CombatEnemyDeath,
    });

    /// <summary>Only the boar attacks; other animals stay silent here.</summary>
    public static void Attack(UnityEngine.Component actor, string animalId)
    {
        if (Animal(animalId) == "boar")
            Play(actor, SfxIds.WildlifeBoarAttack);
    }

    /// <summary>The animal notices the player: boar grunts (charges), deer snorts, rabbit bolts through the grass.</summary>
    public static void Alert(UnityEngine.Component actor, string animalId) => Play(actor, Animal(animalId) switch
    {
        "deer" => SfxIds.WildlifeDeerAlert,
        "boar" => SfxIds.WildlifeBoarAlert,
        "rabbit" => SfxIds.WildlifeRabbitAlert,
        _ => null,
    });

    /// <summary>Rare idle voice while wandering near the player (rabbits are silent).</summary>
    public static void Idle(UnityEngine.Component actor, string animalId) => Play(actor, Animal(animalId) switch
    {
        "deer" => SfxIds.WildlifeDeerIdle,
        "boar" => SfxIds.WildlifeBoarIdle,
        _ => null,
    });

    private static void Play(UnityEngine.Component actor, string id)
    {
        if (actor != null && id != null)
            SoundFXManager.PlaySfxAt(id, actor.transform.position);
    }
}
