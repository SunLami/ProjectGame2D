using System.Collections.Generic;
using UnityEngine;

// Builds an itemId -> ItemSO lookup from every ItemSO (including EquipmentItemSO) under a
// Resources folder. Used to resolve InventorySaveData entries back into real item references.
public static class ItemLookup
{
    public static Dictionary<string, ItemSO> BuildFromResources(string resourcesPath = "Items")
    {
        var lookup = new Dictionary<string, ItemSO>();

        foreach (ItemSO item in Resources.LoadAll<ItemSO>(resourcesPath))
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            lookup[item.itemId] = item;
        }

        return lookup;
    }
}
