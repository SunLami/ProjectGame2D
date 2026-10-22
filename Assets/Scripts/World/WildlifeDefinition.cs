using UnityEngine;

[CreateAssetMenu(menuName = "Game/World/Wildlife Definition")]
public sealed class WildlifeDefinition : ScriptableObject
{
    [SerializeField] private string _animalId;
    [SerializeField, Min(1)] private float _health = 20f;
    [SerializeField, Min(0)] private float _speed = 1.5f;
    [SerializeField] private bool _aggressive;
    [SerializeField, Min(0)] private float _contactDamage;
    [SerializeField] private ResourceLootEntry[] _loot;
    public string AnimalId => _animalId;
    public float Health => _health;
    public float Speed => _speed;
    public bool Aggressive => _aggressive;
    public float ContactDamage => _contactDamage;
    public ResourceLootEntry[] Loot => _loot;
}
