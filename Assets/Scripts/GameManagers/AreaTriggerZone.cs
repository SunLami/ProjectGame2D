using System;
using UnityEngine;

/// <summary>
/// Scene-placed trigger volume that fires a domain event when the Player enters a named area.
/// Non-visual, config-only (areaId) -- no gameplay logic lives here. Currently placed in DemoScene
/// per DemoSceneWorkflow (promote to a production world scene later without code changes).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class AreaTriggerZone : MonoBehaviour
{
    [SerializeField] private string _areaId;

    public static event Action<string> PlayerEnteredArea;

    public string AreaId => _areaId;

    internal static void RaiseEnteredForTests(string areaId) => PlayerEnteredArea?.Invoke(areaId);

    private void Reset()
    {
        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }

    private void OnEnable() => AreaZoneRegistry.Register(_areaId, transform);

    private void OnDisable()
    {
        AreaZoneRegistry.Unregister(_areaId, transform);
        AreaZoneRegistry.SetPlayerInside(_areaId, false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(_areaId) || !other.CompareTag("Player"))
            return;

        AreaZoneRegistry.SetPlayerInside(_areaId, true);
        PlayerEnteredArea?.Invoke(_areaId);
    }

    // Not part of the original one-shot-pulse contract (see class remarks) -- only feeds
    // AreaZoneRegistry's inside/outside state for the direction indicator's "you've arrived"
    // marker, not a new domain event; nothing else needs to know when the player leaves.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(_areaId) || !other.CompareTag("Player"))
            return;

        AreaZoneRegistry.SetPlayerInside(_areaId, false);
    }
}
