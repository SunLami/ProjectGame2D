using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentCatalog", menuName = "Scriptable Objects/Equipment Catalog")]
public class EquipmentCatalog : ScriptableObject
{
    [Header("Head")]
    public EquipmentItemSO[] headItems;

    [Header("Body")]
    public EquipmentItemSO[] bodyItems;

    [Header("Weapon")]
    public EquipmentItemSO[] weaponItems;

    [Header("Ring")]
    public EquipmentItemSO[] ringItems;

    [Header("Necklace")]
    public EquipmentItemSO[] necklaceItems;

    [Header("Foot")]
    public EquipmentItemSO[] footItems;

    [Header("Shield")]
    public EquipmentItemSO[] shieldItems;
}
