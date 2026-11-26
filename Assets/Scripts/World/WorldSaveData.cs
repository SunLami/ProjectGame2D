using System;
using System.Collections.Generic;

[Serializable]
public sealed class WorldSaveData
{
    public List<WorldObjectSaveData> objects = new();

    public static WorldSaveData Merge(WorldSaveData previous, WorldSaveData current)
    {
        var records = new Dictionary<string, WorldObjectSaveData>(StringComparer.Ordinal);
        AddRecords(previous);
        AddRecords(current);
        return new WorldSaveData { objects = new List<WorldObjectSaveData>(records.Values) };

        void AddRecords(WorldSaveData data)
        {
            if (data?.objects == null)
                return;
            foreach (WorldObjectSaveData record in data.objects)
                if (!string.IsNullOrEmpty(record?.persistentId))
                    records[record.persistentId] = record;
        }
    }
}
