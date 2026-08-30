using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-driven recipe content. recipeId is the stable identity CraftingService keys off of; not
/// mutated at runtime. CraftingService is a single transaction engine shared by every recipe --
/// adding a recipe never means adding a `CraftIronSword()` method (DataDrivenDevelopment.md).
/// </summary>
[CreateAssetMenu(fileName = "NewRecipeDefinition", menuName = "Game/Crafting/Recipe Definition")]
public sealed class RecipeDefinition : ScriptableObject
{
    [SerializeField] private string _recipeId;
    [SerializeField] private string _displayName;
    [SerializeField] private RecipeIngredientEntry[] _ingredients;
    [SerializeField] private string _outputItemId;
    [SerializeField, Min(1)] private int _outputQuantity = 1;

    [Tooltip("Empty = craftable anywhere. Otherwise must match the stationTag passed to " +
        "CraftingManager.TryCraft (e.g. 'station.forge').")]
    [SerializeField] private string _requiredStationTag;

    [Tooltip("Optional stable npcId that offers this recipe as a Crafting capability.")]
    [SerializeField] private string _npcId;

    public string RecipeId => _recipeId;
    public string DisplayName => _displayName;
    public IReadOnlyList<RecipeIngredientEntry> Ingredients => _ingredients ?? Array.Empty<RecipeIngredientEntry>();
    public string OutputItemId => _outputItemId;
    public int OutputQuantity => _outputQuantity;
    public string RequiredStationTag => _requiredStationTag;
    public string NpcId => _npcId;
}
