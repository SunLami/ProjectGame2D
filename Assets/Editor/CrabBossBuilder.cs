using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Builds the Water crab boss prefab + BossDefinition asset (D-099/D-101): imports the crab clip sheets
/// (Assets/Art/BossArena/Water/Boss/Anim), fills a BossClipPlayer (Idle/Move AI clips, Snap/Recovery layered clips and
/// Burrow/Emerge/Spit/Whirl derived from them for now) and wires BossController. Idempotent. Menu: Tools > Project Game > Boss.</summary>
public static class CrabBossBuilder
{
    public const string Folder = "Assets/Bosses/WaterCrab";
    public const string PrefabPath = Folder + "/WaterCrabBoss.prefab";
    public const string DefinitionPath = Folder + "/WaterCrabBoss.asset";
    private const string AnimFolder = "Assets/Art/BossArena/Water/Boss/Anim/";
    private const float Ppu = 32f;

    [MenuItem("Tools/Project Game/Boss/Build Water Crab Prefab")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        foreach (string sheet in new[] { "Crab_Idle_9f", "Crab_Move_9f", "Crab_Snap_9f", "Crab_Recovery_9f", "Crab_Burrow_9f" })
            ConfigureSheet(AnimFolder + sheet + ".png", 9);

        var definition = AssetDatabase.LoadAssetAtPath<BossDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = BossDefinition.CreateWaterCrabDefaults();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }

        // refresh the Water-specific presentation of an asset created by an earlier build
        definition.impactVfx = "Water/WaterRainSplash_Hit";
        definition.phaseVfx = "Water/WaterGatherRing_Swirl";
        EditorUtility.SetDirty(definition);

        Sprite[] idle = LoadFrames("Crab_Idle_9f");
        Sprite[] move = LoadFrames("Crab_Move_9f");
        Sprite[] snap = LoadFrames("Crab_Snap_9f");
        Sprite[] recovery = LoadFrames("Crab_Recovery_9f");
        Sprite[] burrow = LoadFrames("Crab_Burrow_9f");

        var root = new GameObject("WaterCrabBoss");
        root.transform.localScale = Vector3.one * 0.75f;
        var body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        var renderer = body.AddComponent<SpriteRenderer>();
        renderer.sprite = idle.FirstOrDefault();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 5;

        var player = body.AddComponent<BossClipPlayer>();
        FillClips(player, new[]
        {
            Clip(BossClipId.Idle, idle, 8f, true, -1),
            Clip(BossClipId.Move, move, 9f, true, -1),
            Clip(BossClipId.Snap, snap, 12f, false, 4),
            Clip(BossClipId.Recovery, recovery, 8f, false, 6),
            Clip(BossClipId.Burrow, burrow, 12f, false, 7), // sinks into the sand, holds almost gone while the mound chases
            Clip(BossClipId.Emerge, burrow.Reverse().ToArray(), 12f, false, -1),
            // still borrowed from Snap (dedicated Spit/Whirl clips are a later polish):
            Clip(BossClipId.Spit, snap.Take(4).ToArray(), 10f, false, 3),
            Clip(BossClipId.Whirl, snap.Take(5).ToArray(), 9f, false, 4),
        });

        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(root.transform, false);
        shadow.transform.localPosition = new Vector3(0f, -3.2f, 0f);
        shadow.transform.localScale = new Vector3(6f, 1.6f, 1f);
        var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = BossTelegraph.CircleSprite;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.35f);
        shadowRenderer.sortingLayerName = "Default";
        shadowRenderer.sortingOrder = -50;

        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        var hurtbox = root.AddComponent<CircleCollider2D>();
        hurtbox.isTrigger = true;
        hurtbox.radius = 2.8f;
        hurtbox.offset = new Vector2(0f, 0.2f);
        var solidHurtbox = root.AddComponent<CircleCollider2D>(); // found by overlap-based player skills, pushes nobody
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

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = enemyLayer;
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("CrabBossBuilder: built " + PrefabPath);
    }

    private readonly struct ClipSpec
    {
        public readonly BossClipId Id;
        public readonly Sprite[] Frames;
        public readonly float Fps;
        public readonly bool Loop;
        public readonly int Hold;

        public ClipSpec(BossClipId id, Sprite[] frames, float fps, bool loop, int hold)
        {
            Id = id;
            Frames = frames;
            Fps = fps;
            Loop = loop;
            Hold = hold;
        }
    }

    private static ClipSpec Clip(BossClipId id, Sprite[] frames, float fps, bool loop, int hold) => new ClipSpec(id, frames, fps, loop, hold);

    private static void FillClips(BossClipPlayer player, ClipSpec[] specs)
    {
        var serialized = new SerializedObject(player);
        serialized.FindProperty("_renderer").objectReferenceValue = player.GetComponent<SpriteRenderer>();
        SerializedProperty list = serialized.FindProperty("_clips");
        list.arraySize = specs.Length;
        for (int i = 0; i < specs.Length; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("id").enumValueIndex = (int)specs[i].Id;
            element.FindPropertyRelative("fps").floatValue = specs[i].Fps;
            element.FindPropertyRelative("loop").boolValue = specs[i].Loop;
            element.FindPropertyRelative("holdFrame").intValue = specs[i].Hold;
            SerializedProperty frames = element.FindPropertyRelative("frames");
            frames.arraySize = specs[i].Frames.Length;
            for (int f = 0; f < specs[i].Frames.Length; f++)
                frames.GetArrayElementAtIndex(f).objectReferenceValue = specs[i].Frames[f];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite[] LoadFrames(string sheet)
    {
        return AssetDatabase.LoadAllAssetsAtPath(AnimFolder + sheet + ".png").OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    internal static void ConfigureSheet(string path, int frames, float ppu = Ppu)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("CrabBossBuilder: missing " + path);
            return;
        }

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
        if (texture == null)
            return;

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
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate(),
            };
        }

        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
    }
}
