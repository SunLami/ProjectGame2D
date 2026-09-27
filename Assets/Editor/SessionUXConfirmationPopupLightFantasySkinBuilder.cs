using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SessionUXConfirmationPopupLightFantasySkinBuilder
{
    private const string SessionRoot = "Assets/Resources/UI/SessionUX/DarkInventoryStyle/";
    private const string DialogueRoot = "Assets/Resources/UI/Dialogue/DarkInventoryStyle/";
    private const string GameplayUiPrefabPath = "Assets/Prefabs/UI/GameplayUIRoot.prefab";

    [MenuItem("Tools/ProjectGame2D/UI/Apply SessionUX Confirmation Popup Light Fantasy Skin")]
    public static void Apply()
    {
        PauseMenuUI pauseMenu = FindLoadedSceneObject<PauseMenuUI>();
        if (pauseMenu == null) throw new InvalidOperationException("PauseMenuUI was not found in the active scene.");

        ApplyTo(pauseMenu);

        EditorSceneManager.MarkSceneDirty(pauseMenu.gameObject.scene);
        EditorSceneManager.SaveScene(pauseMenu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("SessionUX ConfirmationPopup Light Fantasy skin applied.");
    }

    [MenuItem("Tools/ProjectGame2D/UI/Apply SessionUX Confirmation Popup Skin To GameplayUIRoot Prefab")]
    public static void ApplyToGameplayUiPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameplayUiPrefabPath);
        try
        {
            PauseMenuUI pauseMenu = root.GetComponentInChildren<PauseMenuUI>(true);
            if (pauseMenu == null) throw new InvalidOperationException("PauseMenuUI was not found in GameplayUIRoot.prefab.");
            ApplyTo(pauseMenu);
            PrefabUtility.SaveAsPrefabAsset(root, GameplayUiPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("SessionUX ConfirmationPopup skin applied to GameplayUIRoot.prefab.");
    }

    private static void ApplyTo(PauseMenuUI pauseMenu)
    {

        Transform popup = pauseMenu.transform.Find("MenuWindow/SessionUX/ConfirmationPopup");
        if (popup == null) throw new InvalidOperationException("SessionUX/ConfirmationPopup was not found.");

        Sprite board = ImportSprite(SessionRoot + "session_confirmation_board_v3.png");
        Sprite primary = ImportSprite(DialogueRoot + "dialogue_action_button_v1.png");

        Image dim = popup.GetComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.04f, 0.025f, 0.01f, 0.78f);

        Transform panel = RequireChild(popup, "Panel");
        SetImage(panel.gameObject, board);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.localScale = Vector3.one;
        panelRect.sizeDelta = new Vector2(500f, 245f);
        DisableOutline(panel.gameObject);

        Transform titleTransform = RequireChild(panel, "Title");
        RectTransform titleRect = titleTransform.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.localScale = Vector3.one;
        titleRect.anchoredPosition = new Vector2(0f, 62f);
        titleRect.sizeDelta = new Vector2(390f, 48f);
        TMP_Text title = titleTransform.GetComponent<TMP_Text>();
        title.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
        title.enableAutoSizing = true;
        title.fontSizeMin = 10f;
        title.fontSizeMax = 15f;
        title.fontSize = 15f;
        title.color = new Color(1f, 0.93f, 0.72f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        StyleButton(RequireChild(panel, "SaveAction").GetComponent<Button>(), primary, false, 12f);
        StyleButton(RequireChild(panel, "WithoutSaveAction").GetComponent<Button>(), primary, true, -22f);
        StyleButton(RequireChild(panel, "CancelAction").GetComponent<Button>(), primary, true, -56f);

        EditorUtility.SetDirty(pauseMenu.gameObject);
    }

    private static void StyleButton(Button button, Sprite normal, bool danger, float anchoredY)
    {
        SetImage(button.gameObject, normal);
        DisableOutline(button.gameObject);
        button.targetGraphic = button.GetComponent<Image>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = danger ? new Color(0.72f, 0.30f, 0.25f, 1f) : Color.white;
        colors.highlightedColor = danger ? new Color(1f, 0.48f, 0.38f, 1f) : new Color(0.58f, 0.82f, 1f, 1f);
        colors.pressedColor = danger ? new Color(0.52f, 0.18f, 0.16f, 1f) : new Color(0.68f, 0.55f, 0.30f, 1f);
        colors.disabledColor = new Color(0.45f, 0.42f, 0.34f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = new Vector2(0f, anchoredY);
        rect.sizeDelta = new Vector2(230f, 24f);

        MainMenuButtonHoverVisual hoverVisual = button.GetComponent<MainMenuButtonHoverVisual>();
        if (hoverVisual != null) UnityEngine.Object.DestroyImmediate(hoverVisual);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        label.fontSizeMax = 13f;
        label.color = new Color(1f, 0.94f, 0.72f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        EditorUtility.SetDirty(button);
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

    private static T FindLoadedSceneObject<T>() where T : Component
    {
        foreach (T component in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded)
                return component;
        return null;
    }
}
