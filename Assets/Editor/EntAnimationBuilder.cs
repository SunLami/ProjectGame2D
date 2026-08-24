using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EntAnimationBuilder
{
    private const string SpriteFolder = "Assets/Sprites/EnemySprite/Top-Down Pixel Ent Character Sprites/PNG/Ent1/Without_shadow";
    private const string OutputFolder = "Assets/Animations/EntAnimations";
    private const int CellSize = 128;
    private const float FrameDuration = 0.1f;

    private sealed class StateDefinition
    {
        public string Name;
        public int FramesPerDirection;
        public bool Loop;
        public bool Attack;
    }

    private static readonly StateDefinition[] States =
    {
        new() { Name = "Idle", FramesPerDirection = 4, Loop = true },
        new() { Name = "Walk", FramesPerDirection = 6, Loop = true },
        new() { Name = "Run", FramesPerDirection = 8, Loop = true },
        new() { Name = "Attack", FramesPerDirection = 7, Attack = true },
        new() { Name = "Hurt", FramesPerDirection = 4 },
        new() { Name = "Death", FramesPerDirection = 6 }
    };

    private static readonly (string Name, int Row)[] Directions =
    {
        ("Down", 0),
        ("Up", 1),
        ("Left", 2),
        ("Right", 3)
    };

    [MenuItem("Tools/Ent/Rebuild Ent1 Animation Clips")]
    public static void Build()
    {
        EnsureFolder(OutputFolder);

        foreach (StateDefinition state in States)
        {
            string texturePath = $"{SpriteFolder}/Ent1_{state.Name}_without_shadow.png";
            SliceGrid(texturePath, state.FramesPerDirection, 4);
            Sprite[] sprites = LoadSprites(texturePath, state.FramesPerDirection * 4);

            foreach ((string direction, int row) in Directions)
                BuildClip(state, direction, row, sprites);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Ent1 animation build complete: 24 directional clips with virtual end frames.");
    }

    private static void SliceGrid(string texturePath, int columns, int rows)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Texture importer not found: {texturePath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        string baseName = System.IO.Path.GetFileNameWithoutExtension(texturePath);
        SpriteMetaData[] metadata = new SpriteMetaData[columns * rows];
        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                metadata[index] = new SpriteMetaData
                {
                    name = $"{baseName}_{index}",
                    rect = new Rect(
                        column * CellSize,
                        (rows - row - 1) * CellSize,
                        CellSize,
                        CellSize),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
                index++;
            }
        }

#pragma warning disable 618
        importer.spritesheet = metadata;
#pragma warning restore 618
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadSprites(string texturePath, int expectedCount)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => ParseTrailingIndex(sprite.name))
            .ToArray();

        if (sprites.Length != expectedCount)
            throw new InvalidOperationException(
                $"Expected {expectedCount} sprites at {texturePath}, found {sprites.Length}.");

        return sprites;
    }

    private static void BuildClip(
        StateDefinition state,
        string direction,
        int directionRow,
        Sprite[] sprites)
    {
        string path = $"{OutputFolder}/Ent_{state.Name}_{direction}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = $"Ent_{state.Name}_{direction}";
        clip.frameRate = 10f;

        int start = directionRow * state.FramesPerDirection;
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[state.FramesPerDirection + 1];
        for (int frame = 0; frame < state.FramesPerDirection; frame++)
        {
            keys[frame] = new ObjectReferenceKeyframe
            {
                time = frame * FrameDuration,
                value = sprites[start + frame]
            };
        }

        int virtualSource = state.Loop ? start : start + state.FramesPerDirection - 1;
        keys[^1] = new ObjectReferenceKeyframe
        {
            time = state.FramesPerDirection * FrameDuration,
            value = sprites[virtualSource]
        };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = state.Loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, BuildEvents(state));
        EditorUtility.SetDirty(clip);
    }

    private static AnimationEvent[] BuildEvents(StateDefinition state)
    {
        if (!state.Attack)
            return Array.Empty<AnimationEvent>();

        float lastRealFrameTime = (state.FramesPerDirection - 1) * FrameDuration;
        float virtualFrameTime = state.FramesPerDirection * FrameDuration;
        return new[]
        {
            new AnimationEvent
            {
                time = Mathf.Max(FrameDuration, (state.FramesPerDirection / 2) * FrameDuration),
                functionName = "OpenAttackWindow"
            },
            new AnimationEvent
            {
                time = lastRealFrameTime,
                functionName = "CloseAttackHitbox"
            },
            new AnimationEvent
            {
                time = virtualFrameTime,
                functionName = "FinishAttackAnimation"
            }
        };
    }

    private static int ParseTrailingIndex(string name)
    {
        int separator = name.LastIndexOf('_');
        return separator >= 0 && int.TryParse(name[(separator + 1)..], out int index)
            ? index
            : int.MaxValue;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
