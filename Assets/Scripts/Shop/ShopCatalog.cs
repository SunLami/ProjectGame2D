using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Single source of truth for authored ShopDefinitions, mirroring QuestCatalog.</summary>
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Game/Shop/Shop Catalog")]
public sealed class ShopCatalog : ScriptableObject, IShopResolver
{
    [SerializeField] private ShopDefinition[] _shops;

    private Dictionary<string, ShopDefinition> _byId;

    public IReadOnlyList<ShopDefinition> AllShops => _shops ?? Array.Empty<ShopDefinition>();

    public bool TryResolve(string shopId, out ShopDefinition definition)
    {
        if (string.IsNullOrEmpty(shopId))
        {
            definition = null;
            return false;
        }

        EnsureLookup();
        return _byId.TryGetValue(shopId, out definition);
    }

    private void EnsureLookup()
    {
        if (_byId != null)
            return;

        _byId = new Dictionary<string, ShopDefinition>(StringComparer.Ordinal);
        if (_shops == null)
            return;

        foreach (ShopDefinition shop in _shops)
        {
            if (shop == null || string.IsNullOrEmpty(shop.ShopId) || _byId.ContainsKey(shop.ShopId))
                continue;

            _byId.Add(shop.ShopId, shop);
        }
    }
}
