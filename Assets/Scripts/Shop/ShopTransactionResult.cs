// Result of ShopManager.TryPurchase/TrySell -- always returned even on failure so callers (NPC
// interaction/UI) can show a specific reason instead of a generic false.
public enum ShopTransactionResult
{
    Success,
    ShopNotFound,
    ItemNotInStock,
    InsufficientGold,
    InsufficientInventoryCapacity,
    InsufficientItemQuantity,
    GameplayNotAllowed
}
