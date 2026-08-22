/// <summary>Read-only slot summary returned by ISaveSlotRepository for MainMenu display.</summary>
public readonly struct SaveSlotInfo
{
    public SaveSlotInfo(int slotId, SaveSlotStatus status, SaveSlotMetadata metadata)
    {
        SlotId = slotId;
        Status = status;
        Metadata = metadata;
    }

    public int SlotId { get; }
    public SaveSlotStatus Status { get; }

    /// <summary>Null unless Status == Valid.</summary>
    public SaveSlotMetadata Metadata { get; }
}
