using UnityEngine;

// Grants the starting inventory items (from the real ItemDatabase) for a brand-new character.
// Must only be called for a New Game session -- Continue restores from the save instead, and
// calling this unconditionally on scene load would duplicate starter items on every reload.
public class InventorySeeder : MonoBehaviour
{
    [SerializeField] private ItemDatabase _database;

    public void SeedStartingInventory()
    {
        if (InventoryManager.Instance == null || _database == null || _database.items == null) return;

        foreach (ItemDatabase.Entry entry in _database.items)
        {
            if (entry.item == null) continue;
            InventoryManager.Instance.AddItem(entry.item, entry.amount);
        }
    }
}
