using System;
using UnityEngine;

[Serializable]
public sealed class ResourceLootEntry
{
    [SerializeField] private ItemSO _item;
    [SerializeField, Range(0f, 1f)] private float _chance = 1f;
    [SerializeField, Min(1)] private int _minimumQuantity = 1;
    [SerializeField, Min(1)] private int _maximumQuantity = 1;

    public ItemSO Item => _item;
    public float Chance => _chance;
    public int MinimumQuantity => _minimumQuantity;
    public int MaximumQuantity => _maximumQuantity;

    public bool TryRoll(out InventoryItemGrant grant)
    {
        grant = default;
        if (_item == null || _chance <= 0f || UnityEngine.Random.value > _chance)
            return false;

        int minimum = Mathf.Max(1, _minimumQuantity);
        int maximum = Mathf.Max(minimum, _maximumQuantity);
        grant = new InventoryItemGrant(_item, UnityEngine.Random.Range(minimum, maximum + 1));
        return true;
    }

    internal void ConfigureForTests(ItemSO item, float chance, int minimumQuantity, int maximumQuantity)
    {
        _item = item;
        _chance = Mathf.Clamp01(chance);
        _minimumQuantity = Mathf.Max(1, minimumQuantity);
        _maximumQuantity = Mathf.Max(_minimumQuantity, maximumQuantity);
    }
}
