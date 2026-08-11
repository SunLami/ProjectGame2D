using UnityEngine;
using UnityEngine.U2D.Animation;

public enum EquipSlot
{
    Head,
    Body,
    Weapon,
    Ring,
    Necklace,
    Foot,
    Shield,
}

[CreateAssetMenu(fileName = "NewEquipmentItem", menuName = "Scriptable Objects/Equipment Item")]
public class EquipmentItemSO : ItemSO
{
    [Header("Equipment")]
    public EquipSlot slot;
    public SpriteLibraryAsset spriteLibraryAsset;

    // Optional: only used when slot == Body, for armor sets that also swap a head accessory (helmet/visor).
    public SpriteLibraryAsset headSpriteLibraryAsset;

    private void Reset()
    {
        isStackable = false;
        maxStackSize = 1;
    }
}
