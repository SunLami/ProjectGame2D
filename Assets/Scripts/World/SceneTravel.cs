using UnityEngine;

/// <summary>What the Player (a per-scene object, re-created on every scene load) carries across a map
/// switch. Everything else the player owns (inventory, equipment, quests...) lives in Bootstrap managers
/// and survives on its own.</summary>
public readonly struct PlayerTravelSnapshot
{
    public PlayerTravelSnapshot(int level, int experience, float health, float stamina)
    {
        Level = level;
        Experience = experience;
        Health = health;
        Stamina = stamina;
    }

    public int Level { get; }
    public int Experience { get; }
    public float Health { get; }
    public float Stamina { get; }
}

/// <summary>
/// In-memory map-to-map travel (D-089). <see cref="TryTravel"/> captures the player's progression, asks
/// <see cref="SceneFlowService"/> to load the destination scene and remembers where the player should
/// appear; <see cref="SceneTravelArrival"/> in the destination applies it. Nothing is written to disk.
///
/// Scope guard: only sessions WITHOUT a save payload (Development) may travel for now. A session that
/// carries a GameSaveData would have PlayerSpawnReadinessSource re-apply the snapshot on arrival (resetting
/// unsaved progress, farm plots and position), so those sessions are refused with a message until the
/// save/travel rules are decided.
/// </summary>
public static class SceneTravel
{
    public const string RefusedSaveSessionMessage = "Travel is unavailable in a saved game yet.";

    private static bool _pending;
    private static string _pendingSpawnId;
    private static PlayerTravelSnapshot _pendingSnapshot;

    public static bool HasPending => _pending;
    public static string PendingSpawnId => _pendingSpawnId;
    public static PlayerTravelSnapshot PendingSnapshot => _pendingSnapshot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => Clear();

    /// <summary>Returns null when travel can start, otherwise a short reason for the player.</summary>
    public static string CheckCanTravel()
    {
        if (SceneFlowService.Instance == null || SceneFlowService.Instance.IsTransitioning)
            return "Travel is not available right now.";

        GameSessionManager sessions = GameSessionManager.Instance;
        if (sessions == null || !sessions.HasActiveSession)
            return "No active game session.";

        if (sessions.Current.SaveData != null)
            return RefusedSaveSessionMessage;

        if (PlayerStat.Instance == null || PlayerStat.Instance.IsDead)
            return "You cannot travel while defeated.";

        if (GameStateManager.Instance == null)
            return "Travel is not available right now.";

        return null;
    }

    /// <summary>Starts a travel to `sceneName`, placing the player at spawn `spawnId` on arrival.</summary>
    public static bool TryTravel(string sceneName, string spawnId, out string failureReason)
    {
        failureReason = CheckCanTravel();
        if (failureReason != null)
            return false;

        PlayerStat stat = PlayerStat.Instance;
        if (stat != null)
        {
            _pendingSnapshot = new PlayerTravelSnapshot(stat.Level, stat.CurrentExperience, stat.Health, stat.Stamina);
            _pendingSpawnId = spawnId;
            _pending = true;
        }

        if (!SceneFlowService.Instance.TryLoadGameplay(sceneName))
        {
            Clear();
            failureReason = "The destination could not be loaded.";
            return false;
        }

        SoundFXManager.PlaySfx(SfxIds.WorldGateExit);
        return true;
    }

    /// <summary>Takes the pending handoff (destination scene calls this once).</summary>
    public static bool TryConsume(out string spawnId, out PlayerTravelSnapshot snapshot)
    {
        spawnId = _pendingSpawnId;
        snapshot = _pendingSnapshot;
        bool had = _pending;
        Clear();
        return had;
    }

    private static void Clear()
    {
        _pending = false;
        _pendingSpawnId = null;
        _pendingSnapshot = default;
    }
}
