using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TutorialOverlayLightFantasySkinBuilder
{
    private const string TutorialRoot = "Assets/Resources/UI/Tutorial/LightFantasy/";
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

        Sprite instructionBoard = ImportSprite(TutorialRoot + "tutorial_instruction_panel_hd.png");
        Sprite tutorialTitle = ImportSprite(TutorialRoot + "tutorial_title_banner_hd.png");
        Sprite skipDialog = ImportSprite(TutorialRoot + "tutorial_skip_dialog_hd.png");
        Sprite primaryButton = ImportSprite(MainMenuRoot + "landing_action_button.png");
        Sprite dangerButton = ImportSprite(MainMenuRoot + "slot_delete_button.png");
        Sprite hoverButton = ImportSprite(MainMenuRoot + "landing_action_button_hover.png");

        SetImage(instruction.gameObject, instructionBoard, false, true);
        RectTransform instructionRect = instruction.GetComponent<RectTransform>();
        instructionRect.sizeDelta = new Vector2(360f, 92f);
        instructionRect.anchoredPosition = new Vector2(instructionRect.anchoredPosition.x, 80f);

        TMP_Text header = RequireChild(instruction, "Header").GetComponent<TMP_Text>();
        SetTopLeftRect(header.rectTransform, new Vector2(148f, 46f), new Vector2(14f, -5f));
        header.enabled = false;
        EnsureTitleBanner(header.transform, tutorialTitle);

        TMP_Text instructionText = RequireChild(instruction, "InstructionText").GetComponent<TMP_Text>();
        SetTopLeftRect(instructionText.rectTransform, new Vector2(248f, 30f), new Vector2(18f, -52f));
        StyleText(instructionText, 12f, new Color(0.28f, 0.14f, 0.055f, 1f), TextAlignmentOptions.TopLeft);

        Button skipButton = RequireChild(instruction, "SkipButton").GetComponent<Button>();
        StyleButton(skipButton, dangerButton, hoverButton, new Vector2(-45f, 20f), new Vector2(72f, 28f));

        Image dim = confirmation.GetComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.04f, 0.025f, 0.01f, 0.76f);
        dim.raycastTarget = true;

        SetImage(dialog.gameObject, skipDialog, false, true);
        dialog.GetComponent<RectTransform>().sizeDelta = new Vector2(570f, 245f);

        TMP_Text title = RequireChild(dialog, "Title").GetComponent<TMP_Text>();
        SetTopCenteredRect(title.rectTransform, new Vector2(430f, 34f), -72f);
        StyleText(title, 23f, new Color(0.28f, 0.14f, 0.055f, 1f), TextAlignmentOptions.Center);

        TMP_Text message = RequireChild(dialog, "Message").GetComponent<TMP_Text>();
        SetTopCenteredRect(message.rectTransform, new Vector2(450f, 64f), -122f);
        StyleText(message, 14f, new Color(0.32f, 0.18f, 0.08f, 1f), TextAlignmentOptions.Center);

        Button confirm = RequireChild(dialog, "ConfirmSkipButton").GetComponent<Button>();
        Button cancel = RequireChild(dialog, "CancelSkipButton").GetComponent<Button>();
        StyleButton(confirm, dangerButton, hoverButton, new Vector2(-100f, 30f), new Vector2(180f, 44f));
        StyleButton(cancel, primaryButton, hoverButton, new Vector2(100f, 30f), new Vector2(180f, 44f));

        EditorUtility.SetDirty(overlayUI.gameObject);
        EditorSceneManager.MarkSceneDirty(overlayUI.gameObject.scene);
        EditorSceneManager.SaveScene(overlayUI.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("TutorialOverlayRoot Light Fantasy skin applied.");
    }

    private static void StyleButton(Button button, Sprite normal, Sprite hover, Vector2 position, Vector2 size)
    {
        SetImage(button.gameObject, normal, false, true);
        button.targetGraphic = button.GetComponent<Image>();
        button.transition = Selectable.Transition.None;

        RectTransform rect = button.GetComponent<RectTransform>();
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
    }

    private static void SetTopLeftRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
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

    private static void EnsureTitleBanner(Transform headerTransform, Sprite sprite)
    {
        Transform existing = headerTransform.Find("SkinTutorialTitleBanner");
        GameObject bannerObject = existing != null
            ? existing.gameObject
            : new GameObject("SkinTutorialTitleBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bannerObject.transform.SetParent(headerTransform, false);

        RectTransform rect = bannerObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        SetImage(bannerObject, sprite, true, false);
        bannerObject.transform.SetAsLastSibling();
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
