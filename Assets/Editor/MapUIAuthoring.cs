using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Builds the functional Minimap (always-on HUD corner) and FullMap (GameplayMenuPage.Map, opened
// with M) UI structure around the static map snapshot from MapSnapshotBaker: RawImage/Image
// viewports wired to MinimapController/FullMapController, plus zone/date text, a player marker,
// and a Close button. Run Tools/ProjectGame2D/UI/Bake Map Snapshot before this. Uses Codex's Dark
// Inventory Style minimap frame/mask/close-button/marker art (Handoffs/ClaudeToCodex.md
// READY_FOR_CODEX_MAP_ART); MinimapController.Awake() falls back to a procedural circle only if a
// sprite is ever missing.
public static class MapUIAuthoring
{
    private const string PrefabPath = "Assets/Resources/UI/Gameplay/UnifiedHUD/UnifiedGameplayHUD.prefab";
    private const string SnapshotPath = "Assets/Resources/UI/Map/map_snapshot.png";
    private const string ArtRoot = "Assets/Resources/UI/Map/DarkInventoryStyle/";
    private const string FramePath = ArtRoot + "minimap_frame_v1.png";
    private const string MaskPath = ArtRoot + "minimap_mask_v1.png";
    private const string CloseButtonPath = ArtRoot + "map_close_button_v1.png";
    private const string MarkerPath = ArtRoot + "player_marker_v1.png";
    private const string TagPath = ArtRoot + "minimap_tag_v1.png";

    [MenuItem("Tools/ProjectGame2D/UI/Rebuild Map UI (Minimap + FullMap)")]
    public static void Rebuild()
    {
        Sprite snapshot = AssetDatabase.LoadAssetAtPath<Sprite>(SnapshotPath);
        if (snapshot == null)
            throw new InvalidOperationException(
                $"Map snapshot not found at {SnapshotPath}. Run Tools/ProjectGame2D/UI/Bake Map Snapshot first.");

        Sprite frameSprite = LoadSprite(FramePath);
        Sprite maskSprite = LoadSprite(MaskPath);
        Sprite closeSprite = LoadSprite(CloseButtonPath);
        Sprite markerSprite = LoadSprite(MarkerPath);
        Sprite tagSprite = LoadSprite(TagPath);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BuildMinimap(root, snapshot, frameSprite, maskSprite, markerSprite, tagSprite);
            BuildFullMap(root, snapshot, closeSprite, markerSprite);

            UnifiedGameplayHudController hud = root.GetComponent<UnifiedGameplayHudController>();
            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_minimap").objectReferenceValue = root.transform.Find("Minimap").gameObject;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Map UI (Minimap + FullMap) rebuilt.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildMinimap(GameObject root, Sprite snapshot, Sprite frameSprite, Sprite maskSprite,
        Sprite markerSprite, Sprite tagSprite)
    {
        Transform existing = root.transform.Find("Minimap");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        // Pushed down from the corner (extra -20 on Y) to leave room above the ring for ZoneText --
        // the real frame art's ring is thick enough that text inside the old 96x96 box overlapped it.
        RectTransform minimap = CreatePointRect("Minimap", root.transform, new Vector2(1f, 1f),
            new Vector2(-14f, -34f), new Vector2(96f, 96f));

        // minimap_frame_v1's ring and minimap_mask_v1's solid circle share the same 384x384 canvas
        // space, with the mask sized to exactly fill the frame's inner hole -- so Frame and MapView
        // must occupy the identical rect (no inset) for the two to line up pixel-for-pixel.
        Image frame = minimap.gameObject.AddComponent<Image>();
        frame.sprite = frameSprite;
        frame.color = Color.white;
        frame.raycastTarget = false;

        RectTransform view = CreateStretchRect("MapView", minimap, Vector2.zero, Vector2.zero);
        Image maskShape = view.gameObject.AddComponent<Image>();
        maskShape.sprite = maskSprite;
        maskShape.color = Color.white;
        maskShape.raycastTarget = false;
        Mask mask = view.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateStretchRect("Content", view, Vector2.zero, Vector2.zero);
        RawImage viewImage = content.gameObject.AddComponent<RawImage>();
        viewImage.texture = snapshot.texture;
        viewImage.uvRect = new Rect(0f, 0f, 0.22f, 0.22f);
        viewImage.raycastTarget = false;

        RectTransform marker = CreatePlayerMarker(content, 10f, markerSprite);

        // Sit fully OUTSIDE the ring (above/below the 96x96 circle, in the margin freed by the
        // Minimap offset above) with a small dark plate behind them -- the real ring art is thick
        // enough that text inside the circle's own bounding box overlapped it, and text directly
        // over the game world with no backing was hard to read against bright/busy terrain.
        RectTransform zonePlate = CreatePointRect("ZonePlate", minimap, new Vector2(0.5f, 1f),
            new Vector2(0f, 16f), new Vector2(88f, 14f));
        Image zonePlateImage = zonePlate.gameObject.AddComponent<Image>();
        zonePlateImage.sprite = tagSprite;
        zonePlateImage.type = Image.Type.Sliced;
        zonePlateImage.color = Color.white;
        zonePlateImage.preserveAspect = false;
        zonePlateImage.raycastTarget = false;
        TMP_Text zoneText = CreatePointText("ZoneText", zonePlate, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(76f, 14f), "Heart Village", 6.5f, TextAlignmentOptions.Center);
        zoneText.color = new Color(1f, 0.85f, 0.45f, 1f);
        zoneText.fontStyle = FontStyles.Bold;
        AddReadabilityShadow(zoneText);

        RectTransform datePlate = CreatePointRect("DatePlate", minimap, new Vector2(0.5f, 0f),
            new Vector2(0f, -15f), new Vector2(60f, 14f));
        Image datePlateImage = datePlate.gameObject.AddComponent<Image>();
        datePlateImage.sprite = tagSprite;
        datePlateImage.type = Image.Type.Sliced;
        datePlateImage.color = Color.white;
        datePlateImage.preserveAspect = false;
        datePlateImage.raycastTarget = false;
        TMP_Text dateText = CreatePointText("DateText", datePlate, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(50f, 12f), "1 Jan", 6.5f, TextAlignmentOptions.Center);
        dateText.color = new Color(0.85f, 0.90f, 1f, 1f);
        dateText.fontStyle = FontStyles.Bold;
        AddReadabilityShadow(dateText);

        MinimapController controller = minimap.gameObject.AddComponent<MinimapController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("_mapImage").objectReferenceValue = viewImage;
        so.FindProperty("_playerMarker").objectReferenceValue = marker;
        so.FindProperty("_frameImage").objectReferenceValue = frame;
        so.FindProperty("_maskImage").objectReferenceValue = maskShape;
        so.FindProperty("_zoneText").objectReferenceValue = zoneText;
        so.FindProperty("_dateText").objectReferenceValue = dateText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildFullMap(GameObject root, Sprite snapshot, Sprite closeSprite, Sprite markerSprite)
    {
        Transform mapPopupTransform = root.transform.Find("MapPopup")
            ?? throw new InvalidOperationException("MapPopup was not found in UnifiedGameplayHUD.prefab.");
        GameObject mapPopup = mapPopupTransform.gameObject;

        for (int i = mapPopupTransform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(mapPopupTransform.GetChild(i).gameObject);
        foreach (FullMapController stale in mapPopup.GetComponents<FullMapController>())
            UnityEngine.Object.DestroyImmediate(stale);

        RectTransform popupRect = (RectTransform)mapPopupTransform;
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.offsetMin = Vector2.zero;
        popupRect.offsetMax = Vector2.zero;

        Image dim = mapPopup.GetComponent<Image>() ?? mapPopup.AddComponent<Image>();
        dim.sprite = null;
        dim.color = new Color(0.03f, 0.025f, 0.02f, 1f);
        dim.raycastTarget = true;

        // Edge-to-edge fullscreen -- no window-style margins. FullMapController computes a
        // cover-fit sizeDelta for MapImage so it always fills this exactly, with no black bars.
        RectTransform viewport = CreateStretchRect("Viewport", mapPopupTransform, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image viewportBg = viewport.gameObject.AddComponent<Image>();
        viewportBg.color = new Color(0.03f, 0.025f, 0.02f, 1f);
        viewportBg.raycastTarget = true;

        RectTransform mapImageRect = CreatePointRect("MapImage", viewport, new Vector2(0.5f, 0.5f),
            Vector2.zero, snapshot.rect.size);
        Image mapImage = mapImageRect.gameObject.AddComponent<Image>();
        mapImage.sprite = snapshot;
        mapImage.raycastTarget = false;

        RectTransform marker = CreatePlayerMarker(mapImageRect, 16f, markerSprite);

        TMP_Text hint = CreatePointText("ZoomHint", mapPopupTransform, new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(400f, 24f), "Scroll to zoom", 13f, TextAlignmentOptions.Center);
        hint.color = new Color(0.90f, 0.87f, 0.80f, 0.85f);

        // Close button art already depicts the X -- no separate TMP label overlay needed.
        RectTransform closeRect = CreatePointRect("CloseButton", mapPopupTransform, new Vector2(1f, 1f),
            new Vector2(-30f, -30f), new Vector2(44f, 44f));
        Image closeImage = closeRect.gameObject.AddComponent<Image>();
        closeImage.sprite = closeSprite;
        closeImage.color = Color.white;
        Button closeButton = closeRect.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeButton.onClick,
            root.GetComponent<UnifiedGameplayHudController>().ClosePopup);

        FullMapController controller = mapPopup.AddComponent<FullMapController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("_viewport").objectReferenceValue = viewport;
        so.FindProperty("_mapImage").objectReferenceValue = mapImage;
        so.FindProperty("_playerMarker").objectReferenceValue = marker;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddReadabilityShadow(TMP_Text text)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    // player_marker_v1 already points up with no established facing contract to rotate against
    // (per Handoffs/ClaudeToCodex.md) -- render it as-is, no per-frame rotation.
    private static RectTransform CreatePlayerMarker(Transform parent, float size, Sprite sprite)
    {
        RectTransform marker = CreatePointRect("PlayerMarker", parent, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(size, size));
        Image markerImage = marker.gameObject.AddComponent<Image>();
        markerImage.sprite = sprite;
        markerImage.color = Color.white;
        markerImage.preserveAspect = true;
        markerImage.raycastTarget = false;
        return marker;
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
            throw new InvalidOperationException($"Missing map art sprite: {path}");
        return sprite;
    }

    // Point anchor (anchorMin == anchorMax == anchor); pivot matches anchor so `position` reads
    // naturally as "offset from that corner/edge" and `size` is the box's plain width/height.
    private static RectTransform CreatePointRect(string name, Transform parent, Vector2 anchor, Vector2 position,
        Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    // Stretch anchor (anchorMin (0,0), anchorMax (1,1)) with symmetric-friendly inset offsets.
    private static RectTransform CreateStretchRect(string name, Transform parent, Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private static TMP_Text CreatePointText(string name, Transform parent, Vector2 anchor, Vector2 position,
        Vector2 size, string value, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreatePointRect(name, parent, anchor, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }
}
