/// <summary>Shared failure-message text for EquipmentManager.Equip/Unequip, used by both
/// InventorySlotUI and EquipmentSlotUI so the two entry points (double-click, drag-and-drop)
/// report the same reason.</summary>
internal static class EquipFeedback
{
    public static string BuildEquipFailureMessage(EquipmentItemSO item)
    {
        if (PlayerStat.Instance != null && PlayerStat.Instance.Level < item.requiredLevel)
            return $"Cannot equip {item.itemName}: requires level {item.requiredLevel}.";

        return $"Cannot equip {item.itemName}: inventory is full.";
    }

    public static string BuildUnequipFailureMessage(EquipmentItemSO item)
    {
        return $"Cannot unequip {item.itemName}: inventory is full.";
    }
}
