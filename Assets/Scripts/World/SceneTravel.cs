using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>What the Player (a per-scene object, re-created on every scene load) carries across a map
/// switch. Everything else the player owns (inventory, equipment, quests...) lives in Bootstrap managers
/// and survives on its own.</summary>
public readonly struct PlayerTravelSnapshot
{
    public PlayerTravelSnapshot(int level, int experience, float health, float stamina, SkillProgressionSaveData skills = null)
    {
        Level = level;
        Experience = experience;
        Skills = skills;
        Health = health;
        Stamina = stamina;
    }

    public int Level { get; }
    public SkillProgressionSaveData Skills { get; }
    public int Experience { get; }
    public float Health { get; }
    public float Stamina { get; }
}

/// <summary>
/// In-memory map-to-map travel (D-089). <see cref="TryTravel"/> captures the player's progression, asks
/// <see cref="SceneFlowService"/> to load the destination scene and remembers where the player should
/// appear; <see cref="SceneTravelArrival"/> in the destination applies it. Nothing is written to disk.
///
/// World gates and the MapDuy boss routes support save-backed sessions using live managers
/// and capturing scene-owned state in memory.
/// </summary>
public static class SceneTravel
{
    public const string RefusedSaveSessionMessage = "Travel is unavailable in a saved game yet.";

    private static bool _pending;
    public static bool IsWorldGateArrival { get; private set; }

    public static string ResolveSavedScene(GameSaveData save, string fallback)
    {
        string scene = save?.player?.location?.sceneId;
        return (scene == "MapDuy" || IsBossArena(scene)) && Application.CanStreamedLevelBeLoaded(scene)
            ? scene : fallback;
    }

    internal const string BossReturnSpawn = "spawn.mapduy.boss_pillar_return";
    internal static bool IsBossArena(string scene) =>
        scene is "BossArena_Earth" or "BossArena_Water" or "BossArena_Wind";

    internal static bool IsSavedBossRoute(string source, string destination, string spawnId) =>
        (source == "MapDuy" && IsBossArena(destination) && spawnId == "arena_entry")
        || (IsBossArena(source) && destination == "MapDuy" && spawnId == BossReturnSpawn);

    private static string _pendingSpawnId;
    private static PlayerTravelSnapshot _pendingSnapshot;

    public static bool HasPending => _pending;
    public static string PendingSpawnId => _pendingSpawnId;
    public static PlayerTravelSnapshot PendingSnapshot => _pendingSnapshot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => Clear();

    /// <summary>Returns null when travel can start, otherwise a short reason for the player.</summary>
    public static string CheckCanTravel() => CheckCanTravel(false);

    private static string CheckCanTravel(bool worldGate, bool respawning = false)
    {
        if (SceneFlowService.Instance == null || SceneFlowService.Instance.IsTransitioning)
            return "Travel is not available right now.";

        GameSessionManager sessions = GameSessionManager.Instance;
        if (sessions == null || !sessions.HasActiveSession)
            return "No active game session.";

        if (!worldGate && sessions.Current.SaveData != null)
            return RefusedSaveSessionMessage;

        if (PlayerStat.Instance == null || PlayerStat.Instance.IsDead != respawning)
            return "You cannot travel while defeated.";

        if (GameStateManager.Instance == null)
            return "Travel is not available right now.";

        return null;
    }

    /// <summary>Starts a travel to `sceneName`, placing the player at spawn `spawnId` on arrival.</summary>
    public static bool TryTravel(string sceneName, string spawnId, out string failureReason) =>
        TryTravel(sceneName, spawnId, default, false, out failureReason);

    internal static bool TryTravelThroughBossPillar(Scene source, string sceneName, string spawnId,
        out string failureReason) =>
        TryTravel(sceneName, spawnId, source, IsSavedBossRoute(source.name, sceneName, spawnId), out failureReason);

    internal static bool TryTravelThroughWorldGate(Scene source, string sceneName, string spawnId,
        out string failureReason)
    {
        bool validRoute = (source.name == "MapNhat" && sceneName == "MapDuy")
            || (source.name == "MapDuy" && sceneName == "MapNhat");
        return TryTravel(sceneName, spawnId, source, validRoute, out failureReason);
    }

    internal static bool TryRespawnInVillage(Scene source, out string failureReason) =>
        TryTravel("MapNhat", VillageSpawnId, source, true, out failureReason, respawning: true);

    internal const string VillageSpawnId = "spawn.mapnhat.start";

    private static bool TryTravel(string sceneName, string spawnId, Scene source, bool worldGate,
        out string failureReason, bool respawning = false)
    {
        failureReason = CheckCanTravel(worldGate, respawning);
        if (failureReason != null)
            return false;

        if (worldGate)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName) || string.IsNullOrEmpty(spawnId))
            {
                failureReason = "The destination could not be loaded.";
                return false;
            }

            GameSaveData save = GameSessionManager.Instance.Current.SaveData;
            if (save != null)
            {
                foreach (GameObject root in source.GetRootGameObjects())
                {
                    var world = root.GetComponent<WorldObjectRegistry>();
                    if (world != null)
                        save.world = WorldSaveData.Merge(save.world, world.ToSaveData());
                    var farm = root.GetComponent<FarmingManager>();
                    if (farm != null)
                        save.farming = farm.ToSaveData();
                }
            }
        }

        IsWorldGateArrival = worldGate;
        PlayerStat stat = PlayerStat.Instance;
        if (stat != null)
        {
            _pendingSnapshot = new PlayerTravelSnapshot(stat.Level, stat.CurrentExperience, respawning ? -1f : stat.Health, respawning ? stat.MaxStamina : stat.Stamina, stat.Skills.Capture());
            _pendingSpawnId = spawnId;
            _pending = true;
        }

        if (!SceneFlowService.Instance.TryLoadGameplay(sceneName))
        {
            Clear();
            failureReason = "The destination could not be loaded.";
            return false;
        }

        if (!respawning)
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

    internal static void Clear()
    {
        IsWorldGateArrival = false;
        _pending = false;
        _pendingSpawnId = null;
        _pendingSnapshot = default;
    }
}
