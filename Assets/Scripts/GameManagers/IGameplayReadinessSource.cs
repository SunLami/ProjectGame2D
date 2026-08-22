using System;

/// <summary>
/// Extension point for GameplayReadinessGate. Phase 2/3 restore steps (save, world,
/// inventory, quest, scene-bound registration) implement this to gate the
/// Loading -> Playing transition without changing GameplayReadinessGate itself.
/// </summary>
public interface IGameplayReadinessSource
{
    string SourceId { get; }
    bool IsReady { get; }
    event Action ReadyChanged;
}
