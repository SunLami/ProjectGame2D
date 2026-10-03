using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the Earth golem boss prefab + definition asset and (optionally) adds the DemoScene
/// test harness (D-086). Idempotent: re-running updates the assets in place.</summary>
public static class BossPrefabBuilder
{
    private const string Folder = "Assets/Bosses/EarthGolem";
    private const string SpritePath = "Assets/Art/BossArena/Earth/Boss/EarthGolem_Final_256.png";
    private const string DefinitionPath = Folder + "/EarthGolemBoss.asset";
    private const string PrefabPath = Folder + "/EarthGolemBoss.prefab";

    [MenuItem("Tools/Project Game/Boss/Build Earth Golem Prefab")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        ConfigureSprite();

        var definition = AssetDatabase.LoadAssetAtPath<BossDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = BossDefinition.CreateEarthGolemDefaults();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        var root = new GameObject("EarthGolemBoss");
        root.transform.localScale = Vector3.one * 0.75f; // 256 px at PPU 32 = 8 units -> 6 units (4x the player), see D-088
        var body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        var renderer = body.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 5;
        BossClipSetup.Fill(body.AddComponent<BossClipPlayer>()); // full-frame animations (D-092)

        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(root.transform, false);
        shadow.transform.localPosition = new Vector3(0f, -3.4f, 0f);
        shadow.transform.localScale = new Vector3(5.5f, 1.6f, 1f);
        var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = BossTelegraph.CircleSprite;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.35f);
        shadowRenderer.sortingLayerName = "Default";
        shadowRenderer.sortingOrder = -50;

        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        var hurtbox = root.AddComponent<CircleCollider2D>();
        hurtbox.isTrigger = true;
        hurtbox.radius = 2.5f;
        hurtbox.offset = new Vector2(0f, 0.2f);

        // Player skills (overlap / sweep checks) skip trigger colliders like regular enemies' do not have: add an
        // extra solid hurtbox that excludes every layer, so it can be found by queries but never pushes the player.
        var solidHurtbox = root.AddComponent<CircleCollider2D>();
        solidHurtbox.isTrigger = false;
        solidHurtbox.radius = hurtbox.radius;
        solidHurtbox.offset = hurtbox.offset;
        solidHurtbox.excludeLayers = ~0;

        var controller = root.AddComponent<BossController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("_definition").objectReferenceValue = definition;
        serialized.FindProperty("_body").objectReferenceValue = renderer;
        serialized.FindProperty("_visualRoot").objectReferenceValue = body.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (enemyLayer >= 0)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = enemyLayer;
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("BossPrefabBuilder: built " + PrefabPath);
    }

    [MenuItem("Tools/Project Game/Boss/Add Test Harness To Open Scene")]
    public static void AddHarness()
    {
        Scene scene = SceneManager.GetActiveScene();
        var existing = Object.FindAnyObjectByType<BossEncounterTestHarness>();
        if (existing != null)
        {
            Debug.Log("BossPrefabBuilder: harness already in " + scene.name);
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var definition = AssetDatabase.LoadAssetAtPath<BossDefinition>(DefinitionPath);
        if (prefab == null || definition == null)
        {
            Debug.LogError("BossPrefabBuilder: build the prefab first (Tools > Project Game > Boss > Build Earth Golem Prefab).");
            return;
        }

        var go = new GameObject("BossEncounterTestHarness");
        var harness = go.AddComponent<BossEncounterTestHarness>();
        var serialized = new SerializedObject(harness);
        serialized.FindProperty("_bossPrefab").objectReferenceValue = prefab;
        serialized.FindProperty("_definition").objectReferenceValue = definition;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("BossPrefabBuilder: added BossEncounterTestHarness to " + scene.name + " (save the scene).");
    }

    private static void ConfigureSprite()
    {
        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
