using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps a stable areaId to that AreaTriggerZone's live world Transform and whether the player is
/// currently inside it, for the current scene. Mirrors QuestNpcRegistry's role for NPCs.
/// AreaTriggerZone only ever fired a one-shot enter event by design (the Tutorial ReachArea step
/// just needs a pulse) -- a direction indicator also needs a queryable position and an up-to-date
/// inside/outside state, which nothing exposed before this.
/// </summary>
public static class AreaZoneRegistry
{
    private static readonly Dictionary<string, Transform> ById = new(System.StringComparer.Ordinal);
    private static readonly HashSet<string> PlayerInsideIds = new(System.StringComparer.Ordinal);

    public static void Register(string areaId, Transform transform)
    {
        if (string.IsNullOrEmpty(areaId) || transform == null)
            return;
        ById[areaId] = transform;
    }

    public static void Unregister(string areaId, Transform transform)
    {
        if (string.IsNullOrEmpty(areaId))
            return;
        if (ById.TryGetValue(areaId, out Transform current) && current == transform)
            ById.Remove(areaId);
        PlayerInsideIds.Remove(areaId);
    }

    public static bool TryGet(string areaId, out Transform transform)
    {
        if (string.IsNullOrEmpty(areaId))
        {
            transform = null;
            return false;
        }
        return ById.TryGetValue(areaId, out transform) && transform != null;
    }

    public static void SetPlayerInside(string areaId, bool inside)
    {
        if (string.IsNullOrEmpty(areaId))
            return;
        if (inside)
            PlayerInsideIds.Add(areaId);
        else
            PlayerInsideIds.Remove(areaId);
    }

    public static bool IsPlayerInside(string areaId) =>
        !string.IsNullOrEmpty(areaId) && PlayerInsideIds.Contains(areaId);
}
