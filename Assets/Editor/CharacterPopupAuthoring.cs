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
    private const string DossierBoardPath = "Assets/Resources/UI/Character/DarkInventoryStyle/character_dossier_board_v2.png";
    private const string PortraitStagePath = "Assets/Resources/UI/Character/DarkInventoryStyle/character_portrait_stage_v3.png";
    private const string FontPath = "Assets/Fonts/DigitalDisco SDF v3.asset";
    private static readonly Color Gold = new Color32(231, 187, 104, 255);
    private static readonly Color Ivory = new Color32(242, 228, 197, 255);
    private static readonly Color Muted = new Color32(192, 179, 154, 255);
    private static readonly Color Surface = new Color32(39, 31, 27, 255);
    private static readonly Color Row = new Color32(56, 44, 35, 255);
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
        Sprite board = AssetDatabase.LoadAssetAtPath<Sprite>(DossierBoardPath);
        if (board == null && AssetImporter.GetAtPath(DossierBoardPath) != null)
            board = LoadSprite(DossierBoardPath);
        Sprite slot = LoadSprite(SlotPath);
        Sprite close = LoadSprite(MapUIAuthoring.CloseButtonPath);

        RectTransform window = CreateRect("Window", popup, Vector2.zero, new Vector2(720f, 405f));
        window.gameObject.AddComponent<PopupViewportFit>();
        Image windowBoard = window.gameObject.AddComponent<Image>();
        windowBoard.sprite = board != null ? board : LoadSprite(UnifiedBoardPath);
        windowBoard.raycastTarget = false;

        Text("Header", window, new Vector2(-312f, 158f), new Vector2(320f, 28f), "CHARACTER STATS", 21f, Gold);
        Block("HeaderDivider", window, new Vector2(0f, 136f), new Vector2(638f, 1f), new Color32(150, 107, 57, 255));
        var badge = Block("LevelBadge", window, new Vector2(228f, 158f), new Vector2(82f, 28f), Surface);
        TMP_Text level = CreateText("LevelValue", badge, Vector2.zero, new Vector2(76f, 22f),
            "LV. 1", 13f, TextAlignmentOptions.Center);
        level.color = Gold;

        List<Image> equipmentIcons = BuildEquipmentPanel(window, slot);
        StatTextBindings stats = BuildStatsPanel(window);
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
        popupSo.FindProperty("_levelValue").objectReferenceValue = level;
        BindVital(popupSo, "_health", stats.Health);
        BindVital(popupSo, "_mana", stats.Mana);
        BindVital(popupSo, "_stamina", stats.Stamina);
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

    private static void BindVital(SerializedObject popup, string propertyName, VitalBindings binding)
    {
        SerializedProperty property = popup.FindProperty(propertyName);
        property.FindPropertyRelative("value").objectReferenceValue = binding.Value;
        property.FindPropertyRelative("fill").objectReferenceValue = binding.Fill;
    }

    private static List<Image> BuildEquipmentPanel(RectTransform window, Sprite slotSprite)
    {
        RectTransform panel = CreateRect("EquipmentPanel", window, new Vector2(-162f, -22f),
            new Vector2(304f, 294f));
        Text("Title", panel, new Vector2(-150f, 132f), new Vector2(270f, 18f), "EQUIPMENT", 13f, Gold);
        Block("Divider", panel, new Vector2(0f, 118f), new Vector2(300f, 1f), Gold * 0.55f);

        RectTransform stage = Block("PortraitStage", panel, new Vector2(0f, -6f), new Vector2(158f, 219f), Surface);
        if (AssetImporter.GetAtPath(PortraitStagePath) != null)
        {
            Image stageImage = stage.GetComponent<Image>();
            stageImage.sprite = LoadSprite(PortraitStagePath);
            stageImage.preserveAspect = false;
            stageImage.color = Color.white;
        }
        RectTransform portrait = CreateRect("LiveCharacter", stage, new Vector2(0f, -44f), new Vector2(128f, 192f));
        portrait.gameObject.AddComponent<RawImage>().raycastTarget = false;
        portrait.gameObject.AddComponent<InventoryCharacterPreviewUI>();

        string[] names = { "Head", "Weapon", "Body", "Shield", "Necklace", "Ring", "Feet" };
        Vector2[] positions =
        {
            new(-116f, 77f), new(-116f, -1f), new(-116f, -79f),
            new(116f, 77f), new(116f, -1f), new(116f, -79f), new(0f, -114f)
        };
        var icons = new List<Image>(names.Length);
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            RectTransform slot = CreateRect(name + "Slot", panel, positions[i], new Vector2(43f, 43f));
            Image slotImage = slot.gameObject.AddComponent<Image>();
            slotImage.sprite = slotSprite;
            slotImage.raycastTarget = false;

            Image icon = CreateRect("Icon", slot, Vector2.zero, new Vector2(35f, 35f)).gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            icons.Add(icon);

            TMP_Text label = CreateText("Label", slot, new Vector2(0f, -28f), new Vector2(66f, 14f),
                name, 9f, TextAlignmentOptions.Center);
            label.color = Ivory;
        }
        return icons;
    }

    private static StatTextBindings BuildStatsPanel(RectTransform window)
    {
        var panel = CreateRect("CharacterStatsPanel", window, new Vector2(160f, -22f), new Vector2(320f, 294f));
        Text("Header", panel, new Vector2(-159f, 132f), new Vector2(200f, 18f), "VITALS", 13f, Gold);
        Block("Divider", panel, new Vector2(0f, 118f), new Vector2(318f, 1f), Gold * 0.55f);
        VitalBindings health = BuildVital(panel, "Health", 98f, new Color32(207, 83, 71, 255));
        VitalBindings mana = BuildVital(panel, "Mana", 72f, new Color32(74, 153, 221, 255));
        VitalBindings stamina = BuildVital(panel, "Stamina", 46f, new Color32(115, 173, 93, 255));

        TMP_Text combat = CreateSection(panel, "Combat", new Vector2(0f, -29f), 320f, 99f,
            "Attack\nDefense\nCritical Chance\nCritical Damage", "10.0\n2.0\n5.0%\nx1.50");
        TMP_Text mobility = CreateSection(panel, "Mobility", new Vector2(-82f, -113f), 156f, 67f,
            "Move Speed\nSprint Multiplier\nDodge Chance", "4.0\nx1.75\n0.0%");
        TMP_Text recovery = CreateSection(panel, "Recovery", new Vector2(82f, -113f), 156f, 67f,
            "Damage Reduction\nHealth Regen", "0.0%\n0.0 /s");
        return new StatTextBindings(health, mana, stamina, combat, mobility, recovery);
    }

    private static VitalBindings BuildVital(RectTransform parent, string name, float y, Color accent)
    {
        RectTransform row = CreateRect(name, parent, new Vector2(0f, y), new Vector2(320f, 24f));
        Block("Accent", row, new Vector2(-155f, 1f), new Vector2(3f, 16f), accent);
        Text("Label", row, new Vector2(-147f, 5f), new Vector2(180f, 16f), name, 10f, Ivory);
        TMP_Text value = CreateText("Value", row, new Vector2(94f, 5f), new Vector2(110f, 16f),
            "100 / 100", 11f, TextAlignmentOptions.MidlineRight);
        value.color = Ivory;
        RectTransform border = Block("TrackBorder", row, new Vector2(1f, -7f), new Vector2(300f, 7f), Row);
        RectTransform track = Block("Track", border, Vector2.zero, new Vector2(298f, 5f), Surface);
        RectTransform fill = Block("Fill", track, Vector2.zero, Vector2.zero, accent);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        return new VitalBindings(value, fill);
    }

    private static void BuildCloseButton(GameObject root, RectTransform window, Sprite closeSprite)
    {
        RectTransform closeRect = CreateRect("CloseButton", window, Vector2.one, Vector2.one, new Vector2(-29f, -22f), new Vector2(30f, 30f));
        Image closeImage = closeRect.gameObject.AddComponent<Image>();
        closeImage.sprite = closeSprite;
        closeImage.preserveAspect = true;
        Button closeButton = closeRect.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        UnityEventTools.AddPersistentListener(closeButton.onClick, root.GetComponent<UnifiedGameplayHudController>().ClosePopup);
    }

    private static TMP_Text CreateSection(RectTransform parent, string name, Vector2 position,
        float width, float height, string labels, string values)
    {
        RectTransform section = Block(name, parent, position, new Vector2(width, height), Surface);
        Block("TopRule", section, new Vector2(0f, height * 0.5f), new Vector2(width, 1f), Gold * 0.65f);
        float left = -width * 0.5f + 12f;
        float top = height * 0.5f - 13f;
        Text("Header", section, new Vector2(left, top), new Vector2(width - 24f, 18f),
            name.ToUpperInvariant(), 12f, Gold);
        Block("Divider", section, new Vector2(0f, top - 11f), new Vector2(width - 24f, 1f), Row);
        float bodyHeight = height - 28f;
        bool wide = width > 200f;
        float fontSize = wide ? 10.5f : 8f;
        var label = CreateText("Labels", section, new Vector2(-23f, -13f), new Vector2(width - 68f, bodyHeight),
            labels, fontSize, TextAlignmentOptions.TopLeft);
        label.color = Muted;
        var value = CreateText("Values", section, new Vector2(width * 0.5f - 36f, -12f),
            new Vector2(48f, bodyHeight), values, fontSize, TextAlignmentOptions.TopRight);
        value.color = Ivory;
        float spacing = wide ? 17f : 3f;
        label.lineSpacing = value.lineSpacing = spacing;
        return value;
    }

    private static RectTransform Block(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text Text(string name, Transform parent, Vector2 left, Vector2 size, string value,
        float fontSize, Color color)
    {
        TMP_Text label = CreateText(name, parent, left + Vector2.right * (size.x * 0.5f), size,
            value, fontSize, TextAlignmentOptions.MidlineLeft);
        label.color = color;
        return label;
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
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.19f, 0.105f, 0.055f, 1f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static Sprite LoadSprite(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer
            && (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled || importer.filterMode != FilterMode.Point))
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

    private readonly struct VitalBindings
    {
        public VitalBindings(TMP_Text value, RectTransform fill)
        {
            Value = value;
            Fill = fill;
        }

        public TMP_Text Value { get; }
        public RectTransform Fill { get; }
    }

    private readonly struct StatTextBindings
    {
        public StatTextBindings(VitalBindings health, VitalBindings mana, VitalBindings stamina,
            TMP_Text combat, TMP_Text mobility, TMP_Text recovery)
        {
            Health = health;
            Mana = mana;
            Stamina = stamina;
            Combat = combat;
            Mobility = mobility;
            Recovery = recovery;
        }

        public VitalBindings Health { get; }
        public VitalBindings Mana { get; }
        public VitalBindings Stamina { get; }
        public TMP_Text Combat { get; }
        public TMP_Text Mobility { get; }
        public TMP_Text Recovery { get; }
    }
}
