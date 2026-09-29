using UnityEngine;

// Grants the starting inventory items (from the real ItemDatabase) for a brand-new character.
// Must only be called for a New Game session -- Continue restores from the save instead, and
// calling this unconditionally on scene load would duplicate starter items on every reload.
public class InventorySeeder : MonoBehaviour
{
    [SerializeField] private ItemDatabase _database;
    [Tooltip("Turn off for real play so New Game starts with an empty inventory. Turn back on to seed the full ItemDatabase for testing.")]
    [SerializeField] private bool _seedingEnabled = true;

    // Also toggleable at runtime from the Dev Panel (DevPanelController) so QA doesn't need the
    // Unity Editor to switch between real-play and full-seed New Game sessions.
    public bool SeedingEnabled
    {
        get => _seedingEnabled;
        set => _seedingEnabled = value;
    }

    public void SeedStartingInventory()
    {
        if (!_seedingEnabled) return;
        if (InventoryManager.Instance == null || _database == null || _database.items == null) return;

        foreach (ItemDatabase.Entry entry in _database.items)
        {
            if (entry.item == null) continue;
            InventoryManager.Instance.AddItem(entry.item, entry.amount);
        }
    }
}
