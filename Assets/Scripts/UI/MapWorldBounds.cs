using UnityEngine;

// Shared world-bounds lookup and world<->normalized(0..1) conversion for MinimapController and
// FullMapController, both of which map the Player's live world position onto the static snapshot
// baked by MapSnapshotBaker. The bounds are the same box MapSnapshotBaker photographed, so the
// normalized coordinates line up with the baked image pixel-for-pixel.
public static class MapWorldBounds
{
    private const string BorderMapObjectName = "BorderMap";

    public static bool TryGet(out Bounds bounds)
    {
        GameObject borderMap = GameObject.Find(BorderMapObjectName);
        Collider2D collider = borderMap != null ? borderMap.GetComponent<Collider2D>() : null;
        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }
        bounds = default;
        return false;
    }

    public static Vector2 WorldToNormalized(Bounds bounds, Vector3 worldPosition)
    {
        float x = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldPosition.x);
        float y = Mathf.InverseLerp(bounds.min.y, bounds.max.y, worldPosition.y);
        return new Vector2(x, y);
    }

    // player_marker_v1 points up (0,1) at zero rotation. Player.FacingDirection is already
    // axis-snapped to one of the four cardinal unit vectors, so this maps it directly to a Z
    // rotation for the marker instead of a free/continuous facing angle.
    public static float FacingToMarkerRotationZ(Vector2 facing) =>
        Mathf.Atan2(-facing.x, facing.y) * Mathf.Rad2Deg;
}
