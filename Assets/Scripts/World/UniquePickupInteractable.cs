using UnityEngine;

/// <summary>Minimal persistent unique pickup: a one-of-a-kind world item, distinct from ordinary
/// stackable loot -- collecting it grants the item exactly once and the pickup disappears for
/// good, including across save/load.</summary>
public sealed class UniquePickupInteractable : MonoBehaviour, IPersistentWorldObject
{
    [SerializeField] private string _persistentId;
    [SerializeField] private string _itemId;
    [SerializeField, Min(1)] private int _quantity = 1;

    private bool _collected;
    private IItemResolver _itemResolver;

    public string PersistentId => _persistentId;
    public WorldObjectKind Kind => WorldObjectKind.UniquePickup;
    public bool IsCollected => _collected;

    internal void ConfigureForTests(string persistentId, string itemId, int quantity, IItemResolver itemResolver)
    {
        _persistentId = persistentId;
        _itemId = itemId;
        _quantity = quantity;
        _itemResolver = itemResolver;
    }

    private IItemResolver ItemResolver => _itemResolver ??= new ResourcesItemResolver();

    /// <summary>Collects the pickup exactly once. Returns false (nothing consumed, pickup stays
    /// visible) if already collected or if inventory has no room -- retryable.</summary>
    public bool TryCollect(out bool granted)
    {
        granted = false;
        if (_collected)
            return false;

        if (!ItemResolver.TryResolve(_itemId, out ItemSO item)
            || InventoryManager.Instance == null
            || !InventoryManager.Instance.HasCapacityFor(item, _quantity))
        {
            return false;
        }

        InventoryManager.Instance.AddItem(item, _quantity);
        _collected = true;
        ApplyVisual();
        granted = true;
        return true;
    }

    public WorldObjectState CaptureState() => new(_collected, 0);

    public void RestoreState(WorldObjectState state)
    {
        _collected = state.Flag;
        ApplyVisual();
    }

    // Hides the pickup once collected instead of destroying the GameObject -- WorldObjectRegistry
    // holds a direct serialized reference to this component, which must stay valid for future
    // capture/restore calls across the object's lifetime in this scene.
    private void ApplyVisual() => gameObject.SetActive(!_collected);
}
