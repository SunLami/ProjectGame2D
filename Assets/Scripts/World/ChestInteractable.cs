using UnityEngine;

/// <summary>
/// Minimal persistent chest: grants one reward item stack exactly once. Reward is authored inline
/// (itemId + quantity) rather than via a shared Definition asset -- Phase 8's scope is the
/// persistence mechanism (opened survives save/load), not a general loot-table system.
/// </summary>
public sealed class ChestInteractable : MonoBehaviour, IPersistentWorldObject
{
    [SerializeField] private string _persistentId;
    [SerializeField] private string _rewardItemId;
    [SerializeField, Min(1)] private int _rewardQuantity = 1;

    [Tooltip("Optional -- toggled to reflect the opened/closed visual. Safe to leave unassigned.")]
    [SerializeField] private GameObject _openedIndicator;

    private bool _opened;
    private IItemResolver _itemResolver;

    public string PersistentId => _persistentId;
    public WorldObjectKind Kind => WorldObjectKind.Chest;
    public bool IsOpened => _opened;

    internal void ConfigureForTests(string persistentId, string rewardItemId, int rewardQuantity, IItemResolver itemResolver)
    {
        _persistentId = persistentId;
        _rewardItemId = rewardItemId;
        _rewardQuantity = rewardQuantity;
        _itemResolver = itemResolver;
    }

    private IItemResolver ItemResolver => _itemResolver ??= new ResourcesItemResolver();

    /// <summary>Opens the chest and grants the reward exactly once. Returns false (chest stays
    /// closed, nothing consumed) if already opened or if inventory has no room -- retryable.</summary>
    public bool TryOpen(out bool granted)
    {
        granted = false;
        if (_opened)
            return false;

        if (!ItemResolver.TryResolve(_rewardItemId, out ItemSO item)
            || InventoryManager.Instance == null
            || !InventoryManager.Instance.HasCapacityFor(item, _rewardQuantity))
        {
            return false;
        }

        InventoryManager.Instance.AddItem(item, _rewardQuantity);
        _opened = true;
        ApplyVisual();
        granted = true;
        WorldDomainEvents.RaiseWorldObjectChanged();
        return true;
    }

    public WorldObjectState CaptureState() => new(_opened, 0);

    public void RestoreState(WorldObjectState state)
    {
        _opened = state.Flag;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (_openedIndicator != null)
            _openedIndicator.SetActive(_opened);
    }
}
