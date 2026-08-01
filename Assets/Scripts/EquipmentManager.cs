using UnityEngine;
using UnityEngine.U2D.Animation;

public class EquipmentManager : MonoBehaviour
{
    [Header("Sprite Library Component")]
    public SpriteLibrary bodySpriteLibrary;
    public SpriteLibrary headSpriteLibrary;
    public SpriteLibrary swordSpriteLibrary;

    [Header("List Of LibraryAssets")]
    public SpriteLibraryAsset[] bodyEquipmentAssets;
    public SpriteLibraryAsset[] headEquipmentAssets;
    public SpriteLibraryAsset[] swordEquipmentAssets;

    private int currentBodyIndex = 0;
    private int currentSwordIndex = 0;

    public void EquipBodyByIndex(int index)
    {
        if (bodyEquipmentAssets == null || index < 0 || index >= bodyEquipmentAssets.Length) return;

        currentBodyIndex = index;
        bodySpriteLibrary.spriteLibraryAsset = bodyEquipmentAssets[index];

        if (headSpriteLibrary != null && headEquipmentAssets != null && index < headEquipmentAssets.Length)
            headSpriteLibrary.spriteLibraryAsset = headEquipmentAssets[index];
    }

    public void EquipSwordByIndex(int index)
    {
        if (swordEquipmentAssets == null || index < 0 || index >= swordEquipmentAssets.Length) return;

        currentSwordIndex = index;
        swordSpriteLibrary.spriteLibraryAsset = swordEquipmentAssets[index];
    }
}
