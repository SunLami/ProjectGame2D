using UnityEngine;

// Test-only helper to populate the inventory with sample items at startup so the UI can be tested.
// Not part of the portable prefabs — attach only in test scenes.
public class InventoryTestSeeder : MonoBehaviour
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
