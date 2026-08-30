using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeCatalog", menuName = "Game/Crafting/Recipe Catalog")]
public sealed class RecipeCatalog : ScriptableObject, IRecipeResolver
{
    [SerializeField] private RecipeDefinition[] _recipes;

    private Dictionary<string, RecipeDefinition> _byId;

    public IReadOnlyList<RecipeDefinition> AllRecipes => _recipes ?? Array.Empty<RecipeDefinition>();

    public bool TryResolve(string recipeId, out RecipeDefinition definition)
    {
        if (string.IsNullOrEmpty(recipeId))
        {
            definition = null;
            return false;
        }

        EnsureLookup();
        return _byId.TryGetValue(recipeId, out definition);
    }

    private void EnsureLookup()
    {
        if (_byId != null)
            return;

        _byId = new Dictionary<string, RecipeDefinition>(StringComparer.Ordinal);
        if (_recipes == null)
            return;

        foreach (RecipeDefinition recipe in _recipes)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.RecipeId) || _byId.ContainsKey(recipe.RecipeId))
                continue;

            _byId.Add(recipe.RecipeId, recipe);
        }
    }
}
