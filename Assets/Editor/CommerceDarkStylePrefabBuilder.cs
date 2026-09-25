using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CommerceDarkStylePrefabBuilder
{
    private static bool _autoApplyQueued;
    private const string RootPrefab = "Assets/Prefabs/UI/GameplayUIRoot.prefab";
    private const string OutputFolder = "Assets/Prefabs/UI/Commerce";
    private const string ShopPrefab = OutputFolder + "/ShopWindow.prefab";
    private const string CraftPrefab = OutputFolder + "/CraftingWindow.prefab";
    private const string ShopBoardPath = "Assets/Resources/UI/Commerce/DarkInventoryStyle/commerce_shop_board_v2.png";
    private const string CraftBoardPath = "Assets/Resources/UI/Commerce/DarkInventoryStyle/commerce_crafting_board_v3.png";
    private const string SlotPrefabPath = "Assets/Prefabs/InventorySlotUI.prefab";
    private const string InventoryControllerPrefabPath = "Assets/Prefabs/InventoryUIController.prefab";
    private const string CloseSpritePath = "Assets/Resources/UI/Inventory/LightFantasy/inventory_close_hd.png";
    private const string WideButtonSpritePath = "Assets/Resources/UI/Commerce/DarkInventoryStyle/commerce_button_wide.png";
    private const string SquareButtonSpritePath = "Assets/Resources/UI/Commerce/DarkInventoryStyle/commerce_button_square.png";

    private static readonly Color Gold = new(0.94f, 0.72f, 0.25f, 1f);
    private static readonly Color Ivory = new(0.96f, 0.90f, 0.72f, 1f);
    private static readonly Color Brown = new(0.12f, 0.075f, 0.04f, 0.94f);
    private static readonly Color Hover = new(0.30f, 0.18f, 0.07f, 0.98f);
    private static TMP_FontAsset _font;
    private static Sprite _wideButtonSprite;
    private static Sprite _squareButtonSprite;

    [InitializeOnLoadMethod]
    private static void ApplyOnceWhenImported()
    {
        _autoApplyQueued = true;
        EditorApplication.update -= TryAutoApply;
        EditorApplication.update += TryAutoApply;
    }

    private static void TryAutoApply()
    {
        if (!_autoApplyQueued || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        _autoApplyQueued = false;
        EditorApplication.update -= TryAutoApply;
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefab);
        if (source == null) return;
        Transform commerce = FindDeep(source.transform, "CommerceUIRoot");
        if (commerce == null || commerce.Find("DarkCommerceV2") != null) return;
        Apply();
    }

    [MenuItem("Tools/ProjectGame2D/UI/Rebuild Dark Commerce Prefabs")]
    public static void Apply()
    {
        Sprite shopBoard = ImportSprite(ShopBoardPath);
        Sprite craftBoard = ImportSprite(CraftBoardPath);
        _wideButtonSprite = ImportSprite(WideButtonSpritePath);
        _squareButtonSprite = ImportSprite(SquareButtonSpritePath);
        _font = LoadFont();
        EnsureFolder(OutputFolder);

        GameObject root = PrefabUtility.LoadPrefabContents(RootPrefab);
        try
        {
            Transform commerce = FindDeep(root.transform, "CommerceUIRoot")
                ?? throw new InvalidOperationException("CommerceUIRoot missing from GameplayUIRoot.prefab.");
            ShopCraftingUI controller = commerce.GetComponent<ShopCraftingUI>()
                ?? throw new InvalidOperationException("ShopCraftingUI missing from CommerceUIRoot.");
            ClearChildren(commerce);

            GameObject marker = Rect("DarkCommerceV2", commerce, Vector2.zero, Vector2.zero);
            marker.SetActive(false);
            GameObject backdrop = Rect("Backdrop", commerce, Vector2.zero, Vector2.zero);
            Stretch(backdrop.GetComponent<RectTransform>());
            Image dim = backdrop.AddComponent<Image>();
            dim.color = new Color(0.01f, 0.008f, 0.005f, 0.70f);
            dim.raycastTarget = true;

            ShopRefs generatedShop = BuildShop(backdrop.transform, shopBoard, controller);
            CraftRefs generatedCraft = BuildCrafting(backdrop.transform, craftBoard);
            PrefabUtility.SaveAsPrefabAsset(generatedShop.Window, ShopPrefab);
            PrefabUtility.SaveAsPrefabAsset(generatedCraft.Window, CraftPrefab);

            UnityEngine.Object.DestroyImmediate(generatedShop.Window);
            UnityEngine.Object.DestroyImmediate(generatedCraft.Window);

            GameObject shopInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefab), backdrop.transform);
            GameObject craftInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(CraftPrefab), backdrop.transform);
            ShopRefs shop = CollectShopRefs(shopInstance);
            CraftRefs craft = CollectCraftRefs(craftInstance);
            CommerceSellDropZone dropZone = FindDeep(shopInstance.transform, "SellDropZone")
                ?.GetComponent<CommerceSellDropZone>();
            if (dropZone != null)
            {
                SerializedObject dropSerialized = new(dropZone);
                dropSerialized.FindProperty("_commerceUI").objectReferenceValue = controller;
                dropSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
            BindController(controller, backdrop, shop, craft);

            shop.Window.SetActive(false);
            craft.Window.SetActive(false);
            backdrop.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, RootPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dark Commerce Shop/Crafting prefabs rebuilt and wired.");
    }

    private static ShopRefs CollectShopRefs(GameObject window)
    {
        Transform t = window.transform;
        GameObject buyPanel = FindDeep(t, "BuyPanel").gameObject;
        GameObject sellPanel = FindDeep(t, "SellPanel").gameObject;
        return new ShopRefs(
            window,
            FindDeep(t, "Title").GetComponent<TMP_Text>(),
            FindDeep(t, "GoldText").GetComponent<TMP_Text>(),
            FindDeep(t, "Content"),
            FindDeep(t, "ShopRowTemplate").gameObject,
            FindDeep(t, "Details").GetComponent<TMP_Text>(),
            FindDeep(t, "Quantity").GetComponent<TMP_Text>(),
            FindDeep(t, "Feedback").GetComponent<TMP_Text>(),
            FindDeep(t, "QuantityMinus").GetComponent<Button>(),
            FindDeep(t, "QuantityPlus").GetComponent<Button>(),
            FindDeep(t, "BuyButton").GetComponent<Button>(),
            FindDeep(t, "SellButton").GetComponent<Button>(),
            FindDeep(t, "CloseButton").GetComponent<Button>(),
            FindDeep(t, "BuyTab").GetComponent<Button>(),
            FindDeep(t, "SellTab").GetComponent<Button>(),
            buyPanel,
            sellPanel,
            FindDeep(t, "ItemIcon").GetComponent<Image>(),
            FindDeep(t, "ItemName").GetComponent<TMP_Text>(),
            FindDeep(t, "Quote").GetComponent<TMP_Text>());
    }

    private static CraftRefs CollectCraftRefs(GameObject window)
    {
        Transform t = window.transform;
        Image[] ingredientIcons = new Image[4];
        TMP_Text[] ingredientCounts = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            Transform socket = FindDeep(t, $"IngredientSocket{i + 1}");
            ingredientIcons[i] = socket.Find("Icon").GetComponent<Image>();
            ingredientCounts[i] = socket.Find("Count").GetComponent<TMP_Text>();
        }

        return new CraftRefs(
            window,
            FindDeep(t, "Title").GetComponent<TMP_Text>(),
            FindDeep(t, "Content"),
            FindDeep(t, "RecipeRowTemplate").gameObject,
            FindDeep(t, "RecipeCategoryRowTemplate").gameObject,
            FindDeep(t, "Details").GetComponent<TMP_Text>(),
            FindDeep(t, "OutputIcon").GetComponent<Image>(),
            ingredientIcons,
            ingredientCounts,
            FindDeep(t, "Feedback").GetComponent<TMP_Text>(),
            FindDeep(t, "CraftButton").GetComponent<Button>(),
            FindDeep(t, "CloseButton").GetComponent<Button>());
    }

    private static ShopRefs BuildShop(Transform parent, Sprite board, ShopCraftingUI controller)
    {
        GameObject window = ImageObject("ShopWindow", parent, board, Color.white, true);
        Place(window, Vector2.zero, new Vector2(1536f, 1024f));
        window.transform.localScale = new Vector3(0.39f, 0.39f, 1f);

        TMP_Text title = Text("Title", window.transform, "SHOP", 38, TextAlignmentOptions.Center, Ivory);
        Place(title.gameObject, new Vector2(-315, 426), new Vector2(330, 60));
        Button close = ButtonObject("CloseButton", window.transform, "X", 34);
        Place(close.gameObject, new Vector2(718, 430), new Vector2(64, 64));
        ApplyCloseAsset(close);

        GameObject buyPanel = Rect("BuyPanel", window.transform, Vector2.zero, Vector2.zero);
        Stretch(buyPanel.GetComponent<RectTransform>());
        GameObject buyViewport = Rect("BuyViewport", buyPanel.transform, new Vector2(-405, 130), new Vector2(560, 400));
        Mask mask = buyViewport.AddComponent<Mask>();
        Image viewportImage = buyViewport.AddComponent<Image>();
        viewportImage.color = new Color(0, 0, 0, 0.02f);
        mask.showMaskGraphic = false;
        GameObject buyContent = Rect("Content", buyViewport.transform, Vector2.zero, new Vector2(560, 400));
        TopStretch(buyContent.GetComponent<RectTransform>());
        GridLayoutGroup buyGrid = buyContent.AddComponent<GridLayoutGroup>();
        buyGrid.cellSize = new Vector2(267, 76);
        buyGrid.spacing = new Vector2(10, 12);
        buyGrid.padding = new RectOffset(8, 8, 8, 8);
        buyGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        buyGrid.constraintCount = 2;
        buyGrid.childAlignment = TextAnchor.UpperLeft;
        ContentSizeFitter buyFit = buyContent.AddComponent<ContentSizeFitter>();
        buyFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect buyScroll = buyViewport.AddComponent<ScrollRect>();
        buyScroll.viewport = buyViewport.GetComponent<RectTransform>();
        buyScroll.content = buyContent.GetComponent<RectTransform>();
        buyScroll.horizontal = false;
        GameObject row = CreateShopRowTemplate(buyContent.transform);

        TMP_Text details = Text("Details", buyPanel.transform, "Select an item.", 20, TextAlignmentOptions.TopLeft, Ivory);
        details.lineSpacing = -10;
        details.overflowMode = TextOverflowModes.Ellipsis;
        Place(details.gameObject, new Vector2(-405, -194), new Vector2(520, 76));
        TMP_Text feedback = Text("Feedback", buyPanel.transform, string.Empty, 20, TextAlignmentOptions.Center, Gold);
        Place(feedback.gameObject, new Vector2(-405, -230), new Vector2(540, 24));

        GameObject sellPanel = Rect("SellPanel", window.transform, Vector2.zero, Vector2.zero);
        Stretch(sellPanel.GetComponent<RectTransform>());
        GameObject sellZone = ImageObject("SellDropZone", sellPanel.transform, null, new Color(0.05f, 0.035f, 0.02f, 0.55f), true);
        Place(sellZone, new Vector2(-405, 120), new Vector2(570, 470));
        CommerceSellDropZone drop = sellZone.AddComponent<CommerceSellDropZone>();
        Image sellIcon = ImageObject("ItemIcon", sellZone.transform, null, Color.white, false).GetComponent<Image>();
        Place(sellIcon.gameObject, new Vector2(0, 75), new Vector2(112, 112));
        TMP_Text sellName = Text("ItemName", sellZone.transform, "DROP ITEM HERE TO SELL", 30, TextAlignmentOptions.Center, Ivory);
        Place(sellName.gameObject, new Vector2(0, -35), new Vector2(600, 60));
        TMP_Text sellQuote = Text("Quote", sellZone.transform, string.Empty, 28, TextAlignmentOptions.Center, Gold);
        Place(sellQuote.gameObject, new Vector2(0, -105), new Vector2(500, 50));
        sellPanel.SetActive(false);

        Button buyTab = ButtonObject("BuyTab", window.transform, "BUY", 30);
        Place(buyTab.gameObject, new Vector2(-558, -342), new Vector2(270, 70));
        CreateSelectedAccent(buyTab.transform);
        Button sellTab = ButtonObject("SellTab", window.transform, "SELL", 30);
        Place(sellTab.gameObject, new Vector2(-248, -342), new Vector2(270, 70));
        CreateSelectedAccent(sellTab.transform);

        GameObject controls = Rect("TransactionControls", window.transform, new Vector2(-405, -254), new Vector2(500, 40));
        Button minus = ButtonObject("QuantityMinus", controls.transform, "-", 22, true);
        Place(minus.gameObject, new Vector2(-198, 0), new Vector2(44, 38));
        TMP_Text quantity = Text("Quantity", controls.transform, "1", 21, TextAlignmentOptions.Center, Ivory);
        Place(quantity.gameObject, new Vector2(-142, 0), new Vector2(52, 38));
        Button plus = ButtonObject("QuantityPlus", controls.transform, "+", 22, true);
        Place(plus.gameObject, new Vector2(-86, 0), new Vector2(44, 38));
        Button buy = ButtonObject("BuyButton", controls.transform, "BUY", 21);
        Place(buy.gameObject, new Vector2(105, 0), new Vector2(190, 38));
        Button sell = ButtonObject("SellButton", controls.transform, "SELL", 21);
        Place(sell.gameObject, new Vector2(105, 0), new Vector2(190, 38));

        CommerceInventoryPanelUI inventory = BuildCompactInventory(
            window.transform, new Vector2(312, 20), new Vector2(730, 568),
            new Vector2(104, 104), new Vector2(14, 12), true, false, -365);
        TMP_Text inventoryTitle = Text("InventoryTitle", window.transform, "INVENTORY", 32, TextAlignmentOptions.Center, Ivory);
        Place(inventoryTitle.gameObject, new Vector2(397, 425), new Vector2(360, 50));
        TMP_Text gold = inventory.transform.Find("CurrencyGold/GoldText").GetComponent<TMP_Text>();

        SerializedObject dropSerialized = new(drop);
        dropSerialized.FindProperty("_commerceUI").objectReferenceValue = controller;
        dropSerialized.ApplyModifiedPropertiesWithoutUndo();

        return new ShopRefs(window, title, gold, buyContent.transform, row, details, quantity, feedback,
            minus, plus, buy, sell, close, buyTab, sellTab, buyPanel, sellPanel, sellIcon, sellName, sellQuote);
    }

    private static CraftRefs BuildCrafting(Transform parent, Sprite board)
    {
        GameObject window = ImageObject("CraftingWindow", parent, board, Color.white, true);
        Place(window, Vector2.zero, new Vector2(1536f, 1024f));
        window.transform.localScale = new Vector3(0.39f, 0.39f, 1f);

        TMP_Text title = Text("Title", window.transform, "CRAFTING", 38, TextAlignmentOptions.Center, Ivory);
        Place(title.gameObject, new Vector2(0, 330), new Vector2(330, 58));
        Button close = ButtonObject("CloseButton", window.transform, "X", 32);
        Place(close.gameObject, new Vector2(680, 330), new Vector2(60, 60));
        ApplyCloseAsset(close);

        GameObject viewport = Rect("RecipeViewport", window.transform, new Vector2(-558, -5), new Vector2(270, 545));
        Mask mask = viewport.AddComponent<Mask>();
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0, 0, 0, 0.02f);
        mask.showMaskGraphic = false;
        GameObject content = Rect("Content", viewport.transform, Vector2.zero, new Vector2(270, 545));
        TopStretch(content.GetComponent<RectTransform>());
        VerticalLayoutGroup list = content.AddComponent<VerticalLayoutGroup>();
        list.padding = new RectOffset(3, 3, 3, 3);
        list.spacing = 4;
        list.childControlHeight = true;
        list.childControlWidth = true;
        list.childForceExpandHeight = false;
        ContentSizeFitter fit = content.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = viewport.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content.GetComponent<RectTransform>();
        scroll.horizontal = false;
        GameObject categoryTemplate = CreateRecipeRowTemplate("RecipeCategoryRowTemplate", content.transform, 65, true);
        GameObject blueprintTemplate = CreateRecipeRowTemplate("RecipeRowTemplate", content.transform, 58, false);

        Image output = ImageObject("OutputIcon", window.transform, null, Color.white, false).GetComponent<Image>();
        Place(output.gameObject, new Vector2(-306, 180), new Vector2(104, 120));
        TMP_Text details = Text("Details", window.transform, "Select a category, then choose a blueprint.", 20, TextAlignmentOptions.TopLeft, Ivory);
        details.lineSpacing = -4;
        details.overflowMode = TextOverflowModes.Ellipsis;
        Place(details.gameObject, new Vector2(-90, 187), new Vector2(260, 122));

        Image[] ingredientIcons = new Image[4];
        TMP_Text[] ingredientCounts = new TMP_Text[4];
        float[] xs = { -319, -211, -100, 13 };
        for (int i = 0; i < 4; i++)
        {
            GameObject socket = Rect($"IngredientSocket{i + 1}", window.transform, new Vector2(xs[i], 13), new Vector2(76, 82));
            ingredientIcons[i] = ImageObject("Icon", socket.transform, null, Color.white, false).GetComponent<Image>();
            StretchInset(ingredientIcons[i].rectTransform, 14);
            ingredientCounts[i] = Text("Count", socket.transform, string.Empty, 19, TextAlignmentOptions.BottomRight, Ivory);
            StretchInset(ingredientCounts[i].rectTransform, 4);
        }

        TMP_Text feedback = Text("Feedback", window.transform, string.Empty, 20, TextAlignmentOptions.Center, Gold);
        Place(feedback.gameObject, new Vector2(-155, -135), new Vector2(400, 38));
        Button craft = ButtonObject("CraftButton", window.transform, "CRAFT", 30);
        Place(craft.gameObject, new Vector2(-155, -240), new Vector2(340, 56));
        BuildCompactInventory(
            window.transform, new Vector2(400, -5), new Vector2(570, 448),
            new Vector2(80, 80), new Vector2(13, 12), true, false, -240);

        return new CraftRefs(window, title, content.transform, blueprintTemplate, categoryTemplate, details,
            output, ingredientIcons, ingredientCounts, feedback, craft, close);
    }

    private static CommerceInventoryPanelUI BuildCompactInventory(
        Transform parent,
        Vector2 center,
        Vector2 gridSize,
        Vector2 cellSize,
        Vector2 spacing,
        bool showSlotFrame,
        bool coverBakedGrid,
        float goldY)
    {
        GameObject panel = Rect("CompactInventory", parent, center, new Vector2(gridSize.x + 20, 600));
        CommerceInventoryPanelUI view = panel.AddComponent<CommerceInventoryPanelUI>();
        if (coverBakedGrid)
        {
            GameObject cover = ImageObject("GridBackdrop", panel.transform, null,
                new Color(Brown.r, Brown.g, Brown.b, 1f), false);
            Place(cover, new Vector2(0, 55), new Vector2(gridSize.x + 8, gridSize.y + 8));
        }
        GameObject scrollView = Rect("GridScrollView", panel.transform, new Vector2(0, 55), gridSize);
        Image scrollRaycast = scrollView.AddComponent<Image>();
        scrollRaycast.color = new Color(1f, 1f, 1f, 0f);
        scrollRaycast.raycastTarget = true;

        GameObject viewport = Rect("Viewport", scrollView.transform, Vector2.zero, gridSize);
        Stretch(viewport.GetComponent<RectTransform>());
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = false;
        RectMask2D viewportMask = viewport.AddComponent<RectMask2D>();
        viewportMask.padding = new Vector4(0, 2, 0, 0);

        GameObject grid = Rect("GridSlot", viewport.transform, Vector2.zero, Vector2.zero);
        TopStretch(grid.GetComponent<RectTransform>());
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = cellSize;
        layout.spacing = spacing;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 6;
        layout.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter gridFitter = grid.AddComponent<ContentSizeFitter>();
        gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.content = grid.GetComponent<RectTransform>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 20f;

        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
        GameObject slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, grid.transform);
        slot.name = "SlotTemplate";
        slot.SetActive(false);
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        slotRect.sizeDelta = layout.cellSize;
        if (!showSlotFrame)
        {
            Image slotFrame = slot.GetComponent<Image>();
            if (slotFrame != null) slotFrame.color = new Color(1f, 1f, 1f, 0f);
        }
        else if (coverBakedGrid)
        {
            Image slotFrame = slot.GetComponent<Image>();
            if (slotFrame != null)
            {
                slotFrame.sprite = null;
                slotFrame.color = new Color(0.34f, 0.27f, 0.20f, 1f);
            }
            Outline outline = slot.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.62f, 0.24f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
        GameObject currency = Rect("CurrencyGold", panel.transform, new Vector2(0, goldY), new Vector2(300, 50));
        Image goldIcon = ImageObject("GoldIcon", currency.transform, LoadInventoryGoldSprite(), Color.white, false).GetComponent<Image>();
        Place(goldIcon.gameObject, new Vector2(-38, 0), new Vector2(38, 38));
        goldIcon.preserveAspect = true;
        TMP_Text gold = Text("GoldText", currency.transform, "0", 27, TextAlignmentOptions.MidlineLeft, Gold);
        Place(gold.gameObject, new Vector2(38, 0), new Vector2(100, 42));

        SerializedObject serialized = new(view);
        serialized.FindProperty("_gridRoot").objectReferenceValue = grid.transform;
        serialized.FindProperty("_slotTemplate").objectReferenceValue = slot.GetComponent<InventorySlotUI>();
        serialized.FindProperty("_goldText").objectReferenceValue = gold;
        GameObject inventoryController = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryControllerPrefabPath);
        int inventorySlotCapacity = inventoryController != null
            ? inventoryController.GetComponentsInChildren<InventorySlotUI>(true).Length
            : 0;
        serialized.FindProperty("_slotCapacity").intValue = inventorySlotCapacity;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static Sprite LoadInventoryGoldSprite()
    {
        GameObject inventoryController = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryControllerPrefabPath);
        Transform goldIcon = inventoryController != null ? FindDeep(inventoryController.transform, "GoldIcon") : null;
        return goldIcon != null ? goldIcon.GetComponent<Image>()?.sprite : null;
    }

    private static void ApplyCloseAsset(Button button)
    {
        Sprite closeSprite = ImportSprite(CloseSpritePath);
        Image image = button.GetComponent<Image>();
        image.sprite = closeSprite;
        image.color = Color.white;
        image.preserveAspect = true;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.gameObject.SetActive(false);
    }

    private static GameObject CreateShopRowTemplate(Transform parent)
    {
        GameObject row = ImageObject("ShopRowTemplate", parent, null, Brown, true);
        row.AddComponent<Button>().targetGraphic = row.GetComponent<Image>();
        Image icon = ImageObject("Icon", row.transform, null, Color.white, false).GetComponent<Image>();
        Place(icon.gameObject, new Vector2(-100, 0), new Vector2(58, 58));
        TMP_Text name = Text("Name", row.transform, "ITEM", 20, TextAlignmentOptions.MidlineLeft, Ivory);
        Place(name.gameObject, new Vector2(30, 10), new Vector2(150, 36));
        TMP_Text price = Text("Price", row.transform, "0 G", 20, TextAlignmentOptions.MidlineLeft, Gold);
        Place(price.gameObject, new Vector2(30, -20), new Vector2(150, 30));
        row.SetActive(false);
        return row;
    }

    private static GameObject CreateRecipeRowTemplate(string name, Transform parent, float height, bool category)
    {
        GameObject row = ImageObject(name, parent, null, category ? Hover : Brown, true);
        Button button = row.AddComponent<Button>();
        button.targetGraphic = row.GetComponent<Image>();
        LayoutElement element = row.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        Image icon = ImageObject("Icon", row.transform, null, Color.white, false).GetComponent<Image>();
        Place(icon.gameObject, new Vector2(-100, 0), new Vector2(category ? 42 : 38, category ? 42 : 38));
        TMP_Text label = Text("Name", row.transform, category ? "+  CATEGORY" : "Blueprint", category ? 22 : 19,
            TextAlignmentOptions.MidlineLeft, category ? Gold : Ivory);
        Place(label.gameObject, new Vector2(20, 0), new Vector2(190, height));
        TMP_Text count = Text("Station", row.transform, string.Empty, 17, TextAlignmentOptions.MidlineRight, Gold);
        Place(count.gameObject, new Vector2(105, 0), new Vector2(50, height));
        row.SetActive(false);
        return row;
    }

    private static void BindController(ShopCraftingUI controller, GameObject backdrop, ShopRefs shop, CraftRefs craft)
    {
        SerializedObject s = new(controller);
        Set(s, "_backdrop", backdrop);
        Set(s, "_shopWindow", shop.Window);
        Set(s, "_craftingWindow", craft.Window);
        Set(s, "_shopTitle", shop.Title);
        Set(s, "_shopGold", shop.Gold);
        Set(s, "_shopListContent", shop.Content);
        Set(s, "_shopRowTemplate", shop.RowTemplate);
        Set(s, "_shopDetails", shop.Details);
        Set(s, "_quantityText", shop.Quantity);
        Set(s, "_shopFeedback", shop.Feedback);
        Set(s, "_quantityMinusButton", shop.Minus);
        Set(s, "_quantityPlusButton", shop.Plus);
        Set(s, "_buyButton", shop.Buy);
        Set(s, "_sellButton", shop.Sell);
        Set(s, "_shopCloseButton", shop.Close);
        Set(s, "_buyTabButton", shop.BuyTab);
        Set(s, "_sellTabButton", shop.SellTab);
        Set(s, "_buyPanel", shop.BuyPanel);
        Set(s, "_sellPanel", shop.SellPanel);
        Set(s, "_sellItemIcon", shop.SellIcon);
        Set(s, "_sellItemName", shop.SellName);
        Set(s, "_sellQuoteText", shop.SellQuote);
        Set(s, "_craftingTitle", craft.Title);
        Set(s, "_recipeListContent", craft.Content);
        Set(s, "_recipeRowTemplate", craft.RowTemplate);
        Set(s, "_recipeCategoryRowTemplate", craft.CategoryTemplate);
        Set(s, "_recipeDetails", craft.Details);
        Set(s, "_recipeOutputIcon", craft.Output);
        SetArray(s, "_ingredientIcons", craft.IngredientIcons);
        SetArray(s, "_ingredientCounts", craft.IngredientCounts);
        Set(s, "_craftingFeedback", craft.Feedback);
        Set(s, "_craftButton", craft.Craft);
        Set(s, "_craftingCloseButton", craft.Close);
        s.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(SerializedObject s, string name, UnityEngine.Object value) => s.FindProperty(name).objectReferenceValue = value;
    private static void SetArray<T>(SerializedObject s, string name, IReadOnlyList<T> values) where T : UnityEngine.Object
    {
        SerializedProperty p = s.FindProperty(name);
        p.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static Button ButtonObject(string name, Transform parent, string label, float fontSize, bool compact = false)
    {
        Sprite sprite = compact ? _squareButtonSprite : _wideButtonSprite;
        GameObject go = ImageObject(name, parent, sprite, Color.white, true);
        go.GetComponent<Image>().preserveAspect = false;
        Button b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        ColorBlock colors = b.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.82f, 0.38f, 1f);
        colors.pressedColor = new Color(0.72f, 0.48f, 0.15f, 1f);
        b.colors = colors;
        TMP_Text text = Text("Label", go.transform, label, fontSize, TextAlignmentOptions.Center, Ivory);
        Stretch(text.rectTransform);
        return b;
    }

    private static void CreateSelectedAccent(Transform tab)
    {
        GameObject accent = ImageObject("SelectedAccent", tab, null, Gold, false);
        RectTransform rect = accent.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 7);
        accent.transform.SetAsFirstSibling();
    }

    private static GameObject ImageObject(string name, Transform parent, Sprite sprite, Color color, bool raycast)
    {
        GameObject go = Rect(name, parent, Vector2.zero, new Vector2(100, 100));
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;
        image.preserveAspect = sprite != null;
        return go;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = Rect(name, parent, Vector2.zero, new Vector2(100, 40));
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static GameObject Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place(go, position, size);
        return go;
    }

    private static void Place(GameObject go, Vector2 position, Vector2 size)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void StretchInset(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void TopStretch(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static Sprite ImportSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing texture: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static TMP_FontAsset LoadFont()
    {
        string[] guids = AssetDatabase.FindAssets("DigitalDisco t:TMP_FontAsset");
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return TMP_Settings.defaultFontAsset;
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string part in path.Substring("Assets/".Length).Split('/'))
        {
            string next = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }

    private sealed class ShopRefs
    {
        public readonly GameObject Window, RowTemplate, BuyPanel, SellPanel;
        public readonly TMP_Text Title, Gold, Details, Quantity, Feedback, SellName, SellQuote;
        public readonly Transform Content;
        public readonly Button Minus, Plus, Buy, Sell, Close, BuyTab, SellTab;
        public readonly Image SellIcon;

        public ShopRefs(GameObject window, TMP_Text title, TMP_Text gold, Transform content, GameObject rowTemplate,
            TMP_Text details, TMP_Text quantity, TMP_Text feedback, Button minus, Button plus, Button buy, Button sell,
            Button close, Button buyTab, Button sellTab, GameObject buyPanel, GameObject sellPanel, Image sellIcon,
            TMP_Text sellName, TMP_Text sellQuote)
        {
            Window = window; Title = title; Gold = gold; Content = content; RowTemplate = rowTemplate;
            Details = details; Quantity = quantity; Feedback = feedback; Minus = minus; Plus = plus;
            Buy = buy; Sell = sell; Close = close; BuyTab = buyTab; SellTab = sellTab;
            BuyPanel = buyPanel; SellPanel = sellPanel; SellIcon = sellIcon; SellName = sellName; SellQuote = sellQuote;
        }
    }

    private sealed class CraftRefs
    {
        public readonly GameObject Window, RowTemplate, CategoryTemplate;
        public readonly TMP_Text Title, Details, Feedback;
        public readonly Transform Content;
        public readonly Image Output;
        public readonly Image[] IngredientIcons;
        public readonly TMP_Text[] IngredientCounts;
        public readonly Button Craft, Close;

        public CraftRefs(GameObject window, TMP_Text title, Transform content, GameObject rowTemplate,
            GameObject categoryTemplate, TMP_Text details, Image output, Image[] ingredientIcons,
            TMP_Text[] ingredientCounts, TMP_Text feedback, Button craft, Button close)
        {
            Window = window; Title = title; Content = content; RowTemplate = rowTemplate;
            CategoryTemplate = categoryTemplate; Details = details; Output = output;
            IngredientIcons = ingredientIcons; IngredientCounts = ingredientCounts;
            Feedback = feedback; Craft = craft; Close = close;
        }
    }
}
