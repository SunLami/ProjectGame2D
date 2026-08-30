using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GameplaySettingsLightFantasySkinBuilder
{
    private const string WarmRoot = "Assets/Resources/UI/GameplaySettings/LightFantasy/";
    private const string MainMenuRoot = "Assets/Resources/UI/MainMenu/LightFantasy/";
    private const string InventoryRoot = "Assets/Resources/UI/Inventory/LightFantasy/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply Gameplay Settings Light Fantasy Skin")]
    public static void Apply()
    {
        SettingsUI settings = UnityEngine.Object.FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (settings == null)
        {
            throw new InvalidOperationException("SettingsUI was not found in the active scene.");
        }

        Transform root = settings.transform;
        Sprite board = ImportSprite(WarmRoot + "settings_board_warm_hd.png");
        Sprite primaryButton = ImportSprite(MainMenuRoot + "landing_action_button.png");
        Sprite dangerButton = ImportSprite(MainMenuRoot + "slot_delete_button.png");
        Sprite sliderTrack = ImportSprite(MainMenuRoot + "settings_slider_track.png");
        Sprite sliderHandle = ImportSprite(MainMenuRoot + "settings_slider_handle.png");
        Sprite toggleOff = ImportSprite(MainMenuRoot + "settings_checkbox_unchecked.png");
        Sprite toggleOn = ImportSprite(MainMenuRoot + "settings_checkbox_checked.png");
        Sprite close = ImportSprite(InventoryRoot + "inventory_close_thin_hd.png");
        Sprite sfxIcon = ImportSprite(WarmRoot + "settings_sfx_icon_hd.png");
        Sprite musicIcon = ImportSprite(WarmRoot + "settings_music_icon_hd.png");
        Sprite settingsTitle = ImportSprite(MainMenuRoot + "settings_title.png");

        SetImage(Find(root, "Panel"), board, false);
        StyleButton(Find(root, "SaveBtn"), primaryButton, "SAVE");
        StyleButton(Find(root, "DeclineBtn"), dangerButton, "CANCEL");
        GameObject closeButton = Find(root, "CloseBtn");
        SetImage(closeButton, close, true);
        closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-24f, -34f);
        GameObject titleObject = Find(root, "Title");
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(titleRect.anchoredPosition.x, -30f);
        titleRect.sizeDelta = new Vector2(150f, 45f);
        titleRect.SetAsLastSibling();
        TMP_Text titleText = titleRect.GetComponent<TMP_Text>();
        if (titleText != null) titleText.enabled = false;
        Transform existingTitleArtwork = titleRect.Find("SkinTitleArtwork");
        GameObject titleArtwork = existingTitleArtwork != null
            ? existingTitleArtwork.gameObject
            : new GameObject("SkinTitleArtwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        titleArtwork.transform.SetParent(titleRect, false);
        RectTransform titleArtworkRect = titleArtwork.GetComponent<RectTransform>();
        titleArtworkRect.anchorMin = Vector2.zero;
        titleArtworkRect.anchorMax = Vector2.one;
        titleArtworkRect.offsetMin = Vector2.zero;
        titleArtworkRect.offsetMax = Vector2.zero;
        SetImage(titleArtwork, settingsTitle, true);
        titleArtwork.GetComponent<Image>().raycastTarget = false;
        GameObject sfxRow = Find(root, "SfxRow");
        GameObject musicRow = Find(root, "MusicRow");
        sfxRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -72f);
        musicRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -102f);
        StyleRowIcon(sfxRow, sfxIcon);
        StyleRowIcon(musicRow, musicIcon);

        Find(root, "FullScreenToggle").GetComponent<RectTransform>().anchoredPosition = new Vector2(-69f, -140f);
        Find(root, "WindowModeToggle").GetComponent<RectTransform>().anchoredPosition = new Vector2(-69f, -169f);
        Find(root, "SaveBtn").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -202f);
        Find(root, "DeclineBtn").GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -236f);

        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
        {
            GameObject backgroundObject = Find(slider.transform, "Background");
            SetImage(backgroundObject, sliderTrack, false);
            backgroundObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 12f);

            RectTransform fillArea = Find(slider.transform, "Fill Area").GetComponent<RectTransform>();
            fillArea.sizeDelta = new Vector2(-12f, 3f);
            Image fill = Find(slider.transform, "Fill").GetComponent<Image>();
            fill.sprite = null;
            fill.color = new Color(0.95f, 0.65f, 0.12f, 1f);

            RectTransform handleArea = Find(slider.transform, "Handle Slide Area").GetComponent<RectTransform>();
            handleArea.sizeDelta = new Vector2(-12f, 0f);
            GameObject handleObject = Find(slider.transform, "Handle");
            handleObject.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 12f);
            SetImage(handleObject, sliderHandle, true);
            slider.targetGraphic = handleObject.GetComponent<Image>();

            fillArea.SetAsFirstSibling();
            backgroundObject.transform.SetSiblingIndex(1);
            handleArea.SetAsLastSibling();
        }

        foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
        {
            SetImage(toggle.gameObject, toggleOff, true);
            Image checkmark = EnsureCheckmark(toggle.transform, toggleOn);
            toggle.targetGraphic = toggle.GetComponent<Image>();
            toggle.graphic = checkmark;
        }

        SerializedObject serializedSettings = new SerializedObject(settings);
        serializedSettings.FindProperty("_checkedSprite").objectReferenceValue = toggleOn;
        serializedSettings.FindProperty("_uncheckedSprite").objectReferenceValue = toggleOff;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.color = text.name == "SkinLabel"
                ? new Color(1f, 0.93f, 0.72f, 1f)
                : new Color(0.28f, 0.15f, 0.07f, 1f);
        }

        EditorUtility.SetDirty(settings.gameObject);
        EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
        EditorSceneManager.SaveScene(settings.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Gameplay Settings Light Fantasy skin applied to the active scene SettingsUI.");
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

    private static GameObject Find(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName) return child.gameObject;
        }
        throw new InvalidOperationException($"Required Settings UI object not found: {objectName}");
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

    private static void StyleButton(GameObject target, Sprite sprite, string label)
    {
        SetImage(target, sprite, false);
        Button button = target.GetComponent<Button>();
        if (button != null) button.targetGraphic = target.GetComponent<Image>();

        Transform existing = target.transform.Find("SkinLabel");
        GameObject labelObject = existing != null
            ? existing.gameObject
            : new GameObject("SkinLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(target.transform, false);
        RectTransform rect = (RectTransform)labelObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 12f;
        text.color = new Color(1f, 0.93f, 0.72f, 1f);
        text.raycastTarget = false;
    }

    private static Image EnsureCheckmark(Transform toggle, Sprite sprite)
    {
        Transform existing = toggle.Find("SkinCheckmark");
        GameObject checkmarkObject = existing != null
            ? existing.gameObject
            : new GameObject("SkinCheckmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkmarkObject.transform.SetParent(toggle, false);
        RectTransform rect = (RectTransform)checkmarkObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = checkmarkObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void StyleRowIcon(GameObject row, Sprite sprite)
    {
        Transform icon = row.transform.Find("Icon");
        if (icon == null) throw new InvalidOperationException($"Icon container not found under {row.name}");
        icon.gameObject.SetActive(true);
        RectTransform iconRect = (RectTransform)icon;
        iconRect.anchoredPosition = new Vector2(34f, iconRect.anchoredPosition.y);
        foreach (Transform child in icon) child.gameObject.SetActive(false);
        Image image = icon.GetComponent<Image>();
        if (image == null) image = icon.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;

        Transform legacyLabel = row.transform.Find("SkinRowLabel");
        if (legacyLabel != null) legacyLabel.gameObject.SetActive(false);
    }
}
