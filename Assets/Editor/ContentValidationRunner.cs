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

        ValidateItems(items, report);
        ValidateEquipmentCatalogs(equipment, report);
        ValidateItemDatabases(report);
        ValidateTileData(report);

        string summary = $"Content validation finished: {report.ErrorCount} error(s), "
            + $"{report.WarningCount} warning(s), {report.CheckedAssetCount} asset(s) checked.";

        if (report.ErrorCount > 0)
            Debug.LogError(summary);
        else if (report.WarningCount > 0)
            Debug.LogWarning(summary);
        else
            Debug.Log(summary);
    }

    private static void ValidateItems(IReadOnlyList<ItemSO> items, ValidationReport report)
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
