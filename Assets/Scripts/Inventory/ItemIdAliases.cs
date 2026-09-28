using System.Collections.Generic;

/// <summary>
/// Compatibility aliases for stable item IDs that were renamed after content had already been
/// usable in local saves. New saves always write the canonical ID from the resolved ItemSO.
/// </summary>
public static class ItemIdAliases
{
    private static readonly IReadOnlyDictionary<string, string> LegacyToCanonical =
        new Dictionary<string, string>
        {
            ["fish.river.Fish1"] = "fish.river.azure_minnow",
            ["fish.river.Fish2"] = "fish.river.mossfin_perch",
            ["fish.river.Fish3"] = "fish.river.sunscale_carp",
            ["fish.river.Fish4"] = "fish.river.rosefin_bream",
            ["fish.river.Fish5"] = "fish.river.silverstream_darter",
            ["fish.river.Fish6"] = "fish.river.amethyst_bass",
            ["fish.river.Fish7"] = "fish.river.green_pike",
            ["fish.river.Fish8"] = "fish.river.pearlstripe_koi",
            ["fish.river.Fish9"] = "fish.river.shadowfin_bass",
            ["fish.river.Fish10"] = "fish.river.ember_koi"
        };

    public static string GetCanonicalId(string itemId) =>
        itemId != null && LegacyToCanonical.TryGetValue(itemId, out string canonicalId)
            ? canonicalId
            : itemId;

    public static void AddLegacyAliases(IDictionary<string, ItemSO> lookup)
    {
        if (lookup == null)
            return;

        foreach (KeyValuePair<string, string> alias in LegacyToCanonical)
        {
            if (!lookup.ContainsKey(alias.Key)
                && lookup.TryGetValue(alias.Value, out ItemSO item))
            {
                lookup[alias.Key] = item;
            }
        }
    }
}
