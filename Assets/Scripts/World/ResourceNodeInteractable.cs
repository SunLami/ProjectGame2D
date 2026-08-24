using System;
using UnityEngine;

/// <summary>
/// Minimal persistent resource node: harvestable for an item, then on cooldown until
/// NextRespawnUtcTicks. Availability is computed on demand from the stored timestamp (D-015) --
/// there is no per-frame Update/poll ticking it back to available; the next TryHarvest call (or a
/// future presentation script) is what notices the cooldown has elapsed.
/// </summary>
public sealed class ResourceNodeInteractable : MonoBehaviour, IPersistentWorldObject
{
    [SerializeField] private string _persistentId;

    [Tooltip("Stable resourceId for Quest Gather objectives, e.g. 'resource.wood.log'.")]
    [SerializeField] private string _resourceId;

    [Tooltip("Optional areaId this node counts as being in for Gather objectives that require a specific area.")]
    [SerializeField] private string _areaId;

    [SerializeField] private string _itemId;
    [SerializeField, Min(1)] private int _quantity = 1;
    [SerializeField, Min(0f)] private float _respawnSeconds = 60f;

    [Tooltip("Optional -- toggled to reflect the available/depleted visual. Safe to leave unassigned.")]
    [SerializeField] private GameObject _depletedIndicator;

    private long _nextRespawnUtcTicks;
    private IItemResolver _itemResolver;

    public string PersistentId => _persistentId;
    public WorldObjectKind Kind => WorldObjectKind.ResourceNode;
    public bool IsAvailable => _nextRespawnUtcTicks == 0 || DateTime.UtcNow.Ticks >= _nextRespawnUtcTicks;

    internal void ConfigureForTests(
        string persistentId, string resourceId, string itemId, int quantity, float respawnSeconds, IItemResolver itemResolver)
    {
        _persistentId = persistentId;
        _resourceId = resourceId;
        _itemId = itemId;
        _quantity = quantity;
        _respawnSeconds = respawnSeconds;
        _itemResolver = itemResolver;
    }

    private IItemResolver ItemResolver => _itemResolver ??= new ResourcesItemResolver();

    /// <summary>Harvests the node if available. Grants the item, raises ResourceGathered (Quest
    /// Gather objective producer) and starts the respawn cooldown. Returns false (nothing granted,
    /// no cooldown change) if on cooldown or inventory has no room.</summary>
    public bool TryHarvest(out bool granted)
    {
        granted = false;
        if (!IsAvailable)
            return false;

        if (!ItemResolver.TryResolve(_itemId, out ItemSO item)
            || InventoryManager.Instance == null
            || !InventoryManager.Instance.HasCapacityFor(item, _quantity))
        {
            return false;
        }

        InventoryManager.Instance.AddItem(item, _quantity);
        QuestDomainEvents.RaiseResourceGathered(_resourceId, _quantity, _areaId);
        _nextRespawnUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(_respawnSeconds).Ticks;
        ApplyVisual();
        granted = true;
        WorldDomainEvents.RaiseWorldObjectChanged();
        return true;
    }

    public WorldObjectState CaptureState() => new(false, _nextRespawnUtcTicks);

    public void RestoreState(WorldObjectState state)
    {
        _nextRespawnUtcTicks = state.NextRespawnUtcTicks;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (_depletedIndicator != null)
            _depletedIndicator.SetActive(!IsAvailable);
    }
}
