#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChestTownGeneralAuthoring
{
    private const string ScenePath = "Assets/Scenes/DemoScene.unity";
    private const string FrameRoot = "Assets/Resources/World/Chest/TownGeneral/";

    [MenuItem("Tools/Project Game 2D/World/Build Town General Chest")]
    public static void Build()
    {
        Texture2D[] frameTextures = new Texture2D[4];
        Sprite[] previewSprites = new Sprite[4];
        for (int i = 0; i < frameTextures.Length; i++)
        {
            string path = $"{FrameRoot}chest_town_general_open_{i}.png";
            frameTextures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            previewSprites[i] = Array.Find(assets, asset => asset is Sprite) as Sprite;
            if (frameTextures[i] == null || previewSprites[i] == null)
                throw new InvalidOperationException($"Chest frame is not imported: {path}");
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject chestObject = GameObject.Find("Chest_TownGeneral");
        if (chestObject == null)
            throw new InvalidOperationException("DemoScene requires Chest_TownGeneral.");

        Transform existing = chestObject.transform.Find("ChestSprite");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject visual = new("ChestSprite", typeof(SpriteRenderer));
        visual.transform.SetParent(chestObject.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        visual.transform.localScale = Vector3.one * 1.5f;
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = previewSprites[0];
        renderer.sortingOrder = 20;

        ChestInteractable chest = chestObject.GetComponent<ChestInteractable>();
        BoxCollider2D collision = chestObject.GetComponent<BoxCollider2D>();
        if (collision == null)
            collision = chestObject.AddComponent<BoxCollider2D>();
        collision.isTrigger = false;
        collision.size = new Vector2(0.9f, 0.55f);
        collision.offset = new Vector2(0f, -0.12f);

        SerializedObject chestData = new(chest);
        chestData.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
        SerializedProperty openFrames = chestData.FindProperty("_openFrameTextures");
        openFrames.arraySize = frameTextures.Length;
        for (int i = 0; i < frameTextures.Length; i++)
            openFrames.GetArrayElementAtIndex(i).objectReferenceValue = frameTextures[i];
        chestData.FindProperty("_frameSeconds").floatValue = 0.12f;
        chestData.FindProperty("_openedIndicator").objectReferenceValue = null;
        chestData.ApplyModifiedPropertiesWithoutUndo();

        PersistentWorldInteractionUI interaction = chestObject.GetComponent<PersistentWorldInteractionUI>();
        SerializedObject interactionData = new(interaction);
        interactionData.FindProperty("_availableVisual").objectReferenceValue = null;
        interactionData.ApplyModifiedPropertiesWithoutUndo();

        SetLegacyVisualActive(chestObject.transform, "PersistentPresentation/AvailableVisual", false);
        SetLegacyVisualActive(chestObject.transform, "PersistentPresentation/OpenedIndicator", false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Town General chest sprite, solid collision, four-frame opening animation, and left-click interaction authored.");
    }

    private static void SetLegacyVisualActive(Transform root, string path, bool active)
    {
        Transform target = root.Find(path);
        if (target != null)
            target.gameObject.SetActive(active);
    }
}
#endif
