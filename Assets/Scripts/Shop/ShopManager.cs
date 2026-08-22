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

    public IShopResolver Catalog => _catalog;

    private IItemResolver ItemResolver => _itemResolver ??= new ResourcesItemResolver();

    internal void ConfigureForTests(ShopCatalog catalog, IItemResolver itemResolver = null)
    {
        _catalog = catalog;
        _itemResolver = itemResolver;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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

        if (!TryFindStock(shop, itemId, out ShopStockEntry stock))
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

        int totalCost = stock.Price * quantity;
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

    /// <summary>Sells quantity of itemId back to shopId at stock price * SellPriceMultiplier. Only
    /// items that are also in this shop's own stock can be sold here -- a general "sell anything"
    /// vendor needs its own base-value field on ItemSO, out of scope for this phase (see
    /// Phase7ImplementationReport.md known limitations).</summary>
    public bool TrySell(string shopId, string itemId, int quantity, out ShopTransactionResult result)
    {
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

        if (!TryFindStock(shop, itemId, out ShopStockEntry stock))
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

        int totalValue = Mathf.RoundToInt(stock.Price * shop.SellPriceMultiplier) * quantity;
        InventoryManager.Instance.RemoveItem(item, quantity);
        InventoryManager.Instance.AddGold(totalValue);
        result = ShopTransactionResult.Success;
        return true;
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
