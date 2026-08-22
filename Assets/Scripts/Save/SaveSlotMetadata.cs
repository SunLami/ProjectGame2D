using System;

/// <summary>
/// Fast-read summary of a slot so MainMenu can render it without deserializing GameSaveData.
/// characterName/characterLevel/areaId/tutorialCompleted stay at their defaults until Phase 3/4
/// populate them from real player/tutorial state; contentChecksum guards save.json integrity.
/// </summary>
[Serializable]
public sealed class SaveSlotMetadata
{
    public int slotIndex;
    public string saveId;
    public int saveVersion;
    public string characterName;
    public int characterLevel;
    public long totalPlayTimeSeconds;
    public string areaId;
    public long lastSavedUtcTicks;
    public bool tutorialCompleted;
    public string contentChecksum;
}
