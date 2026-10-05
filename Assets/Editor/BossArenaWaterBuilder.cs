using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds Assets/Scenes/BossArena_Water.unity (D-097/D-101): the scene starts as a copy of BossArena_Earth (so the per-scene
/// plumbing, player, camera, teleport pillar and skill-kit harness are wired identically), then the Earth world is replaced by
/// the beach: base map, animated sea strips, TideController and the crab arena controller. First playable version: summon
/// with F6 (no shrine/ritual yet). Re-running rebuilds the world part. Menu: Tools > Project Game > Boss.
/// </summary>
public static class BossArenaWaterBuilder
{
    private const string Root = "Assets/Art/BossArena/Water/";
    private const string ScenePath = "Assets/Scenes/BossArena_Water.unity";
    private const string SourceScenePath = "Assets/Scenes/BossArena_Earth.unity";

    // same world mapping as the Earth arena (1320x804 image, 20 px/unit, floor centre (660,440) = world (0,0))
    private const float MapPpu = 20f;
    private static readonly Vector2 MapImageSize = new Vector2(1320f, 804f);
    private static readonly Vector2 FloorCentrePx = new Vector2(660f, 440f);
    private const float WalkHalfWidth = 31.7f;
    private const float WalkBottom = -15.9f;
    private const float WalkTop = 11.25f; // image row ~215: the wet-sand band is walkable, the sea is not

    // geometry of the animated sea strips in the 1320x804 map (see Tools/arena_sea_animate.py)
    private const int Strips = 5;
    private const int StripHead = 74;
    private const int StripWidth = 202;
    private const int TileWidth = 200;
    private const int TileHeight = 236;
    private const int TailX = 1084;
    private const int TailWidth = 184;
    private static readonly int[] Phases = { 0, 3, 6, 2, 5 };

    private const string OrbIconPath = "Assets/Resources/Items/Materials/Icons/OrbWater.png";
    private const string OrbItemPath = "Assets/Resources/Items/Materials/OrbWater.asset";
    public const string OrbItemId = "item.material.orb_water";
    private const float ObjectPpu = 26f;

    [MenuItem("Tools/Project Game/Boss/Arena Water - Build Scene")]
    public static void BuildScene()
    {
        CrabBossBuilder.Build();
        ImportSprites();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            AssetDatabase.CopyAsset(SourceScenePath, ScenePath);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (string stale in new[] { "_ArenaEarth", "_ArenaWater", "BossShrineUI" })
        {
            GameObject old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == stale);
            if (old != null)
                Object.DestroyImmediate(old);
        }

        var arenaRoot = new GameObject("_ArenaWater");
        SceneManager.MoveGameObjectToScene(arenaRoot, scene);
        BuildBase(arenaRoot.transform);
        BuildSea(arenaRoot.transform);
        BuildTide(arenaRoot.transform);
        BuildArenaController(arenaRoot.transform);
        ConfigureBorder(scene);
        BossArenaEarthBuilder.BuildFootstepTilemap(arenaRoot.transform);

        // shrine (D-090) + ritual (D-091) for the crab: conch statue offered a Water Orb, four coral totems fire the beams
        EnableDash();
        ItemSO orb = EnsureOrbItem();
        BossShrineBuilder.Apply(scene, orb);
        BuildTotems(arenaRoot.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings();
        Debug.Log("BossArenaWaterBuilder: BossArena_Water built and saved.");
    }

    private static ItemSO EnsureOrbItem()
    {
        var importer = AssetImporter.GetAtPath(OrbIconPath) as TextureImporter;
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

        var item = AssetDatabase.LoadAssetAtPath<ItemSO>(OrbItemPath);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemSO>();
            AssetDatabase.CreateAsset(item, OrbItemPath);
        }

        item.itemId = OrbItemId;
        item.itemName = "Water Orb";
        item.description = "A cool deep-blue pearl with a living wave inside. Offer it at the conch shrine to call the tidal crab.";
        item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(OrbIconPath);
        item.type = ItemType.Material;
        item.isStackable = true;
        item.maxStackSize = 99;
        EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets();
        return item;
    }

    private static Sprite ImportSingle(string path, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ObjectPpu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
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

    private static void ImportSprites()
    {
        var importer = AssetImporter.GetAtPath(Root + "BaseMap_Water_Empty_1320x804.png") as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = MapPpu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        CrabBossBuilder.ConfigureSheet(Root + "Anim/SeaWave_Tile_8f.png", 8, MapPpu);
        CrabBossBuilder.ConfigureSheet(Root + "Anim/SeaWave_TailTile_8f.png", 8, MapPpu);
    }

    private static Vector3 ImageToWorld(float x, float y) => new Vector3((x - FloorCentrePx.x) / MapPpu, -(y - FloorCentrePx.y) / MapPpu, 0f);

    private static void BuildBase(Transform parent)
    {
        var map = new GameObject("Arena_Base");
        map.transform.SetParent(parent, false);
        map.transform.position = ImageToWorld(MapImageSize.x * 0.5f, MapImageSize.y * 0.5f);
        var renderer = map.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "BaseMap_Water_Empty_1320x804.png");
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = -200;
    }

    private static Sprite[] LoadFrames(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private static void BuildSea(Transform parent)
    {
        var sea = new GameObject("_SeaWaves");
        sea.transform.SetParent(parent, false);
        Sprite[] tile = LoadFrames(Root + "Anim/SeaWave_Tile_8f.png");
        Sprite[] tail = LoadFrames(Root + "Anim/SeaWave_TailTile_8f.png");

        for (int i = 0; i < Strips; i++)
        {
            bool flipped = i % 2 == 1;
            float x = StripHead + StripWidth * i + (flipped ? 2 : 1);
            AddWaveTile(sea.transform, "Wave_" + i, tile, ImageToWorld(x + TileWidth * 0.5f, TileHeight * 0.5f), flipped, Phases[i]);
        }

        AddWaveTile(sea.transform, "Wave_Tail", tail, ImageToWorld(TailX + TailWidth * 0.5f, TileHeight * 0.5f), false, 4);
    }

    private static void AddWaveTile(Transform parent, string name, Sprite[] frames, Vector3 position, bool flipX, int phase)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames.FirstOrDefault();
        renderer.flipX = flipX;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = -180;
        var loop = go.AddComponent<AmbientLoop>();
        var serialized = new SerializedObject(loop);
        SerializedProperty list = serialized.FindProperty("_frames");
        list.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        serialized.FindProperty("_frameRate").floatValue = 8f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        _ = phase; // AmbientLoop has no start offset: neighbours differ by their flip instead
    }

    private static void BuildTide(Transform parent)
    {
        var go = new GameObject("_Tide");
        go.transform.SetParent(parent, false);
        var tide = go.AddComponent<TideController>();
        var serialized = new SerializedObject(tide);
        serialized.FindProperty("_lowTideY").floatValue = WalkTop;
        serialized.FindProperty("_minX").floatValue = -WalkHalfWidth - 2f;
        serialized.FindProperty("_maxX").floatValue = WalkHalfWidth + 2f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildArenaController(Transform parent)
    {
        var holder = new GameObject("_BossArena");
        holder.transform.SetParent(parent, false);
        var spawn = new GameObject("BossSpawnPoint");
        spawn.transform.SetParent(holder.transform, false);
        spawn.transform.position = new Vector3(0f, -2f, 0f);

        // the conch statue in the middle of the beach (pivot = plinth base)
        Sprite conch = ImportSingle(Root + "Objects/ConchStatue.png", new Vector2(0.5f, 0.04f));
        var statue = new GameObject("SummonStatue");
        statue.transform.SetParent(holder.transform, false);
        statue.transform.position = new Vector3(0f, -3.2f, 0f);
        var statueRenderer = statue.AddComponent<SpriteRenderer>();
        statueRenderer.sprite = conch;
        statueRenderer.sortingLayerName = "Default";
        statueRenderer.sortingOrder = 0;
        var statueCollider = statue.AddComponent<BoxCollider2D>();
        statueCollider.size = new Vector2(3.6f, 1.6f);
        statueCollider.offset = new Vector2(0f, 0.9f);

        var controller = holder.AddComponent<BossArenaController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("_statueRenderer").objectReferenceValue = statueRenderer;
        serialized.FindProperty("_statueCollider").objectReferenceValue = statueCollider;
        serialized.FindProperty("_ritualBeamColor").colorValue = new Color(0.35f, 0.85f, 1f, 0.85f);
        serialized.FindProperty("_bossPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CrabBossBuilder.PrefabPath);
        serialized.FindProperty("_definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BossDefinition>(CrabBossBuilder.DefinitionPath);
        serialized.FindProperty("_bossSpawnPoint").objectReferenceValue = spawn.transform;
        serialized.FindProperty("_walkableBounds").rectValue = new Rect(-WalkHalfWidth, WalkBottom, WalkHalfWidth * 2f, WalkTop - WalkBottom);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var director = holder.AddComponent<BossCameraDirector>();
        var directorSerialized = new SerializedObject(director);
        var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
        directorSerialized.FindProperty("_camera").objectReferenceValue = vcam;
        directorSerialized.FindProperty("_confiner").objectReferenceValue = vcam != null ? vcam.GetComponent<CinemachineConfiner2D>() : null;
        directorSerialized.FindProperty("_arena").objectReferenceValue = controller;
        directorSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static readonly Vector2[] TotemPositions =
    {
        new Vector2(-24f, 6f), new Vector2(24f, 6f), new Vector2(-24f, -11f), new Vector2(24f, -11f),
    };

    /// <summary>Four coral totems around the conch; each has an EyePoint that fires a beam in the summon ritual (D-091).</summary>
    private static void BuildTotems(Transform parent)
    {
        Sprite totem = ImportSingle(Root + "Objects/CoralTotem.png", new Vector2(0.5f, 0.04f));
        var props = new GameObject("_Props");
        props.transform.SetParent(parent, false);
        var eyes = new List<Transform>();
        foreach (Vector2 position in TotemPositions)
        {
            var go = new GameObject("CoralTotem");
            go.transform.SetParent(props.transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = totem;
            renderer.flipX = position.x > 0f;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 0;
            var body = go.AddComponent<BoxCollider2D>();
            body.size = new Vector2(1.6f, 1.1f);
            body.offset = new Vector2(0f, 0.5f);
            var eye = new GameObject("EyePoint").transform;
            eye.SetParent(go.transform, false);
            eye.localPosition = new Vector3(position.x > 0f ? 0.1f : -0.1f, 2.55f, 0f);
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

    private static void ConfigureBorder(Scene scene)
    {
        GameObject border = scene.GetRootGameObjects().First(g => g.name == "BorderMap");
        var edge = border.GetComponent<EdgeCollider2D>();
        edge.points = new[]
        {
            new Vector2(-WalkHalfWidth, WalkBottom), new Vector2(-WalkHalfWidth, WalkTop),
            new Vector2(WalkHalfWidth, WalkTop), new Vector2(WalkHalfWidth, WalkBottom),
            new Vector2(-WalkHalfWidth, WalkBottom),
        };
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
