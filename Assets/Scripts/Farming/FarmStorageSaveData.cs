using System;
using System.Collections.Generic;

[Serializable]
public sealed class FarmStorageSaveData
{
    [Serializable]
    public sealed class SlotData
    {
        public string itemId;
        public int quantity;
    }

    public List<SlotData> slots = new();
}
