using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ContentValidationRunner
{
    private const string MenuPath = "Tools/Project Game/Validate Content";
    private static readonly Regex StableIdPattern = new("^[a-z0-9]+(?:\\.[a-z0-9_]+)+$", RegexOptions.Compiled);
    private static readonly Regex LegacyIdPattern = new("^[a-z0-9]+(?:_[a-z0-9]+)+$", RegexOptions.Compiled);

    [MenuItem(MenuPath)]
    public static void ValidateContent()
    {
        var report = new ValidationReport();
        List<ItemSO> items = LoadAssets<ItemSO>();
        List<EquipmentItemSO> equipment = LoadAssets<EquipmentItemSO>();

        HashSet<string> knownItemIds = ValidateItems(items, report);
        ValidateEquipmentCatalogs(equipment, report);
        ValidateItemDatabases(report);
        ValidateTileData(report);
        ValidateTutorialDefinitions(report);
        ValidateQuestDefinitions(report);
        ValidateShopDefinitions(report, knownItemIds);
        ValidateRecipeDefinitions(report, knownItemIds);

        string summary = $"Content validation finished: {report.ErrorCount} error(s), "
            + $"{report.WarningCount} warning(s), {report.CheckedAssetCount} asset(s) checked.";

        if (report.ErrorCount > 0)
            Debug.LogError(summary);
        else if (report.WarningCount > 0)
            Debug.LogWarning(summary);
        else
            Debug.Log(summary);
    }

    private static HashSet<string> ValidateItems(IReadOnlyList<ItemSO> items, ValidationReport report)
    {
        var byId = new Dictionary<string, ItemSO>(StringComparer.Ordinal);

        foreach (ItemSO item in items)
        {
            report.Check(item);
            string path = AssetDatabase.GetAssetPath(item);

            if (string.IsNullOrWhiteSpace(item.itemId))
            {
                report.Error(path, "itemId is empty.", item);
            }
            else
            {
                string id = item.itemId.Trim();
                if (byId.TryGetValue(id, out ItemSO duplicate))
                {
                    report.Error(path,
                        $"itemId '{id}' duplicates '{AssetDatabase.GetAssetPath(duplicate)}'.", item);
                }
                else
                {
                    byId.Add(id, item);
                }

                if (!StableIdPattern.IsMatch(id))
                {
                    if (LegacyIdPattern.IsMatch(id))
                        report.Warning(path, $"itemId '{id}' uses the accepted legacy format; migrate before production saves.", item);
                    else
                        report.Error(path, $"itemId '{id}' does not match the stable ID convention.", item);
                }
            }

            if (string.IsNullOrWhiteSpace(item.itemName))
                report.Error(path, "itemName is empty.", item);
            if (item.icon == null)
                report.Error(path, "icon is missing.", item);
            if (item.maxStackSize < 1)
                report.Error(path, "maxStackSize must be at least 1.", item);
            if (!item.isStackable && item.maxStackSize != 1)
                report.Error(path, "non-stackable item must have maxStackSize = 1.", item);
        }

        return new HashSet<string>(byId.Keys, StringComparer.Ordinal);
    }

    private static void ValidateEquipmentCatalogs(
        IReadOnlyList<EquipmentItemSO> equipment,
        ValidationReport report)
    {
        var expected = new HashSet<EquipmentItemSO>(equipment);
        var cataloged = new HashSet<EquipmentItemSO>();
        List<EquipmentCatalog> catalogs = LoadAssets<EquipmentCatalog>();

        if (catalogs.Count == 0)
            report.Error("Assets", "No EquipmentCatalog asset exists.");

        foreach (EquipmentItemSO item in equipment)
        {
            string path = AssetDatabase.GetAssetPath(item);
            if (item.isStackable || item.maxStackSize != 1)
                report.Error(path, "equipment must be non-stackable with maxStackSize = 1.", item);

            if (RequiresVisual(item.slot) && item.spriteLibraryAsset == null)
                report.Error(path, $"{item.slot} equipment requires a SpriteLibraryAsset.", item);
        }

        foreach (EquipmentCatalog catalog in catalogs)
        {
            report.Check(catalog);
            ValidateCatalogArray(catalog, "headItems", catalog.headItems, EquipSlot.Head, cataloged, report);
            ValidateCatalogArray(catalog, "bodyItems", catalog.bodyItems, EquipSlot.Body, cataloged, report);
            ValidateCatalogArray(catalog, "weaponItems", catalog.weaponItems, EquipSlot.Weapon, cataloged, report);
            ValidateCatalogArray(catalog, "ringItems", catalog.ringItems, EquipSlot.Ring, cataloged, report);
            ValidateCatalogArray(catalog, "necklaceItems", catalog.necklaceItems, EquipSlot.Necklace, cataloged, report);
            ValidateCatalogArray(catalog, "footItems", catalog.footItems, EquipSlot.Foot, cataloged, report);
            ValidateCatalogArray(catalog, "shieldItems", catalog.shieldItems, EquipSlot.Shield, cataloged, report);
        }

        foreach (EquipmentItemSO item in expected)
        {
            if (!cataloged.Contains(item))
                report.Error(AssetDatabase.GetAssetPath(item), "equipment item is missing from every EquipmentCatalog.", item);
        }
    }

    private static void ValidateCatalogArray(
        EquipmentCatalog catalog,
        string fieldName,
        EquipmentItemSO[] entries,
        EquipSlot expectedSlot,
        ISet<EquipmentItemSO> cataloged,
        ValidationReport report)
    {
        string path = AssetDatabase.GetAssetPath(catalog);
        if (entries == null)
        {
            report.Error(path, $"{fieldName} is null.", catalog);
            return;
        }

        var local = new HashSet<EquipmentItemSO>();
        for (int i = 0; i < entries.Length; i++)
        {
            EquipmentItemSO item = entries[i];
            if (item == null)
            {
                report.Error(path, $"{fieldName}[{i}] is null.", catalog);
                continue;
            }

            if (!local.Add(item))
                report.Error(path, $"{fieldName} contains duplicate '{item.name}'.", catalog);
            if (!cataloged.Add(item))
                report.Error(path, $"'{item.name}' appears more than once across equipment catalogs/slots.", catalog);
            if (item.slot != expectedSlot)
                report.Error(path, $"{fieldName}[{i}] references {item.slot} item '{item.name}'.", catalog);
        }
    }

    private static void ValidateItemDatabases(ValidationReport report)
    {
        foreach (ItemDatabase database in LoadAssets<ItemDatabase>())
        {
            report.Check(database);
            string path = AssetDatabase.GetAssetPath(database);
            if (database.items == null)
            {
                report.Error(path, "items is null.", database);
                continue;
            }

            var seen = new HashSet<ItemSO>();
            for (int i = 0; i < database.items.Length; i++)
            {
                ItemDatabase.Entry entry = database.items[i];
                if (entry == null || entry.item == null)
                {
                    report.Error(path, $"items[{i}] has no item reference.", database);
                    continue;
                }

                if (entry.amount <= 0)
                    report.Error(path, $"items[{i}] amount must be greater than zero.", database);
                if (!seen.Add(entry.item))
                    report.Warning(path, $"items contains duplicate entry '{entry.item.name}'.", database);
            }
        }
    }

    private static void ValidateTileData(ValidationReport report)
    {
        var tileOwners = new Dictionary<TileBase, TileDataSO>();

        foreach (TileDataSO tileData in LoadAssets<TileDataSO>())
        {
            report.Check(tileData);
            string path = AssetDatabase.GetAssetPath(tileData);
            ValidateRequiredArray(path, "walkAudioClip", tileData.walkAudioClip, tileData, report);
            ValidateRequiredArray(path, "runAudioClip", tileData.runAudioClip, tileData, report);

            if (tileData.tiles == null || tileData.tiles.Length == 0)
            {
                report.Error(path, "tiles must contain at least one tile.", tileData);
                continue;
            }

            var localTiles = new HashSet<TileBase>();
            for (int i = 0; i < tileData.tiles.Length; i++)
            {
                TileBase tile = tileData.tiles[i];
                if (tile == null)
                {
                    report.Error(path, $"tiles[{i}] is null.", tileData);
                    continue;
                }

                if (!localTiles.Add(tile))
                    report.Error(path, $"tile '{tile.name}' appears more than once in this definition.", tileData);

                if (tileOwners.TryGetValue(tile, out TileDataSO owner) && owner != tileData)
                {
                    report.Error(path,
                        $"tile '{tile.name}' is also owned by '{AssetDatabase.GetAssetPath(owner)}'.", tileData);
                }
                else
                {
                    tileOwners[tile] = tileData;
                }
            }
        }
    }

    private static void ValidateTutorialDefinitions(ValidationReport report)
    {
        var tutorialIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (TutorialDefinition tutorial in LoadAssets<TutorialDefinition>())
        {
            report.Check(tutorial);
            string path = AssetDatabase.GetAssetPath(tutorial);

            if (string.IsNullOrWhiteSpace(tutorial.TutorialId))
                report.Error(path, "tutorialId is empty.", tutorial);
            else if (!tutorialIds.Add(tutorial.TutorialId))
                report.Error(path, $"tutorialId '{tutorial.TutorialId}' is used by more than one TutorialDefinition.", tutorial);

            if (tutorial.Steps == null || tutorial.Steps.Count == 0)
            {
                report.Error(path, "steps must contain at least one step.", tutorial);
                continue;
            }

            var stepIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < tutorial.Steps.Count; i++)
            {
                TutorialStepDefinition step = tutorial.Steps[i];
                if (step == null)
                {
                    report.Error(path, $"steps[{i}] is null.", tutorial);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(step.StepId))
                    report.Error(path, $"steps[{i}] has an empty stepId.", tutorial);
                else if (!stepIds.Add(step.StepId))
                    report.Error(path, $"steps[{i}] stepId '{step.StepId}' duplicates another step in this tutorial.", tutorial);

                if (step.Type == TutorialStepType.ReachArea && string.IsNullOrWhiteSpace(step.TargetAreaId))
                    report.Error(path, $"steps[{i}] ('{step.StepId}') is ReachArea but has no targetAreaId.", tutorial);
            }
        }
    }

    private static void ValidateQuestDefinitions(ValidationReport report)
    {
        List<QuestDefinition> quests = LoadAssets<QuestDefinition>();
        var byId = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);

        foreach (QuestDefinition quest in quests)
        {
            report.Check(quest);
            string path = AssetDatabase.GetAssetPath(quest);

            if (string.IsNullOrWhiteSpace(quest.QuestId))
            {
                report.Error(path, "questId is empty.", quest);
            }
            else
            {
                string id = quest.QuestId.Trim();
                if (byId.TryGetValue(id, out QuestDefinition duplicate))
                    report.Error(path, $"questId '{id}' duplicates '{AssetDatabase.GetAssetPath(duplicate)}'.", quest);
                else
                    byId.Add(id, quest);

                if (!StableIdPattern.IsMatch(id))
                    report.Error(path, $"questId '{id}' does not match the stable ID convention.", quest);
            }

            if (quest.Objectives.Count == 0)
            {
                report.Error(path, "objectives must contain at least one entry.", quest);
            }
            else
            {
                for (int i = 0; i < quest.Objectives.Count; i++)
                    ValidateQuestObjective(path, quest, i, quest.Objectives[i], report);
            }

            ValidateQuestRewards(path, quest, report);

            if (quest.IsMainQuest && quest.PrerequisiteQuestIds.Count == 0)
                report.Warning(path, "isMainQuest quest has no prerequisiteQuestIds -- Main Quest gate expects a Tutorial Quest chain.", quest);
        }

        foreach (QuestDefinition quest in quests)
        {
            string path = AssetDatabase.GetAssetPath(quest);
            foreach (string prerequisiteId in quest.PrerequisiteQuestIds)
            {
                if (string.IsNullOrWhiteSpace(prerequisiteId))
                    report.Error(path, "prerequisiteQuestIds contains an empty entry.", quest);
                else if (!byId.ContainsKey(prerequisiteId))
                    report.Error(path, $"prerequisiteQuestIds references unknown questId '{prerequisiteId}'.", quest);
            }
        }

        DetectQuestPrerequisiteCycles(quests, byId, report);

        List<QuestCatalog> catalogs = LoadAssets<QuestCatalog>();
        if (catalogs.Count == 0 && quests.Count > 0)
            report.Error("Assets", "No QuestCatalog asset exists even though QuestDefinition assets do.");

        var cataloged = new HashSet<QuestDefinition>();
        foreach (QuestCatalog catalog in catalogs)
        {
            report.Check(catalog);
            string path = AssetDatabase.GetAssetPath(catalog);
            foreach (QuestDefinition quest in catalog.AllQuests)
            {
                if (quest == null)
                {
                    report.Error(path, "quests entry is null.", catalog);
                    continue;
                }
                if (!cataloged.Add(quest))
                    report.Error(path, $"quest '{quest.name}' appears more than once across quest catalogs.", catalog);
            }
        }

        foreach (QuestDefinition quest in quests)
        {
            if (!cataloged.Contains(quest))
                report.Error(AssetDatabase.GetAssetPath(quest), "quest is missing from every QuestCatalog.", quest);
        }
    }

    private static void ValidateQuestObjective(
        string path, QuestDefinition quest, int index, QuestObjectiveDefinition objective, ValidationReport report)
    {
        if (objective == null)
        {
            report.Error(path, $"objectives[{index}] is null.", quest);
            return;
        }

        if (string.IsNullOrWhiteSpace(objective.TargetId))
            report.Error(path, $"objectives[{index}] ({objective.Type}) has an empty target ID.", quest);

        if (objective.TargetCount <= 0)
            report.Error(path, $"objectives[{index}] targetCount must be greater than zero.", quest);

        if (string.IsNullOrWhiteSpace(objective.Description))
            report.Error(path, $"objectives[{index}] ({objective.Type}) has an empty description -- required presentation field for Quest UI.", quest);

        if (objective.Type is QuestObjectiveType.Talk or QuestObjectiveType.Obtain
            or QuestObjectiveType.Craft or QuestObjectiveType.Purchase)
        {
            if (!string.IsNullOrWhiteSpace(objective.TargetAreaId))
                report.Warning(path, $"objectives[{index}] ({objective.Type}) sets targetAreaId, which is only read by Gather/Kill.", quest);
        }
    }

    private static void ValidateQuestRewards(string path, QuestDefinition quest, ValidationReport report)
    {
        if (quest.Rewards == null)
            return;

        foreach (QuestRewardItemEntry entry in quest.Rewards.Items)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                report.Error(path, "rewards contains an item entry with an empty itemId.", quest);
            else if (entry.Quantity <= 0)
                report.Error(path, $"reward item '{entry.ItemId}' quantity must be greater than zero.", quest);
        }

        if (quest.Rewards.Gold < 0)
            report.Error(path, "rewards.gold must not be negative.", quest);
        if (quest.Rewards.Experience < 0)
            report.Error(path, "rewards.experience must not be negative.", quest);
    }

    private static void DetectQuestPrerequisiteCycles(
        List<QuestDefinition> quests,
        IReadOnlyDictionary<string, QuestDefinition> byId,
        ValidationReport report)
    {
        var state = new Dictionary<string, int>(StringComparer.Ordinal); // 0 unvisited, 1 in-progress, 2 done
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (QuestDefinition quest in quests)
        {
            if (string.IsNullOrWhiteSpace(quest.QuestId))
                continue;
            Visit(quest.QuestId, new List<string>());
        }

        void Visit(string questId, List<string> chain)
        {
            if (!byId.TryGetValue(questId, out QuestDefinition quest))
                return;
            if (state.TryGetValue(questId, out int visitState))
            {
                if (visitState == 1 && reported.Add(questId))
                {
                    report.Error(
                        AssetDatabase.GetAssetPath(quest),
                        $"prerequisite cycle detected: {string.Join(" -> ", chain)} -> {questId}.",
                        quest);
                }
                return;
            }

            state[questId] = 1;
            chain.Add(questId);
            foreach (string prerequisiteId in quest.PrerequisiteQuestIds)
            {
                if (!string.IsNullOrWhiteSpace(prerequisiteId))
                    Visit(prerequisiteId, chain);
            }
            chain.RemoveAt(chain.Count - 1);
            state[questId] = 2;
        }
    }

    private static void ValidateShopDefinitions(ValidationReport report, HashSet<string> knownItemIds)
    {
        var shopIds = new HashSet<string>(StringComparer.Ordinal);
        List<ShopDefinition> shops = LoadAssets<ShopDefinition>();

        foreach (ShopDefinition shop in shops)
        {
            report.Check(shop);
            string path = AssetDatabase.GetAssetPath(shop);

            if (string.IsNullOrWhiteSpace(shop.ShopId))
                report.Error(path, "shopId is empty.", shop);
            else if (!shopIds.Add(shop.ShopId))
                report.Error(path, $"shopId '{shop.ShopId}' is used by more than one ShopDefinition.", shop);
            else if (!StableIdPattern.IsMatch(shop.ShopId))
                report.Error(path, $"shopId '{shop.ShopId}' does not match the stable ID convention.", shop);

            if (shop.Stock.Count == 0)
            {
                report.Error(path, "stock must contain at least one entry.", shop);
                continue;
            }

            var stockItemIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < shop.Stock.Count; i++)
            {
                ShopStockEntry entry = shop.Stock[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    report.Error(path, $"stock[{i}] has an empty itemId.", shop);
                    continue;
                }

                if (!stockItemIds.Add(entry.ItemId))
                    report.Error(path, $"stock contains duplicate itemId '{entry.ItemId}'.", shop);
                if (!knownItemIds.Contains(entry.ItemId))
                    report.Error(path, $"stock itemId '{entry.ItemId}' does not exist in any ItemSO.", shop);
                if (entry.Price < 0)
                    report.Error(path, $"stock[{i}] ('{entry.ItemId}') price must not be negative.", shop);
            }
        }

        List<ShopCatalog> shopCatalogs = LoadAssets<ShopCatalog>();
        if (shopCatalogs.Count == 0 && shops.Count > 0)
            report.Error("Assets", "No ShopCatalog asset exists even though ShopDefinition assets do.");

        var catalogedShops = new HashSet<ShopDefinition>();
        foreach (ShopCatalog catalog in shopCatalogs)
        {
            report.Check(catalog);
            string path = AssetDatabase.GetAssetPath(catalog);
            foreach (ShopDefinition shop in catalog.AllShops)
            {
                if (shop == null)
                {
                    report.Error(path, "shops entry is null.", catalog);
                    continue;
                }
                if (!catalogedShops.Add(shop))
                    report.Error(path, $"shop '{shop.name}' appears more than once across shop catalogs.", catalog);
            }
        }

        foreach (ShopDefinition shop in shops)
        {
            if (!catalogedShops.Contains(shop))
                report.Error(AssetDatabase.GetAssetPath(shop), "shop is missing from every ShopCatalog.", shop);
        }
    }

    private static void ValidateRecipeDefinitions(ValidationReport report, HashSet<string> knownItemIds)
    {
        var recipeIds = new HashSet<string>(StringComparer.Ordinal);
        List<RecipeDefinition> recipes = LoadAssets<RecipeDefinition>();

        foreach (RecipeDefinition recipe in recipes)
        {
            report.Check(recipe);
            string path = AssetDatabase.GetAssetPath(recipe);

            if (string.IsNullOrWhiteSpace(recipe.RecipeId))
                report.Error(path, "recipeId is empty.", recipe);
            else if (!recipeIds.Add(recipe.RecipeId))
                report.Error(path, $"recipeId '{recipe.RecipeId}' is used by more than one RecipeDefinition.", recipe);
            else if (!StableIdPattern.IsMatch(recipe.RecipeId))
                report.Error(path, $"recipeId '{recipe.RecipeId}' does not match the stable ID convention.", recipe);

            if (recipe.Ingredients.Count == 0)
            {
                report.Error(path, "ingredients must contain at least one entry.", recipe);
            }
            else
            {
                var ingredientItemIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < recipe.Ingredients.Count; i++)
                {
                    RecipeIngredientEntry ingredient = recipe.Ingredients[i];
                    if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.ItemId))
                    {
                        report.Error(path, $"ingredients[{i}] has an empty itemId.", recipe);
                        continue;
                    }

                    if (!ingredientItemIds.Add(ingredient.ItemId))
                        report.Error(path, $"ingredients contains duplicate itemId '{ingredient.ItemId}'.", recipe);
                    if (!knownItemIds.Contains(ingredient.ItemId))
                        report.Error(path, $"ingredient itemId '{ingredient.ItemId}' does not exist in any ItemSO.", recipe);
                    if (ingredient.Quantity <= 0)
                        report.Error(path, $"ingredients[{i}] ('{ingredient.ItemId}') quantity must be greater than zero.", recipe);
                }
            }

            if (string.IsNullOrWhiteSpace(recipe.OutputItemId))
                report.Error(path, "outputItemId is empty.", recipe);
            else if (!knownItemIds.Contains(recipe.OutputItemId))
                report.Error(path, $"outputItemId '{recipe.OutputItemId}' does not exist in any ItemSO.", recipe);

            if (recipe.OutputQuantity <= 0)
                report.Error(path, "outputQuantity must be greater than zero.", recipe);
        }

        List<RecipeCatalog> recipeCatalogs = LoadAssets<RecipeCatalog>();
        if (recipeCatalogs.Count == 0 && recipes.Count > 0)
            report.Error("Assets", "No RecipeCatalog asset exists even though RecipeDefinition assets do.");

        var catalogedRecipes = new HashSet<RecipeDefinition>();
        foreach (RecipeCatalog catalog in recipeCatalogs)
        {
            report.Check(catalog);
            string path = AssetDatabase.GetAssetPath(catalog);
            foreach (RecipeDefinition recipe in catalog.AllRecipes)
            {
                if (recipe == null)
                {
                    report.Error(path, "recipes entry is null.", catalog);
                    continue;
                }
                if (!catalogedRecipes.Add(recipe))
                    report.Error(path, $"recipe '{recipe.name}' appears more than once across recipe catalogs.", catalog);
            }
        }

        foreach (RecipeDefinition recipe in recipes)
        {
            if (!catalogedRecipes.Contains(recipe))
                report.Error(AssetDatabase.GetAssetPath(recipe), "recipe is missing from every RecipeCatalog.", recipe);
        }
    }

    private static void ValidateRequiredArray<T>(
        string path,
        string fieldName,
        T[] values,
        UnityEngine.Object context,
        ValidationReport report) where T : UnityEngine.Object
    {
        if (values == null || values.Length == 0)
        {
            report.Error(path, $"{fieldName} must contain at least one asset.", context);
            return;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == null)
                report.Error(path, $"{fieldName}[{i}] is null.", context);
        }
    }

    private static bool RequiresVisual(EquipSlot slot) =>
        slot is EquipSlot.Head or EquipSlot.Body or EquipSlot.Weapon;

    private static List<T> LoadAssets<T>() where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets" });
        var assets = new List<T>(guids.Length);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                assets.Add(asset);
        }

        return assets;
    }

    private sealed class ValidationReport
    {
        private readonly HashSet<UnityEngine.Object> _checkedAssets = new();

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int CheckedAssetCount => _checkedAssets.Count;

        public void Check(UnityEngine.Object asset) => _checkedAssets.Add(asset);

        public void Error(string path, string message, UnityEngine.Object context = null)
        {
            ErrorCount++;
            Debug.LogError($"[Content Validation] {path}: {message}", context);
        }

        public void Warning(string path, string message, UnityEngine.Object context = null)
        {
            WarningCount++;
            Debug.LogWarning($"[Content Validation] {path}: {message}", context);
        }
    }
}
