using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Bottom-right counterpart to PlayerHUDApprovedBuilder's bottom-left HUD. Builds into
// GameplayUIRoot.prefab itself (sibling of PlayerHUD), idempotent: destroys a previous "SkillHUD"
// child before rebuilding, following the measured diamond centers of skill_hud_frame_v1.png (D-118
// handoff, Assets/Documentation/DevelopmentPlan/Handoffs/CodexToClaude.md, SKILLHUD_SHIELD_ASSETS_ART_READY).
public static class SkillHUDBuilder
{
    private const string FramePath = "Assets/Resources/UI/Gameplay/SkillHUD/skill_hud_frame_v1.png";
    private const string ShieldIconPath = "Assets/Resources/UI/Skills/Icons/V1/shield_block.png";
    private const string FontPath = "Assets/Fonts/DigitalDisco SDF v3.asset";

    // PlayerHUD lives inside this prefab (GameplayUIRoot.prefab root, sibling of PlayerHUD), not as a
    // loose child of either scene's own "_UI" -- DemoScene keeps its own dormant "_UI/UICanvas" copy
    // for standalone edit-mode iteration, but that whole subtree is inactive once Bootstrap's real
    // GameplayUIRoot instance takes over during actual Play (verified: PlayerHUD only renders from
    // "_UI/GameplayUIRoot/PlayerHUD" in Bootstrap, the DemoScene copy is activeInHierarchy=false).
    // Building into the scene instance instead of this prefab would be invisible during real play.
    private const string GameplayUIRootPrefabPath = "Assets/Prefabs/UI/GameplayUIRoot.prefab";

    // 1600x480 source canvas -> 400x120 logical units, same presentation scale (0.72) as PlayerHUD.
    private const float Scale = 400f / 1600f;
    private const float PresentationScale = 0.72f;

    // Diamond centers measured from skill_hud_frame_v1.png's alpha channel (Python/PIL column-run +
    // pinch-point segmentation of the 4-chain, see handoff entry) -- not guessed. (centerX, centerY,
    // bottomTipY), all in source pixel space, Y measured from the top like the rest of this file.
    private static readonly (float cx, float cy, float bottomY)[] LoadoutDiamonds =
    {
        (168.0f, 253.5f, 449f), // Q
        (457.5f, 253.5f, 449f), // E
        (732.0f, 253.5f, 449f), // R
        (1022.0f, 253.0f, 448f) // T
    };
    private static readonly string[] LoadoutKeys = { "Q", "E", "R", "T" };
    private const float LoadoutIconSize = 130f;

    private static readonly Vector2 ShieldCenter = new(1388.5f, 234.5f);
    private const float ShieldIconSize = 170f;

    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

