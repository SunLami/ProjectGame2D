using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CommerceUILightFantasySkinBuilder
{
    private const string CommerceRoot = "Assets/Resources/UI/Commerce/LightFantasy/";
    private const string MainMenuRoot = "Assets/Resources/UI/MainMenu/LightFantasy/";
    private const string InventoryRoot = "Assets/Resources/UI/Inventory/LightFantasy/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply Commerce UI Light Fantasy Skin")]
    public static void Apply()
    {
        ShopCraftingUI commerceUI = UnityEngine.Object.FindAnyObjectByType<ShopCraftingUI>(FindObjectsInactive.Include);
        if (commerceUI == null) throw new InvalidOperationException("ShopCraftingUI was not found in the active scene.");

        Sprite board = ImportSprite(CommerceRoot + "commerce_window_board_hd.png");
        Sprite innerPanel = ImportSprite(CommerceRoot + "commerce_inner_panel_hd.png");
        Sprite primary = ImportSprite(MainMenuRoot + "landing_action_button.png");
        Sprite hover = ImportSprite(MainMenuRoot + "landing_action_button_hover.png");
        Sprite close = ImportSprite(InventoryRoot + "inventory_close_thin_hd.png");
        Sprite goldBadge = ImportSprite(InventoryRoot + "inventory_gold_badge_hd.png");

        Transform backdrop = RequireChild(commerceUI.transform, "Backdrop");
        Image dim = backdrop.GetComponent<Image>();
        if (dim == null) dim = backdrop.gameObject.AddComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.035f, 0.022f, 0.012f, 0.72f);
        dim.raycastTarget = true;

        StyleWindow(RequireChild(backdrop, "ShopWindow"), board, innerPanel, primary, hover, close, goldBadge, true);
        StyleWindow(RequireChild(backdrop, "CraftingWindow"), board, innerPanel, primary, hover, close, goldBadge, false);

        CommerceUILayoutBuilder.Apply();
        EditorUtility.SetDirty(commerceUI.gameObject);
        EditorSceneManager.MarkSceneDirty(commerceUI.gameObject.scene);
        EditorSceneManager.SaveScene(commerceUI.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CommerceUIRoot Light Fantasy skin applied.");
    }

    private static void StyleWindow(Transform window, Sprite board, Sprite innerPanel, Sprite primary, Sprite hover, Sprite close, Sprite goldBadge, bool shop)
    {
        SetImage(window.gameObject, board, false, true);

        Transform header = RequireChild(window, "Header");
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(-140f, 90f);
        headerRect.anchoredPosition = new Vector2(0f, -36f);
        TMP_Text title = RequireChild(header, "Title").GetComponent<TMP_Text>();
        EnsureTitleBadge(header, primary);
        SetCenteredRect(title.rectTransform, new Vector2(390f, 58f), new Vector2(0f, -5f));
        StyleText(title, 34f, new Color(1f, 0.94f, 0.72f, 1f), TextAlignmentOptions.Center);
        title.transform.SetAsLastSibling();

        Button closeButton = RequireChild(header, "CloseButton").GetComponent<Button>();
        SetImage(closeButton.gameObject, close, true, true);
        closeButton.transition = Selectable.Transition.None;
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(54f, 54f);
        closeRect.anchoredPosition = new Vector2(-38f, -5f);
        TMP_Text closeLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
        if (closeLabel != null) closeLabel.enabled = false;

        if (shop)
        {
            TMP_Text gold = RequireChild(header, "Gold").GetComponent<TMP_Text>();
            EnsureGoldBadge(header, goldBadge, gold.transform.GetSiblingIndex());
            RectTransform goldRect = gold.rectTransform;
            goldRect.anchorMin = new Vector2(1f, 0.5f);
            goldRect.anchorMax = new Vector2(1f, 0.5f);
            goldRect.pivot = new Vector2(0.5f, 0.5f);
            goldRect.sizeDelta = new Vector2(210f, 54f);
            goldRect.anchoredPosition = new Vector2(-190f, -5f);
            StyleText(gold, 25f, new Color(1f, 0.88f, 0.25f, 1f), TextAlignmentOptions.Center);
            gold.transform.SetAsLastSibling();
        }

        Transform listPanel = RequireChild(window, "ListPanel");
        Transform detailPanel = RequireChild(window, "DetailPanel");
        SetImage(listPanel.gameObject, innerPanel, false, false);
        SetImage(detailPanel.gameObject, innerPanel, false, false);
        SetCenteredRect(listPanel.GetComponent<RectTransform>(), new Vector2(400f, 440f), new Vector2(-300f, -48f));
        SetCenteredRect(detailPanel.GetComponent<RectTransform>(), new Vector2(560f, 440f), new Vector2(200f, -48f));

        Transform listContent = RequireChild(listPanel, "Content");
        RectTransform contentRect = listContent.GetComponent<RectTransform>();
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, -102f);
        ConfigureListContent(listContent);

        TMP_Text details = RequireChild(detailPanel, "Details").GetComponent<TMP_Text>();
        SetTopLeftRect(details.rectTransform, new Vector2(500f, shop ? 240f : 270f), new Vector2(30f, -82f));
        StyleText(details, 27f, new Color(0.30f, 0.16f, 0.06f, 1f), TextAlignmentOptions.TopLeft);
        details.richText = true;
        details.lineSpacing = 7f;
        details.paragraphSpacing = 2f;

        TMP_Text feedback = RequireChild(detailPanel, "Feedback").GetComponent<TMP_Text>();
        RectTransform feedbackRect = feedback.rectTransform;
        feedbackRect.anchorMin = new Vector2(0.5f, 0f);
        feedbackRect.anchorMax = new Vector2(0.5f, 0f);
        feedbackRect.pivot = new Vector2(0.5f, 0f);
        feedbackRect.sizeDelta = new Vector2(500f, 50f);
        feedbackRect.anchoredPosition = new Vector2(0f, 58f);
        StyleText(feedback, 21f, new Color(0.12f, 0.38f, 0.24f, 1f), TextAlignmentOptions.Center);

        foreach (Button button in window.GetComponentsInChildren<Button>(true))
        {
            if (button == closeButton) continue;
            StyleButton(button, primary, hover);
        }

        if (shop)
            LayoutShopControls(window);
        else
            LayoutCraftButton(window);
    }

    private static void EnsureTitleBadge(Transform header, Sprite sprite)
    {
        Transform existing = header.Find("SkinCommerceTitleBadge");
        GameObject badge = existing != null ? existing.gameObject : new GameObject("SkinCommerceTitleBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badge.transform.SetParent(header, false);
        SetCenteredRect(badge.GetComponent<RectTransform>(), new Vector2(410f, 68f), new Vector2(0f, -5f));
        SetImage(badge, sprite, false, false);
        badge.transform.SetAsFirstSibling();
    }

    private static void EnsureGoldBadge(Transform header, Sprite sprite, int siblingIndex)
    {
        Transform existing = header.Find("SkinCommerceGoldBadge");
        GameObject badge = existing != null ? existing.gameObject : new GameObject("SkinCommerceGoldBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badge.transform.SetParent(header, false);
        RectTransform rect = badge.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 62f);
        rect.anchoredPosition = new Vector2(-190f, 0f);
        SetImage(badge, sprite, false, false);
        badge.transform.SetSiblingIndex(Mathf.Max(0, siblingIndex));
    }

    private static void StyleButton(Button button, Sprite normal, Sprite hover)
    {
        SetImage(button.gameObject, normal, false, true);
        button.targetGraphic = button.GetComponent<Image>();
        button.transition = Selectable.Transition.None;
        MainMenuButtonHoverVisual visual = button.GetComponent<MainMenuButtonHoverVisual>();
        if (visual == null) visual = button.gameObject.AddComponent<MainMenuButtonHoverVisual>();
        SerializedObject serialized = new SerializedObject(visual);
        serialized.FindProperty("_hoverSprite").objectReferenceValue = hover;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            StyleText(label, Mathf.Min(label.fontSize, 25f), new Color(1f, 0.94f, 0.72f, 1f), label.alignment);
    }

    private static void LayoutShopControls(Transform window)
    {
        Transform controls = RequireChild(window, "TransactionControls");
        SetCenteredRect(controls.GetComponent<RectTransform>(), new Vector2(470f, 52f), new Vector2(200f, -205f));
        SetControl(RequireChild(controls, "QuantityMinus").GetComponent<RectTransform>(), new Vector2(52f, 42f), new Vector2(-195f, 0f));
        SetControl(RequireChild(controls, "Quantity").GetComponent<RectTransform>(), new Vector2(48f, 42f), new Vector2(-140f, 0f));
        SetControl(RequireChild(controls, "QuantityPlus").GetComponent<RectTransform>(), new Vector2(52f, 42f), new Vector2(-85f, 0f));
        SetControl(RequireChild(controls, "BuyButton").GetComponent<RectTransform>(), new Vector2(130f, 42f), new Vector2(25f, 0f));
        SetControl(RequireChild(controls, "SellButton").GetComponent<RectTransform>(), new Vector2(130f, 42f), new Vector2(170f, 0f));
    }

    private static void ConfigureListContent(Transform content)
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        for (int i = 0; i < content.childCount; i++)
        {
            Transform row = content.GetChild(i);
            if (!row.name.EndsWith("RowTemplate", StringComparison.Ordinal)) continue;
            LayoutElement element = row.GetComponent<LayoutElement>();
            if (element == null) element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 50f;
            element.preferredHeight = 50f;
            element.flexibleHeight = 0f;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(row.GetComponent<RectTransform>().sizeDelta.x, 50f);
            row.gameObject.SetActive(false);
            EditorUtility.SetDirty(row.gameObject);
        }
    }

    private static void LayoutCraftButton(Transform window)
    {
        RectTransform craft = RequireChild(window, "CraftButton").GetComponent<RectTransform>();
        SetCenteredRect(craft, new Vector2(230f, 56f), new Vector2(200f, -220f));
    }

    private static void SetControl(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
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
        if (child == null) throw new InvalidOperationException("Required Commerce UI object missing: " + parent.name + "/" + name);
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
