#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UnifiedHudAuthoring
{
    private const string PrefabPath = "Assets/Resources/UI/Gameplay/UnifiedHUD/UnifiedGameplayHUD.prefab";
    private const string ScenePath = "Assets/Scenes/DemoScene.unity";

    [MenuItem("Tools/ProjectGame2D/UI/Build Unified Gameplay HUD")]
    public static void Build()
    {
        Sprite frame = LoadSprite("Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/unified_hud_frame.png");
        Sprite health = LoadSprite("Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/health_orb_fill.png");
        Sprite stamina = LoadSprite("Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/stamina_orb_fill.png");
        Sprite statIcon = LoadSprite("Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/stat_icon.png");
        Sprite mapIcon = LoadSprite("Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/map_icon.png");
        Sprite socketBackground = LoadSprite("Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/socket_background_round_brown.png");
        Sprite quickSlotBackground = LoadSprite("Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/quick_slot_background_brown.png");
        Sprite expFill = LoadSprite("Assets/Resources/UI/HUD/ExperienceBar/experience_bar_fill_blue_v2.png");
        Sprite board = LoadSprite("Assets/Resources/UI/Inventory/LightFantasy/inventory_board_hd.png");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
        if (new Object[] { frame, health, stamina, statIcon, mapIcon, socketBackground, quickSlotBackground, expFill, board, font }.Any(x => x == null))
            throw new System.InvalidOperationException("Unified HUD authoring assets could not be loaded.");

        GameObject root = new("UnifiedGameplayHUD", typeof(RectTransform), typeof(UnifiedGameplayHudController));
        try
        {
            SetLayerRecursively(root, 5);
            Stretch(root.GetComponent<RectTransform>());
            UnifiedGameplayHudController controller = root.GetComponent<UnifiedGameplayHudController>();

            RectTransform hud = CreateRect("BottomHUD", root.transform);
            hud.anchorMin = hud.anchorMax = new Vector2(0.5f, 0f);
            hud.pivot = new Vector2(0.5f, 0f);
            hud.anchoredPosition = new Vector2(0f, -2f);
            hud.sizeDelta = new Vector2(700f, 137f);
            hud.localScale = Vector3.one * 0.6f;

            Image healthFill = CreateImage("HealthFill", hud, health);
            Place(healthFill.rectTransform, -279f, 70f, 112f, 112f);
            ConfigureVerticalFill(healthFill);

            Image staminaFill = CreateImage("StaminaFill", hud, stamina);
            Place(staminaFill.rectTransform, 279f, 70f, 112f, 112f);
            ConfigureVerticalFill(staminaFill);

            Image experienceFill = CreateImage("ExperienceFill", hud, expFill);
            Place(experienceFill.rectTransform, 0f, 86f, 244f, 9f);
            experienceFill.type = Image.Type.Filled;
            experienceFill.fillMethod = Image.FillMethod.Horizontal;
            experienceFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            Image statBackground = CreateImage("StatBackground", hud, socketBackground);
            Place(statBackground.rectTransform, -200f, 43f, 53f, 53f);
            statBackground.preserveAspect = true;

            Image mapBackground = CreateImage("MapBackground", hud, socketBackground);
            Place(mapBackground.rectTransform, 200f, 43f, 53f, 53f);
            mapBackground.preserveAspect = true;

            for (int i = 0; i < 8; i++)
            {
                float x = -145f + i * 41.5f;
                Image slotBackground = CreateImage($"QuickSlot{i + 1}Background", hud, quickSlotBackground);
                Place(slotBackground.rectTransform, x, 42f, 55f, 55f);
                slotBackground.preserveAspect = true;
            }

            Image frameImage = CreateImage("Frame", hud, frame);
            Stretch(frameImage.rectTransform);

            TMP_Text experienceText = CreateText("ExperienceText", hud, font, "0 / 100", 9f);
            Place(experienceText.rectTransform, 0f, 86f, 180f, 12f);

            TMP_Text levelText = CreateText("LevelText", hud, font, "LV. 1", 9f);
            Place(levelText.rectTransform, -279f, 15f, 62f, 15f);

            for (int i = 0; i < 8; i++)
            {
                float x = -145f + i * 41.5f;
                TMP_Text key = CreateText($"QuickSlot{i + 1}Key", hud, font, (i + 1).ToString(), 8f);
                Place(key.rectTransform, x, 39f, 30f, 12f);
                key.alignment = TextAlignmentOptions.BottomRight;
                key.color = new Color(1f, 0.86f, 0.48f, 0.85f);
            }

            Button statButton = CreateIconButton("StatButton", hud, statIcon, -200f, 43f, 24f);
            Button mapButton = CreateIconButton("MapButton", hud, mapIcon, 200f, 43f, 24f);
            UnityEventTools.AddPersistentListener(statButton.onClick, controller.OpenCharacterPopup);
            UnityEventTools.AddPersistentListener(mapButton.onClick, controller.OpenMapPopup);

            GameObject characterPopup = CreatePopup("CharacterPopup", root.transform, board, font, "CHARACTER STATS", out TMP_Text characterText);
            GameObject mapPopup = CreatePopup("MapPopup", root.transform, board, font, "WORLD MAP", out _);
            Image mapPreview = CreateImage("MapPreview", mapPopup.transform, mapIcon);
            Place(mapPreview.rectTransform, 0f, -15f, 230f, 230f);
            mapPreview.preserveAspect = true;

            AddCloseButton(characterPopup.transform, controller, font);
            AddCloseButton(mapPopup.transform, controller, font);
            characterPopup.SetActive(false);
            mapPopup.SetActive(false);

            SerializedObject serialized = new(controller);
            serialized.FindProperty("_healthFill").objectReferenceValue = healthFill;
            serialized.FindProperty("_staminaFill").objectReferenceValue = staminaFill;
            serialized.FindProperty("_experienceFill").objectReferenceValue = experienceFill;
            serialized.FindProperty("_experienceText").objectReferenceValue = experienceText;
            serialized.FindProperty("_levelText").objectReferenceValue = levelText;
            serialized.FindProperty("_characterPopup").objectReferenceValue = characterPopup;
            serialized.FindProperty("_characterStatsText").objectReferenceValue = characterText;
            serialized.FindProperty("_mapPopup").objectReferenceValue = mapPopup;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        IntegrateDemoScene();
        AssetDatabase.SaveAssets();
        Debug.Log("UnifiedHudAuthoring: prefab and DemoScene integration completed.");
    }

    private static void IntegrateDemoScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
            .FirstOrDefault(c => c.gameObject.name == "UICanvas");
        if (canvas == null)
            throw new System.InvalidOperationException("DemoScene UICanvas was not found.");

        foreach (string oldName in new[] { "PlayerHUD", "PlayerExperienceBar", "UnifiedGameplayHUD" })
        {
            Transform old = canvas.transform.Find(oldName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        instance.name = "UnifiedGameplayHUD";
        instance.transform.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject CreatePopup(string name, Transform parent, Sprite board, TMP_FontAsset font, string title, out TMP_Text body)
    {
        RectTransform popup = CreateRect(name, parent);
        popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 0.5f);
        popup.sizeDelta = new Vector2(380f, 430f);
        popup.anchoredPosition = Vector2.zero;
        Image background = popup.gameObject.AddComponent<Image>();
        background.sprite = board;
        background.type = Image.Type.Simple;
        background.raycastTarget = true;

        TMP_Text heading = CreateText("Title", popup, font, title, 25f);
        Place(heading.rectTransform, 0f, 150f, 300f, 42f);
        heading.color = new Color(0.12f, 0.22f, 0.46f, 1f);

        body = CreateText("Body", popup, font, string.Empty, 17f);
        Place(body.rectTransform, 0f, -15f, 270f, 270f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.color = new Color(0.22f, 0.12f, 0.06f, 1f);
        return popup.gameObject;
    }

    private static void AddCloseButton(Transform popup, UnifiedGameplayHudController controller, TMP_FontAsset font)
    {
        RectTransform rect = CreateRect("CloseButton", popup);
        Place(rect, 151f, 176f, 34f, 34f);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.22f, 0.52f, 0.95f);
        Button button = rect.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, controller.ClosePopup);
        TMP_Text label = CreateText("Label", rect, font, "X", 20f);
        Stretch(label.rectTransform);
    }

    private static Button CreateIconButton(string name, Transform parent, Sprite icon, float x, float y, float size)
    {
        Image image = CreateImage(name, parent, icon);
        Place(image.rectTransform, x, y, size, size);
        image.preserveAspect = true;
        image.raycastTarget = true;
        return image.gameObject.AddComponent<Button>();
    }

    private static void ConfigureVerticalFill(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Vertical;
        image.fillOrigin = (int)Image.OriginVertical.Bottom;
    }

    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        child.layer = 5;
        return child.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<CanvasRenderer>();
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value, float size)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<CanvasRenderer>();
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.94f, 0.68f, 1f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
#endif
