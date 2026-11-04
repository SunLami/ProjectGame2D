public static class CraftingPresentationCategory
{
    public static string ForItem(ItemSO item)
    {
        if (item is EquipmentItemSO equipment)
            return equipment.slot == EquipSlot.Weapon ? "SWORD" : equipment.slot.ToString().ToUpperInvariant();
        return item == null ? "OTHER" : item.type switch
        {
            ItemType.Material => "MATERIAL",
            ItemType.Consumable => "CONSUMABLE",
            _ => "OTHER"
        };
    }
}
