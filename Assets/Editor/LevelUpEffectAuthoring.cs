using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelUpEffectAuthoring
{
    private const string SpriteFolder = "Assets/Sprites/VFX/LevelUpEffect";
    private const string FullSheetPath = SpriteFolder + "/pipo-mapeffect013a.png";
    private const string BackSheetPath = SpriteFolder + "/pipo-mapeffect013a-back.png";
    private const string FrontSheetPath = SpriteFolder + "/pipo-mapeffect013a-front.png";
    private const string BellSheetPath = SpriteFolder + "/pipo-btleffect219_192.png";
    private const string FontPath = "Assets/Fonts/DigitalDisco SDF v3.asset";
    private const string PrefabPath = "Assets/Prefabs/VFX/LevelUpEffect.prefab";
    private const int PlayerSortingLayerId = unchecked((int)2304662899);
    private const int Columns = 5;
    private const int Rows = 2;
    private const int FrameSize = 400;

    [MenuItem("Tools/Project Game/VFX/Build Yellow SpriteMask Level Up Effect")]
    public static void Build()
    {
        Sprite[] maskFrames = ImportSheet(FullSheetPath, "LevelUp_Mask");
        Sprite[] backFrames = ImportSheet(BackSheetPath, "LevelUp_Back");
        Sprite[] frontFrames = ImportSheet(FrontSheetPath, "LevelUp_Front");
        Sprite[] bellFrames = ImportBellSheet();
        GameObject prefab = BuildPrefab(backFrames, frontFrames, maskFrames, bellFrames);
        IntegrateScene("Assets/Scenes/DemoScene.unity", prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("Yellow SpriteMask level-up VFX built and integrated into DemoScene.");
    }

    [MenuItem("Tools/Project Game/VFX/Remove Level Up Effect From MapNhat")]
    public static void RemoveFromMapNhat()
    {
        RemoveFromScene("Assets/Scenes/MapNhat.unity");
        Debug.Log("Level-up VFX removed from MapNhat.");
    }

    private static Sprite[] ImportSheet(string path, string prefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"Missing VFX sheet: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 200f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        SpriteMetaData[] slices = new SpriteMetaData[Columns * Rows];
        for (int row = 0; row < Rows; row++)
        for (int column = 0; column < Columns; column++)
        {
            int index = row * Columns + column;
            slices[index] = new SpriteMetaData
            {
                name = $"{prefix}_{index + 1:00}",
                rect = new Rect(column * FrameSize, (Rows - row - 1) * FrameSize, FrameSize, FrameSize),
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.275f)
            };
        }
#pragma warning disable CS0618
        importer.spritesheet = slices;
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
    }

    private static Sprite[] ImportBellSheet()
    {
        TextureImporter importer = AssetImporter.GetAtPath(BellSheetPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"Missing bell VFX sheet: {BellSheetPath}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 192f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        SpriteMetaData[] slices = new SpriteMetaData[15];
        for (int row = 0; row < 3; row++)
        for (int column = 0; column < 5; column++)
        {
            int index = row * 5 + column;
            slices[index] = new SpriteMetaData
            {
                name = $"LevelUp_Bell_{index + 1:00}",
                rect = new Rect(column * 192, (2 - row) * 192, 192, 192),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
#pragma warning disable CS0618
        importer.spritesheet = slices;
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(BellSheetPath).OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
    }

    private static GameObject BuildPrefab(Sprite[] backFrames, Sprite[] frontFrames, Sprite[] maskFrames, Sprite[] bellFrames)
    {
        if (backFrames.Length != 10 || frontFrames.Length != 10 || maskFrames.Length != 10 || bellFrames.Length != 15)
            throw new InvalidOperationException("Pipoya level-up sheets contain an unexpected frame count.");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/VFX"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");

        GameObject root = new("LevelUpFX");
        root.transform.localScale = new Vector3(1.6f, 1.35f, 1f);
        GameObject backObject = new("Back");
        backObject.transform.SetParent(root.transform, false);
        SpriteRenderer back = backObject.AddComponent<SpriteRenderer>();
        back.sortingLayerID = PlayerSortingLayerId;
        back.sortingOrder = -100;

        GameObject maskObject = new("FrontMask");
        maskObject.transform.SetParent(root.transform, false);
        SpriteMask mask = maskObject.AddComponent<SpriteMask>();
        mask.alphaCutoff = 0.05f;
        mask.isCustomRangeActive = true;
        mask.frontSortingLayerID = PlayerSortingLayerId;
        mask.frontSortingOrder = 101;
        mask.backSortingLayerID = PlayerSortingLayerId;
        mask.backSortingOrder = 99;

        GameObject frontObject = new("Front");
        frontObject.transform.SetParent(root.transform, false);
        SpriteRenderer front = frontObject.AddComponent<SpriteRenderer>();
        front.sortingLayerID = PlayerSortingLayerId;
        front.sortingOrder = 100;
        front.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        GameObject overhead = new("Overhead");
        overhead.transform.SetParent(root.transform, false);
        overhead.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        overhead.transform.localScale = new Vector3(0.625f, 0.7407407f, 1f);

        GameObject bellObject = new("BellEffect");
        bellObject.transform.SetParent(overhead.transform, false);
        bellObject.transform.localScale = Vector3.one * 2.1f;
        SpriteRenderer bell = bellObject.AddComponent<SpriteRenderer>();
        bell.sortingLayerID = PlayerSortingLayerId;
        bell.sortingOrder = 200;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) throw new InvalidOperationException($"Missing project TMP font: {FontPath}");
        GameObject textObject = new("LevelUpText");
        textObject.transform.SetParent(overhead.transform, false);
        textObject.transform.localScale = Vector3.one * 0.6f;
        TextMeshPro levelUpText = textObject.AddComponent<TextMeshPro>();
        levelUpText.text = "LEVEL UP";
        levelUpText.font = font;
        levelUpText.fontSize = 6f;
        levelUpText.fontStyle = FontStyles.Bold;
        levelUpText.alignment = TextAlignmentOptions.Center;
        levelUpText.color = new Color(1f, 0.88f, 0.2f, 1f);
        levelUpText.rectTransform.sizeDelta = new Vector2(5f, 1.2f);
        levelUpText.renderer.sortingLayerID = PlayerSortingLayerId;
        levelUpText.renderer.sortingOrder = 220;

        LevelUpEffectController controller = root.AddComponent<LevelUpEffectController>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("_subscribeToPlayerStat").boolValue = true;
        serialized.FindProperty("_framesPerSecond").floatValue = 16f;
        serialized.FindProperty("_backRenderer").objectReferenceValue = back;
        serialized.FindProperty("_frontRenderer").objectReferenceValue = front;
        serialized.FindProperty("_frontMask").objectReferenceValue = mask;
        AssignArray(serialized.FindProperty("_backFrames"), backFrames);
        AssignArray(serialized.FindProperty("_frontFrames"), frontFrames);
        AssignArray(serialized.FindProperty("_maskFrames"), maskFrames);
        serialized.FindProperty("_bellFramesPerSecond").floatValue = 18f;
        serialized.FindProperty("_bellRenderer").objectReferenceValue = bell;
        AssignArray(serialized.FindProperty("_bellFrames"), bellFrames);
        serialized.FindProperty("_levelUpText").objectReferenceValue = levelUpText;
        serialized.FindProperty("_textRiseDuration").floatValue = 0.9f;
        serialized.FindProperty("_textRiseDistance").floatValue = 1.45f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AssignArray(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    private static void IntegrateScene(string scenePath, GameObject prefab)
    {
        if (!File.Exists(scenePath)) return;
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject player = scene.GetRootGameObjects().FirstOrDefault(go => go.CompareTag("Player"));
        if (player == null) throw new InvalidOperationException($"Player not found in {scenePath}");
        RemoveChildren(player.transform);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.SetParent(player.transform, false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RemoveFromScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject player = scene.GetRootGameObjects().FirstOrDefault(go => go.CompareTag("Player"));
        if (player == null) throw new InvalidOperationException($"Player not found in {scenePath}");
        RemoveChildren(player.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RemoveChildren(Transform player)
    {
        for (int i = player.childCount - 1; i >= 0; i--)
        {
            Transform child = player.GetChild(i);
            if (child.GetComponent<LevelUpEffectController>() != null || child.name.StartsWith("LevelUpEffect", StringComparison.Ordinal) || child.name.StartsWith("LevelUpFX", StringComparison.Ordinal))
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
}
