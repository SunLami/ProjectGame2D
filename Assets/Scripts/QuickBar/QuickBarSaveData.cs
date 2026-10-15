using System;
using System.Collections.Generic;

[Serializable]
public sealed class QuickBarSaveData
{
    public const int SlotCount = 4;

    public int selectedIndex;
    public List<string> assignedItemIds = CreateEmptyAssignments();

    public static List<string> CreateEmptyAssignments()
    {
        var result = new List<string>(SlotCount);
        for (int i = 0; i < SlotCount; i++) result.Add(null);
        return result;
    }
}
