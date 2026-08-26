using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class PlayerHUDBuilder
{
    private const string ScenePath = "Assets/Scenes/DemoScene.unity";
    private const string FrameTexturePath = "Assets/Resources/UI/Gameplay/PlayerStatusHUD/LightFantasy/player_status_frame.png";
    private const string HealthFillTexturePath = "Assets/Resources/UI/Gameplay/PlayerStatusHUD/LightFantasy/health_fill.png";
    private const string StaminaFillTexturePath = "Assets/Resources/UI/Gameplay/PlayerStatusHUD/LightFantasy/stamina_fill_green.png";
    private const string AvatarTexturePath = "Assets/Resources/UI/Gameplay/PlayerStatusHUD/LightFantasy/default_avatar.png";
    private const string LevelBackgroundTexturePath = "Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/socket_background_round_brown.png";
    private const string PrefabPath = "Assets/Resources/UI/Gameplay/HUD/PlayerHUD.prefab";
    private const string FontPath = "Assets/Fonts/DigitalDisco SDF v3.asset";

    [MenuItem("Tools/ProjectGame2D/UI/Build Player Health Stamina HUD")]
    public static void Build()
    {
        Sprite frameSprite = ImportSprite(FrameTexturePath);
        Sprite healthFillSprite = ImportSprite(HealthFillTexturePath);
        Sprite staminaFillSprite = ImportSprite(StaminaFillTexturePath);
        Sprite avatarSprite = ImportSprite(AvatarTexturePath);
        Sprite levelBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LevelBackgroundTexturePath);
        GameObject prefabRoot = BuildPrefabSource(frameSprite, healthFillSprite, staminaFillSprite, avatarSprite, levelBackgroundSprite);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        UnityEngine.Object.DestroyImmediate(prefabRoot);

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PauseMenuUI pauseMenu = UnityEngine.Object.FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        Canvas canvas = pauseMenu != null ? pauseMenu.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null)
            throw new InvalidOperationException("Gameplay Canvas containing PauseMenuUI was not found.");

        Transform existing = canvas.transform.Find("PlayerHUD");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "PlayerHUD";
        instance.transform.SetParent(canvas.transform, false);
        instance.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("PlayerHUD prefab built and integrated into DemoScene gameplay Canvas.");
    }

    private static GameObject BuildPrefabSource(Sprite frameSprite, Sprite healthFillSprite, Sprite staminaFillSprite, Sprite avatarSprite, Sprite levelBackgroundSprite)
    {
        GameObject root = new GameObject("PlayerHUD", typeof(RectTransform), typeof(PlayerHUDController));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(8f, -8f);
        rootRect.sizeDelta = new Vector2(260f, 98f);
        rootRect.localScale = Vector3.one * 0.62f;

        Image avatar = CreateImage(root.transform, "Avatar", Color.white);
        avatar.sprite = avatarSprite;
        avatar.preserveAspect = true;
        PlaceTopLeft(avatar.rectTransform, 9f, -10f, 66f, 66f);

        Image healthFill = CreateFill(root.transform, "HealthFill", healthFillSprite, 70f, -35f, 173f, 19f);
        Image staminaFill = CreateFill(root.transform, "StaminaFill", staminaFillSprite, 74f, -63f, 148f, 14f);

        Image levelBackground = CreateImage(root.transform, "LevelBackground", Color.white);
        levelBackground.sprite = levelBackgroundSprite;
        levelBackground.preserveAspect = true;
        PlaceTopLeft(levelBackground.rectTransform, 52f, -61f, 34f, 34f);

        Image frame = CreateImage(root.transform, "Frame", Color.white);
        frame.sprite = frameSprite;
        frame.preserveAspect = true;
        Stretch(frame.rectTransform);
        TMP_Text levelText = CreateLevelText(root.transform);

        SerializedObject controller = new SerializedObject(root.GetComponent<PlayerHUDController>());
        controller.FindProperty("_healthFill").objectReferenceValue = healthFill;
        controller.FindProperty("_staminaFill").objectReferenceValue = staminaFill;
        controller.FindProperty("_avatarImage").objectReferenceValue = avatar;
        controller.FindProperty("_levelText").objectReferenceValue = levelText;
        controller.ApplyModifiedPropertiesWithoutUndo();

        avatar.transform.SetAsFirstSibling();
        healthFill.transform.SetSiblingIndex(1);
        staminaFill.transform.SetSiblingIndex(2);
        levelBackground.transform.SetSiblingIndex(3);
        frame.transform.SetSiblingIndex(4);
        levelText.transform.SetAsLastSibling();
        return root;
    }

    private static TMP_Text CreateLevelText(Transform parent)
    {
        GameObject textObject = new GameObject("LevelText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        text.text = "1";
        text.fontSize = 10f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(255, 238, 168, 255);
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(45, 22, 8, 255);
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(69f, -78f);
        rect.sizeDelta = new Vector2(30f, 16f);
        return text;
    }

    private static Image CreateFill(Transform parent, string name, Sprite sprite, float x, float y, float width, float height)
    {
        Image image = CreateImage(parent, name, Color.white);
        image.sprite = sprite;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        return image;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static Sprite ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException($"Texture importer not found: {path}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
