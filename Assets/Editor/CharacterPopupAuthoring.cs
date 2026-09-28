using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterPopupAuthoring
{
    private const string PrefabPath = "Assets/Resources/UI/Gameplay/UnifiedHUD/UnifiedGameplayHUD.prefab";
    private const string UnifiedBoardPath = "Assets/Resources/UI/Character/DarkInventoryStyle/character_popup_unified_v1.png";
    private const string InnerTitlePath = "Assets/Resources/UI/Character/DarkInventoryStyle/character_inner_title_v1.png";
    private const string StatSectionPath = "Assets/Resources/UI/Character/DarkInventoryStyle/character_stat_section_v1.png";
    private const string SlotPath = "Assets/Resources/UI/Inventory/QuestStyle1920/inventory_slot_reference_v4.png";

    [MenuItem("Tools/ProjectGame2D/UI/Rebuild Character Popup")]
    public static void Rebuild()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            RectTransform popup = root.transform.Find("CharacterPopup") as RectTransform;
            if (popup == null)
                throw new InvalidOperationException("CharacterPopup was not found in UnifiedGameplayHUD.prefab.");

            for (int i = popup.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(popup.GetChild(i).gameObject);

            ConfigurePopupRoot(popup);
            BuildPopup(root, popup);
            popup.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("CharacterPopup rebuilt without modifying BottomHUD layout.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigurePopupRoot(RectTransform popup)
    {
        popup.anchorMin = Vector2.zero;
        popup.anchorMax = Vector2.one;
        popup.offsetMin = Vector2.zero;
        popup.offsetMax = Vector2.zero;
        popup.pivot = new Vector2(0.5f, 0.5f);

        Image dim = popup.GetComponent<Image>() ?? popup.gameObject.AddComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.035f, 0.025f, 0.02f, 0.72f);
        dim.raycastTarget = true;
    }

    private static void BuildPopup(GameObject root, RectTransform popup)
    {
        Sprite unifiedBoard = LoadSprite(UnifiedBoardPath);
        // Border values measured from the source bitmaps' actual diagonal corner-bevel bounds
        // (pixel-sampled), not the placeholder 72/72/72/72 and 56/56/56/56 that exceeded the
        // sprites' own trimmed height (138px and 120px), guaranteeing 9-slice corner distortion.
        Sprite innerTitle = LoadSlicedSprite(InnerTitlePath, new Vector4(32f, 28f, 32f, 28f), new Rect(2f, 22f, 997f, 132f));
        Sprite statSection = LoadSlicedSprite(StatSectionPath, new Vector4(16f, 12f, 16f, 12f), new Rect(0f, 51f, 798f, 121f));
        Sprite slot = LoadSprite(SlotPath);
        Sprite close = LoadSprite("Assets/Resources/UI/Inventory/LightFantasy/inventory_close_thin_hd.png");

        RectTransform window = CreateRect("Window", popup, Vector2.zero, new Vector2(760f, 410f));
        Image windowBoard = window.gameObject.AddComponent<Image>();
        windowBoard.sprite = unifiedBoard;
        windowBoard.raycastTarget = false;
        List<Image> equipmentIcons = BuildEquipmentPanel(window, innerTitle, slot);
        StatTextBindings stats = BuildStatsPanel(window, innerTitle, statSection);
        BuildCloseButton(root, window, close);

        CharacterPopupUI popupUi = popup.GetComponent<CharacterPopupUI>() ?? popup.gameObject.AddComponent<CharacterPopupUI>();
        SerializedObject popupSo = new SerializedObject(popupUi);
        SerializedProperty slots = popupSo.FindProperty("_equipmentSlots");
        EquipSlot[] slotTypes = { EquipSlot.Head, EquipSlot.Weapon, EquipSlot.Body, EquipSlot.Shield, EquipSlot.Necklace, EquipSlot.Ring, EquipSlot.Foot };
        slots.arraySize = slotTypes.Length;
        for (int i = 0; i < slotTypes.Length; i++)
        {
            SerializedProperty element = slots.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("slot").enumValueIndex = (int)slotTypes[i];
            element.FindPropertyRelative("icon").objectReferenceValue = equipmentIcons[i];
        }
        popupSo.FindProperty("_levelValue").objectReferenceValue = stats.Level;
        popupSo.FindProperty("_vitalsValues").objectReferenceValue = stats.Vitals;
        popupSo.FindProperty("_combatValues").objectReferenceValue = stats.Combat;
        popupSo.FindProperty("_mobilityValues").objectReferenceValue = stats.Mobility;
        popupSo.FindProperty("_recoveryValues").objectReferenceValue = stats.Recovery;
        popupSo.ApplyModifiedPropertiesWithoutUndo();

        UnifiedGameplayHudController hud = root.GetComponent<UnifiedGameplayHudController>();
        SerializedObject hudSo = new SerializedObject(hud);
        hudSo.FindProperty("_characterPopup").objectReferenceValue = popup.gameObject;
        hudSo.FindProperty("_characterStatsText").objectReferenceValue = null;
        hudSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<Image> BuildEquipmentPanel(RectTransform window, Sprite titleSprite, Sprite slotSprite)
    {
        RectTransform panel = CreateRect("EquipmentPanel", window, new Vector2(-222f, 0f), new Vector2(280f, 400f));

        RectTransform titleFrame = CreateRect("TitleFrame", panel, new Vector2(0f, 130f), new Vector2(190f, 30f));
        Image titleImage = titleFrame.gameObject.AddComponent<Image>();
        titleImage.sprite = titleSprite;
        titleImage.type = Image.Type.Sliced;
        titleImage.raycastTarget = false;
        TMP_Text title = CreateText("Title", titleFrame, Vector2.zero, new Vector2(178f, 26f), "EQUIPMENT", 18f, TextAlignmentOptions.Center);
        title.color = new Color(0.86f, 0.69f, 0.30f, 1f);
        title.fontStyle = FontStyles.Bold;

        (EquipSlot Slot, string Name, Vector2 Position)[] definitions =
        {
            (EquipSlot.Head, "Head", new Vector2(0f, 82f)),
            (EquipSlot.Weapon, "Weapon", new Vector2(-76f, 17f)),
            (EquipSlot.Body, "Body", new Vector2(0f, 17f)),
            (EquipSlot.Shield, "Shield", new Vector2(76f, 17f)),
            (EquipSlot.Necklace, "Necklace", new Vector2(0f, -52f)),
            (EquipSlot.Ring, "Ring", new Vector2(-55f, -125f)),
            (EquipSlot.Foot, "Foot", new Vector2(55f, -125f))
        };

        var icons = new List<Image>(definitions.Length);
        foreach ((EquipSlot _, string name, Vector2 position) in definitions)
        {
            RectTransform slot = CreateRect(name + "Slot", panel, position, new Vector2(58f, 58f));
            Image slotImage = slot.gameObject.AddComponent<Image>();
            slotImage.sprite = slotSprite;
            slotImage.raycastTarget = false;

            RectTransform iconRect = CreateRect("Icon", slot, Vector2.zero, new Vector2(42f, 42f));
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            icons.Add(icon);

            TMP_Text label = CreateText("Label", slot, new Vector2(0f, -34f), new Vector2(74f, 14f), name.ToUpperInvariant(), 7.5f, TextAlignmentOptions.Top);
            label.color = new Color(0.82f, 0.78f, 0.68f, 0.88f);
        }
        return icons;
    }

    private static StatTextBindings BuildStatsPanel(RectTransform window, Sprite titleSprite, Sprite sectionSprite)
    {
        RectTransform panel = CreateRect("CharacterStatsPanel", window, new Vector2(145f, 0f), new Vector2(450f, 400f));

        RectTransform titleFrame = CreateRect("TitleFrame", panel, new Vector2(0f, 132f), new Vector2(310f, 34f));
        Image titleImage = titleFrame.gameObject.AddComponent<Image>();
        titleImage.sprite = titleSprite;
        titleImage.type = Image.Type.Sliced;
        titleImage.raycastTarget = false;
        // 310 wide frame with >=10px inset on each side.
        TMP_Text title = CreateText("Title", titleFrame, Vector2.zero, new Vector2(290f, 30f), "CHARACTER STATS", 20f, TextAlignmentOptions.Center);
        title.color = new Color(0.86f, 0.69f, 0.30f, 1f);
        title.fontStyle = FontStyles.Bold;

        RectTransform badge = CreateRect("LevelBadge", panel, new Vector2(0f, 94f), new Vector2(330f, 28f));
        Image badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.sprite = titleSprite;
        badgeImage.type = Image.Type.Sliced;
        badgeImage.color = new Color(0.72f, 0.82f, 1f, 1f);
        badgeImage.raycastTarget = false;
        TMP_Text level = CreateText("LevelValue", badge, Vector2.zero, new Vector2(310f, 24f), "LV. 1", 15f, TextAlignmentOptions.Center);
        level.color = new Color(1f, 0.88f, 0.47f, 1f);
        level.fontStyle = FontStyles.Bold;

        TMP_Text vitals = CreateSection(panel, sectionSprite, "Vitals", 51f, 54f, "Health\nStamina", "100 / 100\n100 / 100");
        TMP_Text combat = CreateSection(panel, sectionSprite, "Combat", -16f, 72f, "Attack\nDefense\nCritical Chance\nCritical Damage", "10.0\n2.0\n5.0%\nx1.50");
        TMP_Text mobility = CreateSection(panel, sectionSprite, "Mobility", -85f, 58f, "Move Speed\nSprint Multiplier\nDodge Chance", "2.0\nx2.00\n0.0%");
        TMP_Text recovery = CreateSection(panel, sectionSprite, "Recovery", -143f, 50f, "Damage Reduction\nHealth Regeneration", "0.0%\n0.0 /s");

        return new StatTextBindings(level, vitals, combat, mobility, recovery);
    }

    private static void BuildCloseButton(GameObject root, RectTransform window, Sprite closeSprite)
    {
        RectTransform closeRect = CreateRect("CloseButton", window, Vector2.one, Vector2.one, new Vector2(-30f, -30f), new Vector2(34f, 34f));
        Image closeImage = closeRect.gameObject.AddComponent<Image>();
        closeImage.sprite = closeSprite;
        Button closeButton = closeRect.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        UnityEventTools.AddPersistentListener(closeButton.onClick, root.GetComponent<UnifiedGameplayHudController>().ClosePopup);
    }

    private static TMP_Text CreateSection(RectTransform parent, Sprite sectionSprite, string name, float y, float height, string labels, string values)
    {
        RectTransform section = CreateRect(name, parent, new Vector2(0f, y), new Vector2(350f, height));
        Image background = section.gameObject.AddComponent<Image>();
        background.sprite = sectionSprite;
        background.type = Image.Type.Sliced;
        background.color = Color.white;
        background.raycastTarget = false;

        TMP_Text header = CreateText("Header", section, new Vector2(0f, height * 0.5f - 10f), new Vector2(324f, 16f), name.ToUpperInvariant(), 10.5f, TextAlignmentOptions.MidlineLeft);
        header.color = new Color(0.86f, 0.69f, 0.30f, 1f);
        header.fontStyle = FontStyles.Bold;

        float bodyHeight = height - 24f;
        const float bodyY = -6f;
        TMP_Text labelText = CreateText("Labels", section, new Vector2(-72f, bodyY), new Vector2(170f, bodyHeight), labels, 9.5f, TextAlignmentOptions.TopLeft);
        labelText.lineSpacing = 1f;
        labelText.color = new Color(0.90f, 0.87f, 0.80f, 1f);
        TMP_Text valueText = CreateText("Values", section, new Vector2(104f, bodyY), new Vector2(120f, bodyHeight), values, 9.5f, TextAlignmentOptions.TopRight);
        valueText.lineSpacing = 1f;
        valueText.color = new Color(0.78f, 0.86f, 1f, 1f);
        valueText.fontStyle = FontStyles.Bold;
        return valueText;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size) =>
        CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.19f, 0.105f, 0.055f, 1f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static Sprite LoadSprite(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer
            && (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Missing sprite: " + path);
        return sprite;
    }

    private static Sprite LoadSlicedSprite(string path, Vector4 border, Rect contentRect)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            // SpriteImportMode.Single ignores any custom rect written to `spritesheet` -- it always
            // reports the full texture as the sprite rect (verified empirically). These bitmaps
            // carry transparent padding around the visible art, so a full-canvas rect stretches that
            // empty margin into the 9-slice's middle, squashing the real artwork into a fraction of
            // the target rect. Multiple mode with a single named entry is the only way to get Unity
            // to honor a custom rect without touching the source pixels.
            importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
            importer.spritesheet = new SpriteMetaData[]
            {
                new SpriteMetaData
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path),
                    rect = contentRect,
                    border = border,
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                }
            };
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Missing sliced sprite: " + path);
        return sprite;
    }

    private readonly struct StatTextBindings
    {
        public StatTextBindings(TMP_Text level, TMP_Text vitals, TMP_Text combat, TMP_Text mobility, TMP_Text recovery)
        {
            Level = level;
            Vitals = vitals;
            Combat = combat;
            Mobility = mobility;
            Recovery = recovery;
        }

        public TMP_Text Level { get; }
        public TMP_Text Vitals { get; }
        public TMP_Text Combat { get; }
        public TMP_Text Mobility { get; }
        public TMP_Text Recovery { get; }
    }
}
