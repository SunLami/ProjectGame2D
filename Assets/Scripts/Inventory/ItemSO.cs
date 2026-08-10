using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Material,
    Consumable,
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Scriptable Objects/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType type;
    public bool isStackable = true;
    public int maxStackSize = 99;
}
