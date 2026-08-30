using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryLightFantasySkinBuilder
{
    private const string ControllerPrefab = "Assets/Prefabs/InventoryUIController.prefab";
    private const string SlotPrefab = "Assets/Prefabs/InventorySlotUI.prefab";
    private const string SkinRoot = "Assets/Resources/UI/Inventory/LightFantasy/";

    [MenuItem("Tools/ProjectGame2D/UI/Apply Inventory Light Fantasy Skin")]
    public static void Apply()
    {
        Sprite board = ImportSprite(SkinRoot + "inventory_board_thin_hd.png");
        Sprite equipment = ImportSprite(SkinRoot + "equipment_panel_thin_hd.png");
        Sprite slot = ImportSprite(SkinRoot + "inventory_slot_thin_hd.png");
        Sprite close = ImportSprite(SkinRoot + "inventory_close_thin_hd.png");
        Sprite title = ImportSprite(SkinRoot + "inventory_title_hd.png");
        Sprite gridFrame = ImportSprite(SkinRoot + "inventory_grid_border_hd.png");
        Sprite goldBadge = ImportSprite(SkinRoot + "inventory_gold_badge_hd.png");

        EditPrefab(SlotPrefab, root =>
        {
            SetSprite(root, slot);
            SetIconSafeArea(Find(root.transform, "Icon"));
        });
        EditPrefab(ControllerPrefab, root =>
        {
            SetSprite(Find(root.transform, "InventoryPanel"), board);
            SetSprite(Find(root.transform, "EquipmentPanel"), equipment);
            SetSprite(Find(root.transform, "CloseBtn"), close);
            SetSprite(Find(root.transform, "TitleInventory"), title);
            SetGoldBadge(Find(root.transform, "Gold"), goldBadge);
            SetGridFrame(
                Find(root.transform, "GridScrollView"),
                Find(root.transform, "Viewport"),
                gridFrame);

            string[] equipmentSlots =
            {
                "HeadSlot", "BodySlot", "FootSlot", "WeaponSlot",
                "ShieldSlot", "NecklaceSlot", "RingSlot"
            };
            foreach (string slotName in equipmentSlots)
            {
                GameObject equipmentSlot = Find(root.transform, slotName);
                SetSprite(equipmentSlot, slot);
                SetIconSafeArea(Find(equipmentSlot.transform, "Icon"));
            }
        });

        AssetDatabase.SaveAssets();
        Debug.Log("Inventory Light Fantasy skin applied to source prefabs.");
    }

    private static Sprite ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"Texture importer not found: {path}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EditPrefab(string path, Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject Find(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child.gameObject;
            }
        }

        throw new InvalidOperationException($"Required Inventory UI object not found: {objectName}");
    }

    private static void SetSprite(GameObject target, Sprite sprite)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            throw new InvalidOperationException($"Image component not found on: {target.name}");
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
    }

    private static void SetIconSafeArea(GameObject iconObject)
    {
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        Image image = iconObject.GetComponent<Image>();
        if (rect == null || image == null)
        {
            throw new InvalidOperationException($"Inventory icon requires RectTransform and Image: {iconObject.name}");
        }

        rect.anchorMin = new Vector2(0.16f, 0.16f);
        rect.anchorMax = new Vector2(0.84f, 0.84f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        image.preserveAspect = true;
    }

    private static void SetBackgroundSprite(GameObject target, Sprite sprite)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void SetGridFrame(GameObject scrollView, GameObject viewport, Sprite sprite)
    {
        Image scrollViewImage = scrollView.GetComponent<Image>();
        if (scrollViewImage != null)
        {
            scrollViewImage.sprite = null;
            scrollViewImage.color = new Color(1f, 1f, 1f, 0f);
        }

        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage != null)
        {
            viewportImage.sprite = null;
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
        }

        Transform existing = scrollView.transform.Find("GridFrame");
        if (existing == null)
        {
            existing = viewport.transform.Find("GridFrame");
        }

        GameObject frame = existing != null
            ? existing.gameObject
            : new GameObject("GridFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        frame.transform.SetParent(scrollView.transform, false);
        frame.transform.SetAsLastSibling();

        RectTransform rect = (RectTransform)frame.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-12f, -12f);
        rect.offsetMax = new Vector2(12f, 12f);

        SetBackgroundSprite(frame, sprite);
    }

    private static void SetGoldBadge(GameObject gold, Sprite sprite)
    {
        Transform existing = gold.transform.Find("GoldBadge");
        GameObject badge = existing != null
            ? existing.gameObject
            : new GameObject("GoldBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        badge.transform.SetParent(gold.transform, false);
        badge.transform.SetAsFirstSibling();

        LayoutElement layoutElement = badge.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = badge.AddComponent<LayoutElement>();
        }
        layoutElement.ignoreLayout = true;

        RectTransform rect = (RectTransform)badge.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-5f, -4f);
        rect.offsetMax = new Vector2(5f, 4f);

        SetBackgroundSprite(badge, sprite);

        TextMeshProUGUI goldText = Find(gold.transform, "GoldText").GetComponent<TextMeshProUGUI>();
        if (goldText != null)
        {
            goldText.color = new Color(1f, 0.88f, 0.38f, 1f);
        }
    }
}
