using UnityEngine;

// Scene-placed trigger volume that reports a human-readable zone name to MapZoneManager for the
// Minimap/FullMap location label. Deliberately separate from AreaTriggerZone/AreaZoneRegistry,
// which carry a technical areaId feeding Tutorial "reach area" steps and the quest direction
// indicator -- reusing that system here would risk firing quest/tutorial side effects just to
// show a place name on the map.
[RequireComponent(typeof(Collider2D))]
public sealed class MapZoneTrigger : MonoBehaviour
{
    [SerializeField] private string _zoneName;

    private void Reset()
    {
        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(_zoneName) || !other.CompareTag("Player"))
            return;
        MapZoneManager.Instance?.EnterZone(_zoneName);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (string.IsNullOrEmpty(_zoneName) || !other.CompareTag("Player"))
            return;
        MapZoneManager.Instance?.ExitZone(_zoneName);
    }
}
