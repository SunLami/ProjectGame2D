#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the Dark Inventory Style FarmStorageUI Canvas and installs it into
/// Bootstrap._UI next to DialogueUI/GameplayUIRoot -- same persistent-UI
/// location every other overlay lives in, so FarmStorageManager/FarmStorageUI are reachable as soon
/// as gameplay starts, from any scene.</summary>
public static class FarmStorageUIAuthoring
{
    private const string PrefabPath = "Assets/Prefabs/UI/FarmStorageUI.prefab";
    private const string ScenePath = "Assets/Scenes/Bootstrap.unity";
    private const string ArtFolder = "Assets/Resources/UI/Farming/DarkInventoryStyle";
    private const string PanelSpritePath = ArtFolder + "/farm_storage_inventory_board_v4.png";
    private const string InventoryArtFolder = "Assets/Resources/UI/Inventory";
    private const string SlotSpritePath = InventoryArtFolder + "/QuestStyle1920/inventory_slot_reference_v4.png";
    private const string CloseSpritePath = InventoryArtFolder + "/LightFantasy/inventory_close_thin_hd.png";

    private const int GridColumns = 6;
    private const int GridRows = 5;
    private const float SlotSize = 48f;
    private const float SlotSpacing = 10f;

    private static readonly Color GoldText = new(0.87f, 0.78f, 0.59f, 1f);
    private static readonly Color BodyText = new(0.92f, 0.9f, 0.85f, 1f);

