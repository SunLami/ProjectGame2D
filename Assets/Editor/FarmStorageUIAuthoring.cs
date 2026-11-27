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
    private const string PanelSpritePath = ArtFolder + "/farm_storage_inventory_board_v5.png";
    private const string InventoryArtFolder = "Assets/Resources/UI/Inventory";
    private const string SlotSpritePath = InventoryArtFolder + "/QuestStyle1920/inventory_slot_reference_v4.png";
    private const string GridSpritePath = ArtFolder + "/farm_storage_column_frame_v1.png";
    private const string PlaqueSpritePath = ArtFolder + "/farm_storage_heading_v1.png";
    private const string CloseSpritePath = MapUIAuthoring.CloseButtonPath;

    private const int GridColumns = 5;
    private const int GridRows = 5;
    private const float SlotSize = 92f;
    private const float SlotSpacing = 16f;

    private static readonly Vector2 BoardSize = new(1280f, 800f);

    private static readonly Color GoldText = new(0.87f, 0.78f, 0.59f, 1f);
    private static readonly Color BodyText = new(0.92f, 0.9f, 0.85f, 1f);

    [MenuItem("Tools/Project Game/Farming/Build And Install Farm Storage UI")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs/UI");
        ConfigureSpriteImporter(PanelSpritePath);
        ConfigureSpriteImporter(GridSpritePath);
        ConfigureSpriteImporter(PlaqueSpritePath);
        ConfigureSpriteImporter(SlotSpritePath);
        ConfigureSpriteImporter(CloseSpritePath);
        Sprite panelSprite = LoadSprite(PanelSpritePath);
        Sprite slotSprite = LoadSprite(SlotSpritePath);
        Sprite plaqueSprite = LoadSprite(PlaqueSpritePath);
        Sprite gridSprite = LoadSprite(GridSpritePath);
        Sprite closeSprite = LoadSprite(CloseSpritePath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");

        GameObject root = new("FarmStorageUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FarmStorageUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 225;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        PopupViewportFit.ConfigureCanvas(scaler);

        GameObject backdrop = UI("Backdrop", root.transform);
        Stretch(backdrop.GetComponent<RectTransform>());
        Image blocker = backdrop.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject panel = Panel("Panel", backdrop.transform, panelSprite, BoardSize);
        PopupViewportFit.Configure(panel, BoardSize);

        AddPlaque(panel.transform, plaqueSprite, new Vector2(0f, 326f), new Vector2(420f, 60f));
        TMP_Text title = Text("Title", panel.transform, font, 34f, TextAlignmentOptions.Center, GoldText);
        SetRect(title.rectTransform, new Vector2(0f, 326f), new Vector2(520f, 48f));
        title.text = "Farm Storage";

        Button closeButton = Button("CloseButton", panel.transform, closeSprite);
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(580f, 326f), new Vector2(44f, 44f));

        TMP_Text feedback = Text("Feedback", panel.transform, font, 21f, TextAlignmentOptions.Center, GoldText);
        SetRect(feedback.rectTransform, new Vector2(0f, -339f), new Vector2(1140f, 38f));
        feedback.text = string.Empty;

        Transform inventoryContent = BuildColumn(panel.transform, "InventoryColumn", "Inventory", new Vector2(-303f, -15f), font, slotSprite, gridSprite, plaqueSprite);
        Transform storageContent = BuildColumn(panel.transform, "StorageColumn", "Storage", new Vector2(303f, -15f), font, slotSprite, gridSprite, plaqueSprite);

        PopupLayoutAuthoring.Reanchor(title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        PopupLayoutAuthoring.Reanchor(closeButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f));
        PopupLayoutAuthoring.Reanchor(feedback.transform, new Vector2(0f, 0f), new Vector2(1f, 0f));
        PopupLayoutAuthoring.Reanchor(panel.transform.Find("InventoryColumn"), new Vector2(0f, 0f), new Vector2(0.5f, 1f));
        PopupLayoutAuthoring.Reanchor(panel.transform.Find("StorageColumn"), new Vector2(0.5f, 0f), new Vector2(1f, 1f));

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

    private static Transform BuildColumn(Transform parent, string name, string header, Vector2 anchoredPosition, TMP_FontAsset font, Sprite slotSprite, Sprite gridSprite, Sprite plaqueSprite)
    {
        GameObject column = UI(name, parent);
        RectTransform columnRect = column.GetComponent<RectTransform>();
        SetRect(columnRect, anchoredPosition, new Vector2(582f, 590f));

        AddPlaque(column.transform, plaqueSprite, new Vector2(0f, 245f), new Vector2(320f, 44f));
        TMP_Text headerText = Text("Header", column.transform, font, 25f, TextAlignmentOptions.Center, GoldText);
        SetRect(headerText.rectTransform, new Vector2(0f, 245f), new Vector2(510f, 36f));
        headerText.text = header;

        GameObject scrollGO = UI("ScrollView", column.transform);
        RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
        SetRect(scrollRect, new Vector2(0f, -38f), new Vector2(574f, 512f));
        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.sprite = gridSprite;
        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        FarmStorageDropZone dropZone = scrollGO.AddComponent<FarmStorageDropZone>();
        dropZone.Configure(name == "InventoryColumn"
            ? FarmStorageGridSide.Inventory
            : FarmStorageGridSide.Storage);

        GameObject viewport = UI("Viewport", scrollGO.transform);
        Stretch(viewport.GetComponent<RectTransform>(), 22f, 22f, 32f, 32f);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<RectMask2D>();

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

        PopupLayoutAuthoring.Reanchor(headerText.transform, new Vector2(0f, 1f), Vector2.one);
        PopupLayoutAuthoring.Reanchor(scrollRect, Vector2.zero, Vector2.one);

        return content.transform;
    }

    private static void AddPlaque(Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        Image plaque = Image("HeadingFrame", parent, sprite, Color.white);
        SetRect(plaque.rectTransform, position, size);
        plaque.raycastTarget = false;
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
        icon.rectTransform.sizeDelta = new Vector2(64f, 64f);
        icon.preserveAspect = true;
        icon.enabled = false;

        TMP_Text label = Text("Label", row.transform, font, 20f, TextAlignmentOptions.BottomRight, BodyText);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(1f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(1f, 0f);
        labelRect.anchoredPosition = new Vector2(-9f, 7f);
        labelRect.sizeDelta = new Vector2(48f, 24f);
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
        layout.padding = new RectOffset(3, 3, 8, 8);
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
