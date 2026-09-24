using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryLightFantasySkinBuilder
{
    private const string ControllerPrefab = "Assets/Prefabs/InventoryUIController.prefab";
    private const string SlotPrefab = "Assets/Prefabs/InventorySlotUI.prefab";
    private const string SkinRoot = "Assets/Resources/UI/Inventory/LightFantasy/";
    private const string QuestStyleRoot = "Assets/Resources/UI/Inventory/QuestStyle1920/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply Inventory Light Fantasy Skin")]
    public static void Apply()
    {
        Sprite board = ImportSprite(QuestStyleRoot + "inventory_unified_reference_v5.png");
        Sprite equipment = ImportSprite(QuestStyleRoot + "inventory_tooltip_subtle_v2.png");
        Sprite slot = ImportSprite(QuestStyleRoot + "inventory_slot_reference_v4.png");
        Sprite close = ImportSprite(SkinRoot + "inventory_close_thin_hd.png");
        Sprite title = ImportSprite(SkinRoot + "inventory_title_hd.png");
        Sprite gridFrame = ImportSprite(SkinRoot + "inventory_grid_border_hd.png");
        Sprite goldBadge = ImportSprite(SkinRoot + "inventory_gold_badge_hd.png");
        Sprite tooltipBoard = equipment;
        Sprite statIcon = ImportSprite("Assets/Resources/UI/Gameplay/UnifiedHUD/LightFantasy/stat_icon.png");

        EditPrefab(SlotPrefab, root =>
        {
            SetSprite(root, slot);
            SetIconSafeArea(Find(root.transform, "Icon"));
        });
        EditPrefab(ControllerPrefab, root =>
        {
            RemoveLegacyEquipmentArtwork(root.transform);
            ConfigureUnifiedLayout(root.transform);
            BuildOpaqueBackdrop(root.transform);
            SetSprite(Find(root.transform, "InventoryPanel"), board);
            SetSprite(Find(root.transform, "EquipmentPanel"), equipment);
            SetSprite(Find(root.transform, "CloseBtn"), close);
            SetSprite(Find(root.transform, "TitleInventory"), title);
            SetGoldBadge(Find(root.transform, "Gold"), goldBadge);
            SetGridFrame(
                Find(root.transform, "GridScrollView"),
                Find(root.transform, "Viewport"),
                gridFrame);

            string[] equipmentSlots =
            {
                "HeadSlot", "BodySlot", "FootSlot", "WeaponSlot",
                "ShieldSlot", "NecklaceSlot", "RingSlot"
            };
            foreach (string slotName in equipmentSlots)
            {
                GameObject equipmentSlot = Find(root.transform, slotName);
                SetSprite(equipmentSlot, slot);
                SetIconSafeArea(Find(equipmentSlot.transform, "Icon"));
                BuildEquipmentPlaceholder(equipmentSlot);
            }

            BuildTooltip(root.transform, tooltipBoard);
            BuildCharacterPreview(root.transform);
            BuildStatsPanel(root.transform, statIcon);
            HideBakedOverlayDuplicates(root.transform);
        });

        AssetDatabase.SaveAssets();
        Debug.Log("Inventory Light Fantasy skin applied to source prefabs.");
    }

    private static Sprite ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"Texture importer not found: {path}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EditPrefab(string path, Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject Find(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child.gameObject;
            }
        }

        throw new InvalidOperationException($"Required Inventory UI object not found: {objectName}");
    }

    private static Transform FindOptional(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName) return child;
        }

        return null;
    }

    private static void SetSprite(GameObject target, Sprite sprite)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            throw new InvalidOperationException($"Image component not found on: {target.name}");
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
    }

    private static void SetIconSafeArea(GameObject iconObject)
    {
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        Image image = iconObject.GetComponent<Image>();
        if (rect == null || image == null)
        {
            throw new InvalidOperationException($"Inventory icon requires RectTransform and Image: {iconObject.name}");
        }

        rect.anchorMin = new Vector2(0.12f, 0.12f);
        rect.anchorMax = new Vector2(0.88f, 0.88f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        image.preserveAspect = true;
    }

    private static void SetBackgroundSprite(GameObject target, Sprite sprite)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void SetGridFrame(GameObject scrollView, GameObject viewport, Sprite sprite)
    {
        Image scrollViewImage = scrollView.GetComponent<Image>();
        if (scrollViewImage != null)
        {
            scrollViewImage.sprite = null;
            scrollViewImage.color = new Color(1f, 1f, 1f, 0f);
        }

        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage != null)
        {
            viewportImage.sprite = null;
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
        }

        Transform existing = scrollView.transform.Find("GridFrame");
        if (existing == null)
        {
            existing = viewport.transform.Find("GridFrame");
        }

        GameObject frame = existing != null
            ? existing.gameObject
            : new GameObject("GridFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        frame.transform.SetParent(scrollView.transform, false);
        frame.transform.SetAsLastSibling();

        RectTransform rect = (RectTransform)frame.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-12f, -12f);
        rect.offsetMax = new Vector2(12f, 12f);

        SetBackgroundSprite(frame, sprite);
    }

    private static void SetGoldBadge(GameObject gold, Sprite sprite)
    {
        Transform inventoryPanel = gold.transform.parent;
        while (inventoryPanel != null && inventoryPanel.name != "InventoryPanel")
        {
            inventoryPanel = inventoryPanel.parent;
        }
        if (inventoryPanel == null)
        {
            throw new InvalidOperationException("Gold currency must be under InventoryPanel.");
        }
        Transform rowTransform = inventoryPanel.Find("CurrencyRow");
        GameObject row = rowTransform != null
            ? rowTransform.gameObject
            : new GameObject("CurrencyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(inventoryPanel, false);

        RectTransform rowRect = (RectTransform)row.transform;
        rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(126f, -123f);
        rowRect.sizeDelta = new Vector2(280f, 23f);

        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.spacing = 6f;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        gold.transform.SetParent(row.transform, false);
        HorizontalLayoutGroup legacyGoldLayout = gold.GetComponent<HorizontalLayoutGroup>();
        if (legacyGoldLayout != null) UnityEngine.Object.DestroyImmediate(legacyGoldLayout);
        foreach (Transform candidate in inventoryPanel.GetComponentsInChildren<Transform>(true))
        {
            if (candidate != row.transform && candidate.name == "CurrencyRow")
            {
                UnityEngine.Object.DestroyImmediate(candidate.gameObject);
            }
        }
        RectTransform goldRect = (RectTransform)gold.transform;
        goldRect.anchorMin = goldRect.anchorMax = goldRect.pivot = new Vector2(0.5f, 0.5f);
        goldRect.anchoredPosition = Vector2.zero;
        goldRect.sizeDelta = new Vector2(260f, 20f);
        LayoutElement goldLayout = gold.GetComponent<LayoutElement>();
        if (goldLayout == null) goldLayout = gold.AddComponent<LayoutElement>();
        goldLayout.preferredWidth = 260f;
        goldLayout.preferredHeight = 20f;

        Transform existing = gold.transform.Find("GoldBadge");
        GameObject badge = existing != null
            ? existing.gameObject
            : new GameObject("GoldBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        badge.transform.SetParent(gold.transform, false);
        badge.transform.SetAsFirstSibling();

        LayoutElement layoutElement = badge.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = badge.AddComponent<LayoutElement>();
        }
        layoutElement.ignoreLayout = true;

        RectTransform rect = (RectTransform)badge.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-3f, -2f);
        rect.offsetMax = new Vector2(3f, 2f);

        // The v5 board owns the single currency well. Keep this runtime object
        // only for its icon/text binding so a second decorative frame cannot
        // overlap or drift away from the board artwork.
        SetBackgroundSprite(badge, sprite);
        badge.GetComponent<Image>().color = Color.clear;

        TextMeshProUGUI goldText = Find(gold.transform, "GoldText").GetComponent<TextMeshProUGUI>();
        if (goldText != null)
        {
            goldText.color = new Color(1f, 0.88f, 0.38f, 1f);
            RectTransform textRect = goldText.rectTransform;
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(14f, 0f);
            textRect.sizeDelta = new Vector2(52f, 18f);
            goldText.alignment = TextAlignmentOptions.Center;
        }

        RectTransform iconRect = Find(gold.transform, "GoldIcon").GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-22f, 0f);
        iconRect.sizeDelta = new Vector2(18f, 18f);
    }

    private static void BuildEquipmentPlaceholder(GameObject slotObject)
    {
        EquipmentSlotUI slotUI = slotObject.GetComponent<EquipmentSlotUI>();
        if (slotUI == null) return;

        string assetPath = slotUI.Slot switch
        {
            EquipSlot.Head => "Assets/Resources/Items/Head/HeadLv4.asset",
            EquipSlot.Body => "Assets/Resources/Items/Body/BodyLv2.asset",
            EquipSlot.Foot => "Assets/Resources/Items/Foot/FootLv1.asset",
            EquipSlot.Weapon => "Assets/Resources/Items/Weapon/SwordLv1.asset",
            EquipSlot.Shield => "Assets/Resources/Items/Shield/ShieldLv1.asset",
            EquipSlot.Necklace => "Assets/Resources/Items/Necklace/NecklaceLv1.asset",
            EquipSlot.Ring => "Assets/Resources/Items/Ring/RingLv1.asset",
            _ => null
        };
        EquipmentItemSO sample = string.IsNullOrEmpty(assetPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<EquipmentItemSO>(assetPath);

        Transform existing = slotObject.transform.Find("EmptySlotHint");
        GameObject hintObject = existing != null
            ? existing.gameObject
            : new GameObject("EmptySlotHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hintObject.transform.SetParent(slotObject.transform, false);
        hintObject.transform.SetAsFirstSibling();
        Image hint = hintObject.GetComponent<Image>();
        hint.sprite = sample != null ? sample.icon : null;
        hint.color = new Color(0.035f, 0.025f, 0.02f, 0.48f);
        hint.preserveAspect = true;
        hint.raycastTarget = false;
        RectTransform rect = hint.rectTransform;
        rect.anchorMin = new Vector2(0.20f, 0.20f);
        rect.anchorMax = new Vector2(0.80f, 0.80f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        SerializedObject serialized = new(slotUI);
        serialized.FindProperty("_placeholderImage").objectReferenceValue = hint;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureUnifiedLayout(Transform root)
    {
        RectTransform inventory = Find(root, "InventoryPanel").GetComponent<RectTransform>();
        RectTransform equipment = Find(root, "EquipmentPanel").GetComponent<RectTransform>();
        inventory.anchoredPosition = new Vector2(0f, -36f);
        inventory.sizeDelta = new Vector2(600f, 335f);
        equipment.anchoredPosition = new Vector2(-150f, 0f);
        equipment.sizeDelta = new Vector2(240f, 250f);
        // Unified board is rendered by InventoryPanel. Keep the transparent
        // EquipmentPanel above it so equipped item icons and the live preview
        // are not hidden behind the board image.
        equipment.SetAsLastSibling();

        RectTransform grid = Find(root, "GridScrollView").GetComponent<RectTransform>();
        grid.anchoredPosition = new Vector2(126f, 8f);
        grid.sizeDelta = new Vector2(286f, 226f);

        GridLayoutGroup gridLayout = Find(root, "GridSlot").GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 6;
        gridLayout.cellSize = new Vector2(41f, 41f);
        gridLayout.spacing = new Vector2(4f, 4f);

        RectTransform title = Find(root, "TitleInventory").GetComponent<RectTransform>();
        title.anchoredPosition = new Vector2(0f, 145f);
        RectTransform close = Find(root, "CloseBtn").GetComponent<RectTransform>();
        close.anchoredPosition = new Vector2(270f, 145f);
        close.sizeDelta = new Vector2(24f, 24f);

        string[] names = { "HeadSlot", "BodySlot", "FootSlot", "WeaponSlot", "ShieldSlot", "NecklaceSlot", "RingSlot" };
        Vector2[] positions =
        {
            new(-96f, 76f), new(-96f, 30f), new(-96f, -16f),
            new(96f, 76f), new(96f, 36f), new(96f, -5f), new(96f, -46f)
        };
        for (int i = 0; i < names.Length; i++)
        {
            RectTransform slot = Find(root, names[i]).GetComponent<RectTransform>();
            slot.anchoredPosition = positions[i];
            slot.sizeDelta = new Vector2(34f, 34f);
        }

        Image equipmentImage = equipment.GetComponent<Image>();
        if (equipmentImage != null) equipmentImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private static void BuildCharacterPreview(Transform root)
    {
        Transform equipment = Find(root, "EquipmentPanel").transform;
        Transform existing = equipment.Find("CharacterPreview");
        GameObject preview = existing != null
            ? existing.gameObject
            : new GameObject("CharacterPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(InventoryCharacterPreviewUI));
        preview.transform.SetParent(equipment, false);
        preview.transform.SetAsFirstSibling();
        RectTransform rect = (RectTransform)preview.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 43f);
        rect.sizeDelta = new Vector2(112f, 150f);
        RawImage image = preview.GetComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void BuildOpaqueBackdrop(Transform root)
    {
        RectTransform inventory = Find(root, "InventoryPanel").GetComponent<RectTransform>();
        Transform existing = FindOptional(root, "InventoryOpaqueBackdrop");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        int inventorySiblingIndex = inventory.GetSiblingIndex();
        GameObject backdrop = new("InventoryOpaqueBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(inventory.parent, false);
        backdrop.transform.SetSiblingIndex(inventorySiblingIndex);
        RectTransform rect = (RectTransform)backdrop.transform;
        rect.anchorMin = inventory.anchorMin;
        rect.anchorMax = inventory.anchorMax;
        rect.pivot = inventory.pivot;
        rect.anchoredPosition = inventory.anchoredPosition;
        rect.sizeDelta = new Vector2(554f, 305f);
        Image image = backdrop.GetComponent<Image>();
        image.sprite = null;
        image.color = new Color(0.13f, 0.095f, 0.07f, 1f);
        image.raycastTarget = false;
    }

    private static void BuildStatsPanel(Transform root, Sprite iconSprite)
    {
        Transform inventory = Find(root, "InventoryPanel").transform;
        Transform existing = inventory.Find("InventoryStats");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        TMP_FontAsset font = Find(root, "GoldText").GetComponent<TMP_Text>().font;
        GameObject panel = new("InventoryStats", typeof(RectTransform), typeof(InventoryStatsUI));
        panel.transform.SetParent(inventory, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-150f, -90f);
        panelRect.sizeDelta = new Vector2(226f, 82f);

        string[] labels = { "HP", "ATK", "DEF", "SPD", "CRIT", "STA" };
        Color[] colors =
        {
            new(0.86f, 0.28f, 0.24f), new(0.94f, 0.55f, 0.19f), new(0.30f, 0.65f, 0.92f),
            new(0.35f, 0.82f, 0.54f), new(0.75f, 0.43f, 0.96f), new(0.30f, 0.82f, 0.90f)
        };
        TMP_Text[] values = new TMP_Text[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            int column = i % 2;
            int row = i / 2;
            float x = -55f + column * 110f;
            float y = 24f - row * 24f;
            Image icon = CreateImage(panel.transform, labels[i] + "Icon");
            icon.sprite = iconSprite;
            icon.color = colors[i];
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(x - 38f, y);
            iconRect.sizeDelta = new Vector2(13f, 13f);

            TMP_Text label = CreateText(panel.transform, labels[i] + "Label", font, 7.5f,
                new Color(0.72f, 0.65f, 0.54f), TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(x - 18f, y);
            label.rectTransform.sizeDelta = new Vector2(32f, 15f);
            label.text = labels[i];

            TMP_Text value = CreateText(panel.transform, labels[i] + "Value", font, 8.5f,
                new Color(0.96f, 0.90f, 0.76f), TextAlignmentOptions.Right);
            value.rectTransform.anchorMin = value.rectTransform.anchorMax = value.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            value.rectTransform.anchoredPosition = new Vector2(x + 26f, y);
            value.rectTransform.sizeDelta = new Vector2(52f, 16f);
            value.text = "--";
            values[i] = value;
        }

        SerializedObject serialized = new(panel.GetComponent<InventoryStatsUI>());
        SerializedProperty property = serialized.FindProperty("_valueTexts");
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveLegacyEquipmentArtwork(Transform root)
    {
        // These images belonged to the old static mannequin layout. Keeping them
        // would place a large baked silhouette over the real animated Player preview.
        string[] legacyNames = { "EquipmentFullBody", " Necklace", "Ring" };
        foreach (string legacyName in legacyNames)
        {
            Transform legacy = FindOptional(root, legacyName);
            if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy.gameObject);
        }
    }

    private static void HideBakedOverlayDuplicates(Transform root)
    {
        // The new board already contains its title plate, close glyph and grid well.
        // Keep the existing controls for behaviour/raycasting but hide duplicate art.
        Image title = Find(root, "TitleInventory").GetComponent<Image>();
        title.color = Color.clear;
        title.raycastTarget = false;

        Image close = Find(root, "CloseBtn").GetComponent<Image>();
        close.color = Color.clear;

        Transform gridFrame = FindOptional(root, "GridFrame");
        if (gridFrame != null)
        {
            Image frame = gridFrame.GetComponent<Image>();
            if (frame != null) frame.color = Color.clear;
        }
    }

    private static void BuildTooltip(Transform root, Sprite board)
    {
        Transform existing = root.Find("ItemTooltip");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        TMP_FontAsset font = Find(root, "GoldText").GetComponent<TMP_Text>().font;
        GameObject panel = new("ItemTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(InventoryItemTooltipUI));
        panel.transform.SetParent(root, false);
        panel.transform.SetAsLastSibling();

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        // PositionBeside calculates coordinates in the root-canvas centre space.
        // Keep the tooltip anchored to that same origin so it does not jump to
        // the screen's top-left corner when it is shown.
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(190f, 180f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = board;
        panelImage.type = Image.Type.Simple;
        panelImage.color = Color.white;
        panelImage.raycastTarget = false;
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 0.98f;
        group.blocksRaycasts = false;
        group.interactable = false;

        Image icon = CreateImage(panel.transform, "ItemIcon");
        SetRect(icon.rectTransform, new Vector2(18f, 18f), new Vector2(42f, -25f));
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text name = CreateText(panel.transform, "ItemName", font, 9f, new Color(0.95f, 0.82f, 0.35f, 1f), TextAlignmentOptions.Left);
        SetRect(name.rectTransform, new Vector2(98f, 20f), new Vector2(68f, -18f));
        name.enableAutoSizing = true;
        name.fontSizeMin = 6.5f;
        name.fontSizeMax = 9f;
        name.enableWordWrapping = false;

        TMP_Text type = CreateText(panel.transform, "ItemType", font, 6.5f, new Color(0.80f, 0.76f, 0.70f, 1f), TextAlignmentOptions.Left);
        SetRect(type.rectTransform, new Vector2(98f, 14f), new Vector2(68f, -39f));
        type.enableWordWrapping = false;

        TMP_Text stats = CreateText(panel.transform, "Stats", font, 7f, new Color(0.94f, 0.90f, 0.80f, 1f), TextAlignmentOptions.TopLeft);
        SetRect(stats.rectTransform, new Vector2(132f, 52f), new Vector2(32f, -70f));
        stats.lineSpacing = 2f;

        TMP_Text description = CreateText(panel.transform, "Description", font, 6.5f, new Color(0.78f, 0.71f, 0.60f, 1f), TextAlignmentOptions.TopLeft);
        TMP_FontAsset unicodeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (unicodeFont != null) description.font = unicodeFont;
        SetRect(description.rectTransform, new Vector2(132f, 38f), new Vector2(32f, -128f));
        description.lineSpacing = 1f;

        SerializedObject serialized = new(panel.GetComponent<InventoryItemTooltipUI>());
        serialized.FindProperty("_panel").objectReferenceValue = panelRect;
        serialized.FindProperty("_icon").objectReferenceValue = icon;
        serialized.FindProperty("_nameText").objectReferenceValue = name;
        serialized.FindProperty("_typeText").objectReferenceValue = type;
        serialized.FindProperty("_statsText").objectReferenceValue = stats;
        serialized.FindProperty("_descriptionText").objectReferenceValue = description;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        // Keep the component active in the authored prefab so Awake can
        // register the shared hover service. Awake immediately hides the
        // visual panel until an InventorySlotUI receives PointerEnter.
        panel.SetActive(true);
    }

    private static Image CreateImage(Transform parent, string name)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
