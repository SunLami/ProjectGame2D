using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Authors the Earth Orb item and wires the summoning statue of BossArena_Earth as a shrine (D-090):
/// BossShrineInteractable + HoverOutline on the statue, the BossShrineUI object, and the arena's summon
/// offering (item + count). Idempotent. BossArenaEarthBuilder calls <see cref="Apply"/> after rebuilding the
/// arena so the shrine survives a rebuild. Menu: Tools > Project Game > Boss > Shrine - Build.
/// </summary>
public static class BossShrineBuilder
{
    private const string IconPath = "Assets/Resources/Items/Materials/Icons/OrbEarth.png";
    private const string ItemPath = "Assets/Resources/Items/Materials/OrbEarth.asset";
    private const string ArenaScenePath = "Assets/Scenes/BossArena_Earth.unity";
    private const string UiRootName = "BossShrineUI";
    public const string OrbItemId = "item.material.orb_earth";

    [MenuItem("Tools/Project Game/Boss/Shrine - Build")]
    public static void BuildAll()
    {
        EnsureItem();
        Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        Apply(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene("Assets/Scenes/DemoScene.unity", OpenSceneMode.Single);
        Debug.Log("BossShrineBuilder: Earth Orb item + shrine built.");
    }

    public static ItemSO EnsureItem()
    {
        var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 40f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        var item = AssetDatabase.LoadAssetAtPath<ItemSO>(ItemPath);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemSO>();
            AssetDatabase.CreateAsset(item, ItemPath);
        }

        item.itemId = OrbItemId;
        item.itemName = "Earth Orb";
        item.description = "A heavy amber orb with a living green core. Offer it at the Earth shrine to awaken the golem.";
        item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        item.type = ItemType.Material;
        item.isStackable = true;
        item.maxStackSize = 99;
        EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets();
        return item;
    }

    /// <summary>Wires the shrine into the arena scene that is currently open (`scene`).</summary>
    public static void Apply(Scene scene, ItemSO offering = null)
    {
        ItemSO orb = offering != null ? offering : EnsureItem();

        BossArenaController arena = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<BossArenaController>(true)).FirstOrDefault();
        if (arena == null)
            throw new System.InvalidOperationException("No BossArenaController in " + scene.name);

        // UI object (root level, own canvas)
        GameObject existingUi = scene.GetRootGameObjects().FirstOrDefault(g => g.name == UiRootName);
        if (existingUi != null)
            Object.DestroyImmediate(existingUi);
        var uiObject = new GameObject(UiRootName);
        SceneManager.MoveGameObjectToScene(uiObject, scene);
        var ui = uiObject.AddComponent<BossShrineUI>();

        // the statue becomes the shrine
        var arenaSerialized = new SerializedObject(arena);
        var statueRenderer = (SpriteRenderer)arenaSerialized.FindProperty("_statueRenderer").objectReferenceValue;
        GameObject statue = statueRenderer.gameObject;

        var oldShrine = statue.GetComponent<BossShrineInteractable>();
        if (oldShrine != null)
            Object.DestroyImmediate(oldShrine);
        if (statue.GetComponent<HoverOutline>() == null)
            statue.AddComponent<HoverOutline>();

        var shrine = statue.AddComponent<BossShrineInteractable>();
        var shrineSerialized = new SerializedObject(shrine);
        shrineSerialized.FindProperty("_arena").objectReferenceValue = arena;
        shrineSerialized.FindProperty("_ui").objectReferenceValue = ui;
        shrineSerialized.ApplyModifiedPropertiesWithoutUndo();

        arenaSerialized.FindProperty("_summonItem").objectReferenceValue = orb;
        arenaSerialized.FindProperty("_summonItemCount").intValue = 1;
        arenaSerialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
