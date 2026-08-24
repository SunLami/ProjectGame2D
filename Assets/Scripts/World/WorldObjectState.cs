/// <summary>
/// Runtime-side mirror of one persistent world object's state -- the same small payload shape as
/// WorldObjectSaveData, kept separate so runtime code never depends on the save DTO type directly.
/// Flag means opened/collected/defeated depending on WorldObjectKind; NextRespawnUtcTicks is only
/// meaningful for ResourceNode (0 = available now / not scheduled).
/// </summary>
public readonly struct WorldObjectState
{
    public WorldObjectState(bool flag, long nextRespawnUtcTicks)
    {
        Flag = flag;
        NextRespawnUtcTicks = nextRespawnUtcTicks;
    }

    public bool Flag { get; }
    public long NextRespawnUtcTicks { get; }
}
