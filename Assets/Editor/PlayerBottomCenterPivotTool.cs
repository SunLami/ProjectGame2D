using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PlayerBottomCenterPivotTool
{
    private const string PlayerLibrariesFolder = "Assets/Sprites/SpriteLib/PlayerSpriteLib";
    private static readonly Vector2 SortingPivot = new(0.5f, 0.315f);
    private static readonly string[] PlayerScenes =
    {
        "Assets/Scenes/DemoScene.unity",
        "Assets/Scenes/MapNhat.unity"
    };

    [MenuItem("Tools/Project Game/Player/Apply Sorting Pivot (0.5, 0.315)")]
    public static void Apply()
    {
        string[] libraries = Directory.GetFiles(PlayerLibrariesFolder, "*.spriteLib", SearchOption.AllDirectories)
            .Select(NormalizePath).ToArray();
        string[] textures = AssetDatabase.GetDependencies(libraries, true)
            .Where(path => AssetImporter.GetAtPath(path) is TextureImporter importer
                           && importer.textureType == TextureImporterType.Sprite)
            .Distinct().OrderBy(path => path).ToArray();

        if (textures.Length == 0)
            throw new InvalidOperationException("No Player SpriteLibrary texture dependencies were found.");

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var changedTextures = new List<string>();
        var skippedTextures = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string texturePath in textures)
            {
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                if (provider == null)
                {
                    skippedTextures.Add(texturePath);
                    continue;
                }

                provider.InitSpriteEditorDataProvider();
                SpriteRect[] rects = provider.GetSpriteRects();
                bool changed = false;
                foreach (SpriteRect rect in rects)
                {
                    if (rect.alignment == SpriteAlignment.Custom
                        && Vector2.Distance(rect.pivot, SortingPivot) <= 0.0001f)
                        continue;

                    rect.alignment = SpriteAlignment.Custom;
                    rect.pivot = SortingPivot;
                    changed = true;
                }

                if (!changed)
                    continue;

                provider.SetSpriteRects(rects);
                provider.Apply();
                AssetDatabase.WriteImportSettingsIfDirty(texturePath);
                changedTextures.Add(texturePath);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        foreach (string texturePath in changedTextures)
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
        int changedPlayers = ConfigurePlayerSceneInstances();
        AssetDatabase.SaveAssets();

        Debug.Log($"Player sorting pivot (0.5, 0.315) applied: {changedTextures.Count}/{textures.Length} textures updated, " +
                  $"{changedPlayers} Player scene instances configured with SortingGroup and SpriteSortPoint.Pivot." +
                  (skippedTextures.Count == 0 ? string.Empty : $" Skipped providers: {string.Join(", ", skippedTextures)}"));
    }

    [MenuItem("Tools/Project Game/Player/Validate Sorting Pivot (0.5, 0.315)")]
    public static void Validate()
    {
        string[] libraries = Directory.GetFiles(PlayerLibrariesFolder, "*.spriteLib", SearchOption.AllDirectories)
            .Select(NormalizePath).ToArray();
        string[] textures = AssetDatabase.GetDependencies(libraries, true)
            .Where(path => AssetImporter.GetAtPath(path) is TextureImporter importer
                           && importer.textureType == TextureImporterType.Sprite)
            .Distinct().ToArray();
        var failures = new List<string>();
        foreach (string texture in textures)
        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(texture).OfType<Sprite>())
        {
            Vector2 normalized = new(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            if (Vector2.Distance(normalized, SortingPivot) > 0.001f)
            {
                failures.Add($"{texture}:{sprite.name}");
                break;
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException("Player pivot validation failed: " + string.Join(", ", failures));

        Debug.Log($"Player sorting pivot (0.5, 0.315) validation passed for {textures.Length} SpriteLibrary textures.");
    }

    private static int ConfigurePlayerSceneInstances()
    {
        int changedPlayers = 0;
        foreach (string scenePath in PlayerScenes)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForEditing = !scene.IsValid() || !scene.isLoaded;
            if (openedForEditing)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            bool sceneChanged = false;
            foreach (GameObject player in scene.GetRootGameObjects().Where(root => root.CompareTag("Player")))
            {
                SortingGroup group = player.GetComponent<SortingGroup>();
                if (group == null)
                {
                    group = Undo.AddComponent<SortingGroup>(player);
                    sceneChanged = true;
                }

                group.sortingLayerID = 0;
                group.sortingOrder = 0;
                group.sortAtRoot = true;
                EditorUtility.SetDirty(group);

                foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.spriteSortPoint != SpriteSortPoint.Pivot)
                    {
                        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                        EditorUtility.SetDirty(renderer);
                        sceneChanged = true;
                    }

                    // The requested pivot must remain the actual render origin in game. Do not
                    // compensate the visible sprite after changing importer metadata.
                    if (renderer.transform.localPosition != Vector3.zero)
                    {
                        renderer.transform.localPosition = Vector3.zero;
                        EditorUtility.SetDirty(renderer.transform);
                        sceneChanged = true;
                    }
                }

                changedPlayers++;
            }

            if (sceneChanged)
                EditorSceneManager.SaveScene(scene);
            if (openedForEditing)
                EditorSceneManager.CloseScene(scene, true);
        }
        return changedPlayers;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
