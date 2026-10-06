using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds Assets/Scenes/BossArena_Wind.unity (D-110): "Thien Dai", a cross-shaped sky platform above a sea of clouds. Like the Water
/// arena the scene starts as a copy of BossArena_Earth (so player, camera, teleport pillar and harness are wired identically), then the
/// Earth world is replaced by: scrolling cloud sea, the platform cut-out, bobbing floating islands, the owl shrine statue, four wind
/// totems (ritual emitters), the drifting clouds / wind gusts (<see cref="SkyWeather"/>), the walkable outline and the camera bounds.
/// The controller summons the Wind owl (Assets/Bosses/WindOwl, built by OwlBossBuilder); ArenaWind drifts the player during the fight.
/// Re-running rebuilds the world part. Menu: Tools > Project Game > Boss > Arena Wind - Build Scene.
/// </summary>
public static class BossArenaWindBuilder
{
    private const string Root = "Assets/Art/BossArena/Wind/";
    private const string ScenePath = "Assets/Scenes/BossArena_Wind.unity";
    private const string SourceScenePath = "Assets/Scenes/BossArena_Earth.unity";

    // world mapping: the platform image is 1500x900 at 20 px/unit; the middle of the vertical arm is world x = 0, the platform's
    // vertical middle is world y = 0 (pixel (829, 440), see Tools/arena_extend_sky.py)
    private const float MapPpu = 20f;
    private static readonly Vector2 MapImageSize = new Vector2(1500f, 900f);
    private static readonly Vector2 CentrePx = new Vector2(829f, 440f);

    /// <summary>Outline of the walkable floor (world units, clockwise from the top-left corner of the top arm).</summary>
    public static readonly Vector2[] WalkPolygon =
    {
        new Vector2(-13.45f, 14.75f), new Vector2(13.45f, 14.75f), new Vector2(13.45f, 6f), new Vector2(28.95f, 6f),
        new Vector2(28.95f, -11.6f), new Vector2(13.45f, -11.6f), new Vector2(13.45f, -14.75f), new Vector2(-13.45f, -14.75f),
        new Vector2(-13.45f, -11.6f), new Vector2(-31.65f, -11.6f), new Vector2(-31.65f, 6f), new Vector2(-13.45f, 6f),
    };

    private static readonly Rect WalkBounds = new Rect(-31.65f, -14.75f, 60.6f, 29.5f);
    private const float SeaTileWidth = 51.2f; // 1024 px at 20 px/unit

    private const string OrbIconPath = "Assets/Resources/Items/Materials/Icons/OrbWind.png";
    private const string OrbItemPath = "Assets/Resources/Items/Materials/OrbWind.asset";
    public const string OrbItemId = "item.material.orb_wind";
    private const float ObjectPpu = 24f;

    private static readonly Vector2[] TotemPositions =
    {
        new Vector2(-22f, 2.5f), new Vector2(22f, 2.5f), new Vector2(-22f, -8.5f), new Vector2(22f, -8.5f),
    };

    // floating rock islands out in the cloud sea: position, scale
    private static readonly (Vector2 position, float scale)[] Islands =
    {
        (new Vector2(-36.5f, 15f), 2.1f), (new Vector2(30f, 17.5f), 1.7f), (new Vector2(-35.5f, -16.5f), 1.8f),
        (new Vector2(32f, -17f), 2.2f), (new Vector2(-21f, 19.5f), 1.4f), (new Vector2(19f, -20.5f), 1.5f),
    };

    [MenuItem("Tools/Project Game/Boss/Arena Wind - Build Scene")]
    public static void BuildScene()
    {
        OwlBossBuilder.Build();
        EnsureArtCopies();
        ImportSprites();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            AssetDatabase.CopyAsset(SourceScenePath, ScenePath);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (string stale in new[] { "_ArenaEarth", "_ArenaWater", "_ArenaWind", "BossShrineUI" })
        {
            GameObject old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == stale);
            if (old != null)
                Object.DestroyImmediate(old);
        }

        var arenaRoot = new GameObject("_ArenaWind");
        SceneManager.MoveGameObjectToScene(arenaRoot, scene);
        BuildSea(arenaRoot.transform);
        BuildPlatform(arenaRoot.transform);
        BuildIslands(arenaRoot.transform);
        BuildArenaController(arenaRoot.transform);
        BuildWeather(arenaRoot.transform);
        BuildWind(arenaRoot.transform);
        ConfigureBorderAndCamera(scene);
        MoveSpawns(scene);
        BossArenaEarthBuilder.BuildFootstepTilemap(arenaRoot.transform);

