using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SessionUXConfirmationPopupLightFantasySkinBuilder
{
    private const string SessionRoot = "Assets/Resources/UI/SessionUX/LightFantasy/";
    private const string MainMenuRoot = "Assets/Resources/UI/MainMenu/LightFantasy/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply SessionUX Confirmation Popup Light Fantasy Skin")]
    public static void Apply()
    {
        PauseMenuUI pauseMenu = UnityEngine.Object.FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pauseMenu == null) throw new InvalidOperationException("PauseMenuUI was not found in the active scene.");

        Transform popup = pauseMenu.transform.Find("MenuWindow/SessionUX/ConfirmationPopup");
        if (popup == null) throw new InvalidOperationException("SessionUX/ConfirmationPopup was not found.");

        Sprite board = ImportSprite(SessionRoot + "session_confirmation_board_hd.png");
        Sprite primary = ImportSprite(MainMenuRoot + "landing_action_button.png");
        Sprite danger = ImportSprite(MainMenuRoot + "slot_delete_button.png");
        Sprite hover = ImportSprite(MainMenuRoot + "landing_action_button_hover.png");

        Image dim = popup.GetComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.04f, 0.025f, 0.01f, 0.78f);

        Transform panel = RequireChild(popup, "Panel");
        SetImage(panel.gameObject, board);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 330f);
        DisableOutline(panel.gameObject);

        Transform titleTransform = RequireChild(panel, "Title");
        RectTransform titleRect = titleTransform.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 100f);
        titleRect.sizeDelta = new Vector2(460f, 64f);
        TMP_Text title = titleTransform.GetComponent<TMP_Text>();
        title.fontSize = 28f;
        title.color = new Color(0.28f, 0.14f, 0.055f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        StyleButton(RequireChild(panel, "SaveAction").GetComponent<Button>(), primary, hover, 25f);
        StyleButton(RequireChild(panel, "WithoutSaveAction").GetComponent<Button>(), danger, hover, -40f);
        StyleButton(RequireChild(panel, "CancelAction").GetComponent<Button>(), danger, hover, -105f);

        EditorUtility.SetDirty(pauseMenu.gameObject);
        EditorSceneManager.MarkSceneDirty(pauseMenu.gameObject.scene);
        EditorSceneManager.SaveScene(pauseMenu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("SessionUX ConfirmationPopup Light Fantasy skin applied.");
    }

    private static void StyleButton(Button button, Sprite normal, Sprite hover, float anchoredY)
    {
        SetImage(button.gameObject, normal);
        DisableOutline(button.gameObject);
        button.targetGraphic = button.GetComponent<Image>();
        button.transition = Selectable.Transition.None;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, anchoredY);
        rect.sizeDelta = new Vector2(340f, 48f);

        MainMenuButtonHoverVisual hoverVisual = button.GetComponent<MainMenuButtonHoverVisual>();
        if (hoverVisual == null) hoverVisual = button.gameObject.AddComponent<MainMenuButtonHoverVisual>();
        SerializedObject hoverObject = new SerializedObject(hoverVisual);
        hoverObject.FindProperty("_hoverSprite").objectReferenceValue = hover;
        hoverObject.ApplyModifiedPropertiesWithoutUndo();

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        label.fontSize = 18f;
        label.color = new Color(1f, 0.94f, 0.72f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) throw new InvalidOperationException($"Required ConfirmationPopup object missing: {parent.name}/{name}");
        return child;
    }

    private static void DisableOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    private static void SetImage(GameObject target, Sprite sprite)
    {
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
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
