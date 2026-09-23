using UnityEngine;

[CreateAssetMenu(fileName = "SeedItem", menuName = "Game/Farming/Seed Item")]
public sealed class SeedItemSO : ItemSO
{
    [SerializeField] private CropDefinition _crop;
    public CropDefinition Crop => _crop;

    internal void ConfigureCropForTests(CropDefinition crop) => _crop = crop;
}
