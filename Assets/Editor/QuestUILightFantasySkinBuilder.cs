using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class QuestUILightFantasySkinBuilder
{
    private const string QuestRoot = "Assets/Resources/UI/Quest/LightFantasy/";
    private const string MainMenuRoot = "Assets/Resources/UI/MainMenu/LightFantasy/";
    private const string InventoryRoot = "Assets/Resources/UI/Inventory/LightFantasy/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply Quest UI Light Fantasy Skin")]
    public static void Apply()
    {
        QuestLogUI questUI = UnityEngine.Object.FindAnyObjectByType<QuestLogUI>(FindObjectsInactive.Include);
        if (questUI == null) throw new InvalidOperationException("QuestLogUI was not found in the active scene.");

        Transform root = questUI.transform;
        Transform tracker = RequireChild(root, "QuestTracker");
        Transform logRoot = RequireChild(root, "QuestLogWindow");
        Transform window = RequireChild(logRoot, "Window");
        Transform listPanel = RequireChild(window, "QuestListPanel");
        Transform detailPanel = RequireChild(window, "QuestDetailPanel");

        Sprite logBoard = ImportSprite(QuestRoot + "quest_log_board_hd.png");
        Sprite innerPanel = ImportSprite(QuestRoot + "quest_inner_panel_hd.png");
        Sprite titleBanner = ImportSprite(QuestRoot + "quest_title_banner_hd.png");
        Sprite closeSprite = ImportSprite(InventoryRoot + "inventory_close_thin_hd.png");
        Sprite primaryButton = ImportSprite(MainMenuRoot + "landing_action_button.png");
        Sprite hoverButton = ImportSprite(MainMenuRoot + "landing_action_button_hover.png");

        SetImage(tracker.gameObject, innerPanel, false, false);
        tracker.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 230f);
        TMP_Text trackerTitle = RequireChild(tracker, "Title").GetComponent<TMP_Text>();
        trackerTitle.enabled = false;
        EnsureTrackerTitleBanner(tracker, titleBanner);
        TMP_Text trackerObjectives = RequireChild(tracker, "Objectives").GetComponent<TMP_Text>();
        SetTopLeftRect(trackerObjectives.rectTransform, new Vector2(158f, 145f), new Vector2(16f, -68f));
        StyleTrackerText(trackerObjectives, 11f, false);

        Image dim = logRoot.GetComponent<Image>();
        if (dim == null) dim = logRoot.gameObject.AddComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.035f, 0.022f, 0.012f, 0.72f);
        dim.raycastTarget = true;

        SetImage(window.gameObject, logBoard, false, true);
        window.GetComponent<RectTransform>().sizeDelta = new Vector2(650f, 380f);

        TMP_Text legacyHeader = RequireChild(window, "Header").GetComponent<TMP_Text>();
        legacyHeader.text = "QUEST";
        legacyHeader.enabled = false;
        EnsureTitleBanner(legacyHeader.transform, titleBanner);

        Button close = RequireChild(window, "CloseButton").GetComponent<Button>();
        SetImage(close.gameObject, closeSprite, true, true);
        close.transition = Selectable.Transition.None;
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.sizeDelta = new Vector2(36f, 36f);
        closeRect.anchoredPosition = new Vector2(-18f, -18f);
        TMP_Text closeLabel = close.GetComponentInChildren<TMP_Text>(true);
        if (closeLabel != null) closeLabel.enabled = false;

        SetImage(listPanel.gameObject, innerPanel, false, false);
        SetImage(detailPanel.gameObject, innerPanel, false, false);
        SetCenteredRect(listPanel.GetComponent<RectTransform>(), new Vector2(190f, 255f), new Vector2(-185f, -28f));
        SetCenteredRect(detailPanel.GetComponent<RectTransform>(), new Vector2(340f, 255f), new Vector2(105f, -28f));

        TMP_Text empty = RequireChild(listPanel, "EmptyText").GetComponent<TMP_Text>();
        SetStretchRect(empty.rectTransform, 16f, 18f, 16f, 18f);
        StyleText(empty, 12f, new Color(0.34f, 0.19f, 0.075f, 1f), TextAlignmentOptions.Center);

        TMP_Text detailTitle = RequireChild(detailPanel, "Title").GetComponent<TMP_Text>();
        if (detailTitle.text == "QUEST LOG") detailTitle.text = "QUEST";
        SetTopLeftRect(detailTitle.rectTransform, new Vector2(306f, 28f), new Vector2(17f, -66f));
        StyleText(detailTitle, 17f, new Color(0.22f, 0.11f, 0.035f, 1f), TextAlignmentOptions.TopLeft);
        TMP_Text status = RequireChild(detailPanel, "Status").GetComponent<TMP_Text>();
        SetTopLeftRect(status.rectTransform, new Vector2(306f, 20f), new Vector2(17f, -94f));
        StyleText(status, 11f, new Color(0.10f, 0.34f, 0.24f, 1f), TextAlignmentOptions.TopLeft);
        TMP_Text objectives = RequireChild(detailPanel, "Objectives").GetComponent<TMP_Text>();
        SetTopLeftRect(objectives.rectTransform, new Vector2(306f, 112f), new Vector2(17f, -120f));
        StyleText(objectives, 12f, new Color(0.31f, 0.17f, 0.065f, 1f), TextAlignmentOptions.TopLeft);

        Transform content = RequireChild(listPanel, "Content");
        Transform template = RequireChild(content, "QuestRowTemplate");
        Button rowButton = template.GetComponent<Button>();
        if (rowButton != null) StyleRowButton(rowButton, primaryButton, hoverButton);
        foreach (TMP_Text text in template.GetComponentsInChildren<TMP_Text>(true))
            StyleText(text, text.name == "Status" ? 9f : 11f, new Color(1f, 0.94f, 0.72f, 1f), TextAlignmentOptions.Left);

        EditorUtility.SetDirty(questUI.gameObject);
        EditorSceneManager.MarkSceneDirty(questUI.gameObject.scene);
        EditorSceneManager.SaveScene(questUI.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("QuestUIRoot Light Fantasy skin applied with QUEST title.");
    }

    private static void StyleTrackerText(TMP_Text text, float size, bool title)
    {
        StyleText(text, size, title ? new Color(0.22f, 0.11f, 0.035f, 1f) : new Color(0.31f, 0.17f, 0.065f, 1f), TextAlignmentOptions.TopLeft);
        text.margin = title ? new Vector4(8f, 3f, 8f, 0f) : new Vector4(8f, 2f, 8f, 2f);
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetTopLeftRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetStretchRect(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void StyleRowButton(Button button, Sprite normal, Sprite hover)
    {
        SetImage(button.gameObject, normal, false, true);
        button.transition = Selectable.Transition.None;
        button.targetGraphic = button.GetComponent<Image>();
        MainMenuButtonHoverVisual visual = button.GetComponent<MainMenuButtonHoverVisual>();
        if (visual == null) visual = button.gameObject.AddComponent<MainMenuButtonHoverVisual>();
        SerializedObject serialized = new SerializedObject(visual);
        serialized.FindProperty("_hoverSprite").objectReferenceValue = hover;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureTitleBanner(Transform header, Sprite sprite)
    {
        Transform existing = header.Find("SkinQuestTitleBanner");
        GameObject banner = existing != null ? existing.gameObject : new GameObject("SkinQuestTitleBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        banner.transform.SetParent(header, false);
        RectTransform rect = banner.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(220f, 80f);
        rect.anchoredPosition = new Vector2(-22f, 2f);
        SetImage(banner, sprite, true, false);
        banner.transform.SetAsLastSibling();
    }

    private static void EnsureTrackerTitleBanner(Transform tracker, Sprite sprite)
    {
        Transform existing = tracker.Find("SkinTrackerQuestBanner");
        GameObject banner = existing != null ? existing.gameObject : new GameObject("SkinTrackerQuestBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        banner.transform.SetParent(tracker, false);
        RectTransform rect = banner.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(142f, 52f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        SetImage(banner, sprite, true, false);
        banner.transform.SetAsLastSibling();
    }

    private static void StyleText(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
    {
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) throw new InvalidOperationException("Required Quest UI object missing: " + parent.name + "/" + name);
        return child;
    }

    private static void SetImage(GameObject target, Sprite sprite, bool preserveAspect, bool raycast)
    {
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = raycast;
        Outline outline = target.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
        EditorUtility.SetDirty(image);
    }

    private static Sprite ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Texture importer not found: " + path);
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