        EnableDash();
        ItemSO orb = EnsureOrbItem();
        BossShrineBuilder.Apply(scene, orb);
        BuildTotems(arenaRoot.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings();
        Debug.Log("BossArenaWindBuilder: BossArena_Wind built and saved.");
    }

    // ------------------------------------------------------------------ assets

    private static void EnsureArtCopies()
    {
        // the orb icon is generated into Objects/Cand; the item folder needs its own copy
        string source = Root + "Objects/Cand/OrbWind_64.png";
        if (!File.Exists(OrbIconPath) && File.Exists(source))
        {
            File.Copy(source, OrbIconPath);
            AssetDatabase.ImportAsset(OrbIconPath);
        }
    }

    private static ItemSO EnsureOrbItem()
    {
        var importer = AssetImporter.GetAtPath(OrbIconPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        var item = AssetDatabase.LoadAssetAtPath<ItemSO>(OrbItemPath);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemSO>();
            AssetDatabase.CreateAsset(item, OrbItemPath);
        }

        item.itemId = OrbItemId;
        item.itemName = "Wind Orb";
        item.description = "A pearl with a restless gale swirling inside. Offer it at the owl shrine to call the sky guardian.";
        item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(OrbIconPath);
        item.type = ItemType.Material;
        item.isStackable = true;
        item.maxStackSize = 99;
        EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets();
        return item;
    }

