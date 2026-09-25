using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SessionUXLoadOverlayLightFantasySkinBuilder
{
    private const string SessionRoot = "Assets/Resources/UI/SessionUX/DarkInventoryStyle/";
    private const string DialogueRoot = "Assets/Resources/UI/Dialogue/DarkInventoryStyle/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply SessionUX Load Overlay Light Fantasy Skin")]
    public static void Apply()
    {
        PauseMenuUI pauseMenu = FindLoadedSceneObject<PauseMenuUI>();
        if (pauseMenu == null) throw new InvalidOperationException("PauseMenuUI was not found in the active scene.");

        Transform overlay = pauseMenu.transform.Find("MenuWindow/SessionUX/LoadOverlay");
        if (overlay == null) throw new InvalidOperationException("SessionUX/LoadOverlay was not found under PauseMenuUI.");

        Sprite overlayBoard = ImportSprite(SessionRoot + "session_slot_overlay_board_v1.png");
        Sprite slotCard = ImportSprite(SessionRoot + "session_slot_card_v1.png");
        Sprite primaryButton = ImportSprite(DialogueRoot + "dialogue_action_button_v1.png");

        Image dim = overlay.GetComponent<Image>();
        if (dim != null)
        {
            dim.sprite = null;
            dim.color = new Color(0.08f, 0.05f, 0.02f, 0.72f);
        }

        Transform panel = RequireDirectChild(overlay, "LoadPanel");
        SetImage(panel.gameObject, overlayBoard, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.localScale = Vector3.one;
        panelRect.sizeDelta = new Vector2(430f, 250f);
        DisableOutline(panel.gameObject);

        Transform overlayTitleTransform = RequireDirectChild(panel, "Title");
        RectTransform overlayTitleRect = overlayTitleTransform.GetComponent<RectTransform>();
        overlayTitleRect.localScale = Vector3.one;
        overlayTitleRect.anchoredPosition = new Vector2(0f, -61f);
        overlayTitleRect.sizeDelta = new Vector2(240f, 28f);
        overlayTitleTransform.SetAsLastSibling();
        TMP_Text overlayTitle = overlayTitleTransform.GetComponent<TMP_Text>();
        overlayTitle.enabled = true;
        StyleText(overlayTitle, 14f, new Color(1f, 0.84f, 0.38f, 1f), TextAlignmentOptions.Center);
        Image overlayTitleBanner = EnsureOverlayTitleBanner(overlayTitleTransform, null);
        overlayTitleBanner.enabled = false;

        for (int index = 1; index <= 3; index++)
        {
            Transform slot = RequireDirectChild(panel, "Slot" + index);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.localScale = Vector3.one;
            slotRect.sizeDelta = new Vector2(116f, 165f);
            slotRect.anchoredPosition = new Vector2((index - 2) * 130f, -3f);
            SetImage(slot.gameObject, slotCard, false);
            DisableOutline(slot.gameObject);

            Transform titleTransform = RequireDirectChild(slot, "Title");
            RectTransform titleRect = titleTransform.GetComponent<RectTransform>();
            SetTopAnchoredRect(titleRect, new Vector2(82f, 14f), -3f);
            TMP_Text title = titleTransform.GetComponent<TMP_Text>();
            title.enabled = true;
            Transform oldBadge = titleTransform.Find("SkinSlotBadge");
            if (oldBadge != null) oldBadge.gameObject.SetActive(false);
            Transform statusTransform = RequireDirectChild(slot, "Status");
            SetTopAnchoredRect(statusTransform.GetComponent<RectTransform>(), new Vector2(102f, 20f), -45f);
            TMP_Text status = statusTransform.GetComponent<TMP_Text>();
            Transform detailsTransform = RequireDirectChild(slot, "Details");
            RectTransform detailsRect = detailsTransform.GetComponent<RectTransform>();
            SetTopAnchoredRect(detailsRect, new Vector2(78f, 58f), -77f);
            detailsRect.anchoredPosition = new Vector2(4f, detailsRect.anchoredPosition.y);
            TMP_Text details = detailsTransform.GetComponent<TMP_Text>();
            StyleText(title, 7.5f, new Color(1f, 0.84f, 0.38f, 1f), TextAlignmentOptions.Center);
            StyleText(status, 8.5f, new Color(1f, 0.93f, 0.72f, 1f), TextAlignmentOptions.Center);
            StyleText(details, 6.25f, new Color(0.86f, 0.89f, 0.94f, 1f), TextAlignmentOptions.TopLeft);
            details.enableAutoSizing = true;
            details.fontSizeMin = 4.5f;
            details.fontSizeMax = 6.25f;
            details.textWrappingMode = TextWrappingModes.NoWrap;
            details.margin = new Vector4(2f, 0f, 2f, 0f);

            foreach (Button slotButton in slot.GetComponentsInChildren<Button>(true))
            {
                RectTransform buttonRect = slotButton.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0f);
                buttonRect.anchorMax = new Vector2(0.5f, 0f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.localScale = Vector3.one;
                buttonRect.sizeDelta = new Vector2(96f, 24f);
                buttonRect.anchoredPosition = new Vector2(0f, slotButton.name == "DeleteButton" ? 48f : 18f);
                DisableOutline(slotButton.gameObject);
            }
        }

        foreach (Button button in overlay.GetComponentsInChildren<Button>(true))
        {
            bool danger = button.name == "DeleteButton";
            SetImage(button.gameObject, primaryButton, false);
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
            MainMenuButtonHoverVisual hoverVisual = button.GetComponent<MainMenuButtonHoverVisual>();
            if (hoverVisual != null) UnityEngine.Object.DestroyImmediate(hoverVisual);
            EditorUtility.SetDirty(button);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                StyleText(label, 8f, new Color(1f, 0.94f, 0.72f, 1f), TextAlignmentOptions.Center);
            }
        }

        RectTransform backRect = RequireDirectChild(panel, "BackButton").GetComponent<RectTransform>();
        backRect.localScale = Vector3.one;
        backRect.sizeDelta = new Vector2(120f, 28f);
        backRect.anchoredPosition = new Vector2(0f, 31f);
        backRect.SetAsLastSibling();

        SerializedObject serializedPauseMenu = new SerializedObject(pauseMenu);
        serializedPauseMenu.FindProperty("_slotOverlayTitleBanner").objectReferenceValue = overlayTitleBanner;
        serializedPauseMenu.FindProperty("_saveSlotTitleBanner").objectReferenceValue = null;
        serializedPauseMenu.FindProperty("_loadSlotTitleBanner").objectReferenceValue = null;
        serializedPauseMenu.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(pauseMenu.gameObject);
        EditorSceneManager.MarkSceneDirty(pauseMenu.gameObject.scene);
        EditorSceneManager.SaveScene(pauseMenu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("SessionUX LoadOverlay Light Fantasy skin applied to the active scene.");
    }

    private static Transform RequireDirectChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) throw new InvalidOperationException($"Required LoadOverlay object not found: {parent.name}/{name}");
        return child;
    }

    private static void StyleText(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        EditorUtility.SetDirty(text);
    }

    private static void SetTopAnchoredRect(RectTransform rect, Vector2 size, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, y);
    }

    private static void DisableOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    private static void EnsureSlotBadge(Transform titleTransform, Sprite badge)
    {
        Transform existing = titleTransform.Find("SkinSlotBadge");
        GameObject badgeObject = existing != null
            ? existing.gameObject
            : new GameObject("SkinSlotBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObject.transform.SetParent(titleTransform, false);
        RectTransform rect = badgeObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(150f, 48f);
        SetImage(badgeObject, badge, true);
        badgeObject.GetComponent<Image>().raycastTarget = false;
        badgeObject.transform.SetAsLastSibling();
    }

    private static Image EnsureOverlayTitleBanner(Transform titleTransform, Sprite sprite)
    {
        Transform existing = titleTransform.Find("SkinTitleBanner");
        GameObject bannerObject = existing != null
            ? existing.gameObject
            : new GameObject("SkinTitleBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bannerObject.transform.SetParent(titleTransform, false);
        RectTransform rect = bannerObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 12f);
        rect.sizeDelta = new Vector2(280f, 70f);
        SetImage(bannerObject, sprite, true);
        Image image = bannerObject.GetComponent<Image>();
        image.raycastTarget = false;
        bannerObject.transform.SetAsLastSibling();
        return image;
    }

    private static T FindLoadedSceneObject<T>() where T : Component
    {
        foreach (T component in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded)
                return component;
        return null;
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

    private static void SetImage(GameObject target, Sprite sprite, bool preserveAspect)
    {
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.AddComponent<Image>();
        image.sprite = sprite;
        image.enabled = true;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        EditorUtility.SetDirty(image);
    }
}
