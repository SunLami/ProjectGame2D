using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PauseMenuLightFantasySkinBuilder
{
    private const string PauseRoot = "Assets/Resources/UI/PauseMenu/LightFantasy/";
    private const string MainMenuRoot = "Assets/Resources/UI/MainMenu/LightFantasy/";
    private const string InventoryRoot = "Assets/Resources/UI/Inventory/LightFantasy/";

    private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
    {
        { "ResumeBtn", "RESUME" },
        { "SettingsBtn", "SETTINGS" },
        { "InventoryBtn", "INVENTORY" },
        { "SaveGameBtn", "SAVE" },
        { "LoadGameBtn", "LOAD" },
        { "QuitBtn", "BACK TO MENU" },
        { "MainMenuBtn", "BACK TO MENU" },
        { "QuitDesktopBtn", "EXIT" }
    };

    private static readonly string[] ButtonOrder =
    {
        "ResumeBtn",
        "InventoryBtn",
        "SaveGameBtn",
        "LoadGameBtn",
        "SettingsBtn",
        "MainMenuBtn",
        "QuitDesktopBtn"
    };

    [MenuItem("Tools/ProjectGame2D/UI/Apply Pause Menu Light Fantasy Skin")]
    public static void Apply()
    {
        PauseMenuUI pauseMenu = UnityEngine.Object.FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pauseMenu == null)
        {
            throw new InvalidOperationException("PauseMenuUI was not found in the active scene.");
        }

        Sprite board = ImportSprite(PauseRoot + "pause_menu_board_hd.png");
        Sprite primaryButton = ImportSprite(MainMenuRoot + "landing_action_button.png");
        Sprite hoverButton = ImportSprite(MainMenuRoot + "landing_action_button_hover.png");
        Sprite dangerButton = ImportSprite(MainMenuRoot + "slot_delete_button.png");
        Sprite close = ImportSprite(InventoryRoot + "inventory_close_thin_hd.png");

        Transform root = pauseMenu.transform;
        GameObject panel = Find(root, "Panel");
        SetImage(panel, board, false);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(164f, 340f);

        GameObject closeButton = Find(root, "CloseBtn");
        SetImage(closeButton, close, true);
        closeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);

        GameObject shopButton = TryFind(root, "ShopBtn");
        if (shopButton != null) shopButton.SetActive(false);
        GameObject craftButton = TryFind(root, "CraftBtn");
        if (craftButton != null) craftButton.SetActive(false);

        GameObject listObject = Find(root, "ButtonList");
        for (int index = 0; index < ButtonOrder.Length; index++)
        {
            GameObject orderedButton = TryFind(listObject.transform, ButtonOrder[index]);
            if (orderedButton == null) continue;
            orderedButton.transform.SetSiblingIndex(index);
            RectTransform orderedRect = orderedButton.GetComponent<RectTransform>();
            orderedRect.anchoredPosition = new Vector2(orderedRect.anchoredPosition.x, -48f - (index * 33.33333f));
        }

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (!Labels.TryGetValue(button.name, out string label)) continue;

            bool isDanger = button.name == "QuitDesktopBtn";
            SetImage(button.gameObject, isDanger ? dangerButton : primaryButton, false);
            button.targetGraphic = button.GetComponent<Image>();
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = hoverButton;
            spriteState.selectedSprite = hoverButton;
            button.spriteState = spriteState;

            ColorBlock colors = button.colors;
            colors.normalColor = button.interactable ? Color.white : new Color(0.62f, 0.58f, 0.45f, 0.88f);
            colors.highlightedColor = new Color(0.82f, 1f, 0.84f, 1f);
            colors.pressedColor = new Color(0.78f, 0.68f, 0.42f, 1f);
            colors.disabledColor = new Color(0.58f, 0.55f, 0.44f, 0.72f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x, 28f);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null) layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 28f;
            layout.minHeight = 28f;

            SetLabel(button.transform, label, button.interactable);
        }

        VerticalLayoutGroup list = listObject.GetComponent<VerticalLayoutGroup>();
        if (list != null)
        {
            list.enabled = false;
        }
        ContentSizeFitter fitter = listObject.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        RectTransform dirty = TryFind(root, "DirtyIndicator")?.GetComponent<RectTransform>();
        if (dirty != null)
        {
            dirty.sizeDelta = new Vector2(160f, 18f);
            dirty.anchoredPosition = new Vector2(0f, -181f);
            TMP_Text dirtyText = dirty.GetComponent<TMP_Text>();
            if (dirtyText != null)
            {
                dirtyText.fontSize = 11f;
                dirtyText.color = new Color(1f, 0.86f, 0.38f, 1f);
            }
        }

        EditorUtility.SetDirty(pauseMenu.gameObject);
        EditorSceneManager.MarkSceneDirty(pauseMenu.gameObject.scene);
        EditorSceneManager.SaveScene(pauseMenu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Pause Menu Light Fantasy skin applied to the active scene PauseMenuUI.");
    }

    private static void SetLabel(Transform button, string label, bool interactable)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            GameObject labelObject = new GameObject("SkinLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(button, false);
            RectTransform rect = (RectTransform)labelObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text = labelObject.GetComponent<TextMeshProUGUI>();
        }

        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = label.Length > 10 ? 10f : 12f;
        text.color = interactable
            ? new Color(1f, 0.94f, 0.72f, 1f)
            : new Color(0.38f, 0.31f, 0.22f, 0.78f);
        text.raycastTarget = false;
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
        GameObject result = TryFind(root, objectName);
        if (result != null) return result;
        throw new InvalidOperationException($"Required Pause Menu object not found: {objectName}");
    }

    private static GameObject TryFind(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName) return child.gameObject;
        }
        return null;
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
