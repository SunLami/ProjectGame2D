using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources-backed IItemResolver (D-020: Resources is a migration backend behind the resolver
/// abstraction, not a lookup domain code calls directly). Unlike ItemLookup.BuildFromResources,
/// a duplicate itemId is logged instead of silently overwriting the earlier definition.
/// </summary>
public sealed class ResourcesItemResolver : IItemResolver
{
    private readonly Dictionary<string, ItemSO> _byId = new();

    public ResourcesItemResolver(string resourcesPath = "Items")
    {
        foreach (ItemSO item in Resources.LoadAll<ItemSO>(resourcesPath))
        {
            if (item == null || string.IsNullOrEmpty(item.itemId))
                continue;

            if (_byId.TryGetValue(item.itemId, out ItemSO existing))
            {
                Debug.LogError(
                    $"ResourcesItemResolver: duplicate itemId '{item.itemId}' on '{item.name}' "
                    + $"conflicts with '{existing.name}'; keeping '{existing.name}'.");
                continue;
            }

            _byId[item.itemId] = item;
        }
    }

    public bool TryResolve(string itemId, out ItemSO item)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            item = null;
            return false;
        }

        return _byId.TryGetValue(itemId, out item);
    }
}
