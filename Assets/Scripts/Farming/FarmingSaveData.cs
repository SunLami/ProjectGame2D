using System;
using System.Collections.Generic;

[Serializable]
public sealed class FarmingSaveData
{
    public List<FarmPlotSaveData> plots = new();
}

[Serializable]
public sealed class FarmPlotSaveData
{
    public string plotId;
    public string cropId;
    public long plantedAtUtcTicks;
}
