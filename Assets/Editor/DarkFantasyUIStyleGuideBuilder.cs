using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class DarkFantasyUIStyleGuideBuilder
{
    private const string MenuPath = "Tools/ProjectGame2D/UI/Build Dark Fantasy Style Guide";
    private const string PrefabRoot = "Assets/Prefabs/UI/StyleSystem";
    private const string ScenePath = "Assets/Scenes/UIStyleGuide.unity";
    private const string InventoryRoot = "Assets/Resources/UI/Inventory/";
    private const string QuestRoot = "Assets/Resources/UI/Quest/QuestLog1920/";
    private static readonly Color Gold = new(0.94f, 0.72f, 0.25f, 1f);
    private static readonly Color Ivory = new(0.95f, 0.91f, 0.80f, 1f);
    private static readonly Color Muted = new(0.68f, 0.62f, 0.53f, 1f);

    [MenuItem(MenuPath)]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs/UI", "StyleSystem");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
        Sprite panel = LoadSprite(InventoryRoot + "QuestStyle1920/inventory_tooltip_subtle_v2.png");
        Sprite slot = LoadSprite(InventoryRoot + "QuestStyle1920/inventory_slot_reference_v4.png");
        Sprite action = LoadSprite(QuestRoot + "quest_log_action_button.png");
        Sprite close = LoadSprite(InventoryRoot + "LightFantasy/inventory_close_thin_hd.png");
        Sprite coin = LoadSprite(InventoryRoot + "LightFantasy/inventory_gold_badge_hd.png");

        SavePanelPrefab(panel);
        SaveSlotPrefab(slot);
        SaveButtonPrefab("PrimaryButton", action, new Color(0.22f, 0.38f, 1f), "PRIMARY", font);
        SaveButtonPrefab("DangerButton", action, new Color(0.78f, 0.28f, 0.24f), "DANGER", font);
        SaveClosePrefab(close);
        SaveTooltipPrefab(panel, font);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = new GameObject("StyleGuideCamera", typeof(Camera)).GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.03f, 0.028f);
        camera.orthographic = true;

        GameObject canvasGo = new("UIStyleGuide", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Image backdrop = CreateImage(canvasGo.transform, "Backdrop", null, new Color(0.055f, 0.045f, 0.04f), Vector2.zero, new Vector2(1920f, 1080f));
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.sizeDelta = Vector2.zero;

        Text(canvasGo.transform, "Title", "DARK LIGHT FANTASY UI SYSTEM", font, 42f, Gold, new Vector2(0f, 480f), new Vector2(1100f, 70f), TextAlignmentOptions.Center);
        Text(canvasGo.transform, "Subtitle", "Inventory is the master visual language — 1920 × 1080", font, 20f, Muted, new Vector2(0f, 435f), new Vector2(1000f, 40f), TextAlignmentOptions.Center);

        Transform left = InstantiatePrefab("DarkPanel", canvasGo.transform, new Vector2(-440f, 50f), new Vector2(760f, 680f));
        Text(left, "SectionTitle", "FOUNDATION", font, 30f, Gold, new Vector2(0f, 270f), new Vector2(560f, 50f), TextAlignmentOptions.Center);
        Text(left, "Hierarchy", "TITLE / 30\nSECTION / 22\nLABEL / 18\nVALUE / 18\nHELPER / 15", font, 22f, Ivory, new Vector2(-165f, 80f), new Vector2(300f, 260f), TextAlignmentOptions.Left);
        Text(left, "Spacing", "SPACING\n4  /  8  /  12  /  16\n\nSAFE AREA\n24 PX MINIMUM", font, 20f, Muted, new Vector2(165f, 80f), new Vector2(300f, 260f), TextAlignmentOptions.Left);
        CreateSwatch(left, "Charcoal", new Color(0.07f, 0.07f, 0.075f), new Vector2(-240f, -155f), font);
        CreateSwatch(left, "Walnut", new Color(0.18f, 0.105f, 0.06f), new Vector2(-80f, -155f), font);
        CreateSwatch(left, "Antique Gold", Gold, new Vector2(80f, -155f), font);
        CreateSwatch(left, "Sapphire", new Color(0.08f, 0.35f, 0.80f), new Vector2(240f, -155f), font);

        Transform right = InstantiatePrefab("DarkPanel", canvasGo.transform, new Vector2(440f, 50f), new Vector2(760f, 680f));
        Text(right, "SectionTitle", "MASTER COMPONENTS", font, 30f, Gold, new Vector2(0f, 270f), new Vector2(600f, 50f), TextAlignmentOptions.Center);
        for (int i = 0; i < 4; i++) InstantiatePrefab("DarkSlot", right, new Vector2(-245f + i * 88f, 145f), new Vector2(72f, 72f));
        Text(right, "SlotLabel", "ITEM / EQUIPMENT SLOT", font, 16f, Muted, new Vector2(190f, 145f), new Vector2(230f, 40f), TextAlignmentOptions.Left);
        InstantiatePrefab("PrimaryButton", right, new Vector2(-170f, 35f), new Vector2(260f, 62f));
        InstantiatePrefab("DangerButton", right, new Vector2(170f, 35f), new Vector2(260f, 62f));
        InstantiatePrefab("CloseButton", right, new Vector2(300f, 260f), new Vector2(52f, 52f));
        Transform tooltip = InstantiatePrefab("Tooltip", right, new Vector2(0f, -155f), new Vector2(560f, 240f));
        Image coinImage = CreateImage(right, "CurrencyWell", coin, Color.white, new Vector2(0f, -290f), new Vector2(220f, 40f));
        coinImage.preserveAspect = true;
        Text(right, "CurrencyValue", "12,480 GOLD", font, 16f, Ivory, new Vector2(12f, -290f), new Vector2(150f, 30f), TextAlignmentOptions.Center);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = canvasGo;
        Debug.Log("Dark Fantasy UI Style Guide and master prefabs rebuilt.");
    }

    private static void SavePanelPrefab(Sprite sprite)
    {
        GameObject go = new("DarkPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Image image = go.GetComponent<Image>();
        image.sprite = sprite; image.type = Image.Type.Sliced; image.color = Color.white;
        Save(go, "DarkPanel");
    }

    private static void SaveSlotPrefab(Sprite sprite)
    {
        GameObject go = new("DarkSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Image image = go.GetComponent<Image>();
        image.sprite = sprite; image.type = Image.Type.Sliced; image.color = Color.white;
        Save(go, "DarkSlot");
    }

    private static void SaveButtonPrefab(string name, Sprite sprite, Color tint, string label, TMP_FontAsset font)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        Image image = go.GetComponent<Image>();
        image.sprite = sprite; image.type = Image.Type.Sliced; image.color = tint;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f); colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
        button.colors = colors;
        Text(go.transform, "Label", label, font, 20f, Ivory, Vector2.zero, new Vector2(210f, 38f), TextAlignmentOptions.Center);
        Save(go, name);
    }

    private static void SaveClosePrefab(Sprite sprite)
    {
        GameObject go = new("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        Image image = go.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true;
        go.GetComponent<Button>().targetGraphic = image;
        Save(go, "CloseButton");
    }

    private static void SaveTooltipPrefab(Sprite sprite, TMP_FontAsset font)
    {
        GameObject go = new("Tooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Image image = go.GetComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Sliced; image.color = new Color(1f, 1f, 1f, 0.94f);
        Text(go.transform, "ItemName", "ITEM NAME", font, 22f, new Color(0.78f, 0.48f, 0.96f), new Vector2(0f, 72f), new Vector2(460f, 34f), TextAlignmentOptions.Left);
        Text(go.transform, "Type", "WEAPON  •  EQUIPMENT SLOT", font, 15f, Muted, new Vector2(0f, 34f), new Vector2(460f, 28f), TextAlignmentOptions.Left);
        Text(go.transform, "Stats", "+18 ATTACK DAMAGE\n+5% CRITICAL CHANCE", font, 17f, new Color(0.36f, 0.82f, 0.43f), new Vector2(0f, -20f), new Vector2(460f, 64f), TextAlignmentOptions.TopLeft);
        Text(go.transform, "Description", "Dynamic item description stays inside the safe area.", font, 14f, Muted, new Vector2(0f, -78f), new Vector2(460f, 35f), TextAlignmentOptions.Left);
        Save(go, "Tooltip");
    }

    private static Transform InstantiatePrefab(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{name}.prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        RectTransform rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
        return go.transform;
    }

    private static void CreateSwatch(Transform parent, string label, Color color, Vector2 position, TMP_FontAsset font)
    {
        CreateImage(parent, label + "Swatch", null, color, position, new Vector2(120f, 58f));
        Text(parent, label + "Label", label.ToUpperInvariant(), font, 14f, Ivory, position + new Vector2(0f, -48f), new Vector2(150f, 28f), TextAlignmentOptions.Center);
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
        Image image = go.GetComponent<Image>(); image.sprite = sprite; image.color = color; return image;
    }

    private static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font, float size, Color color, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
        TMP_Text text = go.GetComponent<TMP_Text>(); text.font = font; text.fontSize = size; text.color = color; text.alignment = alignment; text.text = value; text.raycastTarget = false; return text;
    }

    private static void Save(GameObject go, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabRoot}/{name}.prefab");
        Object.DestroyImmediate(go);
    }

    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }
}
