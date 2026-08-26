#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemoResourceNodeAuthoring
{
    private const string ScenePath = "Assets/Scenes/DemoScene.unity";
    private const string ItemFolder = "Assets/Resources/Items/Materials";
    private const string DefinitionFolder = "Assets/Resources/World/ResourceNodes";
    private const string PrefabFolder = "Assets/Prefabs/World/Resources";
    private const string IconFolder = "Assets/Resources/Items/Materials/PlaceholderIcons";

    [MenuItem("Tools/Project Game 2D/Build Demo Resource Nodes")]
    public static void Build()
    {
        EnsureFolder(ItemFolder);
        EnsureFolder(DefinitionFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(IconFolder);

        Sprite copperIcon = CreatePlaceholderIcon("CopperOre", new Color32(190, 91, 45, 255), IconShape.Ore);
        Sprite woodIcon = CreatePlaceholderIcon("WoodLog", new Color32(139, 82, 38, 255), IconShape.Log);
        Sprite herbIcon = CreatePlaceholderIcon("MedicinalLeaf", new Color32(72, 175, 84, 255), IconShape.Leaf);

        ItemSO copper = CreateItem("CopperOre", "item.material.copper_ore", "Copper Ore", copperIcon);
        ItemSO wood = CreateItem("WoodLog", "item.material.wood_log", "Wood Log", woodIcon);
        ItemSO herb = CreateItem("MedicinalLeaf", "item.material.medicinal_leaf", "Medicinal Leaf", herbIcon);

        ResourceNodeDefinition copperDefinition = CreateDefinition(
            "CopperOreVein", "resource.ore.copper", ResourceHarvestType.Mining, 5f, 1f, 30f,
            new LootSpec(copper, 1f, 2, 4));
        ResourceNodeDefinition woodDefinition = CreateDefinition(
            "WoodTree", "resource.wood.tree", ResourceHarvestType.Chopping, 5f, 1f, 30f,
            new LootSpec(wood, 1f, 2, 4));
        ResourceNodeDefinition herbDefinition = CreateDefinition(
            "MedicinalHerb", "resource.herb.medicinal", ResourceHarvestType.Gathering, 1f, 1f, 20f,
            new LootSpec(herb, 1f, 1, 2));

        GameObject copperPrefab = CreatePrefab("CopperOreVein", copperDefinition, copperIcon, new Vector2(0.9f, 0.75f));
        GameObject woodPrefab = CreatePrefab("WoodTree", woodDefinition, woodIcon, new Vector2(0.9f, 1.25f));
        GameObject herbPrefab = CreatePrefab("MedicinalHerb", herbDefinition, herbIcon, new Vector2(0.75f, 0.75f));

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform parent = FindOrCreateResourceParent(scene);
        RemoveOldDemoNodes(scene);

        var nodes = new List<ResourceNodeInteractable>
        {
            InstantiateNode(copperPrefab, parent, "ResourceNode_CopperOre", "world.resource.demo.copper.01", new Vector3(3f, -3f, 0f)),
            InstantiateNode(woodPrefab, parent, "ResourceNode_WoodTree", "world.resource.demo.wood.01", new Vector3(5f, -3f, 0f)),
            InstantiateNode(herbPrefab, parent, "ResourceNode_MedicinalHerb", "world.resource.demo.herb.01", new Vector3(4f, -1.5f, 0f))
        };

        WorldObjectRegistry registry = Object.FindAnyObjectByType<WorldObjectRegistry>(FindObjectsInactive.Include);
        if (registry == null)
            throw new MissingReferenceException("DemoScene requires a WorldObjectRegistry.");

        SerializedObject registryData = new(registry);
        SerializedProperty entries = registryData.FindProperty("_entries");
        var preserved = new List<MonoBehaviour>();
        for (int i = 0; i < entries.arraySize; i++)
        {
            if (entries.GetArrayElementAtIndex(i).objectReferenceValue is MonoBehaviour entry
                && entry != null && entry is not ResourceNodeInteractable)
                preserved.Add(entry);
        }
        entries.arraySize = preserved.Count + nodes.Count;
        for (int i = 0; i < preserved.Count; i++)
            entries.GetArrayElementAtIndex(i).objectReferenceValue = preserved[i];
        for (int i = 0; i < nodes.Count; i++)
            entries.GetArrayElementAtIndex(preserved.Count + i).objectReferenceValue = nodes[i];
        registryData.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Demo resource nodes authored: Copper Ore Vein, Wood Tree, Medicinal Herb.");
    }

    private static ItemSO CreateItem(string assetName, string itemId, string displayName, Sprite icon)
    {
        string path = $"{ItemFolder}/{assetName}.asset";
        ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemSO>();
            AssetDatabase.CreateAsset(item, path);
        }
        item.itemId = itemId;
        item.itemName = displayName;
        item.description = $"Temporary demo material for {displayName}. Replace its art without changing itemId.";
        item.icon = icon;
        item.type = ItemType.Material;
        item.isStackable = true;
        item.maxStackSize = 99;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static ResourceNodeDefinition CreateDefinition(
        string assetName, string resourceId, ResourceHarvestType harvestType,
        float health, float harvestDamage, float respawnSeconds, params LootSpec[] loot)
    {
        string path = $"{DefinitionFolder}/{assetName}.asset";
        ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        SerializedObject data = new(definition);
        data.FindProperty("_resourceId").stringValue = resourceId;
        data.FindProperty("_harvestType").enumValueIndex = (int)harvestType;
        data.FindProperty("_requiredToolType").enumValueIndex = (int)HarvestToolType.None;
        data.FindProperty("_maximumHealth").floatValue = health;
        data.FindProperty("_harvestDamage").floatValue = harvestDamage;
        data.FindProperty("_gatheringDuration").floatValue = 1.2f;
        data.FindProperty("_respawnSeconds").floatValue = respawnSeconds;
        SerializedProperty table = data.FindProperty("_lootTable");
        table.arraySize = loot.Length;
        for (int i = 0; i < loot.Length; i++)
        {
            SerializedProperty entry = table.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("_item").objectReferenceValue = loot[i].Item;
            entry.FindPropertyRelative("_chance").floatValue = loot[i].Chance;
            entry.FindPropertyRelative("_minimumQuantity").intValue = loot[i].Minimum;
            entry.FindPropertyRelative("_maximumQuantity").intValue = loot[i].Maximum;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static GameObject CreatePrefab(
        string assetName, ResourceNodeDefinition definition, Sprite sprite, Vector2 colliderSize)
    {
        var root = new GameObject(assetName);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 20;
        visual.transform.localScale = Vector3.one * 1.5f;

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = colliderSize;
        ResourceNodeInteractable node = root.AddComponent<ResourceNodeInteractable>();
        SerializedObject data = new(node);
        data.FindProperty("_definition").objectReferenceValue = definition;
        data.FindProperty("_visualRoot").objectReferenceValue = visual;
        data.FindProperty("_flashRenderer").objectReferenceValue = renderer;
        data.ApplyModifiedPropertiesWithoutUndo();

        string path = $"{PrefabFolder}/{assetName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static ResourceNodeInteractable InstantiateNode(
        GameObject prefab, Transform parent, string name, string persistentId, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.position = position;
        ResourceNodeInteractable node = instance.GetComponent<ResourceNodeInteractable>();
        SerializedObject data = new(node);
        data.FindProperty("_persistentId").stringValue = persistentId;
        data.FindProperty("_areaId").stringValue = "area.demo";
        data.ApplyModifiedPropertiesWithoutUndo();
        return node;
    }

    private static Transform FindOrCreateResourceParent(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform existing = root.transform.Find("WorldObjects/Resources");
            if (existing != null) return existing;
        }

        GameObject world = GameObject.Find("WorldObjects") ?? new GameObject("WorldObjects");
        var resources = new GameObject("Resources");
        resources.transform.SetParent(world.transform, false);
        return resources.transform;
    }

    private static void RemoveOldDemoNodes(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (ResourceNodeInteractable node in root.GetComponentsInChildren<ResourceNodeInteractable>(true))
            Object.DestroyImmediate(node.gameObject);
    }

    private static Sprite CreatePlaceholderIcon(string name, Color32 color, IconShape shape)
    {
        string path = $"{IconFolder}/{name}.png";
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color32[32 * 32];
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            bool filled = shape switch
            {
                IconShape.Ore => Mathf.Abs(x - 16) + Mathf.Abs(y - 15) < 12,
                IconShape.Log => x >= 7 && x <= 24 && y >= 10 && y <= 21,
                _ => ((x - 15) * (x - 15)) / 100f + ((y - 16) * (y - 16)) / 49f <= 1f && x + y > 20
            };
            pixels[y * 32 + x] = filled ? color : new Color32(0, 0, 0, 0);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string part in path.Substring("Assets/".Length).Split('/'))
        {
            string next = $"{current}/{part}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }

    private readonly struct LootSpec
    {
        public readonly ItemSO Item;
        public readonly float Chance;
        public readonly int Minimum;
        public readonly int Maximum;
        public LootSpec(ItemSO item, float chance, int minimum, int maximum)
            => (Item, Chance, Minimum, Maximum) = (item, chance, minimum, maximum);
    }

    private enum IconShape { Ore, Log, Leaf }
}
#endif
