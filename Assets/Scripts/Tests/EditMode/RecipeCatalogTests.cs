using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class RecipeCatalogTests
{
    private static RecipeDefinition MakeDefinition(string recipeId)
    {
        var definition = ScriptableObject.CreateInstance<RecipeDefinition>();
        SetPrivate(definition, "_recipeId", recipeId);
        return definition;
    }

    private static RecipeCatalog MakeCatalog(params RecipeDefinition[] recipes)
    {
        var catalog = ScriptableObject.CreateInstance<RecipeCatalog>();
        SetPrivate(catalog, "_recipes", recipes);
        return catalog;
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    [Test]
    public void TryResolve_FindsRecipeByStableId()
    {
        RecipeDefinition plank = MakeDefinition("recipe.material.plank");
        RecipeCatalog catalog = MakeCatalog(plank);
        try
        {
            Assert.IsTrue(catalog.TryResolve("recipe.material.plank", out RecipeDefinition resolved));
            Assert.AreEqual(plank, resolved);
            Assert.AreEqual(1, catalog.AllRecipes.Count);
        }
        finally
        {
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(plank);
        }
    }

    [Test]
    public void TryResolve_UnknownOrEmptyId_ReturnsFalse()
    {
        RecipeCatalog catalog = MakeCatalog(MakeDefinition("recipe.material.plank"));
        try
        {
            Assert.IsFalse(catalog.TryResolve("recipe.unknown", out RecipeDefinition resolved));
            Assert.IsNull(resolved);
            Assert.IsFalse(catalog.TryResolve(null, out _));
        }
        finally
        {
            Object.DestroyImmediate(catalog);
        }
    }
}