    [MenuItem("Tools/ProjectGame2D/UI/Build Skill HUD")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build Skill HUD in Edit Mode only.");

        Sprite frameSprite = Import(FramePath);
        Sprite shieldIconSprite = Import(ShieldIconPath);

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(GameplayUIRootPrefabPath);
        try
        {
            Transform existing = prefabRoot.transform.Find("SkillHUD");
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var root = new GameObject("SkillHUD", typeof(RectTransform), typeof(SkillHUDController));
            root.transform.SetParent(prefabRoot.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(1f, 0f);
            rootRect.anchoredPosition = new Vector2(-32f / 2.4f, 32f / 2.4f);
            rootRect.sizeDelta = new Vector2(1600f * Scale, 480f * Scale);
            rootRect.localScale = new Vector3(PresentationScale, PresentationScale, 1f);

            var frame = Image(root.transform, "Frame", frameSprite, Color.white, 0, 0, 1600, 480);
            frame.preserveAspect = true;

            var loadoutSlots = new SkillHUDSlotUI[LoadoutDiamonds.Length];
            for (int i = 0; i < LoadoutDiamonds.Length; i++)
            {
                (float cx, float cy, float bottomY) = LoadoutDiamonds[i];
                loadoutSlots[i] = BuildLoadoutSlot(root.transform, i, cx, cy);

                TMP_Text key = Text(root.transform, "SkillSlot" + (i + 1) + "Key", LoadoutKeys[i],
                    18f, cx - 45f, bottomY - 34f, 90, 36);
                key.alignment = TextAlignmentOptions.Center;
                key.color = new Color(1f, .94f, .79f);
            }

            SkillHUDSlotUI shieldSlot = BuildShieldSlot(root.transform, shieldIconSprite);

            // Only _catalog is baked in -- it is a ScriptableObject asset, safe to serialize into a
            // prefab. _castBridge/_player are scene objects (SkillTestHarness/Player live in
            // DemoScene, not in this UI prefab) and are resolved at runtime by SkillHUDController
            // itself (FindAnyObjectByType fallback) -- baking them here would silently serialize as
            // null, the same class of bug diagnosed earlier this session for InventorySeeder wiring.
            var controller = new SerializedObject(root.GetComponent<SkillHUDController>());
            string[] catalogGuids = AssetDatabase.FindAssets("t:SkillCatalog");
            if (catalogGuids.Length > 0)
                controller.FindProperty("_catalog").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<SkillCatalog>(AssetDatabase.GUIDToAssetPath(catalogGuids[0]));
            SerializedProperty slotsProp = controller.FindProperty("_loadoutSlots");
            slotsProp.arraySize = loadoutSlots.Length;
            for (int i = 0; i < loadoutSlots.Length; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = loadoutSlots[i];
            controller.FindProperty("_shieldSlot").objectReferenceValue = shieldSlot;
            controller.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, GameplayUIRootPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("SkillHUD built into GameplayUIRoot.prefab: 4 Q/E/R/T loadout slots + 1 Shield slot, bottom-right.");
    }

    private static SkillHUDSlotUI BuildLoadoutSlot(Transform parent, int index, float cx, float cy)
    {
        var container = new GameObject("SkillSlot" + (index + 1), typeof(RectTransform), typeof(SkillHUDSlotUI));
        container.transform.SetParent(parent, false);
        Place(container.GetComponent<RectTransform>(), cx - LoadoutIconSize / 2f, cy - LoadoutIconSize / 2f, LoadoutIconSize, LoadoutIconSize);

        var icon = Stretch(Image(container.transform, "Icon", null, Color.white, 0, 0, LoadoutIconSize, LoadoutIconSize));
        icon.preserveAspect = true;
        icon.enabled = false; // no sprite yet -- an enabled Image with a null sprite draws as a solid block

        var cooldown = Stretch(Image(container.transform, "Cooldown", null, new Color(0f, 0f, 0f, 0.72f), 0, 0, LoadoutIconSize, LoadoutIconSize));
        cooldown.type = UnityEngine.UI.Image.Type.Filled;
        cooldown.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        cooldown.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
        cooldown.fillClockwise = true;
        cooldown.fillAmount = 0f;

        var slot = container.GetComponent<SkillHUDSlotUI>();
        slot.Configure(icon, cooldown, null);
        return slot;
    }

    private static SkillHUDSlotUI BuildShieldSlot(Transform parent, Sprite shieldIcon)
    {
        var container = new GameObject("ShieldSlot", typeof(RectTransform), typeof(SkillHUDSlotUI));
        container.transform.SetParent(parent, false);
        Place(container.GetComponent<RectTransform>(), ShieldCenter.x - ShieldIconSize / 2f, ShieldCenter.y - ShieldIconSize / 2f, ShieldIconSize, ShieldIconSize);

        var icon = Stretch(Image(container.transform, "Icon", shieldIcon, Color.white, 0, 0, ShieldIconSize, ShieldIconSize));
        icon.preserveAspect = true;

        // Additive highlight while Player.IsBlocking -- sits above the icon, starts disabled.
        var highlight = Stretch(Image(container.transform, "ActiveHighlight", null, new Color(0.65f, 0.95f, 1f, 0.45f), 0, 0, ShieldIconSize, ShieldIconSize));
        highlight.enabled = false;

        var slot = container.GetComponent<SkillHUDSlotUI>();
        slot.Configure(icon, null, highlight);
        return slot;
    }

    private static Image Image(Transform parent, string name, Sprite sprite, Color color, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), x, y, w, h);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image Stretch(Image image)
    {
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        return image;
    }

    private static TMP_Text Text(Transform parent, string name, string value, float size, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        Place(text.rectTransform, x, y, w, h);
        text.font = Font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = size - 2f;
        text.fontSizeMax = size;
        return text;
    }

    private static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x * Scale, -y * Scale);
        rect.sizeDelta = new Vector2(w * Scale, h * Scale);
    }

    private static Sprite Import(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
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
