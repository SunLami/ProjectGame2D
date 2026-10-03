using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds Assets/Scenes/BossArena_Earth.unity (D-087). The scene starts as a copy of MapNhat so the
/// per-scene plumbing (_SceneContext, Player, Cinemachine camera, readiness gate) is wired exactly like a
/// production scene; the world is replaced by the Earth arena: base map, summoning statue + floor seal,
/// decor along the edges (never in the combat area), environment motion, colliders, camera confiner.
/// Re-running rebuilds the world part. Menu: Tools > Project Game > Boss.
/// </summary>
public static class BossArenaEarthBuilder
{
    private const string Root = "Assets/Art/BossArena/Earth/";
    private const string ScenePath = "Assets/Scenes/BossArena_Earth.unity";
    private const string SourceScenePath = "Assets/Scenes/MapNhat.unity";
    private const string BossPrefabPath = "Assets/Bosses/EarthGolem/EarthGolemBoss.prefab";
    private const string BossDefinitionPath = "Assets/Bosses/EarthGolem/EarthGolemBoss.asset";

    // World mapping of the 1320x804 base map (D-088, rebalanced): 20 px per unit => walkable area 63.6 x 32 units
    // (about 1.5 screens wide at the base camera size), floor centre (660,440) = world (0,0).
    private const float MapPpu = 20f;
    // Decor/spawn positions below were authored for the old 9 PPU map; this brings them to the current scale.
    private const float DecorScale = 9f / 20f;
    private static readonly Vector2 MapImageSize = new Vector2(1320f, 804f);
    private static readonly Vector2 FloorCentrePx = new Vector2(660f, 440f);
    private const float WalkHalfWidth = 31.7f;
    private const float WalkBottom = -15.9f;
    private const float WalkTop = 15.7f;

    private struct Sheet
    {
        public string Path; public float Ppu; public Vector2 Pivot; public int Frames;
        public Sheet(string path, float ppu, Vector2 pivot, int frames) { Path = path; Ppu = ppu; Pivot = pivot; Frames = frames; }
    }

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 Bottom = new Vector2(0.5f, 0.05f);

    private static readonly Sheet[] Sheets =
    {
        new Sheet("BaseMap_Earth_Empty_1320x804.png", MapPpu, Center, 1),
        new Sheet("Objects/FloorSeal.png", 16f, Center, 1),
        new Sheet("Objects/FloorSeal_RuneGlow_8f.png", 16f, Center, 8),
        new Sheet("Objects/Boulders.png", 26f, Bottom, 1),
        new Sheet("Objects/BrokenPillar.png", 26f, Bottom, 1),
        new Sheet("Objects/Anim/Brazier_Flame_9f.png", 26f, Bottom, 9),
        new Sheet("Objects/Anim/Banner_Sway_9f.png", 30f, Center, 9),
        new Sheet("Objects/Anim/Vines_Sway_9f.png", 30f, new Vector2(0.5f, 0.95f), 9),
        new Sheet("Objects/Anim/GrassTuft_Sway_9f.png", 26f, new Vector2(0.5f, 0.2f), 9),
        new Sheet("Objects/Anim/GolemStatue_RunePulse_9f.png", 26f, Bottom, 9),
        new Sheet("Objects/Guardian/GuardianStatue_FaceSE.png", 26f, Bottom, 1),
        new Sheet("Objects/Guardian/GuardianStatue_FaceNE.png", 26f, Bottom, 1),
        new Sheet("Objects/Summon/SummonStatue_Idle_9f.png", 20f, new Vector2(0.5f, 0f), 9),
        new Sheet("Objects/Summon/SummonStatue_Vanish_17f.png", 20f, new Vector2(0.5f, 0f), 17),
        new Sheet("Objects/Summon/DustPuff_Anim_9f.png", 20f, new Vector2(0.5f, 0.2f), 9),
        new Sheet("Objects/Weather/SandGust_Anim_9f.png", 16f, Center, 9),
        new Sheet("Objects/Weather/Tumbleweed_Roll_9f.png", 24f, Center, 9),
    };

    // ------------------------------------------------------------------ sprite import

