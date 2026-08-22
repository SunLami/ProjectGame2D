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

    internal static void RaiseEnteredForTests(string areaId) => PlayerEnteredArea?.Invoke(areaId);

    private void Reset()
    {
        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(_areaId) || !other.CompareTag("Player"))
            return;

        PlayerEnteredArea?.Invoke(_areaId);
    }
}
