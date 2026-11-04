using System;
using System.Collections.Generic;
using UnityEngine;

// Tests may run with Bootstrap's persistent inventory already alive. Preserve it and give
// each fixture empty slots/zero gold instead of creating a duplicate singleton.
internal sealed class InventoryTestScope : IDisposable
{
    private readonly InventoryManager _inventory;
    private readonly GameObject _ownedRoot;
    private readonly InventorySaveData _saved;
    private readonly Dictionary<string, ItemSO> _items = new();

    public InventoryTestScope()
    {
        _inventory = InventoryManager.Instance;
        if (_inventory == null)
        {
            _ownedRoot = new GameObject("InventoryManagerFixture");
            _inventory = _ownedRoot.AddComponent<InventoryManager>();
        }
        else
        {
            _saved = _inventory.ToSaveData();
            foreach (var slot in _inventory.Slots)
                if (slot.item != null) _items[slot.item.itemId] = slot.item;
        }
        _inventory.LoadFromSaveData(new InventorySaveData(), new Dictionary<string, ItemSO>());
    }

    public void Dispose()
    {
        if (_ownedRoot != null) UnityEngine.Object.DestroyImmediate(_ownedRoot);
        else if (_inventory != null && _saved != null) _inventory.LoadFromSaveData(_saved, _items);
    }
}
