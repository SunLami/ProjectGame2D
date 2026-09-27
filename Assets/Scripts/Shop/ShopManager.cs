using UnityEngine;

/// <summary>
/// Atomic buy/sell transaction engine, separate from UI (Roadmap Phase 7: "Tach ShopService khoi
/// UI"). Session-scoped persistent singleton like QuestManager/InventoryManager, torn down by
/// GameplaySceneLifetime. Does not subscribe to any event -- it is a producer: on a successful
/// purchase it raises QuestDomainEvents.ItemPurchased exactly once, closing the Phase 6 Quest
/// integration gap for Purchase objectives. Never partially spends gold/consumes items: every
/// TryPurchase/TrySell fully validates before mutating anything.
/// </summary>
public sealed class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private ShopCatalog _catalog;

    private IItemResolver _itemResolver;
    private IRecipeResolver _recipeResolver;

    public IShopResolver Catalog => _catalog;

    private IItemResolver ItemResolver => _itemResolver ??= new ResourcesItemResolver();

    internal void ConfigureForTests(ShopCatalog catalog, IItemResolver itemResolver = null, IRecipeResolver recipeResolver = null)
    {
        _catalog = catalog;
        _itemResolver = itemResolver;
        _recipeResolver = recipeResolver;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Editor-only parent (e.g. "_Managers") keeps the Hierarchy tidy; detach before
            // DontDestroyOnLoad, which only works on root GameObjects.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Buys quantity of itemId from shopId. Validates gold and inventory capacity fully
    /// before spending/adding anything.</summary>
    public bool TryPurchase(string shopId, string itemId, int quantity, out ShopTransactionResult result)
    {
        if (!TryCreateBuyQuote(shopId, itemId, quantity, out int unitPrice, out result))
            return false;
        return TryPurchaseQuoted(shopId, itemId, quantity, unitPrice, out _, out result);
    }

    public bool TryCreateBuyQuote(string shopId, string itemId, int quantity, out int unitPrice, out ShopTransactionResult result)
    {
        unitPrice = 0;
        if (!GameStateManager.AllowsGameplayInput)
        {
            result = ShopTransactionResult.GameplayNotAllowed;
            return false;
        }

        if (_catalog == null || !_catalog.TryResolve(shopId, out ShopDefinition shop))
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }

        if (!IsBuyItem(shop, itemId))
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }

        if (quantity <= 0)
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }

        if (!ItemResolver.TryResolve(itemId, out ItemSO item))
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }

        if (item.MaxBuyPrice > 0)
            unitPrice = Random.Range(item.MinBuyPrice, item.MaxBuyPrice + 1);
        else if (TryFindStock(shop, itemId, out ShopStockEntry stock))
            unitPrice = stock.Price;
        else
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }
        result = ShopTransactionResult.Success;
        return true;
    }

    public bool TryPurchaseQuoted(string shopId, string itemId, int quantity, int unitPrice, out int totalCost, out ShopTransactionResult result)
    {
        totalCost = 0;
        if (!GameStateManager.AllowsGameplayInput)
        {
            result = ShopTransactionResult.GameplayNotAllowed;
            return false;
        }
        if (_catalog == null || !_catalog.TryResolve(shopId, out ShopDefinition shop))
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }
        if (!IsBuyItem(shop, itemId) || !ItemResolver.TryResolve(itemId, out ItemSO item))
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }
        bool validQuote = item.MaxBuyPrice > 0
            ? unitPrice >= item.MinBuyPrice && unitPrice <= item.MaxBuyPrice
            : TryFindStock(shop, itemId, out ShopStockEntry stock) && unitPrice == stock.Price;
        if (!validQuote || quantity <= 0)
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }
        totalCost = unitPrice * quantity;
        if (InventoryManager.Instance == null || InventoryManager.Instance.Gold < totalCost)
        {
            result = ShopTransactionResult.InsufficientGold;
            return false;
        }
        if (!InventoryManager.Instance.HasCapacityFor(item, quantity))
        {
            result = ShopTransactionResult.InsufficientInventoryCapacity;
            return false;
        }
        InventoryManager.Instance.SpendGold(totalCost);
        InventoryManager.Instance.AddItem(item, quantity);
        result = ShopTransactionResult.Success;
        QuestDomainEvents.RaiseItemPurchased(itemId, quantity, shopId);
        return true;
    }

    /// <summary>Sells at a random per-item quote from ItemSO. A shop accepts its explicit Sell Items
    /// plus outputs of recipes offered by the same NPC. Legacy shops without Sell Items accept stock.</summary>
    public bool TrySell(string shopId, string itemId, int quantity, out ShopTransactionResult result)
    {
        if (!TryCreateSellQuote(shopId, itemId, quantity, out int unitPrice, out result))
            return false;
        return TrySellQuoted(shopId, itemId, quantity, unitPrice, out _, out result);
    }

    public bool TryCreateSellQuote(string shopId, string itemId, int quantity, out int unitPrice, out ShopTransactionResult result)
    {
        unitPrice = 0;
        if (!GameStateManager.AllowsGameplayInput)
        {
            result = ShopTransactionResult.GameplayNotAllowed;
            return false;
        }

        if (_catalog == null || !_catalog.TryResolve(shopId, out ShopDefinition shop))
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }

        if (!CanBuyFromPlayer(shop, itemId))
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }

        if (quantity <= 0)
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }

        if (!ItemResolver.TryResolve(itemId, out ItemSO item))
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }

        if (InventoryManager.Instance == null || !InventoryManager.Instance.HasItem(item, quantity))
        {
            result = ShopTransactionResult.InsufficientItemQuantity;
            return false;
        }

        if (item.MaxSellPrice > 0)
        {
            unitPrice = Random.Range(item.MinSellPrice, item.MaxSellPrice + 1);
        }
        else if (TryFindStock(shop, itemId, out ShopStockEntry stock))
        {
            unitPrice = Mathf.RoundToInt(stock.Price * shop.SellPriceMultiplier);
        }
        else
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }
        result = ShopTransactionResult.Success;
        return true;
    }

    public bool TrySellQuoted(string shopId, string itemId, int quantity, int unitPrice, out int totalValue, out ShopTransactionResult result)
    {
        totalValue = 0;
        if (!GameStateManager.AllowsGameplayInput)
        {
            result = ShopTransactionResult.GameplayNotAllowed;
            return false;
        }
        if (_catalog == null || !_catalog.TryResolve(shopId, out ShopDefinition shop))
        {
            result = ShopTransactionResult.ShopNotFound;
            return false;
        }
        if (!CanBuyFromPlayer(shop, itemId) || !ItemResolver.TryResolve(itemId, out ItemSO item))
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }
        bool validQuote = item.MaxSellPrice > 0
            ? unitPrice >= item.MinSellPrice && unitPrice <= item.MaxSellPrice
            : TryFindStock(shop, itemId, out ShopStockEntry stock)
              && unitPrice == Mathf.RoundToInt(stock.Price * shop.SellPriceMultiplier);
        if (!validQuote)
        {
            result = ShopTransactionResult.ItemNotInStock;
            return false;
        }
        if (quantity <= 0 || InventoryManager.Instance == null || !InventoryManager.Instance.HasItem(item, quantity))
        {
            result = ShopTransactionResult.InsufficientItemQuantity;
            return false;
        }

        totalValue = unitPrice * quantity;
        InventoryManager.Instance.RemoveItem(item, quantity);
        InventoryManager.Instance.AddGold(totalValue);
        result = ShopTransactionResult.Success;
        return true;
    }

    public bool CanBuyFromPlayer(string shopId, string itemId)
        => _catalog != null && _catalog.TryResolve(shopId, out ShopDefinition shop) && CanBuyFromPlayer(shop, itemId);

    private bool CanBuyFromPlayer(ShopDefinition shop, string itemId)
    {
        foreach (ItemSO item in shop.SellItems)
            if (item != null && item.itemId == itemId)
                return true;
        if (!shop.UsesExplicitSellItems && TryFindStock(shop, itemId, out _)) return true;
        IRecipeResolver recipes = _recipeResolver ?? CraftingManager.Instance?.Catalog;
        if (recipes == null) return false;
        foreach (RecipeDefinition recipe in recipes.AllRecipes)
            if (recipe != null && recipe.NpcId == shop.NpcId && recipe.OutputItemId == itemId)
                return true;
        return false;
    }

    private static bool IsBuyItem(ShopDefinition shop, string itemId)
    {
        if (shop.UsesExplicitBuyItems)
        {
            foreach (ItemSO item in shop.BuyItems)
                if (item != null && item.itemId == itemId)
                    return true;
            return false;
        }
        return TryFindStock(shop, itemId, out _);
    }

    private static bool TryFindStock(ShopDefinition shop, string itemId, out ShopStockEntry stock)
    {
        foreach (ShopStockEntry entry in shop.Stock)
        {
            if (entry != null && entry.ItemId == itemId)
            {
                stock = entry;
                return true;
            }
        }
        stock = null;
        return false;
    }
}