    [MenuItem("Tools/Project Game/Farming/Build And Install Farm Storage UI")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs/UI");
        ConfigureSpriteImporter(PanelSpritePath);
        ConfigureSpriteImporter(SlotSpritePath);
        ConfigureSpriteImporter(CloseSpritePath);
        Sprite panelSprite = LoadSprite(PanelSpritePath);
        Sprite slotSprite = LoadSprite(SlotSpritePath);
        Sprite closeSprite = LoadSprite(CloseSpritePath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");

        GameObject root = new("FarmStorageUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FarmStorageUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 225;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject backdrop = UI("Backdrop", root.transform);
        Stretch(backdrop.GetComponent<RectTransform>());
        Image blocker = backdrop.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject panel = Panel("Panel", backdrop.transform, panelSprite, new Vector2(900f, 600f));

        TMP_Text title = Text("Title", panel.transform, font, 22f, TextAlignmentOptions.Center, GoldText);
        SetRect(title.rectTransform, new Vector2(0f, 248f), new Vector2(360f, 34f));
        title.text = "Farm Storage";

        Button closeButton = Button("CloseButton", panel.transform, closeSprite);
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(468f, 264f), new Vector2(36f, 36f));

        TMP_Text feedback = Text("Feedback", panel.transform, font, 13f, TextAlignmentOptions.Center, GoldText);
        SetRect(feedback.rectTransform, new Vector2(0f, -214f), new Vector2(760f, 24f));
        feedback.text = string.Empty;

        Transform inventoryContent = BuildColumn(panel.transform, "InventoryColumn", "Inventory", new Vector2(-205f, 0f), font, slotSprite);
        Transform storageContent = BuildColumn(panel.transform, "StorageColumn", "Storage", new Vector2(205f, 0f), font, slotSprite);

        GameObject rowTemplate = BuildRowTemplate(panel.transform, font, slotSprite);

        FarmStorageUI storageUI = root.GetComponent<FarmStorageUI>();
        SerializedObject data = new(storageUI);
        data.FindProperty("_backdrop").objectReferenceValue = backdrop;
        data.FindProperty("_title").objectReferenceValue = title;
        data.FindProperty("_feedbackText").objectReferenceValue = feedback;
        data.FindProperty("_closeButton").objectReferenceValue = closeButton;
        data.FindProperty("_inventoryListContent").objectReferenceValue = inventoryContent;
        data.FindProperty("_rowTemplate").objectReferenceValue = rowTemplate;
        data.FindProperty("_storageListContent").objectReferenceValue = storageContent;
        data.ApplyModifiedPropertiesWithoutUndo();

        backdrop.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        InstallInBootstrap(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Farm Storage UI prefab built and installed in Bootstrap._UI.");
    }

    private static Transform BuildColumn(Transform parent, string name, string header, Vector2 anchoredPosition, TMP_FontAsset font, Sprite slotSprite)
    {
        GameObject column = UI(name, parent);
        RectTransform columnRect = column.GetComponent<RectTransform>();
        SetRect(columnRect, anchoredPosition, new Vector2(420f, 460f));

        TMP_Text headerText = Text("Header", column.transform, font, 15f, TextAlignmentOptions.Center, GoldText);
        SetRect(headerText.rectTransform, new Vector2(0f, 188f), new Vector2(380f, 30f));
        headerText.text = header;

        GameObject scrollGO = UI("ScrollView", column.transform);
        RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
        SetRect(scrollRect, new Vector2(0f, -20f), new Vector2(380f, 280f));
        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = Color.clear;
        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        FarmStorageDropZone dropZone = scrollGO.AddComponent<FarmStorageDropZone>();
        dropZone.Configure(name == "InventoryColumn"
            ? FarmStorageGridSide.Inventory
            : FarmStorageGridSide.Storage);

        GameObject viewport = UI("Viewport", scrollGO.transform);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject scrollContent = UI("ScrollContent", viewport.transform);
        RectTransform scrollContentRect = scrollContent.GetComponent<RectTransform>();
        ConfigureGridRect(scrollContentRect);

        GameObject emptySlots = UI("EmptySlots", scrollContent.transform);
        RectTransform emptySlotsRect = emptySlots.GetComponent<RectTransform>();
        ConfigureGridRect(emptySlotsRect);
        ConfigureGridLayout(emptySlots.AddComponent<GridLayoutGroup>());
        for (int i = 0; i < GridColumns * GridRows; i++)
        {
            Image emptySlot = Image($"EmptySlot_{i + 1:00}", emptySlots.transform, slotSprite, Color.white);
            emptySlot.raycastTarget = false;
        }

        GameObject content = UI("Content", scrollContent.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        ConfigureGridRect(contentRect);
        ConfigureGridLayout(content.AddComponent<GridLayoutGroup>());

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = scrollContentRect;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        return content.transform;
    }

    private static GameObject BuildRowTemplate(Transform parent, TMP_FontAsset font, Sprite rowSprite)
    {
        GameObject row = UI("RowTemplate", parent);
        LayoutElement element = row.AddComponent<LayoutElement>();
        element.preferredWidth = SlotSize;
        element.preferredHeight = SlotSize;
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(SlotSize, SlotSize);

        Image background = row.AddComponent<Image>();
        background.sprite = rowSprite;
        background.color = Color.white;
        Button button = row.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        Image icon = Image("Icon", row.transform, null, Color.white);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        icon.rectTransform.anchoredPosition = Vector2.zero;
        icon.rectTransform.sizeDelta = new Vector2(36f, 36f);
        icon.preserveAspect = true;
        icon.enabled = false;

        TMP_Text label = Text("Label", row.transform, font, 11f, TextAlignmentOptions.BottomRight, BodyText);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(1f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(1f, 0f);
        labelRect.anchoredPosition = new Vector2(-4f, 3f);
        labelRect.sizeDelta = new Vector2(30f, 16f);
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;

        row.SetActive(false);
        return row;
    }

    private static void ConfigureGridRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 320f);
    }

    private static void ConfigureGridLayout(GridLayoutGroup layout)
    {
        layout.padding = new RectOffset(21, 21, 20, 20);
        layout.cellSize = new Vector2(SlotSize, SlotSize);
        layout.spacing = new Vector2(SlotSpacing, SlotSpacing);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = GridColumns;
    }

    private static void InstallInBootstrap(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform uiRoot = GameObject.Find("_UI")?.transform;
        if (uiRoot == null)
            throw new MissingReferenceException("Bootstrap scene requires a _UI root to install FarmStorageUI next to DialogueUI.");

        Transform existing = uiRoot.Find("FarmStorageUI");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.SetParent(uiRoot, false);
        instance.name = "FarmStorageUI";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject Panel(string name, Transform parent, Sprite sprite, Vector2 size)
    {
        GameObject value = UI(name, parent);
        RectTransform rect = value.GetComponent<RectTransform>();
        SetRect(rect, Vector2.zero, size);
        Image image = value.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        return value;
    }

    private static Button Button(string name, Transform parent, Sprite sprite)
    {
        GameObject value = UI(name, parent);
        Image image = value.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        Button button = value.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static GameObject UI(string name, Transform parent)
    {
        GameObject value = new(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        return value;
    }

    private static Image Image(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject value = UI(name, parent);
        Image image = value.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject value = UI(name, parent);
        TextMeshProUGUI text = value.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new MissingReferenceException($"Farm Storage UI sprite is missing or not imported as Sprite: {path}");
        return sprite;
    }

    private static void ConfigureSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new MissingReferenceException($"Farm Storage UI texture is missing: {path}");

        if (importer.textureType == TextureImporterType.Sprite &&
            importer.spriteImportMode == SpriteImportMode.Single &&
            importer.alphaIsTransparency && !importer.mipmapEnabled &&
            importer.filterMode == FilterMode.Point &&
            importer.textureCompression == TextureImporterCompression.Uncompressed)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string part in path.Substring("Assets/".Length).Split('/'))
        {
            string next = $"{current}/{part}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }
}
#endif
