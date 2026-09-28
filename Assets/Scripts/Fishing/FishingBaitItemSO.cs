using UnityEngine;

[CreateAssetMenu(fileName = "FishingBait", menuName = "Project Game 2D/Fishing/Fishing Bait Item")]
public sealed class FishingBaitItemSO : ItemSO
{
    [SerializeField] private FishingBaitTier _tier = FishingBaitTier.Small;

    public FishingBaitTier Tier => _tier;

    private void OnValidate()
    {
        isStackable = true;
        type = ItemType.Consumable;
    }
}
