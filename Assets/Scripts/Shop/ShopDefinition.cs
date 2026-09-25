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

    [Header("Item Lists")]
    [Tooltip("Items displayed in the Buy tab. Add ItemSO assets here; pricing comes from each item's Buy Min/Max range.")]
    [SerializeField] private ItemSO[] _buyItems;

    [Tooltip("Items this NPC accepts in the Sell tab. Recipe outputs offered by this NPC are also accepted.")]
    [SerializeField] private ItemSO[] _sellItems;

    [Tooltip("Legacy sell-back fallback = stock entry price * this multiplier when an ItemSO has no Sell Min/Max range.")]
    [SerializeField, Range(0f, 1f)] private float _sellPriceMultiplier = 0.5f;

    public string ShopId => _shopId;
    public string DisplayName => _displayName;
    public string NpcId => _npcId;
    public IReadOnlyList<ShopStockEntry> Stock => _stock ?? Array.Empty<ShopStockEntry>();
    public IReadOnlyList<ItemSO> BuyItems => _buyItems ?? Array.Empty<ItemSO>();
    public IReadOnlyList<ItemSO> SellItems => _sellItems ?? Array.Empty<ItemSO>();
    public bool UsesExplicitBuyItems => _buyItems != null && _buyItems.Length > 0;
    public bool UsesExplicitSellItems => _sellItems != null && _sellItems.Length > 0;
    public float SellPriceMultiplier => _sellPriceMultiplier;
}
