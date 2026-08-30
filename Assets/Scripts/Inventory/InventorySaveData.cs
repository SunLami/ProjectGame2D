using System;
using System.Collections.Generic;

// Plain serializable snapshot of an inventory's contents, independent of any ScriptableObject
// asset. Meant to be written to/read from a per-player save file (JSON, etc.) later — items are
// referenced by ItemSO.itemId so the save file stays small and resolves back through ItemLookup.
[Serializable]
public class InventorySaveData
{
    [Serializable]
    public class SlotData
    {
        public string itemId;
        public int quantity;
    }

    public List<SlotData> slots = new List<SlotData>();
    public int gold;
}
