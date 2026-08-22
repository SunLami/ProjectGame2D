using System;

// Plain serializable per-object world state. Only objects with a registered persistentId are
// saved; unknown/removed content is skipped on restore rather than failing the whole save
// (SaveAndWorldPersistence.md "record khong resolve duoc bi bo qua kem warning").
[Serializable]
public sealed class WorldObjectSaveData
{
    public string persistentId;
    public WorldObjectKind kind;
    public bool flag;
    public long nextRespawnUtcTicks;
}
