using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class EquipmentContentTests
{
    private static readonly string[] RetiredIds =
    {
        "shield_lvl9", "ring_lvl9", "ring_lvl10", "necklace_lvl9", "necklace_lvl10"
    };

    [Test]
    public void ActiveEquipment_HasUniqueIdsValidLevelAndEconomy()
    {
        EquipmentItemSO[] items = Resources.LoadAll<EquipmentItemSO>("Items");

        Assert.AreEqual(55, items.Length);
        Assert.AreEqual(items.Length, items.Select(item => item.itemId).Distinct().Count());
        Assert.IsTrue(items.All(item => item.requiredLevel >= 1));
        Assert.IsTrue(items.All(item => item.MinBuyPrice > 0 && item.MaxBuyPrice >= item.MinBuyPrice));
        Assert.IsTrue(items.All(item => item.MinSellPrice > 0 && item.MaxSellPrice >= item.MinSellPrice));
        Assert.IsFalse(items.Any(item => RetiredIds.Contains(item.itemId)));
    }

    [Test]
    public void EquipmentRecipes_CoverEveryActiveEquipmentAndResolveAllIngredients()
    {
        RecipeCatalog catalog = AssetDatabase.LoadAssetAtPath<RecipeCatalog>("Assets/Crafting/RecipeCatalog.asset");
        var resolver = new ResourcesItemResolver();
        EquipmentItemSO[] items = Resources.LoadAll<EquipmentItemSO>("Items");
        HashSet<string> equipmentIds = items.Select(item => item.itemId).ToHashSet();
        RecipeDefinition[] recipes = catalog.AllRecipes.Where(recipe => equipmentIds.Contains(recipe.OutputItemId)).ToArray();

        Assert.AreEqual(55, recipes.Length);
        Assert.AreEqual(55, recipes.Select(recipe => recipe.RecipeId).Distinct().Count());
        Assert.AreEqual(55, recipes.Select(recipe => recipe.OutputItemId).Distinct().Count());
        foreach (RecipeDefinition recipe in recipes)
        {
            Assert.IsTrue(resolver.TryResolve(recipe.OutputItemId, out _), $"Missing output {recipe.OutputItemId}");
            foreach (RecipeIngredientEntry ingredient in recipe.Ingredients)
                Assert.IsTrue(resolver.TryResolve(ingredient.ItemId, out _), $"Missing ingredient {ingredient.ItemId} in {recipe.RecipeId}");
        }
    }
}

