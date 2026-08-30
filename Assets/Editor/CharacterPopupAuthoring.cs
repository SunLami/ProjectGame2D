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
        Sprite equipmentFrame = LoadSprite("Assets/Resources/UI/Inventory/LightFantasy/equipment_panel_hd.png");
        Sprite board = LoadSprite("Assets/Resources/UI/Inventory/LightFantasy/inventory_board_thin_hd.png");
        Sprite slot = LoadSprite("Assets/Resources/UI/Inventory/LightFantasy/inventory_slot_thin_hd.png");
        Sprite close = LoadSprite("Assets/Resources/UI/Inventory/LightFantasy/inventory_close_thin_hd.png");

        RectTransform window = CreateRect("Window", popup, Vector2.zero, new Vector2(760f, 410f));
        List<Image> equipmentIcons = BuildEquipmentPanel(window, equipmentFrame, slot);
        StatTextBindings stats = BuildStatsPanel(root, window, board, close);

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

    private static List<Image> BuildEquipmentPanel(RectTransform window, Sprite frameSprite, Sprite slotSprite)
    {
        RectTransform panel = CreateRect("EquipmentPanel", window, new Vector2(-222f, 0f), new Vector2(280f, 400f));
        Image frame = panel.gameObject.AddComponent<Image>();
        frame.sprite = frameSprite;
        frame.raycastTarget = false;

        RectTransform parchment = CreateRect("ParchmentInterior", panel, new Vector2(0f, -8f), new Vector2(224f, 316f));
        Image parchmentImage = parchment.gameObject.AddComponent<Image>();
        parchmentImage.color = new Color(0.70f, 0.49f, 0.31f, 0.94f);
        parchmentImage.raycastTarget = false;
        parchment.SetAsFirstSibling();

        TMP_Text title = CreateText("Title", panel, new Vector2(0f, 130f), new Vector2(190f, 30f), "EQUIPMENT", 18f, TextAlignmentOptions.Center);
        title.color = new Color(0.08f, 0.20f, 0.48f, 1f);
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

            TMP_Text label = CreateText("Label", slot, new Vector2(0f, -37f), new Vector2(74f, 15f), name.ToUpperInvariant(), 8f, TextAlignmentOptions.Center);
            label.color = new Color(0.25f, 0.13f, 0.06f, 0.88f);
        }
        return icons;
    }

    private static StatTextBindings BuildStatsPanel(GameObject root, RectTransform window, Sprite boardSprite, Sprite closeSprite)
    {
        RectTransform panel = CreateRect("CharacterStatsPanel", window, new Vector2(145f, 0f), new Vector2(450f, 400f));
        Image board = panel.gameObject.AddComponent<Image>();
        board.sprite = boardSprite;
        board.raycastTarget = false;

        TMP_Text title = CreateText("Title", panel, new Vector2(0f, 130f), new Vector2(310f, 34f), "CHARACTER STATS", 20f, TextAlignmentOptions.Center);
        title.color = new Color(0.08f, 0.20f, 0.48f, 1f);
        title.fontStyle = FontStyles.Bold;

        RectTransform badge = CreateRect("LevelBadge", panel, new Vector2(0f, 98f), new Vector2(330f, 28f));
        Image badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.color = new Color(0.10f, 0.24f, 0.52f, 0.92f);
        badgeImage.raycastTarget = false;
        TMP_Text level = CreateText("LevelValue", badge, Vector2.zero, new Vector2(310f, 24f), "LV. 1", 15f, TextAlignmentOptions.Center);
        level.color = new Color(1f, 0.88f, 0.47f, 1f);
        level.fontStyle = FontStyles.Bold;

        TMP_Text vitals = CreateSection(panel, "Vitals", 55f, 66f, "Health\nStamina", "100 / 100\n100 / 100");
        TMP_Text combat = CreateSection(panel, "Combat", -22f, 82f, "Attack\nDefense\nCritical Chance\nCritical Damage", "10.0\n2.0\n5.0%\nx1.50");
        TMP_Text mobility = CreateSection(panel, "Mobility", -91f, 62f, "Move Speed\nSprint Multiplier\nDodge Chance", "2.0\nx2.00\n0.0%");
        TMP_Text recovery = CreateSection(panel, "Recovery", -139f, 38f, "Damage Reduction\nHealth Regeneration", "0.0%\n0.0 /s");

        RectTransform closeRect = CreateRect("CloseButton", panel, new Vector2(194f, 166f), new Vector2(34f, 34f));
        Image closeImage = closeRect.gameObject.AddComponent<Image>();
        closeImage.sprite = closeSprite;
        Button closeButton = closeRect.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        UnityEventTools.AddPersistentListener(closeButton.onClick, root.GetComponent<UnifiedGameplayHudController>().ClosePopup);

        return new StatTextBindings(level, vitals, combat, mobility, recovery);
    }

    private static TMP_Text CreateSection(RectTransform parent, string name, float y, float height, string labels, string values)
    {
        RectTransform section = CreateRect(name, parent, new Vector2(0f, y), new Vector2(350f, height));
        Image background = section.gameObject.AddComponent<Image>();
        background.color = new Color(0.32f, 0.19f, 0.105f, 0.16f);
        background.raycastTarget = false;

        TMP_Text header = CreateText("Header", section, new Vector2(0f, height * 0.5f - 13f), new Vector2(324f, 20f), name.ToUpperInvariant(), 11f, TextAlignmentOptions.Left);
        header.color = new Color(0.08f, 0.25f, 0.55f, 1f);
        header.fontStyle = FontStyles.Bold;

        int rowCount = labels.Split('\n').Length;
        float bodyHeight = rowCount * 18f;
        float bodyY = height * 0.5f - 24f - bodyHeight * 0.5f;
        TMP_Text labelText = CreateText("Labels", section, new Vector2(-72f, bodyY), new Vector2(170f, bodyHeight), labels, 10f, TextAlignmentOptions.TopLeft);
        labelText.lineSpacing = 12f;
        TMP_Text valueText = CreateText("Values", section, new Vector2(104f, bodyY), new Vector2(120f, bodyHeight), values, 10f, TextAlignmentOptions.TopRight);
        valueText.lineSpacing = 12f;
        valueText.color = new Color(0.08f, 0.18f, 0.42f, 1f);
        valueText.fontStyle = FontStyles.Bold;
        return valueText;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
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
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException("Missing sprite: " + path);
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