    [MenuItem("Tools/Project Game/Boss/Arena Earth - 1 Import Sprites")]
    public static void ImportSprites()
    {
        foreach (Sheet sheet in Sheets)
            ConfigureSheet(Root + sheet.Path, sheet);
        AssetDatabase.SaveAssets();
        Debug.Log("BossArenaEarthBuilder: sprites imported/sliced (" + Sheets.Length + " sheets).");
    }

    private static void ConfigureSheet(string path, Sheet sheet)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("BossArenaEarthBuilder: missing " + path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = sheet.Ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.spriteImportMode = sheet.Frames <= 1 ? SpriteImportMode.Single : SpriteImportMode.Multiple;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            return;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        if (sheet.Frames <= 1)
        {
            SpriteRect[] rects = provider.GetSpriteRects();
            if (rects.Length == 0)
                return;

            rects[0].alignment = SpriteAlignment.Custom;
            rects[0].pivot = sheet.Pivot;
            provider.SetSpriteRects(rects);
        }
        else
        {
            int frameWidth = texture.width / sheet.Frames;
            string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            var rects = new SpriteRect[sheet.Frames];
            for (int i = 0; i < sheet.Frames; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = baseName + "_" + i,
                    rect = new Rect(i * frameWidth, 0, frameWidth, texture.height),
                    alignment = SpriteAlignment.Custom,
                    pivot = sheet.Pivot,
                    spriteID = GUID.Generate(),
                };
            }

            provider.SetSpriteRects(rects);
        }

        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadFrames(string relativePath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Root + relativePath).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private static Sprite LoadSingle(string relativePath) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + relativePath);

    // ------------------------------------------------------------------ scene build

    [MenuItem("Tools/Project Game/Boss/Arena Earth - 2 Build Scene")]
    public static void BuildScene()
    {
        ImportSprites();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            AssetDatabase.CopyAsset(SourceScenePath, ScenePath);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        StripMapNhatContent(scene);

        GameObject old = GameObject.Find("_ArenaEarth");
        if (old != null)
            Object.DestroyImmediate(old);

        var arenaRoot = new GameObject("_ArenaEarth");
        SceneManager.MoveGameObjectToScene(arenaRoot, scene);

        BuildBase(arenaRoot.transform);
        Transform props = new GameObject("_Props").transform;
        props.SetParent(arenaRoot.transform, false);
        BuildProps(props);
        BuildSummonStatue(arenaRoot.transform);
        BuildWeather(arenaRoot.transform);
        BuildSpawnPoints(scene, arenaRoot.transform);
        ConfigureBorderAndCamera(scene);
        BuildFootstepTilemap(arenaRoot.transform);
        MovePlayerToEntry(scene);
        BossShrineBuilder.Apply(scene); // statue becomes the clickable shrine (D-090)
        BossRitualBuilder.Apply(scene); // guardian statues fire the summon lasers (D-091)

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings();
        Debug.Log("BossArenaEarthBuilder: BossArena_Earth built and saved.");
    }

    private static void StripMapNhatContent(Scene scene)
    {
        string[] remove =
        {
            "Forest_MainMap", "Scene Fusion Guids", "Wooden_FishingRod", "FishingFeature", "FarmingFeature",
            "NorthExitToMapDuy",
        };

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (remove.Contains(root.name))
            {
                Object.DestroyImmediate(root);
                continue;
            }

            if (root.name == "_SceneContext")
            {
                var quest = root.GetComponent<TrainingAreaQuestFlow>();
                if (quest != null)
                    Object.DestroyImmediate(quest);
            }
        }
    }

    private static void BuildBase(Transform parent)
    {
        Vector2 imageCentreOffset = (MapImageSize * 0.5f - FloorCentrePx) / MapPpu; // image centre relative to floor centre (px y is down)
        var map = new GameObject("Arena_Base");
        map.transform.SetParent(parent, false);
        map.transform.position = new Vector3(0f, -imageCentreOffset.y, 0f);
        var renderer = map.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSingle("BaseMap_Earth_Empty_1320x804.png");
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = -200;

        var seal = new GameObject("Arena_FloorSeal");
        seal.transform.SetParent(parent, false);
        var sealRenderer = seal.AddComponent<SpriteRenderer>();
        sealRenderer.sprite = LoadSingle("Objects/FloorSeal.png");
        sealRenderer.sortingLayerName = "Default";
        sealRenderer.sortingOrder = -190;

        var glow = new GameObject("Arena_FloorSealGlow");
        glow.transform.SetParent(seal.transform, false);
        var glowRenderer = glow.AddComponent<SpriteRenderer>();
        Sprite[] glowFrames = LoadFrames("Objects/FloorSeal_RuneGlow_8f.png");
        glowRenderer.sprite = glowFrames.FirstOrDefault();
        glowRenderer.sortingLayerName = "Default";
        glowRenderer.sortingOrder = -189;
        glowRenderer.color = new Color(1f, 1f, 1f, 0.45f);
    }

    private static void BuildProps(Transform props)
    {
        Sprite[] brazier = LoadFrames("Objects/Anim/Brazier_Flame_9f.png");
        Sprite[] banner = LoadFrames("Objects/Anim/Banner_Sway_9f.png");
        Sprite[] vines = LoadFrames("Objects/Anim/Vines_Sway_9f.png");
        Sprite[] grass = LoadFrames("Objects/Anim/GrassTuft_Sway_9f.png");
        Sprite[] golem = LoadFrames("Objects/Anim/GolemStatue_RunePulse_9f.png");

        // Decor sits on the wall / in the corners only: the combat area in the middle stays empty.
        foreach (float x in new[] { -51.6f, -33.3f, -14.8f, 14.8f, 33f, 51.4f })
            Decor(props, "Banner", banner, new Vector2(x, 40.6f), -150, 8f);
        foreach (float x in new[] { -27f, 27f })
            Decor(props, "Vines", vines, new Vector2(x, 44.5f), -149, 7f);

        foreach (float x in new[] { -24f, 24f })
        {
            Decor(props, "Brazier", brazier, new Vector2(x, 33.6f), 0, 9f);
            Decor(props, "Brazier", brazier, new Vector2(x, -34.2f), 0, 9f);
        }

        // golem guardian statues in the four corners, facing inwards
        foreach (Vector2 corner in new[] { new Vector2(-62.8f, 27.8f), new Vector2(62.8f, 27.8f), new Vector2(-62.8f, -30f), new Vector2(62.8f, -30f) })
        {
            GameObject statue = Decor(props, "GuardianStatue", golem, corner, 0, 6f);
            if (corner.x > 0f)
                statue.GetComponent<SpriteRenderer>().flipX = true;
            var body = statue.AddComponent<BoxCollider2D>();
            body.size = new Vector2(2f, 0.9f);
            body.offset = new Vector2(0f, 0.45f);
        }

        // corner clutter inside the grass patches
        Decor(props, "Boulders", new[] { LoadSingle("Objects/Boulders.png") }, new Vector2(-67f, -31f), 0, 1f);
        Decor(props, "Boulders", new[] { LoadSingle("Objects/Boulders.png") }, new Vector2(67f, -31f), 0, 1f);
        Decor(props, "BrokenPillar", new[] { LoadSingle("Objects/BrokenPillar.png") }, new Vector2(-67f, 19f), 0, 1f);
        Decor(props, "BrokenPillar", new[] { LoadSingle("Objects/BrokenPillar.png") }, new Vector2(67f, 19f), 0, 1f);
        foreach (Vector2 p in new[] { new Vector2(-68f, 30f), new Vector2(68f, 30f), new Vector2(-69f, 8f), new Vector2(69f, 8f), new Vector2(-69f, -14f), new Vector2(69f, -14f), new Vector2(-60f, -33f), new Vector2(60f, -33f) })
            Decor(props, "GrassTuft", grass, p, 0, 7f);
    }

    private static GameObject Decor(Transform parent, string name, Sprite[] frames, Vector2 position, int order, float frameRate)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position * DecorScale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames.FirstOrDefault();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        if (frames.Length > 1)
        {
            var loop = go.AddComponent<AmbientLoop>();
            var serialized = new SerializedObject(loop);
            SetSprites(serialized.FindProperty("_frames"), frames);
            serialized.FindProperty("_frameRate").floatValue = frameRate;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        return go;
    }

    private static void SetSprites(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    private static void BuildSummonStatue(Transform parent)
    {
        var holder = new GameObject("_BossArena");
        holder.transform.SetParent(parent, false);

        Sprite[] idle = LoadFrames("Objects/Summon/SummonStatue_Idle_9f.png");
        Sprite[] vanish = LoadFrames("Objects/Summon/SummonStatue_Vanish_17f.png");
        Sprite[] dust = LoadFrames("Objects/Summon/DustPuff_Anim_9f.png");

        var statue = new GameObject("SummonStatue");
        statue.transform.SetParent(holder.transform, false);
        statue.transform.position = new Vector3(0f, -1.4f, 0f); // pivot is the plinth base; the body sits over the seal centre
        var renderer = statue.AddComponent<SpriteRenderer>();
        renderer.sprite = idle.FirstOrDefault();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 0;
        var collider = statue.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(3.4f, 1.6f);
        collider.offset = new Vector2(0f, 0.9f);

        var spawn = new GameObject("BossSpawnPoint");
        spawn.transform.SetParent(holder.transform, false);
        spawn.transform.position = Vector3.zero;

        var controller = holder.AddComponent<BossArenaController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("_bossPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        serialized.FindProperty("_definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BossDefinition>(BossDefinitionPath);
        serialized.FindProperty("_bossSpawnPoint").objectReferenceValue = spawn.transform;
        serialized.FindProperty("_walkableBounds").rectValue = new Rect(-WalkHalfWidth, WalkBottom, WalkHalfWidth * 2f, WalkTop - WalkBottom);
        serialized.FindProperty("_statueRenderer").objectReferenceValue = renderer;
        serialized.FindProperty("_statueCollider").objectReferenceValue = collider;
        SetSprites(serialized.FindProperty("_statueIdleFrames"), idle);
        SetSprites(serialized.FindProperty("_statueVanishFrames"), vanish);
        SetSprites(serialized.FindProperty("_dustFrames"), dust);
        GameObject glow = GameObject.Find("Arena_FloorSealGlow");
        serialized.FindProperty("_sealGlow").objectReferenceValue = glow != null ? glow.GetComponent<SpriteRenderer>() : null;
        SetSprites(serialized.FindProperty("_sealGlowFrames"), LoadFrames("Objects/FloorSeal_RuneGlow_8f.png"));
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // camera framing for the fight (follow a point between player and boss, zoom to fit both)
        var director = holder.AddComponent<BossCameraDirector>();
        var directorSerialized = new SerializedObject(director);
        var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
        directorSerialized.FindProperty("_camera").objectReferenceValue = vcam;
        directorSerialized.FindProperty("_confiner").objectReferenceValue = vcam != null ? vcam.GetComponent<CinemachineConfiner2D>() : null;
        directorSerialized.FindProperty("_arena").objectReferenceValue = controller;
        directorSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildWeather(Transform parent)
    {
        var go = new GameObject("_Weather");
        go.transform.SetParent(parent, false);
        var weather = go.AddComponent<ArenaWeather>();
        var serialized = new SerializedObject(weather);
        SetSprites(serialized.FindProperty("_gustFrames"), LoadFrames("Objects/Weather/SandGust_Anim_9f.png"));
        SetSprites(serialized.FindProperty("_tumbleFrames"), LoadFrames("Objects/Weather/Tumbleweed_Roll_9f.png"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildSpawnPoints(Scene scene, Transform arenaRoot)
    {
        GameObject context = scene.GetRootGameObjects().First(g => g.name == "_SceneContext");
        Transform start = context.transform.childCount > 0 ? context.transform.GetChild(0) : new GameObject().transform;
        start.SetParent(context.transform, false);
        start.name = "Spawn_ArenaEntry";
        start.position = new Vector3(0f, -14f, 0f);

        // re-running the builder must not stack duplicate spawn points
        foreach (string stale in new[] { "Spawn_BossCenter", "Spawn_ShrineAnchor" })
        {
            Transform old = context.transform.Find(stale);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        Transform bossSpawn = Create(context.transform, "Spawn_BossCenter", Vector3.zero);
        Transform shrine = Create(context.transform, "Spawn_ShrineAnchor", new Vector3(0f, -1.4f, 0f));

        var registry = context.GetComponent<SpawnRegistry>();
        var serialized = new SerializedObject(registry);
        SerializedProperty entries = serialized.FindProperty("_entries");
        entries.arraySize = 3;
        SetEntry(entries.GetArrayElementAtIndex(0), "arena_entry", start);
        SetEntry(entries.GetArrayElementAtIndex(1), "boss_spawn", bossSpawn);
        SetEntry(entries.GetArrayElementAtIndex(2), "shrine_anchor", shrine);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform Create(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go.transform;
    }

    private static void SetEntry(SerializedProperty entry, string id, Transform point)
    {
        entry.FindPropertyRelative("spawnId").stringValue = id;
        entry.FindPropertyRelative("point").objectReferenceValue = point;
    }

    private static void ConfigureBorderAndCamera(Scene scene)
    {
        GameObject border = scene.GetRootGameObjects().First(g => g.name == "BorderMap");
        border.transform.position = Vector3.zero;

        // image world extents (camera must never show beyond the painted map)
        float halfWidth = MapImageSize.x / MapPpu * 0.5f - 0.5f;
        float centreY = (MapImageSize.y * 0.5f - FloorCentrePx.y) / MapPpu * -1f;
        float halfHeight = MapImageSize.y / MapPpu * 0.5f - 0.5f;

        var confinerBox = border.GetComponent<BoxCollider2D>();
        confinerBox.isTrigger = true;
        confinerBox.offset = new Vector2(0f, centreY);
        confinerBox.size = new Vector2(halfWidth * 2f, halfHeight * 2f);

        var edge = border.GetComponent<EdgeCollider2D>();
        edge.points = new[]
        {
            new Vector2(-WalkHalfWidth, WalkBottom), new Vector2(-WalkHalfWidth, WalkTop),
            new Vector2(WalkHalfWidth, WalkTop), new Vector2(WalkHalfWidth, WalkBottom),
            new Vector2(-WalkHalfWidth, WalkBottom),
        };

        GameObject cm = scene.GetRootGameObjects().First(g => g.name == "CM Camera");
        var camera = cm.GetComponent<CinemachineCamera>();
        LensSettings lens = camera.Lens;
        lens.OrthographicSize = 12f;
        camera.Lens = lens;
        var confiner = cm.GetComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = confinerBox;
        confiner.InvalidateBoundingShapeCache();
    }

    internal static void BuildFootstepTilemap(Transform parent)
    {
        // MapManager picks the footstep clip from the tile under the player's feet; an invisible tilemap
        // of rock tiles gives the arena stone footsteps instead of a null-reference.
        TileBase rock = null;
        foreach (string guid in AssetDatabase.FindAssets("t:TileDataSO"))
        {
            var data = AssetDatabase.LoadAssetAtPath<TileDataSO>(AssetDatabase.GUIDToAssetPath(guid));
            rock = data.tiles?.FirstOrDefault(t => t != null);
            if (rock != null)
                break;
        }

        var gridObject = new GameObject("FootstepGrid");
        gridObject.transform.SetParent(parent, false);
        gridObject.AddComponent<Grid>();
        var tilemapObject = new GameObject("FootstepTilemap");
        tilemapObject.transform.SetParent(gridObject.transform, false);
        var tilemap = tilemapObject.AddComponent<Tilemap>();

        if (rock != null)
        {
            var bounds = new BoundsInt(-36, -20, 0, 72, 40, 1);
            var tiles = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = rock;
            tilemap.SetTilesBlock(bounds, tiles);
        }

        MapManager manager = Object.FindAnyObjectByType<MapManager>();
        if (manager != null)
        {
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("_tilemap").objectReferenceValue = tilemap;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void MovePlayerToEntry(Scene scene)
    {
        Player player = Object.FindAnyObjectByType<Player>();
        if (player != null)
            player.transform.position = new Vector3(0f, -14f, 0f);
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
