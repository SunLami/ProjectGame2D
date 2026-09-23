using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class QuestAcceptPopupPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/QuestAcceptPopup.prefab";
    private const string BoardPath = "Assets/Resources/UI/Quest/QuestAccept1920/quest_accept_board_dynamic_rewards_v2.png";
    private const string RewardSlotPath = "Assets/Resources/UI/Inventory/LightFantasy/inventory_slot_hd.png";
    private static readonly Color Cream = new(0.96f, 0.91f, 0.76f, 1f);
    private static readonly Color Gold = new(0.83f, 0.60f, 0.24f, 1f);
    private static readonly Color Muted = new(0.72f, 0.68f, 0.65f, 1f);

    [MenuItem("Tools/ProjectGame2D/UI/Rebuild Quest Accept Popup 1920")]
    public static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            QuestAcceptPopupUI ui = root.GetComponent<QuestAcceptPopupUI>();
            if (ui == null) throw new InvalidOperationException("QuestAcceptPopupUI missing from prefab.");

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform overlay = Require(root.transform, "PopupOverlay").GetComponent<RectTransform>();
            Stretch(overlay, 0f);
            Image overlayImage = overlay.GetComponent<Image>();
            overlayImage.sprite = null;
            overlayImage.type = Image.Type.Simple;
            overlayImage.color = new Color(0.015f, 0.02f, 0.035f, 0.58f);
            overlayImage.raycastTarget = true;

            Transform card = Require(overlay, "Card");
            Center(card.GetComponent<RectTransform>(), new Vector2(320f, 400f), Vector2.zero);
            Image cardImage = card.GetComponent<Image>();
            cardImage.sprite = Load(BoardPath);
            cardImage.type = Image.Type.Simple;
            cardImage.preserveAspect = true;
            cardImage.color = Color.white;

            TMP_FontAsset font = Require(card, "QuestPromptText").GetComponent<TMP_Text>().font;
            TMP_Text header = ChildText(card, "HeaderTitle", font);
            Style(header, 16f, Cream, TextAlignmentOptions.Center);
            header.text = "QUEST OFFER";
            Rect(header.rectTransform, new Vector2(220f, 28f), new Vector2(50f, -19f));

            Image categoryIcon = ChildImage(card, "QuestCategoryIcon", Load("Assets/Resources/UI/Quest/Tracker1920/category_side.png"));
            Place(categoryIcon.rectTransform, new Vector2(28f, 28f), new Vector2(36f, -61f));
            categoryIcon.preserveAspect = true;

            TMP_Text prompt = Require(card, "QuestPromptText").GetComponent<TMP_Text>();
            Style(prompt, 13f, Cream, TextAlignmentOptions.Left);
            Rect(prompt.rectTransform, new Vector2(222f, 30f), new Vector2(72f, -61f));

            TMP_Text objectivesTitle = ChildText(card, "ObjectivesTitle", font);
            Style(objectivesTitle, 9.5f, Gold, TextAlignmentOptions.Left);
            objectivesTitle.text = "OBJECTIVES";
            Rect(objectivesTitle.rectTransform, new Vector2(130f, 18f), new Vector2(38f, -105f));

            ConfigureObjectives(card, font);
            ConfigureRewards(card, font);
            ConfigureButtons(card, font);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("QuestAcceptPopup rebuilt for 1920x1080 authoring.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureObjectives(Transform card, TMP_FontAsset font)
    {
        Transform container = Require(card, "ObjectivesContainer");
        Rect(container.GetComponent<RectTransform>(), new Vector2(244f, 112f), new Vector2(38f, -94f));
        VerticalLayoutGroup vertical = container.GetComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(0, 0, 0, 0);
        vertical.spacing = 5f;
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = true;
        vertical.childForceExpandWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandHeight = false;

        Transform row = Require(container, "ObjectiveRowTemplate");
        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 24f;
        HorizontalLayoutGroup horizontal = row.GetComponent<HorizontalLayoutGroup>();
        horizontal.padding = new RectOffset(0, 0, 0, 0);
        horizontal.spacing = 5f;
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandHeight = true;

        Image icon = ChildImage(row, "Icon", Load("Assets/Resources/UI/Quest/Tracker1920/objective_location.png"));
        icon.transform.SetAsFirstSibling();
        icon.preserveAspect = true;
        LayoutElement iconLayout = icon.GetComponent<LayoutElement>() ?? icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 22f;
        iconLayout.preferredHeight = 22f;

        TMP_Text label = Require(row, "Label").GetComponent<TMP_Text>();
        Style(label, 8f, Cream, TextAlignmentOptions.MidlineLeft);
        LayoutElement labelLayout = label.GetComponent<LayoutElement>();
        labelLayout.preferredWidth = 175f;
        labelLayout.flexibleWidth = 1f;

        TMP_Text progress = Require(row, "Progress").GetComponent<TMP_Text>();
        Style(progress, 8f, Cream, TextAlignmentOptions.MidlineRight);
        LayoutElement progressLayout = progress.GetComponent<LayoutElement>();
        progressLayout.preferredWidth = 36f;
        progressLayout.flexibleWidth = 0f;
        progress.transform.SetAsLastSibling();
    }

    private static void ConfigureRewards(Transform card, TMP_FontAsset font)
    {
        Transform rewards = Require(card, "RewardsColumn");
        Rect(rewards.GetComponent<RectTransform>(), new Vector2(320f, 400f), Vector2.zero);
        VerticalLayoutGroup vertical = rewards.GetComponent<VerticalLayoutGroup>();
        vertical.enabled = false;

        TMP_Text title = Require(rewards, "RewardsTitle").GetComponent<TMP_Text>();
        Style(title, 9.5f, Gold, TextAlignmentOptions.Center);
        title.text = "REWARDS";
        Rect(title.rectTransform, new Vector2(160f, 18f), new Vector2(80f, -238f));

        Transform rewardRow = Require(rewards, "RewardRow");
        Rect(rewardRow.GetComponent<RectTransform>(), new Vector2(244f, 44f), new Vector2(38f, -258f));
        HorizontalLayoutGroup horizontal = rewardRow.GetComponent<HorizontalLayoutGroup>();
        horizontal.spacing = 6f;
        horizontal.childAlignment = TextAnchor.MiddleCenter;
        horizontal.childControlWidth = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandHeight = false;

        Transform slot = Require(rewardRow, "RewardSlotTemplate");
        LayoutElement slotLayout = slot.GetComponent<LayoutElement>();
        slotLayout.preferredWidth = 42f;
        slotLayout.preferredHeight = 42f;
        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = Load(RewardSlotPath);
        slotImage.type = Image.Type.Simple;
        slotImage.preserveAspect = true;
        slotImage.color = Color.white;
        Image rewardIcon = Require(slot, "Icon").GetComponent<Image>();
        Stretch(rewardIcon.rectTransform, 5f);
        rewardIcon.preserveAspect = true;
        TMP_Text qty = Require(slot, "Qty").GetComponent<TMP_Text>();
        Style(qty, 7f, Cream, TextAlignmentOptions.BottomRight);
        Stretch(qty.rectTransform, 1f);

        TMP_Text summary = Require(rewards, "RewardSummaryText").GetComponent<TMP_Text>();
        Style(summary, 8f, Cream, TextAlignmentOptions.Center);
        Rect(summary.rectTransform, new Vector2(200f, 18f), new Vector2(60f, -306f));
    }

    private static void ConfigureButtons(Transform card, TMP_FontAsset font)
    {
        Transform row = Require(card, "ButtonRow");
        Rect(row.GetComponent<RectTransform>(), new Vector2(252f, 34f), new Vector2(34f, -318f));
        HorizontalLayoutGroup horizontal = row.GetComponent<HorizontalLayoutGroup>();
        horizontal.padding = new RectOffset(0, 0, 0, 0);
        horizontal.spacing = 12f;
        horizontal.childAlignment = TextAnchor.MiddleCenter;
        horizontal.childControlWidth = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandHeight = true;

        Button decline = Require(row, "DeclineButton").GetComponent<Button>();
        Button accept = Require(row, "AcceptButton").GetComponent<Button>();
        row.SetSiblingIndex(row.parent.childCount - 1);
        decline.transform.SetAsLastSibling();
        ConfigureButton(accept, font, "ACCEPT", 132f);
        ConfigureButton(decline, font, "DECLINE", 108f);
    }

    private static void ConfigureButton(Button button, TMP_FontAsset font, string text, float width)
    {
        LayoutElement layout = button.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 32f;
        Image image = button.GetComponent<Image>();
        image.sprite = null;
        image.color = new Color(1f, 1f, 1f, 0.01f);
        button.transition = Selectable.Transition.ColorTint;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        Style(label, 9f, Color.white, TextAlignmentOptions.Center);
        label.text = text;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(4f, -1f);
        label.rectTransform.offsetMax = new Vector2(-4f, -5f);
    }

    private static TMP_Text ChildText(Transform parent, string name, TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (existing == null) go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = font;
        return text;
    }

    private static Image ChildImage(Transform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null) go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static void Style(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
    {
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;
        text.raycastTarget = false;
    }

    private static Sprite Load(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite nested) return nested;
        throw new InvalidOperationException("Sprite not imported: " + path);
    }

    private static Transform Require(Transform parent, string path) =>
        parent.Find(path) ?? throw new InvalidOperationException("Missing QuestAccept object: " + parent.name + "/" + path);

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Rect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Place(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
