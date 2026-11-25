using System;

public static class FarmingDomainEvents
{
    public static event Action FarmStateChanged;
    public static event Action<string, string> CropPlanted;
    public static event Action<string, string, int> CropHarvested;

    public static void RaiseCropPlanted(string plotId, string cropId)
    {
        CropPlanted?.Invoke(plotId, cropId);
        FarmStateChanged?.Invoke();
    }

    /// <summary>Farm/storage state changed without a player plant/harvest (e.g. quest supply).
    /// Only marks the session dirty; never counts toward quest objectives.</summary>
    public static void RaiseFarmStateChanged() => FarmStateChanged?.Invoke();

    public static void RaiseCropHarvested(string plotId, string cropId, int quantity)
    {
        CropHarvested?.Invoke(plotId, cropId, quantity);
        FarmStateChanged?.Invoke();
    }
}
