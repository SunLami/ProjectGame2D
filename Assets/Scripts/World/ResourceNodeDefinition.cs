using UnityEngine;

[CreateAssetMenu(fileName = "ResourceNodeDefinition", menuName = "Game/World/Resource Node Definition")]
public sealed class ResourceNodeDefinition : ScriptableObject
{
    [SerializeField] private string _resourceId;
    [SerializeField] private ResourceHarvestType _harvestType;
    [SerializeField] private HarvestToolType _requiredToolType;
    [SerializeField, Min(1f)] private float _maximumHealth = 3f;
    [SerializeField, Min(0.01f)] private float _harvestDamage = 1f;
    [SerializeField, Range(1f, 1.5f)] private float _gatheringDuration = 1.2f;
    [SerializeField, Min(0f)] private float _respawnSeconds = 60f;
    [SerializeField] private ResourceLootEntry[] _lootTable;

    public string ResourceId => _resourceId;
    public ResourceHarvestType HarvestType => _harvestType;
    public HarvestToolType RequiredToolType => _requiredToolType;
    public float MaximumHealth => Mathf.Max(1f, _maximumHealth);
    public float HarvestDamage => Mathf.Max(0.01f, _harvestDamage);
    public float GatheringDuration => Mathf.Clamp(_gatheringDuration, 1f, 1.5f);
    public float RespawnSeconds => Mathf.Max(0f, _respawnSeconds);
    public ResourceLootEntry[] LootTable => _lootTable;

    private void OnValidate()
    {
        _maximumHealth = Mathf.Max(1f, _maximumHealth);
        _harvestDamage = Mathf.Max(0.01f, _harvestDamage);
        _gatheringDuration = Mathf.Clamp(_gatheringDuration, 1f, 1.5f);
        _respawnSeconds = Mathf.Max(0f, _respawnSeconds);
    }

    internal void ConfigureForTests(
        string resourceId,
        ResourceHarvestType harvestType,
        HarvestToolType requiredToolType,
        float maximumHealth,
        float harvestDamage,
        float gatheringDuration,
        float respawnSeconds,
        ResourceLootEntry[] lootTable)
    {
        _resourceId = resourceId;
        _harvestType = harvestType;
        _requiredToolType = requiredToolType;
        _maximumHealth = Mathf.Max(1f, maximumHealth);
        _harvestDamage = Mathf.Max(0.01f, harvestDamage);
        _gatheringDuration = Mathf.Clamp(gatheringDuration, 1f, 1.5f);
        _respawnSeconds = Mathf.Max(0f, respawnSeconds);
        _lootTable = lootTable;
    }
}
