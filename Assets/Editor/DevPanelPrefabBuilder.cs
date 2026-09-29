using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds Assets/Prefabs/UI/Debug/DevPanel.prefab: the hand-editable authoring hierarchy
/// for DevPanelController (see that script's summary). Run once from the menu below, then edit the
/// prefab's RectTransforms/colors/text directly in Prefab Mode -- re-running this menu item
/// discards any such hand edits and rebuilds from scratch, same contract as D-046's
/// ShopWindow/CraftingWindow authoring prefabs (see DecisionRegister.md).
/// Only the fixed shell is built here; the Teleport row list is populated at runtime by
/// DevPanelController.RefreshTeleportButtons because its content varies per scene/session.</summary>
public static class DevPanelPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/Debug/DevPanel.prefab";
    private const float PanelWidth = 440f;
    private const float PanelHeight = 560f;
    private const float RowHeight = 34f;

    [MenuItem("Tools/ProjectGame2D/UI/Build Dev Panel")]
    public static void Build()
    {
        string folder = Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        GameObject root = new("DevPanel", typeof(RectTransform), typeof(DevPanelController));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            DevPanelController controller = root.GetComponent<DevPanelController>();

            Button toggle = CreateButton(rootRect, "DevToggleButton", "DEV", new Color(0.55f, 0.08f, 0.08f, 0.95f));
            RectTransform toggleRect = (RectTransform)toggle.transform;
            toggleRect.anchorMin = toggleRect.anchorMax = toggleRect.pivot = new Vector2(1f, 1f);
            toggleRect.sizeDelta = new Vector2(80f, 32f);
            toggleRect.anchoredPosition = new Vector2(-18f, -142f); // below the 96x96 Minimap anchored at (-14,-34)
            UnityEventTools.AddPersistentListener(toggle.onClick, controller.TogglePanel);

            GameObject panelObject = new("DevPanelRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(rootRect, false);
            RectTransform panelRect = (RectTransform)panelObject.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelRect.anchoredPosition = new Vector2(18f, -18f);
            panelObject.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.94f);

            TMP_Text feedbackText = BuildHeaderAndFeedback(panelRect, controller);
            RectTransform teleportContainer = BuildScrollContent(panelRect, controller);

            SerializedObject serialized = new(controller);
            serialized.FindProperty("_panelRoot").objectReferenceValue = panelObject;
            serialized.FindProperty("_feedbackText").objectReferenceValue = feedbackText;
            serialized.FindProperty("_teleportContainer").objectReferenceValue = teleportContainer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("DevPanelPrefabBuilder: rebuilt " + PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
    }

    private static TMP_Text BuildHeaderAndFeedback(RectTransform panelRect, DevPanelController controller)
    {
        RectTransform header = CreateRect("Header", panelRect);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 32f);
        header.anchoredPosition = Vector2.zero;

        TMP_Text title = CreateLabel(header, "Title", "DEV PANEL", 16, FontStyles.Bold);
        RectTransform titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(12f, 0f);
        titleRect.offsetMax = Vector2.zero;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        Button close = CreateButton(header, "CloseButton", "X", new Color(0.3f, 0.1f, 0.1f, 1f));
        RectTransform closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.sizeDelta = new Vector2(28f, 28f);
        closeRect.anchoredPosition = new Vector2(-2f, 0f);
        UnityEventTools.AddPersistentListener(close.onClick, controller.ClosePanel);

        // Feedback strip -- fixed under the header (not scrolling content) so the result of any
        // action anywhere in the panel is always visible without scrolling to find it.
        RectTransform feedbackStrip = CreateRect("FeedbackStrip", panelRect);
        feedbackStrip.anchorMin = new Vector2(0f, 1f);
        feedbackStrip.anchorMax = new Vector2(1f, 1f);
        feedbackStrip.pivot = new Vector2(0.5f, 1f);
        feedbackStrip.sizeDelta = new Vector2(0f, 36f);
        feedbackStrip.anchoredPosition = new Vector2(0f, -32f);
        Image feedbackBg = feedbackStrip.gameObject.AddComponent<Image>();
        feedbackBg.color = new Color(0f, 0f, 0f, 0.25f);
        feedbackBg.raycastTarget = false;

        TMP_Text feedbackText = CreateLabel(feedbackStrip, "FeedbackText", "Ready.", 12, FontStyles.Italic);
        RectTransform feedbackRect = (RectTransform)feedbackText.transform;
        feedbackRect.anchorMin = Vector2.zero;
        feedbackRect.anchorMax = Vector2.one;
        feedbackRect.offsetMin = new Vector2(12f, 2f);
        feedbackRect.offsetMax = new Vector2(-12f, -2f);
        feedbackText.alignment = TextAlignmentOptions.MidlineLeft;
        feedbackText.enableWordWrapping = true;
        feedbackText.color = new Color(1f, 0.85f, 0.4f, 1f);

        return feedbackText;
    }

    private static RectTransform BuildScrollContent(RectTransform panelRect, DevPanelController controller)
    {
        GameObject scrollObject = new("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(panelRect, false);
        RectTransform scrollRectTransform = (RectTransform)scrollObject.transform;
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = new Vector2(0f, -68f);
        scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f); // raycast target only

        GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = (RectTransform)viewportObject.transform;
        Stretch(viewportRect);
        viewportObject.GetComponent<Image>().color = Color.white;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = (RectTransform)contentObject.transform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero; // stretch anchors already give full width; nonzero
                                               // sizeDelta.x here would add extra width on top.

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(12, 12, 8, 12);
        contentLayout.spacing = 6f;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        // Must be true: children's own LayoutElement.preferredHeight only takes effect when the
        // parent group is allowed to resize them -- false silently ignores it and leaves every
        // child at its untouched creation-time default size instead.
        contentLayout.childControlHeight = true;

        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        Transform content = contentObject.transform;
        BuildProgressionSection(content, controller);
        RectTransform teleportContainer = BuildSpawnCurrencyCombatTeleportSections(content, controller);
        return teleportContainer;
    }

    private static void BuildProgressionSection(Transform content, DevPanelController controller)
    {
        CreateSectionLabel(content, "PROGRESSION");

        RectTransform row1 = CreateRow(content);
        Button levelUp = CreateButton(row1, "LevelUpButton", "Level Up +1", new Color(0.12f, 0.34f, 0.12f, 1f), flexible: true);
        UnityEventTools.AddPersistentListener(levelUp.onClick, controller.OnLevelUpClicked);

        RectTransform row2 = CreateRow(content);
        TMP_InputField setLevelField = CreateInputField(row2, "SetLevelField", "Level", TMP_InputField.ContentType.IntegerNumber, 90f);
        Button setLevel = CreateButton(row2, "SetLevelButton", "Set Level", new Color(0.12f, 0.34f, 0.12f, 1f), flexible: true);
        UnityEventTools.AddPersistentListener(setLevel.onClick, controller.OnSetLevelClicked);

        RectTransform row3 = CreateRow(content);
        Button fullHeal = CreateButton(row3, "FullHealButton", "Full Heal", new Color(0.6f, 0.1f, 0.1f, 1f), flexible: true);
        UnityEventTools.AddPersistentListener(fullHeal.onClick, controller.OnFullHealClicked);
        Button fullStamina = CreateButton(row3, "FullStaminaButton", "Full Stamina", new Color(0.1f, 0.35f, 0.45f, 1f), flexible: true);
        UnityEventTools.AddPersistentListener(fullStamina.onClick, controller.OnFullStaminaClicked);

        SerializedObject serialized = new(controller);
        serialized.FindProperty("_setLevelField").objectReferenceValue = setLevelField;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform BuildSpawnCurrencyCombatTeleportSections(Transform content, DevPanelController controller)
    {
        // -- Spawn Item --
        CreateSectionLabel(content, "SPAWN ITEM (equipment / material / seed / bait / fish)");
        RectTransform spawnRow = CreateRow(content);
        TMP_InputField itemIdField = CreateInputField(spawnRow, "ItemIdField", "itemId", TMP_InputField.ContentType.Standard, 180f);
        TMP_InputField amountField = CreateInputField(spawnRow, "AmountField", "Qty", TMP_InputField.ContentType.IntegerNumber, 60f);
        amountField.text = "1";
        Button spawn = CreateButton(spawnRow, "SpawnButton", "Spawn", new Color(0.5f, 0.35f, 0.05f, 1f), flexible: true);
        UnityEventTools.AddPersistentListener(spawn.onClick, controller.OnSpawnItemClicked);

        // -- Currency --
        CreateSectionLabel(content, "CURRENCY");
        RectTransform currencyRow = CreateRow(content);
        Button gold100 = CreateButton(currencyRow, "Gold100Button", "+100 Gold", new Color(0.45f, 0.4f, 0.05f, 1f), flexible: true);
        UnityEventTools.AddPersistentListener(gold100.onClick, controller.OnGold100Clicked);
        Button gold1000 = CreateButton(currencyRow, "Gold1000Button", "+1000 Gold", new Color(0.45f, 0.4f, 0.05f, 1f), flexible: true);
        UnityEventTools.AddPersistentListener(gold1000.onClick, controller.OnGold1000Clicked);
        Button gold9999 = CreateButton(currencyRow, "Gold9999Button", "+9999 Gold", new Color(0.45f, 0.4f, 0.05f, 1f), flexible: true);
        UnityEventTools.AddPersistentListener(gold9999.onClick, controller.OnGold9999Clicked);

        // -- Combat --
        CreateSectionLabel(content, "COMBAT");
        RectTransform combatRow = CreateRow(content);
        Toggle godModeToggle = CreateToggle(combatRow, "GodModeToggle", "God Mode (invulnerable)");

        // -- Inventory --
        CreateSectionLabel(content, "INVENTORY");
        RectTransform inventoryRow = CreateRow(content);
        Toggle seedStartingItemsToggle = CreateToggle(inventoryRow, "SeedStartingItemsToggle", "Seed Starting Items (next New Game)");

        // -- Teleport (shell only; rows are populated at runtime) --
        CreateSectionLabel(content, "TELEPORT");
        RectTransform teleportContainer = CreateRect("TeleportContainer", content);
        teleportContainer.anchorMin = new Vector2(0f, 1f);
        teleportContainer.anchorMax = new Vector2(1f, 1f);
        teleportContainer.pivot = new Vector2(0.5f, 1f);
        teleportContainer.sizeDelta = Vector2.zero;
        VerticalLayoutGroup teleportLayout = teleportContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        teleportLayout.childForceExpandWidth = true;
        teleportLayout.childForceExpandHeight = false;
        teleportLayout.childControlWidth = true;
        teleportLayout.childControlHeight = true;
        teleportLayout.spacing = 4f;

        SerializedObject serialized = new(controller);
        serialized.FindProperty("_itemIdField").objectReferenceValue = itemIdField;
        serialized.FindProperty("_amountField").objectReferenceValue = amountField;
        serialized.FindProperty("_godModeToggle").objectReferenceValue = godModeToggle;
        serialized.FindProperty("_seedStartingItemsToggle").objectReferenceValue = seedStartingItemsToggle;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return teleportContainer;
    }

    // ------------------------------------------------------------------------------- UI factories

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static RectTransform CreateRow(Transform parent, float height = RowHeight)
    {
        RectTransform row = CreateRect("Row", parent);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
        return row;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string text, float fontSize, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static void CreateSectionLabel(Transform parent, string text)
    {
        RectTransform row = CreateRow(parent, height: 22f);
        TMP_Text label = CreateLabel(row, "Label", text, 13, FontStyles.Bold);
        label.color = new Color(1f, 0.82f, 0.38f, 1f);
        RectTransform labelRect = (RectTransform)label.transform;
        Stretch(labelRect);
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, bool flexible = false)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateLabel(go.transform, "Label", label, 12, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.Center;
        Stretch((RectTransform)text.transform);

        LayoutElement element = go.AddComponent<LayoutElement>();
        if (flexible)
            element.flexibleWidth = 1f;
        else
            element.preferredWidth = 110f;

        return button;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, TMP_InputField.ContentType contentType, float width)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

        TMP_Text textComponent = CreateLabel(go.transform, "Text", string.Empty, 13);
        textComponent.color = Color.white;
        RectTransform textRect = (RectTransform)textComponent.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);

        TMP_Text placeholderComponent = CreateLabel(go.transform, "Placeholder", placeholder, 13, FontStyles.Italic);
        placeholderComponent.color = new Color(1f, 1f, 1f, 0.4f);
        RectTransform placeholderRect = (RectTransform)placeholderComponent.transform;
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(8f, 2f);
        placeholderRect.offsetMax = new Vector2(-8f, -2f);

        TMP_InputField field = go.GetComponent<TMP_InputField>();
        field.textComponent = textComponent;
        field.placeholder = placeholderComponent;
        field.contentType = contentType;

        go.AddComponent<LayoutElement>().preferredWidth = width;
        return field;
    }

    private static Toggle CreateToggle(Transform parent, string name, string label)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);

        GameObject background = new("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(go.transform, false);
        RectTransform backgroundRect = (RectTransform)background.transform;
        backgroundRect.anchorMin = backgroundRect.anchorMax = backgroundRect.pivot = new Vector2(0f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(22f, 22f);
        backgroundRect.anchoredPosition = new Vector2(2f, 0f);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.15f);

        GameObject checkmark = new("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkmark.transform.SetParent(background.transform, false);
        RectTransform checkmarkRect = (RectTransform)checkmark.transform;
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.offsetMin = new Vector2(3f, 3f);
        checkmarkRect.offsetMax = new Vector2(-3f, -3f);
        checkmark.GetComponent<Image>().color = new Color(0.9f, 0.2f, 0.2f, 1f);

        TMP_Text label2 = CreateLabel(go.transform, "Label", label, 13);
        RectTransform labelRect = (RectTransform)label2.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(30f, 0f);
        labelRect.offsetMax = Vector2.zero;
        label2.alignment = TextAlignmentOptions.MidlineLeft;

        Toggle toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.isOn = false;

        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return toggle;
    }
}
