using System.Collections.Generic;

public interface IRecipeResolver
{
    bool TryResolve(string recipeId, out RecipeDefinition definition);
    IReadOnlyList<RecipeDefinition> AllRecipes { get; }
}
