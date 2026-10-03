using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Imports the Earth golem animation sheets (9 frames of 256x256 each, D-092) and fills a
/// <see cref="BossClipPlayer"/> with them. Called by BossPrefabBuilder; also usable on its own through the
/// menu to re-slice after a sheet changed. Sheets come from Tools/fetch_boss_clip.py.</summary>
public static class BossClipSetup
{
    private const string AnimFolder = "Assets/Art/BossArena/Earth/Boss/Anim/";
    private const float Ppu = 32f; // same as the still sprite

    private readonly struct ClipInfo
    {
        public readonly BossClipId Id;
        public readonly string Sheet;
        public readonly float Fps;
        public readonly bool Loop;
        public readonly int Hold;

        public ClipInfo(BossClipId id, string sheet, float fps, bool loop, int hold)
        {
            Id = id;
            Sheet = sheet;
            Fps = fps;
            Loop = loop;
            Hold = hold;
        }
    }

    // hold = frame the clip waits on until the skill releases it (-1: plays straight through)
    private static readonly ClipInfo[] Clips =
    {
        new ClipInfo(BossClipId.Idle, "Idle_9f", 8f, true, -1),
        new ClipInfo(BossClipId.Move, "Move_9f", 9f, true, -1),
        new ClipInfo(BossClipId.Slam, "Slam_9f", 14f, false, 4),
        new ClipInfo(BossClipId.Raise, "Raise_9f", 12f, false, 5),
        new ClipInfo(BossClipId.Stomp, "Stomp_9f", 14f, false, 4),
        new ClipInfo(BossClipId.Fist, "Fist_9f", 14f, false, 5),
        new ClipInfo(BossClipId.Resonance, "Resonance_9f", 10f, false, 6),
        new ClipInfo(BossClipId.Recovery, "Recovery_9f", 8f, false, 6),
    };

    private const string FistSheetPath = "Assets/Resources/VFX/Skills/Earth/RockFist_Fly.png";

    /// <summary>The flying stone fist of Rocket Fist (9 frames of 96x64, knuckles pointing +X).</summary>
    [MenuItem("Tools/Project Game/Boss/Import Rocket Fist Sprite")]
    public static void ImportFistSheet()
    {
        ConfigureSheet(FistSheetPath, 9);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Project Game/Boss/Import Earth Golem Clips")]
    public static void ImportSheets()
    {
        ImportFistSheet();
        foreach (ClipInfo clip in Clips)
            ConfigureSheet(AnimFolder + clip.Sheet + ".png", 9);
        AssetDatabase.SaveAssets();
        Debug.Log("BossClipSetup: clip sheets imported/sliced.");
    }

    public static void Fill(BossClipPlayer player)
    {
        ImportSheets();
        var serialized = new SerializedObject(player);
        serialized.FindProperty("_renderer").objectReferenceValue = player.GetComponent<SpriteRenderer>();
        SerializedProperty list = serialized.FindProperty("_clips");
        list.arraySize = Clips.Length;
        for (int i = 0; i < Clips.Length; i++)
        {
            ClipInfo info = Clips[i];
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("id").enumValueIndex = (int)info.Id;
            element.FindPropertyRelative("fps").floatValue = info.Fps;
            element.FindPropertyRelative("loop").boolValue = info.Loop;
            element.FindPropertyRelative("holdFrame").intValue = info.Hold;

            Sprite[] frames = LoadFrames(AnimFolder + info.Sheet + ".png");
            SerializedProperty frameList = element.FindPropertyRelative("frames");
            frameList.arraySize = frames.Length;
            for (int f = 0; f < frames.Length; f++)
                frameList.GetArrayElementAtIndex(f).objectReferenceValue = frames[f];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite[] LoadFrames(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private static void ConfigureSheet(string path, int frames)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("BossClipSetup: missing " + path);
            return;
        }

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = Ppu;
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
