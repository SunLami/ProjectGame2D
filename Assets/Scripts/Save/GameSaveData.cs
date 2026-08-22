using System;

/// <summary>
/// Root save DTO. Phase 2 only carries the fields the file/slot foundation needs to prove
/// atomic write and versioning. Player/inventory/equipment/tutorial/quest/world domains are
/// added by their owning phase as additive fields, bumping CurrentSaveVersion when the shape
/// changes so FileSaveSlotRepository can tell compatible saves from incompatible ones.
/// </summary>
[Serializable]
public sealed class GameSaveData
{
    public const int CurrentSaveVersion = 1;

    public int saveVersion = CurrentSaveVersion;
    public string saveId;
    public long totalPlayTimeSeconds;
}
