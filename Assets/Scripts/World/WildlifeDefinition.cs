using UnityEngine;

[CreateAssetMenu(menuName = "Game/World/Wildlife Definition")]
public sealed class WildlifeDefinition : ScriptableObject
{
    [SerializeField] private string _animalId;
    [SerializeField, Min(1)] private float _health = 20f;
    [SerializeField, Min(0)] private float _speed = 1.5f;
    [SerializeField] private bool _aggressive;
    [SerializeField, Min(0)] private float _contactDamage;
    [SerializeField, Min(0)] private int _experienceReward;
    [SerializeField] private ResourceLootEntry[] _loot;
    [Tooltip("Player distance at which the animal starts chasing (aggressive) or fleeing.")]
    [SerializeField, Min(0.5f)] private float _reactionRange = 4f;
    [Tooltip("Max distance from its spawn point before the animal is pulled back home.")]
    [SerializeField, Min(1f)] private float _leashRange = 8f;
    public string AnimalId => _animalId;
    public float Health => _health;
    public float Speed => _speed;
    public bool Aggressive => _aggressive;
    public float ContactDamage => _contactDamage;
    public int ExperienceReward => _experienceReward;
    public ResourceLootEntry[] Loot => _loot;
    public float ReactionRange => _reactionRange;
    public float LeashRange => Mathf.Max(_leashRange, _reactionRange);
}
