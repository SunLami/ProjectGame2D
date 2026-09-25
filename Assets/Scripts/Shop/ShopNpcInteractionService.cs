using System;

/// <summary>
/// Capability seam a future NPC component composes instead of touching ShopManager internals
/// directly, mirroring QuestNpcInteractionService. Plain C# so it is unit-testable without a scene/
/// GameObject.
/// </summary>
public sealed class ShopNpcInteractionService
{
    private readonly ShopManager _shopManager;

    public ShopNpcInteractionService(ShopManager shopManager)
    {
        _shopManager = shopManager ?? throw new ArgumentNullException(nameof(shopManager));
    }

    /// <summary>Shop this npcId owns, if any.</summary>
    public bool TryGetShop(string npcId, out ShopDefinition shop)
    {
        shop = null;
        if (_shopManager.Catalog == null || string.IsNullOrEmpty(npcId))
            return false;

        foreach (ShopDefinition candidate in _shopManager.Catalog.AllShops)
        {
            if (string.Equals(candidate.NpcId, npcId, StringComparison.Ordinal))
            {
                shop = candidate;
                return true;
            }
        }
        return false;
    }

    /// <summary>Buys through npcId's own shop only -- rejects a shopId this npcId does not own.</summary>
    public bool TryPurchase(string npcId, string shopId, string itemId, int quantity, out ShopTransactionResult result)
    {
        if (!TryGetShop(npcId, out ShopDefinition shop) || shop.ShopId != shopId)
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }
        return _shopManager.TryPurchase(shopId, itemId, quantity, out result);
    }

    public bool TryCreateBuyQuote(string npcId, string shopId, string itemId, int quantity, out int unitPrice, out ShopTransactionResult result)
    {
        unitPrice = 0;
        if (!TryGetShop(npcId, out ShopDefinition shop) || shop.ShopId != shopId)
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }
        return _shopManager.TryCreateBuyQuote(shopId, itemId, quantity, out unitPrice, out result);
    }

    public bool TryPurchaseQuoted(string npcId, string shopId, string itemId, int quantity, int unitPrice, out int totalCost, out ShopTransactionResult result)
    {
        totalCost = 0;
        if (!TryGetShop(npcId, out ShopDefinition shop) || shop.ShopId != shopId)
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }
        return _shopManager.TryPurchaseQuoted(shopId, itemId, quantity, unitPrice, out totalCost, out result);
    }

    /// <summary>Sells through npcId's own shop only -- rejects a shopId this npcId does not own.</summary>
    public bool TrySell(string npcId, string shopId, string itemId, int quantity, out ShopTransactionResult result)
    {
        if (!TryGetShop(npcId, out ShopDefinition shop) || shop.ShopId != shopId)
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }
        return _shopManager.TrySell(shopId, itemId, quantity, out result);
    }

    public bool TryCreateSellQuote(string npcId, string shopId, string itemId, int quantity, out int unitPrice, out ShopTransactionResult result)
    {
        unitPrice = 0;
        if (!TryGetShop(npcId, out ShopDefinition shop) || shop.ShopId != shopId)
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }
        return _shopManager.TryCreateSellQuote(shopId, itemId, quantity, out unitPrice, out result);
    }

    public bool TrySellQuoted(string npcId, string shopId, string itemId, int quantity, int unitPrice, out int totalValue, out ShopTransactionResult result)
    {
        totalValue = 0;
        if (!TryGetShop(npcId, out ShopDefinition shop) || shop.ShopId != shopId)
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }
        return _shopManager.TrySellQuoted(shopId, itemId, quantity, unitPrice, out totalValue, out result);
    }
}
