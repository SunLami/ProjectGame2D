// Kinds of persistent world entity the save system tracks by a small state payload
// (SaveAndWorldPersistence.md "World DTO nen la danh sach record theo ID va payload nho").
// Ordinary enemies are deliberately NOT a kind here -- they respawn by always being present in
// the scene and are never given a persistentId (Roadmap Phase 8 acceptance: no per-instance save
// record for regular enemies).
public enum WorldObjectKind
{
    Chest,
    UniquePickup,
    Boss,
    ResourceNode
}
