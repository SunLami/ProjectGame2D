using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class QuestLogWindowPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/GameplayUIRoot.prefab";
    private const string BoardPath = "Assets/Resources/UI/Quest/QuestLog1920/quest_log_board_dynamic_actions_v3.png";
    private const string ActionButtonPath = "Assets/Resources/UI/Quest/QuestLog1920/quest_log_action_button.png";
    private static readonly Color Cream = new(0.96f, 0.91f, 0.76f, 1f);
    private static readonly Color Gold = new(0.83f, 0.60f, 0.24f, 1f);
    private static readonly Color Muted = new(0.68f, 0.63f, 0.60f, 1f);

    [MenuItem("Tools/ProjectGame2D/UI/Rebuild Quest Log Window 1920")]
    public static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            QuestLogUI ui = root.GetComponentInChildren<QuestLogUI>(true);
            if (ui == null) throw new InvalidOperationException("QuestLogUI missing from GameplayUIRoot prefab.");

            Transform logRoot = Require(ui.transform, "QuestLogWindow");
            Transform window = Require(logRoot, "Window");
            Transform list = Require(window, "QuestListPanel");
            Transform detail = Require(window, "QuestDetailPanel");
            TMP_FontAsset font = Require(detail, "Title").GetComponent<TMP_Text>().font;

            ConfigureWindow(logRoot, window);
            ConfigureHeader(window, font);
            ConfigureList(list, font);
            ConfigureDetail(detail, font);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("QuestLogWindow rebuilt for 1920x1080 authoring.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureWindow(Transform logRoot, Transform window)
    {
        Image dim = logRoot.GetComponent<Image>();
        dim.color = new Color(0.025f, 0.035f, 0.06f, 0.48f);
        dim.raycastTarget = true;

        RectTransform rect = window.GetComponent<RectTransform>();
        Center(rect, new Vector2(560f, 340f), Vector2.zero);
        Image image = EnsureImage(window.gameObject, Load(BoardPath));
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    private static void ConfigureHeader(Transform window, TMP_FontAsset font)
    {
        TMP_Text legacy = Require(window, "Header").GetComponent<TMP_Text>();
        legacy.enabled = true;
        legacy.text = "QUEST LOG";
        legacy.font = font;
        legacy.fontSize = 18f;
        legacy.fontStyle = FontStyles.SmallCaps;
        legacy.color = Cream;
        legacy.alignment = TextAlignmentOptions.Center;
        TopStretch(legacy.rectTransform, 54f, 20f, 49f, 5f);

        Transform oldBanner = legacy.transform.Find("SkinQuestTitleBanner");
        if (oldBanner != null) UnityEngine.Object.DestroyImmediate(oldBanner.gameObject);

        Button close = Require(window, "CloseButton").GetComponent<Button>();
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(30f, 30f);
        closeRect.anchoredPosition = new Vector2(-14f, -14f);
        EnsureImage(close.gameObject, Load("Assets/Resources/UI/Inventory/LightFantasy/inventory_close_thin_hd.png")).preserveAspect = true;
        TMP_Text label = close.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.enabled = false;

        Image divider = ChildImage(window, "HeaderDivider", null);
        RectTransform dividerRect = divider.rectTransform;
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(1f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.offsetMin = new Vector2(18f, -42f);
        dividerRect.offsetMax = new Vector2(-18f, -40.5f);
        divider.color = Gold;
    }

    private static void ConfigureList(Transform panel, TMP_FontAsset font)
    {
        Center(panel.GetComponent<RectTransform>(), new Vector2(190f, 270f), new Vector2(-158f, -22f));
        Image panelImage = EnsureImage(panel.gameObject, null);
        panelImage.color = Color.clear;

        Transform obsoleteFilters = panel.Find("Filters");
        if (obsoleteFilters != null) UnityEngine.Object.DestroyImmediate(obsoleteFilters.gameObject);

        Transform content = panel.Find("Viewport/Content") ?? panel.Find("Content");
        if (content == null) throw new InvalidOperationException("Missing Quest Log object: QuestListPanel/Viewport/Content");
        Transform viewport = panel.Find("Viewport");
        if (viewport == null)
        {
            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(panel, false);
            viewport = viewportObject.transform;
        }
        viewport.SetAsFirstSibling();
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 8f);
        viewportRect.offsetMax = new Vector2(-25f, -8f);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportImage.raycastTarget = true;

        content.SetParent(viewport, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup vertical = content.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
        {
            vertical.spacing = 4f;
            vertical.padding = new RectOffset(0, 0, 0, 0);
            vertical.childControlWidth = true;
            vertical.childForceExpandWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandHeight = false;
        }
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Scrollbar scrollbar = BuildScrollbar(panel);
        ScrollRect scrollRect = panel.GetComponent<ScrollRect>() ?? panel.gameObject.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 22f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = 0f;

        Transform row = Require(content, "QuestRowTemplate");
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 50f);
        LayoutElement rowLayout = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
        rowLayout.minWidth = -1f;
        rowLayout.preferredWidth = -1f;
        rowLayout.flexibleWidth = 1f;
        rowLayout.preferredHeight = 50f;
        Image rowImage = EnsureImage(row.gameObject, null);
        rowImage.color = new Color(0.075f, 0.068f, 0.085f, 0.94f);
        Outline rowOutline = row.GetComponent<Outline>() ?? row.gameObject.AddComponent<Outline>();
        rowOutline.effectColor = new Color(0.42f, 0.31f, 0.25f, 0.9f);
        rowOutline.effectDistance = new Vector2(1f, -1f);
        Button rowButton = row.GetComponent<Button>();
        if (rowButton != null)
        {
            MainMenuButtonHoverVisual legacyHover = row.GetComponent<MainMenuButtonHoverVisual>();
            if (legacyHover != null)
                UnityEngine.Object.DestroyImmediate(legacyHover);
            rowButton.transition = Selectable.Transition.ColorTint;
            rowButton.targetGraphic = rowImage;
            ColorBlock colors = rowButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.05f, 0.98f, 1f);
            colors.selectedColor = new Color(0.48f, 0.24f, 0.30f, 1f);
            colors.pressedColor = new Color(0.35f, 0.18f, 0.22f, 1f);
            rowButton.colors = colors;
        }

        Image icon = ChildImage(row, "CategoryIcon", Load("Assets/Resources/UI/Quest/Tracker1920/category_main.png"));
        Place(icon.rectTransform, new Vector2(28f, 28f), new Vector2(10f, -10f), new Vector2(0f, 1f));
        icon.preserveAspect = true;

        TMP_Text title = Require(row, "Title").GetComponent<TMP_Text>();
        Style(title, font, 9f, Color.white, TextAlignmentOptions.Left);
        Rect(title.rectTransform, new Vector2(114f, 17f), new Vector2(40f, -7f));
        TMP_Text status = Require(row, "Status").GetComponent<TMP_Text>();
        Style(status, font, 7f, new Color(0.82f, 0.90f, 1f, 1f), TextAlignmentOptions.Left);
        Rect(status.rectTransform, new Vector2(76f, 13f), new Vector2(40f, -29f));
        TMP_Text category = ChildText(row, "Category", font, 7f, Gold);
        category.alignment = TextAlignmentOptions.Center;
        Rect(category.rectTransform, new Vector2(48f, 13f), new Vector2(113f, -29f));
        Image pin = ChildImage(row, "TrackedPin", Load("Assets/Resources/UI/Quest/Tracker1920/objective_location.png"));
        Place(pin.rectTransform, new Vector2(12f, 12f), new Vector2(-10f, -25f), new Vector2(1f, 1f));
        pin.preserveAspect = true;

        TMP_Text empty = Require(panel, "EmptyText").GetComponent<TMP_Text>();
        Style(empty, font, 9f, Muted, TextAlignmentOptions.Center);
        empty.text = "NO QUESTS IN THIS CATEGORY";
        empty.rectTransform.offsetMin = new Vector2(12f, 12f);
        empty.rectTransform.offsetMax = new Vector2(-28f, -12f);
    }

    private static Scrollbar BuildScrollbar(Transform panel)
    {
        Transform old = panel.Find("Scrollbar");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

        GameObject root = new("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        root.transform.SetParent(panel, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.offsetMin = new Vector2(-20f, 12f);
        rect.offsetMax = new Vector2(-7f, -12f);
        Image track = root.GetComponent<Image>();
        track.color = new Color(0.06f, 0.055f, 0.07f, 0.85f);

        GameObject sliding = new("SlidingArea", typeof(RectTransform));
        sliding.transform.SetParent(root.transform, false);
        Stretch(sliding.GetComponent<RectTransform>(), 2f);
        GameObject handle = new("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(sliding.transform, false);
        Stretch(handle.GetComponent<RectTransform>(), 0f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.04f, 0.31f, 0.78f, 1f);
        Outline outline = handle.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(1f, -1f);

        Scrollbar scrollbar = root.GetComponent<Scrollbar>();
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    private static void ConfigureDetail(Transform panel, TMP_FontAsset font)
    {
        Center(panel.GetComponent<RectTransform>(), new Vector2(330f, 270f), new Vector2(100f, -22f));
        Image panelImage = EnsureImage(panel.gameObject, null);
        panelImage.color = Color.clear;

        Image icon = ChildImage(panel, "CategoryIcon", Load("Assets/Resources/UI/Quest/Tracker1920/category_main.png"));
        Place(icon.rectTransform, new Vector2(28f, 28f), new Vector2(29f, -30.5f), new Vector2(0f, 1f));
        icon.preserveAspect = true;

        TMP_Text title = Require(panel, "Title").GetComponent<TMP_Text>();
        Style(title, font, 13.5f, Cream, TextAlignmentOptions.Left);
        Rect(title.rectTransform, new Vector2(276f, 23f), new Vector2(36f, -10f));
        TMP_Text category = ChildText(panel, "CategoryLabel", font, 8.5f, Gold);
        Rect(category.rectTransform, new Vector2(128f, 15f), new Vector2(66f, -37f));
        TMP_Text status = Require(panel, "Status").GetComponent<TMP_Text>();
        Style(status, font, 8.5f, new Color(0.45f, 0.80f, 0.55f, 1f), TextAlignmentOptions.Right);
        Rect(status.rectTransform, new Vector2(110f, 15f), new Vector2(198f, -37f));

        TMP_Text objectiveHeading = ChildText(panel, "ObjectivesHeading", font, 10.5f, Gold);
        objectiveHeading.text = "OBJECTIVES";
        Rect(objectiveHeading.rectTransform, new Vector2(120f, 18f), new Vector2(36f, -68f));
        Image objectiveIcon = ChildImage(panel, "ObjectiveIcon", Load("Assets/Resources/UI/Quest/Tracker1920/objective_kill.png"));
        Place(objectiveIcon.rectTransform, new Vector2(26f, 26f), new Vector2(30f, -90f), new Vector2(0f, 1f));
        objectiveIcon.preserveAspect = true;

        TMP_Text objectives = Require(panel, "Objectives").GetComponent<TMP_Text>();
        Style(objectives, font, 8.5f, Cream, TextAlignmentOptions.TopLeft);
        Rect(objectives.rectTransform, new Vector2(260f, 48f), new Vector2(64f, -97f));

        Transform rewards = ReplaceContainer(panel, "Rewards");
        RectTransform rewardsRect = rewards.GetComponent<RectTransform>();
        rewardsRect.anchorMin = new Vector2(0f, 1f);
        rewardsRect.anchorMax = new Vector2(1f, 1f);
        rewardsRect.pivot = new Vector2(0.5f, 1f);
        rewardsRect.offsetMin = new Vector2(16f, -214f);
        rewardsRect.offsetMax = new Vector2(-16f, -173f);
        TMP_Text rewardTitle = ChildText(rewards, "Title", font, 9.5f, Gold);
        rewardTitle.text = "REWARDS";
        rewardTitle.alignment = TextAlignmentOptions.Center;
        rewardTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        rewardTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        rewardTitle.rectTransform.offsetMin = new Vector2(0f, -17f);
        rewardTitle.rectTransform.offsetMax = Vector2.zero;
        TMP_Text rewardSummary = ChildText(rewards, "RewardSummary", font, 9f, Cream);
        rewardSummary.alignment = TextAlignmentOptions.Center;
        rewardSummary.rectTransform.anchorMin = new Vector2(0f, 0f);
        rewardSummary.rectTransform.anchorMax = new Vector2(1f, 0f);
        rewardSummary.rectTransform.offsetMin = Vector2.zero;
        rewardSummary.rectTransform.offsetMax = new Vector2(0f, 21f);

        Button track = Require(panel, "TrackQuestButton").GetComponent<Button>();
        Place(track.GetComponent<RectTransform>(), new Vector2(76f, 22f), new Vector2(46f, 20f), new Vector2(0f, 0f));
        StyleButton(track, font, Gold, "TRACK QUEST");
        Button abandon = Require(panel, "AbandonQuestButton").GetComponent<Button>();
        Place(abandon.GetComponent<RectTransform>(), new Vector2(76f, 22f), new Vector2(-26f, 20f), new Vector2(1f, 0f));
        StyleButton(abandon, font, new Color(0.55f, 0.13f, 0.12f, 1f), "ABANDON");
    }

    private static void CreateFilter(Transform parent, string name, string text, TMP_FontAsset font, bool selected)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = Load("Assets/Resources/UI/MainMenu/LightFantasy/landing_action_button.png");
        image.type = Image.Type.Sliced;
        image.color = selected ? Color.white : new Color(0.78f, 0.86f, 1f, 1f);
        TMP_Text label = ChildText(go.transform, "Label", font, 6.5f, Color.white);
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform, 1f);
    }

    private static void StyleButton(Button button, TMP_FontAsset font, Color tint, string fallback)
    {
        Image image = EnsureImage(button.gameObject, Load(ActionButtonPath));
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = new Color(tint.r, tint.g, tint.b, 1f);
        image.raycastTarget = true;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Style(label, font, 8.5f, Color.white, TextAlignmentOptions.Center);
            if (string.IsNullOrWhiteSpace(label.text)) label.text = fallback;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(3f, -2f);
            label.rectTransform.offsetMax = new Vector2(-3f, -8f);
        }
    }

    private static Transform ReplaceContainer(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Image ChildImage(Transform parent, string name, Sprite sprite)
    {
        Transform old = parent.Find(name);
        GameObject go = old != null ? old.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (old == null) go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text ChildText(Transform parent, string name, TMP_FontAsset font, float size, Color color)
    {
        Transform old = parent.Find(name);
        GameObject go = old != null ? old.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (old == null) go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>() ?? go.AddComponent<TextMeshProUGUI>();
        Style(text, font, size, color, TextAlignmentOptions.Left);
        return text;
    }

    private static void Style(TMP_Text text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.margin = Vector4.zero;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static Image EnsureImage(GameObject go, Sprite sprite)
    {
        Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static Sprite Load(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer
            && (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite nested) return nested;
        throw new InvalidOperationException("Sprite not imported: " + path);
    }

    private static Transform Require(Transform parent, string path) =>
        parent.Find(path) ?? throw new InvalidOperationException("Missing Quest Log object: " + parent.name + "/" + path);

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Rect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Place(RectTransform rect, Vector2 size, Vector2 position, Vector2 anchor)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void TopStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top);
        rect.offsetMax = new Vector2(-right, -bottom);
    }
}
