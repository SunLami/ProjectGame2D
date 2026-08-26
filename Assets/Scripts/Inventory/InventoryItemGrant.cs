public readonly struct InventoryItemGrant
{
    public InventoryItemGrant(ItemSO item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public ItemSO Item { get; }
    public int Quantity { get; }
}
