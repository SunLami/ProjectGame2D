using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places the teleport pillar + map-selection UI in DemoScene, and a return pillar + travel arrival in
/// BossArena_Earth (D-089). Idempotent: re-running replaces the objects it created.
/// Menu: Tools > Project Game > Boss > Teleport - Build Pillars.
/// </summary>
public static class TeleportPillarBuilder
{
    private const string SheetPath = "Assets/Art/Teleport/TeleportPillar_Idle_9f.png";
    private const string EarthIconPath = "Assets/Art/BossArena/Earth/Boss/EarthGolem_Final_256.png";
    private const string DemoScenePath = "Assets/Scenes/DemoScene.unity";
    private const string ArenaScenePath = "Assets/Scenes/BossArena_Earth.unity";
    private const string WaterArenaScenePath = "Assets/Scenes/BossArena_Water.unity";
    private const string WindArenaScenePath = "Assets/Scenes/BossArena_Wind.unity";
    private const string CrabIconPath = "Assets/Art/BossArena/Water/Boss/CrabCand_D_seed404_256.png";
    private const string PillarRoot = "TeleportPillar";
    private const string UiRoot = "BossTeleportSelectUI";

    // West of the start, clear of the enemy camp on the east side (enemies chase within 5 units of their post).
    private static readonly Vector2 DemoPillarPosition = new Vector2(-6f, 2.5f);

