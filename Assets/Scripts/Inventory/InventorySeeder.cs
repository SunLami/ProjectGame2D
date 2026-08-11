using UnityEngine;

// Grants the starting inventory items (from the real ItemDatabase) to the player at startup.
public class InventorySeeder : MonoBehaviour
{
    [SerializeField] private ItemDatabase _database;

    private void Start()
    {
        if (InventoryManager.Instance == null || _database == null || _database.items == null) return;

        foreach (ItemDatabase.Entry entry in _database.items)
        {
            if (entry.item == null) continue;
            InventoryManager.Instance.AddItem(entry.item, entry.amount);
        }
    }
}
