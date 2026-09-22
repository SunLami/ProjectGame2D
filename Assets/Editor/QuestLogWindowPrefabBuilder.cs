using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class QuestLogWindowPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/GameplayUIRoot.prefab";
    private static readonly Color Cream = new(0.22f, 0.12f, 0.055f, 1f);
    private static readonly Color Gold = new(0.83f, 0.60f, 0.24f, 1f);
    private static readonly Color Muted = new(0.42f, 0.31f, 0.20f, 1f);
    private static readonly Color Blue = new(0.03f, 0.28f, 0.67f, 1f);

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
        Image image = EnsureImage(window.gameObject, Load("Assets/Resources/UI/Quest/QuestLog1920/quest_log_board_scroll_v2.png"));
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
        TopStretch(legacy.rectTransform, 54f, 20f, 54f, 10f);

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
        Center(panel.GetComponent<RectTransform>(), new Vector2(190f, 270f), new Vector2(-170f, -22f));
        Image panelImage = EnsureImage(panel.gameObject, null);
        panelImage.color = Color.clear;

        Transform obsoleteFilters = panel.Find("Filters");
        if (obsoleteFilters != null) UnityEngine.Object.DestroyImmediate(obsoleteFilters.gameObject);

        Transform content = Require(panel, "Content");
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
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbarSpacing = 0f;

        Transform row = Require(content, "QuestRowTemplate");
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 43f);
        LayoutElement rowLayout = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 43f;
        Image rowImage = EnsureImage(row.gameObject, Load("Assets/Resources/UI/MainMenu/LightFantasy/landing_action_button.png"));
        rowImage.type = Image.Type.Sliced;
        rowImage.color = Color.white;

        Image icon = ChildImage(row, "CategoryIcon", Load("Assets/Resources/UI/Quest/Tracker1920/category_main.png"));
        Place(icon.rectTransform, new Vector2(30f, 30f), new Vector2(20f, -21.5f), new Vector2(0f, 1f));
        icon.preserveAspect = true;

        TMP_Text title = Require(row, "Title").GetComponent<TMP_Text>();
        Style(title, font, 8.5f, Color.white, TextAlignmentOptions.Left);
        Rect(title.rectTransform, new Vector2(118f, 19f), new Vector2(42f, -7f));
        TMP_Text status = Require(row, "Status").GetComponent<TMP_Text>();
        Style(status, font, 6f, new Color(0.82f, 0.90f, 1f, 1f), TextAlignmentOptions.Left);
        Rect(status.rectTransform, new Vector2(95f, 12f), new Vector2(42f, -26f));
        TMP_Text category = ChildText(row, "Category", font, 5.5f, Gold);
        Rect(category.rectTransform, new Vector2(46f, 12f), new Vector2(104f, -26f));
        Image pin = ChildImage(row, "TrackedPin", Load("Assets/Resources/UI/Quest/Tracker1920/objective_location.png"));
        Place(pin.rectTransform, new Vector2(13f, 13f), new Vector2(-11f, -21.5f), new Vector2(1f, 1f));
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
        Place(icon.rectTransform, new Vector2(42f, 42f), new Vector2(30f, -31f), new Vector2(0f, 1f));
        icon.preserveAspect = true;

        TMP_Text title = Require(panel, "Title").GetComponent<TMP_Text>();
        Style(title, font, 14f, Cream, TextAlignmentOptions.Left);
        Rect(title.rectTransform, new Vector2(248f, 24f), new Vector2(57f, -13f));
        TMP_Text category = ChildText(panel, "CategoryLabel", font, 7f, Gold);
        Rect(category.rectTransform, new Vector2(130f, 15f), new Vector2(58f, -38f));
        TMP_Text status = Require(panel, "Status").GetComponent<TMP_Text>();
        Style(status, font, 7f, new Color(0.45f, 0.80f, 0.55f, 1f), TextAlignmentOptions.Right);
        Rect(status.rectTransform, new Vector2(120f, 15f), new Vector2(190f, -38f));

        TMP_Text objectiveHeading = ChildText(panel, "ObjectivesHeading", font, 9f, Gold);
        objectiveHeading.text = "OBJECTIVES";
        Rect(objectiveHeading.rectTransform, new Vector2(150f, 18f), new Vector2(16f, -70f));
        Image objectiveIcon = ChildImage(panel, "ObjectiveIcon", Load("Assets/Resources/UI/Quest/Tracker1920/objective_kill.png"));
        Place(objectiveIcon.rectTransform, new Vector2(25f, 25f), new Vector2(28f, -105f), new Vector2(0f, 1f));
        objectiveIcon.preserveAspect = true;

        TMP_Text objectives = Require(panel, "Objectives").GetComponent<TMP_Text>();
        Style(objectives, font, 8.5f, Cream, TextAlignmentOptions.TopLeft);
        Rect(objectives.rectTransform, new Vector2(270f, 62f), new Vector2(48f, -91f));

        Transform rewards = ReplaceContainer(panel, "Rewards");
        RectTransform rewardsRect = rewards.GetComponent<RectTransform>();
        rewardsRect.anchorMin = new Vector2(0f, 1f);
        rewardsRect.anchorMax = new Vector2(1f, 1f);
        rewardsRect.pivot = new Vector2(0.5f, 1f);
        rewardsRect.offsetMin = new Vector2(16f, -205f);
        rewardsRect.offsetMax = new Vector2(-16f, -164f);
        TMP_Text rewardTitle = ChildText(rewards, "Title", font, 8f, Gold);
        rewardTitle.text = "REWARDS";
        rewardTitle.alignment = TextAlignmentOptions.Center;
        rewardTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        rewardTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        rewardTitle.rectTransform.offsetMin = new Vector2(0f, -17f);
        rewardTitle.rectTransform.offsetMax = Vector2.zero;
        TMP_Text rewardSummary = ChildText(rewards, "RewardSummary", font, 7.5f, Cream);
        rewardSummary.alignment = TextAlignmentOptions.Center;
        rewardSummary.rectTransform.anchorMin = new Vector2(0f, 0f);
        rewardSummary.rectTransform.anchorMax = new Vector2(1f, 0f);
        rewardSummary.rectTransform.offsetMin = Vector2.zero;
        rewardSummary.rectTransform.offsetMax = new Vector2(0f, 21f);

        Button track = Require(panel, "TrackQuestButton").GetComponent<Button>();
        Place(track.GetComponent<RectTransform>(), new Vector2(116f, 30f), new Vector2(73f, 20f), new Vector2(0f, 0f));
        StyleButton(track, font, Gold, "TRACK QUEST");
        Button abandon = Require(panel, "AbandonQuestButton").GetComponent<Button>();
        Place(abandon.GetComponent<RectTransform>(), new Vector2(116f, 30f), new Vector2(-73f, 20f), new Vector2(1f, 0f));
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
        Image image = EnsureImage(button.gameObject, Load("Assets/Resources/UI/MainMenu/LightFantasy/landing_action_button.png"));
        image.type = Image.Type.Sliced;
        image.color = tint;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Style(label, font, 8f, Color.white, TextAlignmentOptions.Center);
            if (string.IsNullOrWhiteSpace(label.text)) label.text = fallback;
            Stretch(label.rectTransform, 3f);
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