    [MenuItem("Tools/Project Game/Boss/Teleport - Build Pillars")]
    public static void BuildAll()
    {
        Sprite[] frames = ImportSheet();
        Sprite earthIcon = ImportIcon(EarthIconPath);
        Sprite crabIcon = ImportIcon(CrabIconPath);

        BuildDemoScene(frames, earthIcon, crabIcon);
        BuildArena(frames, ArenaScenePath);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(WaterArenaScenePath) != null)
            BuildArena(frames, WaterArenaScenePath);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(WindArenaScenePath) != null)
            BuildArena(frames, WindArenaScenePath);

        EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        Debug.Log("TeleportPillarBuilder: pillars built in DemoScene and BossArena_Earth.");
    }

    // ------------------------------------------------------------------ assets

    private static Sprite[] ImportSheet()
    {
        var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Missing " + SheetPath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 26f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
        int frameCount = texture.width / 80;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            rects[i] = new SpriteRect
            {
                name = "TeleportPillar_Idle_" + i,
                rect = new Rect(i * 80, 0, 80, texture.height),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.04f),
                spriteID = GUID.Generate(),
            };
        }

        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private static Sprite ImportIcon(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ------------------------------------------------------------------ scenes

    private static void BuildDemoScene(Sprite[] frames, Sprite earthIcon, Sprite crabIcon)
    {
        Scene scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        RemoveExisting(scene);

        BossTeleportSelectUI ui = CreateUi(scene);
        bool windAvailable = AssetDatabase.LoadAssetAtPath<SceneAsset>(WindArenaScenePath) != null;
        var destinations = new[]
        {
            new TeleportDestination
            {
                id = "boss.earth", displayName = "Earth Golem", sceneName = "BossArena_Earth", spawnId = "arena_entry",
                description = "A sealed stone courtyard guarded by a golem of living rock. Slam, spikes, falling stone and a shockwave ultimate.",
                available = true, icon = earthIcon, accent = new Color(0.85f, 0.62f, 0.25f, 1f),
            },
            new TeleportDestination
            {
                id = "boss.water", displayName = "Tidal Crab", sceneName = "BossArena_Water", spawnId = "arena_entry",
                description = "A tidal beach where an armoured crab burrows, spits bubbles and calls the sea. The water rises and falls as you fight.",
                available = true, icon = crabIcon, accent = new Color(0.3f, 0.6f, 0.95f, 1f),
            },
            new TeleportDestination
            {
                id = "boss.wind", displayName = "Sky Owl", sceneName = windAvailable ? "BossArena_Wind" : "", spawnId = windAvailable ? "arena_entry" : "",
                description = windAvailable
                    ? "A cross-shaped terrace floating above a sea of clouds. A wild wind drifts you toward the edge while the sky owl dives, cuts and calls a storm."
                    : "A floating terrace above the clouds where the wind decides who falls.",
                available = windAvailable, accent = new Color(0.7f, 0.85f, 0.9f, 1f),
            },
        };

        GameObject pillar = CreatePillar(scene, frames, DemoPillarPosition, ui, destinations, null,
            "BOSS TELEPORT", "Choose a sealed arena to enter.");

        // where the player reappears when travelling back to this scene
        GameObject context = scene.GetRootGameObjects().First(g => g.name == "_SceneContext");
        Transform returnSpawn = new GameObject("Spawn_TeleportPillarReturn").transform;
        returnSpawn.SetParent(context.transform, false);
        returnSpawn.position = DemoPillarPosition + new Vector2(0f, -2.4f);
        AddSpawn(context.GetComponent<SpawnRegistry>(), "teleport_pillar_return", returnSpawn);
        AddArrival(context, "teleport_pillar_return");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TeleportPillarBuilder: DemoScene pillar at " + pillar.transform.position);
    }

    private static void BuildArena(Sprite[] frames, string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RemoveExisting(scene);

        BossTeleportSelectUI ui = CreateUi(scene);
        var destinations = new[]
        {
            new TeleportDestination
            {
                id = "return.demo", displayName = "Heart Village", sceneName = "DemoScene", spawnId = "teleport_pillar_return",
                description = "Leave the arena and return to the village.", available = true,
                accent = new Color(0.55f, 0.85f, 0.5f, 1f),
            },
        };

        var arena = Object.FindAnyObjectByType<BossArenaController>();
        Vector2 pillarPosition = scenePath == WindArenaScenePath ? new Vector2(7f, -13.2f) : new Vector2(5f, -13.6f);
        GameObject pillar = CreatePillar(scene, frames, pillarPosition, ui, destinations, arena,
            "LEAVE THE ARENA", "Return to the village.");

        GameObject context = scene.GetRootGameObjects().First(g => g.name == "_SceneContext");
        AddArrival(context, "arena_entry");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TeleportPillarBuilder: arena return pillar at " + pillar.transform.position);
    }

    private static void RemoveExisting(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == PillarRoot || root.name == UiRoot)
                Object.DestroyImmediate(root);
        }
    }

    private static BossTeleportSelectUI CreateUi(Scene scene)
    {
        var go = new GameObject(UiRoot);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go.AddComponent<BossTeleportSelectUI>();
    }

    private static GameObject CreatePillar(Scene scene, Sprite[] frames, Vector2 position, BossTeleportSelectUI ui,
        TeleportDestination[] destinations, BossArenaController arenaLock, string title, string subtitle)
    {
        var root = new GameObject(PillarRoot);
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = position;

        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = frames.FirstOrDefault();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 0;

        // hover/click area (first collider on the object = the trigger the interactable uses)
        var click = root.AddComponent<BoxCollider2D>();
        click.size = new Vector2(2.4f, 4f);
        click.offset = new Vector2(0f, 2f);

        root.AddComponent<HoverOutline>();
        var loop = root.AddComponent<AmbientLoop>();
        var loopSerialized = new SerializedObject(loop);
        SetSprites(loopSerialized.FindProperty("_frames"), PingPong(frames, 6)); // frames 6-8 of the generated clip drift off-model
        loopSerialized.FindProperty("_frameRate").floatValue = 8f;
        loopSerialized.ApplyModifiedPropertiesWithoutUndo();

        var interactable = root.AddComponent<TeleportPillarInteractable>();
        var serialized = new SerializedObject(interactable);
        SerializedProperty list = serialized.FindProperty("_destinations");
        list.arraySize = destinations.Length;
        for (int i = 0; i < destinations.Length; i++)
        {
            SerializedProperty item = list.GetArrayElementAtIndex(i);
            TeleportDestination d = destinations[i];
            item.FindPropertyRelative("id").stringValue = d.id;
            item.FindPropertyRelative("displayName").stringValue = d.displayName;
            item.FindPropertyRelative("description").stringValue = d.description;
            item.FindPropertyRelative("sceneName").stringValue = d.sceneName;
            item.FindPropertyRelative("spawnId").stringValue = d.spawnId;
            item.FindPropertyRelative("available").boolValue = d.available;
            item.FindPropertyRelative("icon").objectReferenceValue = d.icon;
            item.FindPropertyRelative("accent").colorValue = d.accent;
        }

        serialized.FindProperty("_ui").objectReferenceValue = ui;
        serialized.FindProperty("_title").stringValue = title;
        serialized.FindProperty("_subtitle").stringValue = subtitle;
        serialized.FindProperty("_lockWhileFighting").objectReferenceValue = arenaLock;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // the plinth blocks walking through it
        var solid = new GameObject("Base");
        solid.transform.SetParent(root.transform, false);
        var box = solid.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.9f, 0.9f);
        box.offset = new Vector2(0f, 0.6f);
        return root;
    }

    private static void AddSpawn(SpawnRegistry registry, string id, Transform point)
    {
        var serialized = new SerializedObject(registry);
        SerializedProperty entries = serialized.FindProperty("_entries");
        for (int i = entries.arraySize - 1; i >= 0; i--)
        {
            if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("spawnId").stringValue == id)
                entries.DeleteArrayElementAtIndex(i);
        }

        int index = entries.arraySize;
        entries.arraySize = index + 1;
        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("spawnId").stringValue = id;
        entry.FindPropertyRelative("point").objectReferenceValue = point;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddArrival(GameObject context, string defaultSpawn)
    {
        var existing = context.GetComponent<SceneTravelArrival>();
        if (existing != null)
            Object.DestroyImmediate(existing);

        var arrival = context.AddComponent<SceneTravelArrival>();
        var serialized = new SerializedObject(arrival);
        serialized.FindProperty("_spawnRegistry").objectReferenceValue = context.GetComponent<SpawnRegistry>();
        serialized.FindProperty("_defaultSpawnId").stringValue = defaultSpawn;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>0..n-1 then back down to 1, so the loop has no jump at the seam.</summary>
    private static Sprite[] PingPong(Sprite[] frames, int count)
    {
        count = Mathf.Min(count, frames.Length);
        var order = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < count; i++)
            order.Add(frames[i]);
        for (int i = count - 2; i >= 1; i--)
            order.Add(frames[i]);
        return order.ToArray();
    }

    private static void SetSprites(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
