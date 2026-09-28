using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps a stable npcId to that NPC's live world Transform for the current scene. QuestManager and
/// QuestNpcInteractionService deal only in npcId strings (by design -- see
/// TutorialAndQuestProgression.md NPC roles), so anything that needs an actual world position for
/// a quest-relevant NPC (e.g. a direction indicator pointing the player toward whoever they need
/// to talk to) has nowhere else to resolve one. Plain static dictionary, not a MonoBehaviour --
/// NPCs register themselves in OnEnable/unregister in OnDisable, so it always reflects whichever
/// NPCs are actually alive in the currently loaded scene(s), with no persistence/scene-teardown
/// bookkeeping needed here.
/// </summary>
public static class QuestNpcRegistry
{
    private static readonly Dictionary<string, Transform> ById = new(System.StringComparer.Ordinal);

    public static void Register(string npcId, Transform transform)
    {
        if (string.IsNullOrEmpty(npcId) || transform == null)
            return;
        ById[npcId] = transform;
    }

    public static void Unregister(string npcId, Transform transform)
    {
        if (string.IsNullOrEmpty(npcId))
            return;
        if (ById.TryGetValue(npcId, out Transform current) && current == transform)
            ById.Remove(npcId);
    }

    public static bool TryGet(string npcId, out Transform transform)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            transform = null;
            return false;
        }

        if (ById.TryGetValue(npcId, out transform) && transform != null)
            return true;

        // OnEnable registration is the normal path. During Editor Play Mode, however, a script or
        // asset reload can clear this static dictionary without toggling already-active scene NPCs,
        // leaving the quest tracker alive but its direction target unresolved. Repair only a missed
        // lookup, still matching the authored stable npcId rather than a GameObject/display name.
        foreach (QuestNpcInteractionUI npc in Object.FindObjectsByType<QuestNpcInteractionUI>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!string.Equals(npc.NpcId, npcId, System.StringComparison.Ordinal))
                continue;

            transform = npc.transform;
            ById[npcId] = transform;
            return true;
        }

        transform = null;
        return false;
    }
}
