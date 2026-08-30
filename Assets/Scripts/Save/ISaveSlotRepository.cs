/// <summary>
/// Owns save slot file/metadata CRUD for slots 1..MaximumSlotId. Domain capture/restore
/// (player, inventory, quest, world) is orchestrated by later phases on top of GameSaveData;
/// this contract only knows how to persist and validate whatever GameSaveData it's given.
/// </summary>
public interface ISaveSlotRepository
{
    SaveSlotInfo GetSlotInfo(int slotId);
    SaveSlotInfo[] GetAllSlotInfo();
    bool TryReadSave(int slotId, out GameSaveData data);
    SaveOperationResult WriteSave(int slotId, GameSaveData data);
    SaveOperationResult DeleteSlot(int slotId);
}
