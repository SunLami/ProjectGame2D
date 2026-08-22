using System;

/// <summary>
/// Root save DTO. Inventory/equipment/tutorial/quest/world domains are added by their owning
/// phase as additive fields, bumping CurrentSaveVersion when the shape changes so
/// FileSaveSlotRepository can tell compatible saves from incompatible ones.
/// </summary>
[Serializable]
public sealed class GameSaveData
{
    public const int CurrentSaveVersion = 2;

    public int saveVersion = CurrentSaveVersion;
    public string saveId;
    public long totalPlayTimeSeconds;
    public PlayerSaveData player;
}
