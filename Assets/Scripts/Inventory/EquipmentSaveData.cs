using System;
using System.Collections.Generic;

// Plain serializable snapshot of equipped items, independent of any ScriptableObject asset.
// Only occupied slots are recorded; an absent slot means nothing is equipped there.
[Serializable]
public class EquipmentSaveData
{
    [Serializable]
    public class SlotData
    {
        public EquipSlot slot;
        public string itemId;
    }

    public List<SlotData> slots = new List<SlotData>();
}
