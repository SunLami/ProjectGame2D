using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SessionUXLoadOverlayLightFantasySkinBuilder
{
    private const string SessionRoot = "Assets/Resources/UI/SessionUX/LightFantasy/";
    private const string MainMenuRoot = "Assets/Resources/UI/MainMenu/LightFantasy/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply SessionUX Load Overlay Light Fantasy Skin")]
    public static void Apply()
    {
        PauseMenuUI pauseMenu = UnityEngine.Object.FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pauseMenu == null) throw new InvalidOperationException("PauseMenuUI was not found in the active scene.");

        Transform overlay = pauseMenu.transform.Find("MenuWindow/SessionUX/LoadOverlay");
        if (overlay == null) throw new InvalidOperationException("SessionUX/LoadOverlay was not found under PauseMenuUI.");

        Sprite overlayBoard = ImportSprite(SessionRoot + "session_slot_overlay_board_hd.png");
        Sprite slotCard = ImportSprite(SessionRoot + "session_slot_card_hd.png");
        Sprite primaryButton = ImportSprite(MainMenuRoot + "landing_action_button.png");
        Sprite hoverButton = ImportSprite(MainMenuRoot + "landing_action_button_hover.png");
        Sprite dangerButton = ImportSprite(MainMenuRoot + "slot_delete_button.png");
        Sprite loadTitleBanner = ImportSprite(SessionRoot + "session_load_title_banner_hd.png");
        Sprite saveTitleBanner = ImportSprite(SessionRoot + "session_save_title_banner_hd.png");
        Sprite[] slotBadges =
        {
            ImportSprite(MainMenuRoot + "slot_badge_1.png"),
            ImportSprite(MainMenuRoot + "slot_badge_2.png"),
            ImportSprite(MainMenuRoot + "slot_badge_3.png")
        };

        Image dim = overlay.GetComponent<Image>();
        if (dim != null)
        {
            dim.sprite = null;
            dim.color = new Color(0.08f, 0.05f, 0.02f, 0.72f);
        }

        Transform panel = RequireDirectChild(overlay, "LoadPanel");
        SetImage(panel.gameObject, overlayBoard, false);

        Transform overlayTitleTransform = RequireDirectChild(panel, "Title");
        RectTransform overlayTitleRect = overlayTitleTransform.GetComponent<RectTransform>();
        overlayTitleRect.anchoredPosition = new Vector2(overlayTitleRect.anchoredPosition.x, -92f);
        overlayTitleRect.sizeDelta = new Vector2(560f, 52f);
        overlayTitleTransform.SetAsLastSibling();
        TMP_Text overlayTitle = overlayTitleTransform.GetComponent<TMP_Text>();
        overlayTitle.enabled = false;
        Image overlayTitleBanner = EnsureOverlayTitleBanner(overlayTitleTransform, loadTitleBanner);

        for (int index = 1; index <= 3; index++)
        {
            Transform slot = RequireDirectChild(panel, "Slot" + index);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(240f, 320f);
            slotRect.anchoredPosition = new Vector2((index - 2) * 250f, -8f);
            SetImage(slot.gameObject, slotCard, false);

            Transform titleTransform = RequireDirectChild(slot, "Title");
            RectTransform titleRect = titleTransform.GetComponent<RectTransform>();
            SetTopAnchoredRect(titleRect, new Vector2(220f, 40f), -45f);
            TMP_Text title = titleTransform.GetComponent<TMP_Text>();
            title.enabled = false;
            EnsureSlotBadge(titleTransform, slotBadges[index - 1]);
            Transform statusTransform = RequireDirectChild(slot, "Status");
            SetTopAnchoredRect(statusTransform.GetComponent<RectTransform>(), new Vector2(220f, 36f), -85f);
            TMP_Text status = statusTransform.GetComponent<TMP_Text>();
            Transform detailsTransform = RequireDirectChild(slot, "Details");
            SetTopAnchoredRect(detailsTransform.GetComponent<RectTransform>(), new Vector2(208f, 110f), -158f);
            TMP_Text details = detailsTransform.GetComponent<TMP_Text>();
            StyleText(title, 18f, new Color(0.25f, 0.12f, 0.04f, 1f), TextAlignmentOptions.Center);
            StyleText(status, 16f, new Color(0.58f, 0.30f, 0.055f, 1f), TextAlignmentOptions.Center);
            StyleText(details, 12f, new Color(0.25f, 0.14f, 0.07f, 1f), TextAlignmentOptions.TopLeft);

            foreach (Button slotButton in slot.GetComponentsInChildren<Button>(true))
            {
                RectTransform buttonRect = slotButton.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0f);
                buttonRect.anchorMax = new Vector2(0.5f, 0f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.sizeDelta = new Vector2(190f, 44f);
                buttonRect.anchoredPosition = new Vector2(0f, slotButton.name == "DeleteButton" ? 88f : 38f);
            }
        }

        foreach (Button button in overlay.GetComponentsInChildren<Button>(true))
        {
            bool danger = button.name == "DeleteButton";
            SetImage(button.gameObject, danger ? dangerButton : primaryButton, false);
            button.targetGraphic = button.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            SpriteState state = button.spriteState;
            state.highlightedSprite = hoverButton;
            state.selectedSprite = danger ? dangerButton : primaryButton;
            button.spriteState = state;

            MainMenuButtonHoverVisual hoverVisual = button.GetComponent<MainMenuButtonHoverVisual>();
            if (hoverVisual == null) hoverVisual = button.gameObject.AddComponent<MainMenuButtonHoverVisual>();
            SerializedObject hoverVisualObject = new SerializedObject(hoverVisual);
            hoverVisualObject.FindProperty("_hoverSprite").objectReferenceValue = hoverButton;
            hoverVisualObject.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                StyleText(label, 18f, new Color(1f, 0.94f, 0.72f, 1f), TextAlignmentOptions.Center);
            }
        }

        RectTransform backRect = RequireDirectChild(panel, "BackButton").GetComponent<RectTransform>();
        backRect.anchoredPosition = new Vector2(backRect.anchoredPosition.x, 66f);
        backRect.SetAsLastSibling();

        SerializedObject serializedPauseMenu = new SerializedObject(pauseMenu);
        serializedPauseMenu.FindProperty("_slotOverlayTitleBanner").objectReferenceValue = overlayTitleBanner;
        serializedPauseMenu.FindProperty("_saveSlotTitleBanner").objectReferenceValue = saveTitleBanner;
        serializedPauseMenu.FindProperty("_loadSlotTitleBanner").objectReferenceValue = loadTitleBanner;
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
