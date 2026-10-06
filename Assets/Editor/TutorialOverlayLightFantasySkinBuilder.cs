using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TutorialOverlayLightFantasySkinBuilder
{
    private const string TutorialRoot = "Assets/Resources/UI/Tutorial/DarkInventoryStyle/";
    private const string MainMenuRoot = "Assets/Resources/UI/MainMenu/LightFantasy/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply Tutorial Overlay Light Fantasy Skin")]
    public static void Apply()
    {
        TutorialOverlayUI overlayUI = UnityEngine.Object.FindAnyObjectByType<TutorialOverlayUI>(FindObjectsInactive.Include);
        if (overlayUI == null) throw new InvalidOperationException("TutorialOverlayUI was not found in the active scene.");

        Transform root = overlayUI.transform;
        Transform instruction = RequireChild(root, "InstructionPanel");
        Transform confirmation = RequireChild(root, "SkipConfirmation");
        Transform dialog = RequireChild(confirmation, "Dialog");

        Sprite instructionBoard = ImportSprite(TutorialRoot + "tutorial_instruction_panel_v1.png", new Rect(2f, 80f, 1435f, 213f));
        Sprite primaryButton = ImportSprite(MainMenuRoot + "landing_action_button.png");
        Sprite dangerButton = ImportSprite(MainMenuRoot + "slot_delete_button.png");
        Sprite hoverButton = ImportSprite(MainMenuRoot + "landing_action_button_hover.png");
        // SkipConfirmation reuses the SessionUX confirmation board + Dialogue action button (same
        // Dark Inventory Style assets as SessionUXConfirmationPopupLightFantasySkinBuilder) instead
        // of the old LightFantasy skip-dialog/landing-button assets -- those looked out of place
        // next to the rest of the (now Dark Inventory Style) gameplay UI. See D-049. InstructionPanel
        // (above/below) still uses the LightFantasy primary/danger/hover trio -- untouched on purpose.
        Sprite skipDialog = ImportSprite("Assets/Resources/UI/SessionUX/DarkInventoryStyle/session_confirmation_board_v3.png");
        Sprite actionButton = ImportSprite("Assets/Resources/UI/Dialogue/DarkInventoryStyle/dialogue_action_button_v1.png");

        SetImage(instruction.gameObject, instructionBoard, false, true);
        RectTransform instructionRect = instruction.GetComponent<RectTransform>();
        instructionRect.sizeDelta = new Vector2(360f, 92f);
        instructionRect.anchoredPosition = new Vector2(instructionRect.anchoredPosition.x, 80f);

        TMP_Text header = RequireChild(instruction, "Header").GetComponent<TMP_Text>();
        SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(120f, 20f), new Vector2(0f, -9f));
        header.enabled = true;
        header.text = "TUTORIAL";
        StyleText(header, 14f, new Color(0.86f, 0.69f, 0.30f, 1f), TextAlignmentOptions.Center);
        header.fontStyle = FontStyles.Bold;
        RemoveStaleTitleBanner(header.transform);

        TMP_Text instructionText = RequireChild(instruction, "InstructionText").GetComponent<TMP_Text>();
        SetRect(instructionText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(245f, 32f), new Vector2(18f, -5f));
        StyleText(instructionText, 12f, new Color(0.93f, 0.89f, 0.78f, 1f), TextAlignmentOptions.MidlineLeft);

        Button skipButton = RequireChild(instruction, "SkipButton").GetComponent<Button>();
        StyleButton(skipButton, dangerButton, hoverButton, new Vector2(1f, 0.5f), new Vector2(-48f, -5f), new Vector2(72f, 28f));

        Image dim = confirmation.GetComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.04f, 0.025f, 0.01f, 0.76f);
        dim.raycastTarget = true;

        SetImage(dialog.gameObject, skipDialog, false, true);
        dialog.GetComponent<RectTransform>().sizeDelta = new Vector2(570f, 245f);

        TMP_Text title = RequireChild(dialog, "Title").GetComponent<TMP_Text>();
        SetTopCenteredRect(title.rectTransform, new Vector2(430f, 34f), -72f);
        // Light-on-dark now that the board is the dark charcoal SessionUX one, not the old light
        // parchment skip-dialog art (dark-brown text on charcoal would be unreadable).
        StyleText(title, 23f, new Color(1f, 0.93f, 0.72f, 1f), TextAlignmentOptions.Center);

        TMP_Text message = RequireChild(dialog, "Message").GetComponent<TMP_Text>();
        SetTopCenteredRect(message.rectTransform, new Vector2(450f, 64f), -122f);
        StyleText(message, 14f, new Color(0.85f, 0.80f, 0.70f, 1f), TextAlignmentOptions.Center);

        Button confirm = RequireChild(dialog, "ConfirmSkipButton").GetComponent<Button>();
        Button cancel = RequireChild(dialog, "CancelSkipButton").GetComponent<Button>();
        StyleActionButton(confirm, actionButton, danger: true, new Vector2(-100f, 30f), new Vector2(180f, 44f));
        StyleActionButton(cancel, actionButton, danger: false, new Vector2(100f, 30f), new Vector2(180f, 44f));

        EditorUtility.SetDirty(overlayUI.gameObject);
        EditorSceneManager.MarkSceneDirty(overlayUI.gameObject.scene);
        EditorSceneManager.SaveScene(overlayUI.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("TutorialOverlayRoot Light Fantasy skin applied.");
    }

    private static void StyleButton(Button button, Sprite normal, Sprite hover, Vector2 position, Vector2 size) =>
        StyleButton(button, normal, hover, null, position, size);

    private static void StyleButton(Button button, Sprite normal, Sprite hover, Vector2? anchor, Vector2 position, Vector2 size)
    {
        SetImage(button.gameObject, normal, false, true);
        button.targetGraphic = button.GetComponent<Image>();
        button.transition = Selectable.Transition.None;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (anchor.HasValue)
        {
            rect.anchorMin = anchor.Value;
            rect.anchorMax = anchor.Value;
            rect.pivot = anchor.Value;
        }
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Outline outline = button.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        MainMenuButtonHoverVisual hoverVisual = button.GetComponent<MainMenuButtonHoverVisual>();
        if (hoverVisual == null) hoverVisual = button.gameObject.AddComponent<MainMenuButtonHoverVisual>();
        SerializedObject hoverObject = new SerializedObject(hoverVisual);
        hoverObject.FindProperty("_hoverSprite").objectReferenceValue = hover;
        hoverObject.ApplyModifiedPropertiesWithoutUndo();

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(label, 14f, new Color(1f, 0.94f, 0.72f, 1f), TextAlignmentOptions.Center);
        if (anchor.HasValue)
        {
            // Absolute centering: stretch-fill the button rect instead of trusting whatever offset
            // the label previously had, so the SKIP text sits dead-center regardless of button size.
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
    }

    // ColorTint-based button styling for the SkipConfirmation dialog (Dark Inventory Style) -- same
    // formula as SessionUXConfirmationPopupLightFantasySkinBuilder.StyleButton, distinct from the
    // hover-sprite-swap StyleButton below which InstructionPanel's SkipButton still uses.
    private static void StyleActionButton(Button button, Sprite sprite, bool danger, Vector2 position, Vector2 size)
    {
        Image image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
        EditorUtility.SetDirty(image);

        Outline outline = button.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        MainMenuButtonHoverVisual hoverVisual = button.GetComponent<MainMenuButtonHoverVisual>();
        if (hoverVisual != null) UnityEngine.Object.DestroyImmediate(hoverVisual);

        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = danger ? new Color(0.72f, 0.30f, 0.25f, 1f) : Color.white;
        colors.highlightedColor = danger ? new Color(1f, 0.48f, 0.38f, 1f) : new Color(0.58f, 0.82f, 1f, 1f);
        colors.pressedColor = danger ? new Color(0.52f, 0.18f, 0.16f, 1f) : new Color(0.68f, 0.55f, 0.30f, 1f);
        colors.disabledColor = new Color(0.45f, 0.42f, 0.34f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        EditorUtility.SetDirty(button);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
            label.color = new Color(1f, 0.94f, 0.72f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            EditorUtility.SetDirty(label);
        }
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetTopCenteredRect(RectTransform rect, Vector2 size, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, y);
    }

    private static void RemoveStaleTitleBanner(Transform headerTransform)
    {
        Transform stale = headerTransform.Find("SkinTutorialTitleBanner");
        if (stale != null)
            UnityEngine.Object.DestroyImmediate(stale.gameObject);
    }

    private static void StyleText(TMP_Text text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) throw new InvalidOperationException($"Required TutorialOverlay object missing: {parent.name}/{name}");
        return child;
    }

    private static void SetImage(GameObject target, Sprite sprite, bool preserveAspect, bool raycastTarget)
    {
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = raycastTarget;

        Outline outline = target.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
        EditorUtility.SetDirty(image);
    }

    private static Sprite ImportSprite(string path) => ImportSprite(path, null);

    private static Sprite ImportSprite(string path, Rect? contentRect)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"Texture importer not found: {path}");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = contentRect.HasValue ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        if (contentRect.HasValue)
        {
            // SpriteImportMode.Single ignores any custom rect written to `spritesheet` -- it always
            // reports the full texture as the sprite rect (verified empirically). This bitmap carries
            // ~80px of transparent padding above/below the visible board art, so an untrimmed rect
            // stretches that empty margin into the rendered Image, leaving the real board smaller
            // than the InstructionPanel RectTransform and floating other children (e.g. the Header)
            // outside its visible bounds. Multiple mode with one named entry is the only way to get
            // Unity to honor a custom rect without touching the source pixels.
#pragma warning disable CS0618
            importer.spritesheet = new SpriteMetaData[]
            {
                new SpriteMetaData
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path),
                    rect = contentRect.Value,
                    border = Vector4.zero,
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                }
            };
#pragma warning restore CS0618
        }

        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
