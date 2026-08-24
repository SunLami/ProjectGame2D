using System;
using System.Collections.Generic;

/// <summary>
/// Capability seam a future NPC component composes instead of touching CraftingManager internals
/// directly, mirroring QuestNpcInteractionService/ShopNpcInteractionService.
/// </summary>
public sealed class CraftingNpcInteractionService
{
    private readonly CraftingManager _craftingManager;

    public CraftingNpcInteractionService(CraftingManager craftingManager)
    {
        _craftingManager = craftingManager ?? throw new ArgumentNullException(nameof(craftingManager));
    }

    /// <summary>Recipes this npcId offers as a Crafting capability, if any.</summary>
    public IReadOnlyList<RecipeDefinition> GetOfferedRecipes(string npcId)
    {
        var offered = new List<RecipeDefinition>();
        if (_craftingManager.Catalog == null || string.IsNullOrEmpty(npcId))
            return offered;

        foreach (RecipeDefinition candidate in _craftingManager.Catalog.AllRecipes)
        {
            if (string.Equals(candidate.NpcId, npcId, StringComparison.Ordinal))
                offered.Add(candidate);
        }
        return offered;
    }

    /// <summary>Crafts recipeId only if npcId actually offers it -- rejects a recipe this npcId
    /// does not provide as a capability.</summary>
    public bool TryCraft(string npcId, string recipeId, string stationTag, out CraftingTransactionResult result)
    {
        foreach (RecipeDefinition candidate in GetOfferedRecipes(npcId))
        {
            if (candidate.RecipeId == recipeId)
                return _craftingManager.TryCraft(recipeId, stationTag, out result);
        }

        result = CraftingTransactionResult.RecipeNotFound;
        return false;
    }
}
