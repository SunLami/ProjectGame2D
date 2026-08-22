using System.Collections.Generic;

/// <summary>
/// In-memory ISaveSlotRepository for MainMenu/UI development and tests that need slot behavior
/// without touching disk or the player's real save files.
/// </summary>
public sealed class InMemorySaveSlotRepository : ISaveSlotRepository
{
    private readonly Dictionary<int, GameSaveData> _slots = new();

    public SaveSlotInfo GetSlotInfo(int slotId)
    {
        ValidateSlotId(slotId);

        if (!_slots.TryGetValue(slotId, out GameSaveData data))
            return new SaveSlotInfo(slotId, SaveSlotStatus.Empty, null);

        SaveSlotMetadata metadata = new()
        {
            slotIndex = slotId,
            saveId = data.saveId,
            saveVersion = data.saveVersion,
            totalPlayTimeSeconds = data.totalPlayTimeSeconds
        };
        return new SaveSlotInfo(slotId, SaveSlotStatus.Valid, metadata);
    }

    public SaveSlotInfo[] GetAllSlotInfo()
    {
        int count = GameSessionManager.MaximumSlotId - GameSessionManager.MinimumSlotId + 1;
        SaveSlotInfo[] result = new SaveSlotInfo[count];
        for (int i = 0; i < count; i++)
            result[i] = GetSlotInfo(GameSessionManager.MinimumSlotId + i);

        return result;
    }

    public bool TryReadSave(int slotId, out GameSaveData data)
    {
        ValidateSlotId(slotId);
        return _slots.TryGetValue(slotId, out data);
    }

    public SaveOperationResult WriteSave(int slotId, GameSaveData data)
    {
        ValidateSlotId(slotId);

        if (data == null)
            return SaveOperationResult.Failure("GameSaveData is null.");
        if (string.IsNullOrWhiteSpace(data.saveId))
            return SaveOperationResult.Failure("GameSaveData.saveId is empty.");

        _slots[slotId] = data;
        return SaveOperationResult.Ok();
    }

    public SaveOperationResult DeleteSlot(int slotId)
    {
        ValidateSlotId(slotId);
        _slots.Remove(slotId);
        return SaveOperationResult.Ok();
    }

    private static void ValidateSlotId(int slotId)
    {
        if (slotId < GameSessionManager.MinimumSlotId || slotId > GameSessionManager.MaximumSlotId)
        {
            throw new System.ArgumentOutOfRangeException(nameof(slotId),
                $"Slot id must be between {GameSessionManager.MinimumSlotId} and {GameSessionManager.MaximumSlotId}.");
        }
    }
}
