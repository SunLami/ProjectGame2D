using System;
using UnityEngine;

/// <summary>One map the pillar can send the player to (D-089). Locked entries are shown as "coming soon".</summary>
[Serializable]
public sealed class TeleportDestination
{
    public string id = "boss.earth";
    public string displayName = "Earth Golem";
    [TextArea(2, 4)] public string description = "A sealed stone courtyard guarded by a golem of living rock.";
    [Tooltip("Scene loaded through SceneFlowService (must be in Build Settings).")]
    public string sceneName = "BossArena_Earth";
    [Tooltip("SpawnRegistry id in the destination scene where the player appears.")]
    public string spawnId = "arena_entry";
    public bool available = true;
    public Sprite icon;
    public Color accent = new Color(0.85f, 0.62f, 0.25f, 1f);
}

/// <summary>
/// World object that opens the map-selection UI when the player left-clicks it in range (D-089):
/// GameCursorManager resolves the hover (Interact cursor) and calls <see cref="TryOpen"/>. The destination
/// list and the UI are scene-bound via the Inspector, so the same prefab works in any scene.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class TeleportPillarInteractable : MonoBehaviour
{
    [SerializeField] private TeleportDestination[] _destinations = Array.Empty<TeleportDestination>();
    [SerializeField] private BossTeleportSelectUI _ui;
    [SerializeField] private string _title = "BOSS TELEPORT";
    [SerializeField] private string _subtitle = "Choose a sealed arena to enter.";
    [Tooltip("Optional: the pillar is locked while this arena's summon/boss fight is running (no leaving mid-fight).")]
    [SerializeField] private BossArenaController _lockWhileFighting;

    public TeleportDestination[] Destinations => _destinations;
    public bool IsAvailable => _ui != null && !_ui.IsOpen && _destinations.Length > 0
        && (_lockWhileFighting == null || _lockWhileFighting.State == BossArenaController.ArenaState.StatueIdle);

    private void Awake()
    {
        Collider2D interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;
    }

    public bool TryOpen()
    {
        if (!IsAvailable)
            return false;

        return _ui.Open(_destinations, OnDestinationChosen, _title, _subtitle);
    }

    private static void OnDestinationChosen(TeleportDestination destination)
    {
        if (destination == null)
            return;

        if (!SceneTravel.TryTravel(destination.sceneName, destination.spawnId, out string reason))
            WorldMessagePopupUI.Show(reason);
    }
}
