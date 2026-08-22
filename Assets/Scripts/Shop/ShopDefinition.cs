using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-driven shop content. shopId is the stable identity ShopService/ShopNpcInteractionService
/// key off of; not mutated at runtime (DataDrivenDevelopment.md three-layer model -- there is no
/// separate ShopRuntimeState in this phase because stock never depletes, see ShopStockEntry).
/// </summary>
[CreateAssetMenu(fileName = "NewShopDefinition", menuName = "Game/Shop/Shop Definition")]
public sealed class ShopDefinition : ScriptableObject
{
    [SerializeField] private string _shopId;
    [SerializeField] private string _displayName;

    [Tooltip("Stable npcId that owns/offers this shop.")]
    [SerializeField] private string _npcId;

    [SerializeField] private ShopStockEntry[] _stock;

    [Tooltip("Sell-back price = stock entry price * this multiplier. Selling is only supported for " +
        "items that are also in this shop's own stock list (see ShopManager.TrySell remarks).")]
    [SerializeField, Range(0f, 1f)] private float _sellPriceMultiplier = 0.5f;

    public string ShopId => _shopId;
    public string DisplayName => _displayName;
    public string NpcId => _npcId;
    public IReadOnlyList<ShopStockEntry> Stock => _stock ?? Array.Empty<ShopStockEntry>();
    public float SellPriceMultiplier => _sellPriceMultiplier;
}