    private static Sprite ImportSingle(string path, float ppu, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.SetTextureSettings(settings);
            importer.spritePivot = pivot;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>Slices a horizontal strip of `frames` equal frames with a custom pivot; returns the sprites in order.</summary>
    private static Sprite[] ImportStrip(string path, int frames, float ppu, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return new Sprite[0];

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        int frameWidth = texture.width / frames;
        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new SpriteRect[frames];
        for (int i = 0; i < frames; i++)
        {
            rects[i] = new SpriteRect
            {
                name = baseName + "_" + i,
                rect = new Rect(i * frameWidth, 0, frameWidth, texture.height),
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = GUID.Generate(),
            };
        }

        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
        return LoadFrames(path);
    }

    private static Sprite[] LoadFrames(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private static void ImportSprites()
    {
        ImportSingle(Root + "Platform_Wind_1500x900.png", MapPpu, new Vector2(0.5f, 0.5f));
        ImportSingle(Root + "SeaTile_Wind_1024x900.png", MapPpu, new Vector2(0.5f, 0.5f));
    }

    private static Sprite[] PingPong(Sprite[] frames)
    {
        var order = new List<Sprite>(frames);
        for (int i = frames.Length - 2; i >= 1; i--)
            order.Add(frames[i]);
        return order.ToArray();
    }

    private static void SetSprites(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    private static Vector3 ImageCentreWorld()
    {
        return new Vector3((MapImageSize.x * 0.5f - CentrePx.x) / MapPpu, -(MapImageSize.y * 0.5f - CentrePx.y) / MapPpu, 0f);
    }

    // ------------------------------------------------------------------ world

    private static void BuildSea(Transform parent)
    {
        var scroll = new GameObject("_CloudSea");
        scroll.transform.SetParent(parent, false);
        Vector3 centre = ImageCentreWorld();
        float left = centre.x - MapImageSize.x / MapPpu * 0.5f - SeaTileWidth; // one tile left of the painted map
        scroll.transform.position = Vector3.zero;
        Sprite tile = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "SeaTile_Wind_1024x900.png");
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("SeaTile" + i);
            go.transform.SetParent(scroll.transform, false);
            go.transform.position = new Vector3(left + SeaTileWidth * (i + 0.5f), centre.y, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = tile;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = -210;
        }

        var component = scroll.AddComponent<SkyScroll>();
        var serialized = new SerializedObject(component);
        serialized.FindProperty("_speed").floatValue = 0.7f;
        serialized.FindProperty("_tileWidth").floatValue = SeaTileWidth;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildPlatform(Transform parent)
    {
        var go = new GameObject("Arena_Platform");
        go.transform.SetParent(parent, false);
        go.transform.position = ImageCentreWorld();
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Platform_Wind_1500x900.png");
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = -200;
    }

    private static void BuildIslands(Transform parent)
    {
        Sprite island = ImportSingle(Root + "Objects/FloatingIsland.png", 26f, new Vector2(0.5f, 0.5f));
        var holder = new GameObject("_Islands");
        holder.transform.SetParent(parent, false);
        for (int i = 0; i < Islands.Length; i++)
        {
            var go = new GameObject("FloatingIsland" + i);
            go.transform.SetParent(holder.transform, false);
            go.transform.position = Islands[i].position;
            go.transform.localScale = new Vector3(Islands[i].scale * (i % 2 == 0 ? 1f : -1f), Islands[i].scale, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = island;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = -195;
            var bob = go.AddComponent<AmbientBob>();
            var serialized = new SerializedObject(bob);
            serialized.FindProperty("_amplitude").floatValue = 0.3f + 0.05f * (i % 3);
            serialized.FindProperty("_periodSeconds").floatValue = 4.5f + 0.7f * i;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void BuildArenaController(Transform parent)
    {
        var holder = new GameObject("_BossArena");
        holder.transform.SetParent(parent, false);
        var spawn = new GameObject("BossSpawnPoint");
        spawn.transform.SetParent(holder.transform, false);
        spawn.transform.position = new Vector3(0f, -2f, 0f);

        // the owl statue in the middle of the platform (pivot = plinth base)
        Sprite owl = ImportSingle(Root + "Objects/OwlStatue.png", ObjectPpu, new Vector2(0.5f, 0.04f));
        Sprite[] idle = PingPong(ImportStrip(Root + "Objects/OwlStatue_Idle_9f.png", 9, ObjectPpu, new Vector2(0.5f, 0.04f)).Take(4).ToArray()); // frames 0-3: a clean glow pulse
        var statue = new GameObject("SummonStatue");
        statue.transform.SetParent(holder.transform, false);
        statue.transform.position = new Vector3(0f, -3.4f, 0f);
        var statueRenderer = statue.AddComponent<SpriteRenderer>();
        statueRenderer.sprite = owl;
        statueRenderer.sortingLayerName = "Default";
        statueRenderer.sortingOrder = 0;
        var statueCollider = statue.AddComponent<BoxCollider2D>();
        statueCollider.size = new Vector2(3.2f, 1.5f);
        statueCollider.offset = new Vector2(0f, 0.85f);

        var controller = holder.AddComponent<BossArenaController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("_statueRenderer").objectReferenceValue = statueRenderer;
        serialized.FindProperty("_statueCollider").objectReferenceValue = statueCollider;
        SetSprites(serialized.FindProperty("_statueIdleFrames"), idle);
        serialized.FindProperty("_idleFrameRate").floatValue = 6f;
        serialized.FindProperty("_ritualBeamColor").colorValue = new Color(0.72f, 0.93f, 1f, 0.85f);
        serialized.FindProperty("_bossPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(OwlBossBuilder.PrefabPath);
        serialized.FindProperty("_definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BossDefinition>(OwlBossBuilder.DefinitionPath);
        serialized.FindProperty("_bossSpawnPoint").objectReferenceValue = spawn.transform;
        serialized.FindProperty("_walkableBounds").rectValue = WalkBounds;
        SerializedProperty polygon = serialized.FindProperty("_walkablePolygon");
        polygon.arraySize = WalkPolygon.Length;
        for (int i = 0; i < WalkPolygon.Length; i++)
            polygon.GetArrayElementAtIndex(i).vector2Value = WalkPolygon[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var director = holder.AddComponent<BossCameraDirector>();
        var directorSerialized = new SerializedObject(director);
        var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
        directorSerialized.FindProperty("_camera").objectReferenceValue = vcam;
        directorSerialized.FindProperty("_confiner").objectReferenceValue = vcam != null ? vcam.GetComponent<CinemachineConfiner2D>() : null;
        directorSerialized.FindProperty("_arena").objectReferenceValue = controller;
        directorSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Four wind totems around the owl; each has an EyePoint (the floating crystal) that fires a beam in the summon ritual.</summary>
    private static void BuildTotems(Transform parent)
    {
        Sprite totem = ImportSingle(Root + "Objects/WindTotem.png", ObjectPpu, new Vector2(0.5f, 0.04f));
        Sprite[] glow = PingPong(ImportStrip(Root + "Objects/WindTotem_Glow_9f.png", 9, ObjectPpu, new Vector2(0.5f, 0.04f)));
        var props = new GameObject("_Props");
        props.transform.SetParent(parent, false);
        var eyes = new List<Transform>();
        foreach (Vector2 position in TotemPositions)
        {
            var go = new GameObject("WindTotem");
            go.transform.SetParent(props.transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = totem;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 0;
            var body = go.AddComponent<BoxCollider2D>();
            body.size = new Vector2(1.5f, 1.2f);
            body.offset = new Vector2(0f, 0.55f);
            var loop = go.AddComponent<AmbientLoop>();
            var loopSerialized = new SerializedObject(loop);
            SetSprites(loopSerialized.FindProperty("_frames"), glow);
            loopSerialized.FindProperty("_frameRate").floatValue = 6f;
            loopSerialized.ApplyModifiedPropertiesWithoutUndo();
            var eye = new GameObject("EyePoint").transform;
            eye.SetParent(go.transform, false);
            eye.localPosition = new Vector3(0f, 3.7f, 0f);
            eyes.Add(eye);
        }

        var arena = Object.FindAnyObjectByType<BossArenaController>();
        var serialized = new SerializedObject(arena);
        SerializedProperty emitters = serialized.FindProperty("_ritualEmitters");
        emitters.arraySize = eyes.Count;
        for (int i = 0; i < eyes.Count; i++)
            emitters.GetArrayElementAtIndex(i).objectReferenceValue = eyes[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildWind(Transform parent)
    {
        var go = new GameObject("_ArenaWind");
        go.transform.SetParent(parent, false);
        go.AddComponent<ArenaWind>();
    }

    private static void BuildWeather(Transform parent)
    {
        var go = new GameObject("_SkyWeather");
        go.transform.SetParent(parent, false);
        var weather = go.AddComponent<SkyWeather>();
        var serialized = new SerializedObject(weather);
        Sprite puff = ImportSingle(Root + "Weather/Cloud_Puff.png", 24f, new Vector2(0.5f, 0.5f));
        Sprite streak = ImportSingle(Root + "Weather/Cloud_Streak.png", 24f, new Vector2(0.5f, 0.5f));
        SetSprites(serialized.FindProperty("_cloudSprites"), new[] { puff, streak, puff, streak, streak });
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ border, camera, spawns

    private static void ConfigureBorderAndCamera(Scene scene)
    {
        GameObject border = scene.GetRootGameObjects().First(g => g.name == "BorderMap");
        border.transform.position = Vector3.zero;

        // the camera must never show beyond the painted map: confine to the image (minus half a unit)
        Vector3 centre = ImageCentreWorld();
        var confinerBox = border.GetComponent<BoxCollider2D>();
        confinerBox.isTrigger = true;
        confinerBox.offset = new Vector2(centre.x, centre.y);
        confinerBox.size = new Vector2(MapImageSize.x / MapPpu - 1f, MapImageSize.y / MapPpu - 1f);

        var edge = border.GetComponent<EdgeCollider2D>();
        var points = new List<Vector2>(WalkPolygon) { WalkPolygon[0] };
        edge.points = points.ToArray();

        GameObject cm = scene.GetRootGameObjects().First(g => g.name == "CM Camera");
        var camera = cm.GetComponent<CinemachineCamera>();
        LensSettings lens = camera.Lens;
        lens.OrthographicSize = 12f;
        camera.Lens = lens;
        var confiner = cm.GetComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = confinerBox;
        confiner.InvalidateBoundingShapeCache();
    }

    private static void MoveSpawns(Scene scene)
    {
        GameObject context = scene.GetRootGameObjects().First(g => g.name == "_SceneContext");
        Move(context, "Spawn_ArenaEntry", new Vector3(0f, -13.2f, 0f));
        Move(context, "Spawn_BossCenter", new Vector3(0f, -2f, 0f));
        Move(context, "Spawn_ShrineAnchor", new Vector3(0f, -4.8f, 0f));
    }

    private static void Move(GameObject context, string name, Vector3 position)
    {
        Transform t = context.transform.Find(name);
        if (t != null)
            t.position = position;
    }

    /// <summary>The Player's dash is enabled per scene (PlayerDash._dashScenes); the Earth copy only lists DemoScene and Earth, so every boss arena adds itself.</summary>
    private static void EnableDash()
    {
        var dash = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        if (dash == null)
            return;

        var serialized = new SerializedObject(dash);
        SerializedProperty list = serialized.FindProperty("_dashScenes");
        foreach (string scene in new[] { "BossArena_Earth", "BossArena_Water", "BossArena_Wind" })
        {
            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
                present |= list.GetArrayElementAtIndex(i).stringValue == scene;
            if (!present)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = scene;
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath))
            return;

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
