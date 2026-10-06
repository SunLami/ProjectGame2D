using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds Assets/Prefabs/UI/InventoryDiscardConfirm.prefab: the drag-to-discard confirm
/// popup for InventoryWindowUI (see InventoryDiscardConfirmUI.cs / InventorySlotUI.OnEndDrag).
/// Reuses the same Dark Inventory Style assets as SessionUX ConfirmationPopup and Tutorial
/// SkipConfirmation (session_confirmation_board_v3.png board + dialogue_action_button_v1.png
/// buttons) so it matches the rest of the game's confirm-dialog family instead of introducing a
/// new look. Editable afterward in Prefab Mode like any other authoring prefab (D-046 contract) --
/// re-running this menu item discards manual edits and rebuilds from scratch.</summary>
public static class InventoryDiscardConfirmPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/InventoryDiscardConfirm.prefab";
    private const string SessionRoot = "Assets/Resources/UI/SessionUX/DarkInventoryStyle/";
    private const string DialogueRoot = "Assets/Resources/UI/Dialogue/DarkInventoryStyle/";

    [MenuItem("Tools/ProjectGame2D/UI/Build Inventory Discard Confirm")]
    public static void Build()
    {
        Sprite board = ImportSprite(SessionRoot + "session_confirmation_board_v3.png");
        Sprite actionButton = ImportSprite(DialogueRoot + "dialogue_action_button_v1.png");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
        if (board == null || actionButton == null)
            throw new InvalidOperationException("InventoryDiscardConfirm authoring sprites could not be loaded.");

        GameObject root = new("InventoryDiscardConfirmUI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InventoryDiscardConfirmUI));
        try
        {
            Stretch((RectTransform)root.transform);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40; // above InventoryWindowUI's own canvas, matches other modal popups
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            InventoryDiscardConfirmUI controller = root.GetComponent<InventoryDiscardConfirmUI>();

            GameObject dim = CreateImage("DimBackground", root.transform, null, new Color(0.04f, 0.025f, 0.01f, 0.76f));
            Stretch((RectTransform)dim.transform);

            RectTransform confirmPage = CreateRect("ConfirmPage", dim.transform);
            Stretch(confirmPage);
            RectTransform confirmPanel = BuildPanel("Panel", confirmPage, board);
            Image confirmIcon = CreateIcon("Icon", confirmPanel, new Vector2(0f, 60f), 48f);
            TMP_Text confirmMessage = CreateText("MessageText", confirmPanel, font, "Discard {item}?", 17f,
                new Vector2(0f, 8f), new Vector2(430f, 60f));
            Button yesButton = BuildActionButton("YesButton", confirmPanel, actionButton, font, "DISCARD",
                danger: true, anchoredX: -100f);
            Button noButton = BuildActionButton("NoButton", confirmPanel, actionButton, font, "CANCEL",
                danger: false, anchoredX: 100f);

            RectTransform quantityPage = CreateRect("QuantityPage", dim.transform);
            Stretch(quantityPage);
            quantityPage.gameObject.SetActive(false);
            RectTransform quantityPanel = BuildPanel("Panel", quantityPage, board);
            Image quantityIcon = CreateIcon("Icon", quantityPanel, new Vector2(0f, 68f), 40f);
            TMP_Text quantityItemName = CreateText("ItemNameText", quantityPanel, font, "Item Name", 16f,
                new Vector2(0f, 28f), new Vector2(400f, 26f));

            TMP_InputField quantityInput = BuildQuantityInput(quantityPanel, font);

            Button quantityConfirm = BuildActionButton("ConfirmButton", quantityPanel, actionButton, font, "DISCARD",
                danger: true, anchoredX: -100f);
            Button quantityCancel = BuildActionButton("CancelButton", quantityPanel, actionButton, font, "CANCEL",
                danger: false, anchoredX: 100f);

            SerializedObject serialized = new(controller);
            serialized.FindProperty("_root").objectReferenceValue = dim;
            serialized.FindProperty("_confirmPage").objectReferenceValue = confirmPage.gameObject;
            serialized.FindProperty("_quantityPage").objectReferenceValue = quantityPage.gameObject;
            serialized.FindProperty("_confirmIcon").objectReferenceValue = confirmIcon;
            serialized.FindProperty("_confirmMessageText").objectReferenceValue = confirmMessage;
            serialized.FindProperty("_confirmYesButton").objectReferenceValue = yesButton;
            serialized.FindProperty("_confirmNoButton").objectReferenceValue = noButton;
            serialized.FindProperty("_quantityIcon").objectReferenceValue = quantityIcon;
            serialized.FindProperty("_quantityItemNameText").objectReferenceValue = quantityItemName;
            serialized.FindProperty("_quantityInput").objectReferenceValue = quantityInput;
            serialized.FindProperty("_quantityConfirmButton").objectReferenceValue = quantityConfirm;
            serialized.FindProperty("_quantityCancelButton").objectReferenceValue = quantityCancel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("InventoryDiscardConfirmPrefabBuilder: rebuilt " + PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
    }

    private static RectTransform BuildPanel(string name, Transform parent, Sprite board)
    {
        GameObject panel = CreateImage(name, parent, board, Color.white);
        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500f, 245f);
        return rect;
    }

    private static Image CreateIcon(string name, Transform parent, Vector2 position, float size)
    {
        GameObject go = CreateImage(name, parent, null, Color.white);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);
        Image image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Button BuildActionButton(string name, Transform parent, Sprite sprite, TMP_FontAsset font,
        string label, bool danger, float anchoredX)
    {
        GameObject go = CreateImage(name, parent, sprite, Color.white);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(anchoredX, -78f);
        rect.sizeDelta = new Vector2(180f, 44f);

        Button button = go.AddComponent<Button>();
        Image image = go.GetComponent<Image>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = danger ? new Color(0.72f, 0.30f, 0.25f, 1f) : Color.white;
        colors.highlightedColor = danger ? new Color(1f, 0.48f, 0.38f, 1f) : new Color(0.58f, 0.82f, 1f, 1f);
        colors.pressedColor = danger ? new Color(0.52f, 0.18f, 0.16f, 1f) : new Color(0.68f, 0.55f, 0.30f, 1f);
        colors.disabledColor = new Color(0.45f, 0.42f, 0.34f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText("Label", go.transform, font, label, 14f, Vector2.zero, rect.sizeDelta);
        RectTransform textRect = (RectTransform)text.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.color = new Color(1f, 0.94f, 0.72f, 1f);
        text.raycastTarget = false;

        return button;
    }

    private static TMP_InputField BuildQuantityInput(Transform parent, TMP_FontAsset font)
    {
        GameObject go = CreateImage("QuantityInput", parent, null, new Color(0.07f, 0.055f, 0.04f, 0.92f));
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(150f, 38f);

        TMP_Text text = CreateText("Text", go.transform, font, "1", 18f, Vector2.zero, rect.sizeDelta);
        RectTransform textRect = (RectTransform)text.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 2f);
        textRect.offsetMax = new Vector2(-10f, -2f);
        text.color = new Color(1f, 0.94f, 0.72f, 1f);

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.textViewport = textRect;
        input.textComponent = text;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 6;
        input.targetGraphic = go.GetComponent<Image>();
        input.text = "1";
        return input;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value,
        float fontSize, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = go.GetComponent<TMP_Text>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = new Color(1f, 0.93f, 0.72f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = color;
        image.raycastTarget = true;
        return go;
    }

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

    private static Sprite ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"Texture importer not found: {path}");
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
