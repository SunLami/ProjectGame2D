#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class FarmingAuthoring
{
    private const string PlantAtlas = "Assets/Tiles/Tilesets/Top-Down Farm with Animals Pixel Art Asset Pack/Tiled_files/Plants.png";
    private const string DefinitionRoot = "Assets/Game/Farming/Definitions";
    private const string ItemRoot = "Assets/Resources/Items/Farming";
    private const string PrefabRoot = "Assets/Prefabs/Farming";
    private const string CatalogPath = DefinitionRoot + "/FarmingCatalog.asset";
    private const string PlotPrefabPath = PrefabRoot + "/FarmPlot.prefab";

    [MenuItem("Tools/Project Game/Farming/Build And Install Farming")]
    public static void BuildAndInstall()
    {
        EnsureFolders();
        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(PlantAtlas)
            .OfType<Sprite>().ToDictionary(s => s.name, StringComparer.Ordinal);
        if (sprites.Count == 0) throw new InvalidOperationException("Plants atlas sprites were not found.");

        CropDefinition carrot = BuildCrop("Carrot", "crop.carrot", "item.seed.carrot", "Carrot Seeds",
            "item.crop.carrot", "Carrot", sprites, new[] { 80, 81, 82, 83 });
        CropDefinition eggplant = BuildCrop("Eggplant", "crop.eggplant", "item.seed.eggplant", "Eggplant Seeds",
            "item.crop.eggplant", "Eggplant", sprites, new[] { 88, 89, 90, 91 });
        FarmingCatalog catalog = LoadOrCreate<FarmingCatalog>(CatalogPath);
        SetObjectArray(catalog, "_crops", new UnityEngine.Object[] { carrot, eggplant });

        AddSeedsToStartingDatabase(new[]
        {
            AssetDatabase.LoadAssetAtPath<SeedItemSO>($"{ItemRoot}/Seed.Carrot.asset"),
            AssetDatabase.LoadAssetAtPath<SeedItemSO>($"{ItemRoot}/Seed.Eggplant.asset")
        });
        BuildPlotPrefab();
        InstallScene("Assets/Scenes/DemoScene.unity", catalog, false);
        InstallScene("Assets/Scenes/MapNhat.unity", catalog, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FarmingAuthoring: quick-bar content, farming assets, DemoScene and MapNhat integration completed.");
    }

    private static CropDefinition BuildCrop(string label, string cropId, string seedId, string seedName,
        string harvestId, string harvestName, IReadOnlyDictionary<string, Sprite> sprites, int[] indexes)
    {
        Sprite[] stages = indexes.Select(i => sprites.TryGetValue($"Plants_{i}", out Sprite sprite)
            ? sprite : throw new InvalidOperationException($"Plants_{i} was not found.")).ToArray();

        ItemSO harvest = LoadOrCreate<ItemSO>($"{ItemRoot}/Crop.{label}.asset");
        ConfigureItem(harvest, harvestId, harvestName, stages[^1]);
        CropDefinition crop = LoadOrCreate<CropDefinition>($"{DefinitionRoot}/Crop.{label}.asset");
        SerializedObject cropSo = new(crop);
        cropSo.FindProperty("_cropId").stringValue = cropId;
        cropSo.FindProperty("_harvestItem").objectReferenceValue = harvest;
        cropSo.FindProperty("_minimumHarvestQuantity").intValue = 1;
        cropSo.FindProperty("_maximumHarvestQuantity").intValue = 3;
        SerializedProperty stageArray = cropSo.FindProperty("_stages");
        stageArray.arraySize = stages.Length;
        for (int i = 0; i < stages.Length; i++)
        {
            SerializedProperty stage = stageArray.GetArrayElementAtIndex(i);
            stage.FindPropertyRelative("sprite").objectReferenceValue = stages[i];
            stage.FindPropertyRelative("durationSeconds").floatValue = i == stages.Length - 1 ? 0.1f : 20f;
        }
        cropSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(crop);

        SeedItemSO seed = LoadOrCreate<SeedItemSO>($"{ItemRoot}/Seed.{label}.asset");
        ConfigureItem(seed, seedId, seedName, stages[0]);
        SerializedObject seedSo = new(seed);
        seedSo.FindProperty("_crop").objectReferenceValue = crop;
        seedSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(seed);
        return crop;
    }

    private static void ConfigureItem(ItemSO item, string itemId, string itemName, Sprite icon)
    {
        item.itemId = itemId;
        item.itemName = itemName;
        item.description = item is SeedItemSO ? $"Plant to grow {itemName.Replace(" Seeds", string.Empty)}." : "Fresh farm produce.";
        item.icon = icon;
        item.type = ItemType.Material;
        item.isStackable = true;
        item.maxStackSize = 99;
        EditorUtility.SetDirty(item);
    }

    private static void AddSeedsToStartingDatabase(IReadOnlyList<SeedItemSO> seeds)
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/Items/ItemDatabase.asset");
        if (database == null) throw new InvalidOperationException("Starting ItemDatabase was not found.");
        var entries = database.items?.ToList() ?? new List<ItemDatabase.Entry>();
        foreach (SeedItemSO seed in seeds)
        {
            if (seed != null && entries.All(e => e.item != seed))
                entries.Add(new ItemDatabase.Entry { item = seed, amount = 12 });
        }
        database.items = entries.ToArray();
        EditorUtility.SetDirty(database);
    }

    private static void BuildPlotPrefab()
    {
        Sprite highlightSprite = LoadOrCreateHighlightSprite();
        GameObject root = new("FarmPlot", typeof(BoxCollider2D), typeof(FarmPlot));
        try
        {
            BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.9f, 0.9f);

            GameObject highlight = new("EmptyHighlight", typeof(SpriteRenderer));
            highlight.transform.SetParent(root.transform, false);
            SpriteRenderer highlightRenderer = highlight.GetComponent<SpriteRenderer>();
            highlightRenderer.sprite = highlightSprite;
            highlightRenderer.color = new Color(0.65f, 1f, 0.45f, 0.48f);
            highlightRenderer.sortingOrder = 19;
            highlight.SetActive(false);

            GameObject cropVisual = new("CropVisual", typeof(SpriteRenderer));
            cropVisual.transform.SetParent(root.transform, false);
            SpriteRenderer cropRenderer = cropVisual.GetComponent<SpriteRenderer>();
            cropRenderer.sortingOrder = 20;

            SerializedObject plot = new(root.GetComponent<FarmPlot>());
            plot.FindProperty("_areaId").stringValue = "area.farm";
            plot.FindProperty("_cropRenderer").objectReferenceValue = cropRenderer;
            plot.FindProperty("_emptyPlotHighlight").objectReferenceValue = highlight;
            plot.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PlotPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Sprite LoadOrCreateHighlightSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static void InstallScene(string scenePath, FarmingCatalog catalog, bool useFarmTilemap)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        foreach (FarmingManager old in FindSceneObjects<FarmingManager>())
            UnityEngine.Object.DestroyImmediate(old.gameObject);

        GameObject root = new("FarmingFeature", typeof(FarmingManager));
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlotPrefabPath);
        List<Vector3> positions = useFarmTilemap ? FindMapFarmPositions() : FindDemoPositions();
        var plots = new List<FarmPlot>(positions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            instance.name = $"FarmPlot_{i + 1:000}";
            instance.transform.position = positions[i];
            FarmPlot plot = instance.GetComponent<FarmPlot>();
            SerializedObject plotSo = new(plot);
            plotSo.FindProperty("_plotId").stringValue = $"farm.{(useFarmTilemap ? "mapnhat" : "demo")}.plot.{i + 1:000}";
            plotSo.ApplyModifiedPropertiesWithoutUndo();
            plots.Add(plot);
        }

        FarmingManager manager = root.GetComponent<FarmingManager>();
        SerializedObject managerSo = new(manager);
        managerSo.FindProperty("_catalog").objectReferenceValue = catalog;
        SerializedProperty entries = managerSo.FindProperty("_plots");
        entries.arraySize = plots.Count;
        for (int i = 0; i < plots.Count; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = plots[i];
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        foreach (PlayerSpawnReadinessSource readiness in FindSceneObjects<PlayerSpawnReadinessSource>())
        {
            SerializedObject so = new(readiness);
            so.FindProperty("_farmingManager").objectReferenceValue = manager;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (GameplaySessionController session in FindSceneObjects<GameplaySessionController>())
        {
            SerializedObject so = new(session);
            so.FindProperty("_farmingManager").objectReferenceValue = manager;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static List<Vector3> FindMapFarmPositions()
    {
        Tilemap dirt = FindSceneObjects<Tilemap>().FirstOrDefault(t => t.name == "Farm_Tiled_Dirt");
        if (dirt == null) throw new InvalidOperationException("MapNhat/Farm_Tiled_Dirt was not found.");
        var cells = new List<Vector3Int>();
        foreach (Vector3Int cell in dirt.cellBounds.allPositionsWithin)
        {
            if (cell.x % 2 != 0 || cell.y % 2 != 0 || !dirt.HasTile(cell)) continue;
            if (!dirt.HasTile(cell + Vector3Int.left) || !dirt.HasTile(cell + Vector3Int.right)
                || !dirt.HasTile(cell + Vector3Int.up) || !dirt.HasTile(cell + Vector3Int.down)) continue;
            cells.Add(cell);
        }
        return cells.OrderByDescending(c => c.y).ThenBy(c => c.x).Take(48)
            .Select(c => dirt.GetCellCenterWorld(c)).ToList();
    }

    private static List<Vector3> FindDemoPositions()
    {
        Player player = FindSceneObjects<Player>().FirstOrDefault();
        Vector3 origin = player != null ? player.transform.position + new Vector3(4f, 0f) : Vector3.zero;
        var result = new List<Vector3>();
        for (int row = 0; row < 2; row++)
            for (int column = 0; column < 4; column++)
                result.Add(origin + new Vector3(column * 1.15f, -row * 1.15f));
        return result;
    }

    private static void SetObjectArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        SerializedObject so = new(target);
        SerializedProperty array = so.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static List<T> FindSceneObjects<T>() where T : UnityEngine.Object =>
        new(UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include));

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Game", "Farming");
        EnsureFolder("Assets/Game/Farming", "Definitions");
        EnsureFolder("Assets/Resources/Items", "Farming");
        EnsureFolder("Assets/Prefabs", "Farming");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
