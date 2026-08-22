/// <summary>
/// Contract every persistent world entity (chest, unique pickup, boss, resource node) implements.
/// PersistentId is the save/instance identity -- stable, authored, never derived from GameObject
/// name/hierarchy/Unity instance ID (DataDrivenDevelopment.md persistent instance ID rules).
/// RestoreState must be idempotent and must never grant rewards or fire gameplay/progression
/// events -- it only reproduces prior state.
/// </summary>
public interface IPersistentWorldObject
{
    string PersistentId { get; }
    WorldObjectKind Kind { get; }
    WorldObjectState CaptureState();
    void RestoreState(WorldObjectState state);
}
