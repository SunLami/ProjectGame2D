using System.Collections;
using UnityEngine;

/// <summary>
/// Destination half of <see cref="SceneTravel"/> (D-089): when this scene was entered by travel, puts the
/// player at the requested spawn point and restores the progression carried over from the previous map.
/// Lives in the scene context of every scene that can be a travel destination. Does nothing on a normal
/// (menu/save) entry.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class SceneTravelArrival : MonoBehaviour
{
    [SerializeField] private SpawnRegistry _spawnRegistry;
    [Tooltip("Used when the travel request names no spawn, or the name is unknown.")]
    [SerializeField] private string _defaultSpawnId = "arena_entry";

    private IEnumerator Start()
    {
        if (!SceneTravel.TryConsume(out string spawnId, out PlayerTravelSnapshot snapshot))
            yield break;

        // The Player and its PlayerStat are created by the scene itself; wait a frame so both have run
        // Awake/Start (PlayerSpawnReadinessSource may also touch them) before the handoff is applied.
        yield return null;

        Player player = FindAnyObjectByType<Player>();
        PlayerStat stat = PlayerStat.Instance;
        if (stat != null)
        {
            stat.RestoreProgression(snapshot.Level, snapshot.Experience, snapshot.Health);
            // RestoreProgression is silent (no OnLevelUp), so refresh the HUD's level/health explicitly.
            PlayerHUDController hud = FindAnyObjectByType<PlayerHUDController>(FindObjectsInactive.Include);
            if (hud != null)
                hud.Refresh();
        }

        if (player == null || _spawnRegistry == null)
            yield break;

        if (!_spawnRegistry.TryGetSpawn(spawnId, out Vector3 position)
            && !_spawnRegistry.TryGetSpawn(_defaultSpawnId, out position))
        {
            Debug.LogWarning($"SceneTravelArrival: spawn '{spawnId}' not found in {gameObject.scene.name}.", this);
            yield break;
        }

        player.WarpTo(new Vector3(position.x, position.y, player.transform.position.z));
        LastAppliedSpawnId = spawnId;
        LastAppliedPosition = position;
    }

    /// <summary>Diagnostics for tests/tools: where the last arrival put the player.</summary>
    public static string LastAppliedSpawnId { get; private set; }
    public static Vector3 LastAppliedPosition { get; private set; }
}
