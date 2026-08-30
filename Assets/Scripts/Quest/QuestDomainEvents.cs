using System;

/// <summary>
/// Typed gameplay events that bridge other domains to Quest objective tracking
/// (DataDrivenDevelopment.md "Domain events la cau noi"). QuestManager only subscribes; it never
/// polls world state. Obtain (InventoryManager.AddItem) and Kill (EnemyUniversal death) are wired
/// to real production call sites in this phase. Talk/Craft/Purchase/Gather have no dialogue/
/// crafting/shop/resource system yet (Phase 6 Boundary + Phase 7 Roadmap) -- these Raise* methods
/// are the public contract a future system must call; until then tests call them directly as a
/// fake producer, and the gap is recorded in ClaudeToCodex.md.
/// </summary>
public static class QuestDomainEvents
{
    public static event Action<string, string> NpcConversationCompleted;
    public static event Action<string, int> InventoryItemAdded;
    public static event Action<string, int, string> ItemCrafted;
    public static event Action<string, int, string> ItemPurchased;
    public static event Action<string, int, string> ResourceGathered;
    public static event Action<string, string> EnemyKilled;

    public static void RaiseNpcConversationCompleted(string npcId, string outcomeId) =>
        NpcConversationCompleted?.Invoke(npcId, outcomeId);

    public static void RaiseInventoryItemAdded(string itemId, int quantity) =>
        InventoryItemAdded?.Invoke(itemId, quantity);

    public static void RaiseItemCrafted(string itemId, int quantity, string stationId = null) =>
        ItemCrafted?.Invoke(itemId, quantity, stationId);

    public static void RaiseItemPurchased(string itemId, int quantity, string shopId = null) =>
        ItemPurchased?.Invoke(itemId, quantity, shopId);

    public static void RaiseResourceGathered(string resourceId, int quantity, string areaId = null) =>
        ResourceGathered?.Invoke(resourceId, quantity, areaId);

    public static void RaiseEnemyKilled(string enemyId, string areaId = null) =>
        EnemyKilled?.Invoke(enemyId, areaId);
}
