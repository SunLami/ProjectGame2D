using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerHUDApprovedBuilder
{
    public const string PrefabPath = "Assets/Resources/UI/Gameplay/HUD/PlayerHUD.prefab";
    private const string Art = "Assets/Resources/UI/Gameplay/PlayerHUDApproved/";
    private const string Gameplay = "Assets/Prefabs/UI/GameplayUIRoot.prefab";
    private const string Unified = "Assets/Resources/UI/Gameplay/UnifiedHUD/UnifiedGameplayHUD.prefab";
    private const float Scale = 340f / 2172f;
    public const float PresentationScale = .72f;
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");

    [MenuItem("Tools/ProjectGame2D/UI/Build Approved PlayerHUD")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build HUD in Edit Mode only.");
        Sprite frame = Import(Art + "player_hud_frame_v1.png");
        Sprite fill = Import(Art + "player_hud_fill_neutral_v1.png");
        // Neutral fill is intentionally cropped INSIDE its clean rectangular body, excluding AI edge noise.
        var fillImporter = (TextureImporter)AssetImporter.GetAtPath(Art + "player_hud_fill_neutral_v1.png");
        fillImporter.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
        fillImporter.spritesheet = new[] { new SpriteMetaData { name = "PlayerHUDFill", rect = new Rect(85, 400, 1600, 65), pivot = new Vector2(.5f, .5f), alignment = 0 } };
#pragma warning restore CS0618
        fillImporter.SaveAndReimport();
        fill = AssetDatabase.LoadAllAssetsAtPath(Art + "player_hud_fill_neutral_v1.png").OfType<Sprite>().Single();

        var root = new GameObject("PlayerHUD", typeof(RectTransform), typeof(PlayerHUDController), typeof(PlayerExperienceBarController));
        try
        {
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(32f / 2.4f, 32f / 2.4f);
            rect.sizeDelta = new Vector2(340f, 724f * Scale);
            // 588x196 physical pixels at 1920x1080; scale the entire composition together.
            rect.localScale = new Vector3(PresentationScale, PresentationScale, 1f);

            var experience = Meter(root.transform, "Experience", fill, 693, 175, 1357, 57, new Color(1f, .64f, .055f),
                new[] { new Vector2(693,175), new Vector2(2037,175), new Vector2(2050,203), new Vector2(2031,232), new Vector2(735,232) });
            var stamina = Meter(root.transform, "Stamina", fill, 782, 287, 1322, 82, new Color(.12f, .85f, .28f));
            var mana = Meter(root.transform, "Mana", fill, 727, 416, 1377, 82, new Color(.08f, .55f, 1f));
            var health = Meter(root.transform, "Health", fill, 678, 541, 1427, 79, new Color(.95f, .08f, .10f));
            var frameImage = Image(root.transform, "Frame", frame, Color.white, 0, 0, 2172, 724);
            frameImage.preserveAspect = true;

            TMP_Text level = Text(root.transform, "LevelText", "LV. 1", 10, 730, 65, 250, 85);
            TMP_Text xp = Text(root.transform, "ExperienceText", "EXP  0 / 100", 9, 1060, 65, 980, 85);
            xp.alignment = TextAlignmentOptions.MidlineRight;

            // Artwork coordinates are top-left pixels of the retained 2172x724 source.
            Slot(root.transform, 0, 383, 190, 292);
            Slot(root.transform, 1, 181, 366, 470);
            Slot(root.transform, 2, 576, 366, 470);
            Slot(root.transform, 3, 383, 555, 638);

            var hud = new SerializedObject(root.GetComponent<PlayerHUDController>());
            Set(hud, "_healthFill", health); Set(hud, "_staminaFill", stamina);
            Set(hud, "_manaFill", mana); Set(hud, "_avatarImage", null); Set(hud, "_levelText", level);
            hud.ApplyModifiedPropertiesWithoutUndo();
            var exp = new SerializedObject(root.GetComponent<PlayerExperienceBarController>());
            Set(exp, "_experienceFill", experience); Set(exp, "_experienceText", xp);
            exp.FindProperty("_showLabel").boolValue = true;
            exp.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        RemoveBottomHud();
        ClearNestedHudOverrides(Gameplay);
        AssetDatabase.SaveAssets();
        Debug.Log("Approved PlayerHUD saved; four slots, fill-only vitals, EXP, BottomHUD removed.");
    }

    private static void RemoveBottomHud()
    {
        var root = PrefabUtility.LoadPrefabContents(Unified);
        try
        {
            var bottom = root.transform.Find("BottomHUD");
            if (bottom != null) UnityEngine.Object.DestroyImmediate(bottom.gameObject);
            var controller = new SerializedObject(root.GetComponent<UnifiedGameplayHudController>());
            foreach (string field in new[] { "_healthFill", "_staminaFill", "_experienceFill", "_experienceText", "_levelText" }) Set(controller, field, null);
            controller.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, Unified);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static bool IsHudGeometry(PropertyModification mod)
    {
        if (!(mod.target is RectTransform) && !(mod.target is Image)) return false;
        var component = mod.target as Component;
        for (Transform t = component.transform; t != null; t = t.parent)
            if (t.name == "PlayerHUD") return true;
        return false;
    }

    private static void ClearNestedHudOverrides(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud = root.transform.Find("PlayerHUD");
            if (hud == null) throw new InvalidOperationException("PlayerHUD nested instance missing.");
            var mods = PrefabUtility.GetPropertyModifications(hud.gameObject);
            if (mods != null) PrefabUtility.SetPropertyModifications(hud.gameObject, mods.Where(m => !IsHudGeometry(m)).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Explicit command: scoped geometry only, never auto-save an unrelated dirty scene.
    public static int SyncLoadedScenes()
    {
        int removed = 0;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (scene.isDirty) throw new InvalidOperationException("Scene has unsaved changes: " + scene.path);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var hud in root.GetComponentsInChildren<PlayerHUDController>(true))
            {
                var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(hud.gameObject);
                if (outer == null) continue;
                var mods = PrefabUtility.GetPropertyModifications(outer);
                if (mods == null) continue;
                var kept = mods.Where(m => !IsHudGeometry(m)).ToArray();
                removed += mods.Length - kept.Length;
                if (kept.Length != mods.Length)
                {
                    PrefabUtility.SetPropertyModifications(outer, kept);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
            if (scene.isDirty) EditorSceneManager.SaveScene(scene);
        }
        return removed;
    }

    private static void Slot(Transform parent, int index, float cx, float cy, float keyY)
    {
        var hit = Image(parent, "QuickSlot" + (index + 1), null, new Color(1, 1, 1, .001f), cx - 82, cy - 82, 164, 164);
        hit.raycastTarget = true;
        var selection = Image(hit.transform, "Selection", null, new Color(1f, .72f, .12f, .23f), 0, 0, 105, 105);
        Center(selection.rectTransform, 0, 0, 105 * Scale, 105 * Scale);
        selection.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
        selection.enabled = index == 0;
        var icon = Image(hit.transform, "ItemIcon", null, Color.white, 0, 0, 110, 110);
        Center(icon.rectTransform, 0, 0, 110 * Scale, 110 * Scale);
        icon.preserveAspect = true; icon.enabled = false;
        var quantity = Text(hit.transform, "Quantity", "", 7.5f, 0, 0, 70, 80);
        Center(quantity.rectTransform, 40 * Scale, -37 * Scale, 70 * Scale, 80 * Scale);
        quantity.alignment = TextAlignmentOptions.BottomRight;
        var key = Text(parent, "QuickSlot" + (index + 1) + "Key", (index + 1).ToString(), 6.5f, cx - 32, keyY - 36, 64, 72);
        key.alignment = TextAlignmentOptions.Center; key.color = new Color(.16f, .08f, .025f);
        hit.gameObject.AddComponent<QuickBarSlotUI>().Configure(index, icon, quantity, selection);
    }

    private static Image Meter(Transform parent, string name, Sprite sprite, float x, float y, float w, float h, Color color, Vector2[] points = null)
    {
        var opening = new GameObject(name + "Opening", typeof(RectTransform), typeof(CanvasRenderer), typeof(PlayerHUDMeterMask), typeof(Mask));
        opening.transform.SetParent(parent, false);
        Place(opening.GetComponent<RectTransform>(), x, y, w, h);
        if (points == null) points = new[] { new Vector2(x + 19, y), new Vector2(x + w - 28, y),
            new Vector2(x + w, y + h / 2), new Vector2(x + w - 28, y + h),
            new Vector2(x + 19, y + h), new Vector2(x, y + h / 2) };
        opening.GetComponent<PlayerHUDMeterMask>().Configure(points.Select(p => new Vector2((p.x - x) / w, 1f - (p.y - y) / h)).ToArray());
        opening.GetComponent<Mask>().showMaskGraphic = false;
        var background = Image(opening.transform, name + "Track", null, new Color(.055f, .032f, .018f, 1f), 0, 0, w, h);
        var fill = Image(background.transform, name + "Fill", sprite, color, 0, 0, w, h);
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left; fill.fillAmount = 1f;
        return fill;
    }

    private static Image Image(Transform parent, string name, Sprite sprite, Color color, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), x, y, w, h);
        var image = go.GetComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false; return image;
    }

    private static TMP_Text Text(Transform parent, string name, string value, float size, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>(); Place(text.rectTransform, x, y, w, h);
        text.font = Font; text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = new Color(1, .94f, .79f); text.raycastTarget = false;
        text.enableAutoSizing = true; text.fontSizeMin = size - 1; text.fontSizeMax = size;
        text.overflowMode = TextOverflowModes.Ellipsis; return text;
    }

    private static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x * Scale, -y * Scale); rect.sizeDelta = new Vector2(w * Scale, h * Scale);
    }
    private static void Center(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
    }
    private static void Set(SerializedObject target, string field, UnityEngine.Object value) => target.FindProperty(field).objectReferenceValue = value;
    private static Sprite Import(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true; importer.maxTextureSize = 4096; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
