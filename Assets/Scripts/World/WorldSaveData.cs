using System;
using System.Collections.Generic;

[Serializable]
public sealed class WorldSaveData
{
    public List<WorldObjectSaveData> objects = new();
}
