using System;

/// <summary>
/// Fires once per real state change on any persistent world object (chest opened, pickup
/// collected, resource harvested, boss defeated) -- never from WorldObjectRegistry.RestoreState.
/// Session dirty-tracking (Phase 9) subscribes to this instead of polling/serializing the whole
/// world snapshot every frame to detect change.
/// </summary>
public static class WorldDomainEvents
{
    public static event Action WorldObjectChanged;

    public static void RaiseWorldObjectChanged() => WorldObjectChanged?.Invoke();
}
