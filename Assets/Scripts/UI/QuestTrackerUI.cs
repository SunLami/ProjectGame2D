using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Compact, fixed-size and scrollable presentation for tracked quests.</summary>
public sealed class QuestTrackerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color HoverBackground = new(0.08f, 0.08f, 0.08f, 0.58f);

    private readonly Dictionary<string, bool> _expandedByQuestId = new(StringComparer.Ordinal);
    private readonly List<GameObject> _rows = new();

    private Image _background;
    private ScrollRect _scrollRect;
    private RectTransform _content;
    private TMP_Text _titleStyle;
    private TMP_Text _descriptionStyle;

    public bool IsInitialized => _scrollRect != null;

    public void Initialize(TMP_Text titleStyle, TMP_Text descriptionStyle)
    {
        if (_scrollRect != null)
            return;

        _titleStyle = titleStyle;
        _descriptionStyle = descriptionStyle;
        if (_titleStyle != null)
            _titleStyle.gameObject.SetActive(false);
        if (_descriptionStyle != null)
            _descriptionStyle.gameObject.SetActive(false);
        Transform legacyBanner = transform.Find("SkinTrackerQuestBanner");
        if (legacyBanner != null)
            legacyBanner.gameObject.SetActive(false);

        _background = GetComponent<Image>();
        if (_background == null)
            _background = gameObject.AddComponent<Image>();
        _background.sprite = null;
        _background.color = Color.clear;
        _background.raycastTarget = true;

        // Reuse an editor-authored Viewport/Content (prefab layout preview) instead of always
        // creating new ones, so hand-tuned RectTransform sizing/position in the prefab survives
        // into Play mode. Any placeholder rows authored under Content for preview purposes are
        // cleared below -- they are not tracked in _rows and must not linger next to real rows.
        Transform existingViewport = transform.Find("Viewport");
        RectTransform viewport = existingViewport != null
            ? existingViewport.GetComponent<RectTransform>()
            : null;

        if (viewport == null)
        {
            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(transform, false);
            viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport, 6f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;
        }
        else
        {
            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage == null)
                viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;
        }

        Transform existingContent = viewport.Find("Content");
        GameObject contentObject = existingContent != null ? existingContent.gameObject : null;

        if (contentObject == null)
        {
            contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            _content = contentObject.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = Vector2.one;
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = Vector2.zero;
        }
        else
        {
            _content = contentObject.GetComponent<RectTransform>();
        }

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Preview-only placeholder rows (authored in the prefab for editor layout/sizing) are not
        // tracked by _rows, so they must be wiped here before any real SetQuests() call.
        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);

        _scrollRect = GetComponent<ScrollRect>();
        if (_scrollRect == null)
            _scrollRect = gameObject.AddComponent<ScrollRect>();
        _scrollRect.viewport = viewport;
        _scrollRect.content = _content;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 24f;
    }

    public void SetQuests(IReadOnlyList<TrackedQuestView> quests)
    {
        ClearRows();
        if (_content == null || quests == null)
            return;

        for (int i = 0; i < quests.Count; i++)
            CreateRow(quests[i]);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_background != null)
            _background.color = HoverBackground;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_background != null)
            _background.color = Color.clear;
    }

    private void CreateRow(TrackedQuestView quest)
    {
        GameObject row = new($"TrackedQuest_{quest.QuestId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        row.transform.SetParent(_content, false);
        Image rowImage = row.GetComponent<Image>();
        rowImage.color = Color.clear;

        VerticalLayoutGroup rowLayout = row.GetComponent<VerticalLayoutGroup>();
        rowLayout.padding = new RectOffset(3, 3, 2, 3);
        rowLayout.spacing = 1f;
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        row.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = CreateText("TitleTag", row.transform, _titleStyle);
        title.text = $"{CategoryTag(quest.Category)} {quest.Title}";
        title.fontStyle = FontStyles.Bold;
        title.color = CategoryColor(quest.Category);
        title.fontSize = 13f;

        TMP_Text description = CreateText("ShortDescription", row.transform, _descriptionStyle);
        description.text = "- " + quest.ShortDescription;
        description.fontSize = 10.5f;
        description.lineSpacing = 2f;
        description.color = new Color(0.78f, 0.78f, 0.74f, 1f);
        description.margin = new Vector4(15f, 3f, 2f, 4f);

        bool expanded = _expandedByQuestId.TryGetValue(quest.QuestId, out bool saved) && saved;
        description.gameObject.SetActive(expanded);

        Button button = row.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = rowImage;
        button.onClick.AddListener(() => Toggle(quest.QuestId, description));
        _rows.Add(row);
    }

    private void Toggle(string questId, TMP_Text description)
    {
        bool expanded = !description.gameObject.activeSelf;
        _expandedByQuestId[questId] = expanded;
        description.gameObject.SetActive(expanded);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    private void ClearRows()
    {
        foreach (GameObject row in _rows)
        {
            row.SetActive(false);
            row.transform.SetParent(transform, false);
            Destroy(row);
        }
        _rows.Clear();
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_Text style)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (style != null)
        {
            text.font = style.font;
            text.fontSharedMaterial = style.fontSharedMaterial;
        }
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        return text;
    }

    private static string CategoryTag(QuestCategory category) => category switch
    {
        QuestCategory.Main => "[MAIN]",
        QuestCategory.Daily => "[DAILY]",
        _ => "[SIDE]"
    };

    private static Color CategoryColor(QuestCategory category) => category switch
    {
        QuestCategory.Main => new Color(1f, 0.82f, 0.26f, 1f),
        QuestCategory.Daily => new Color(0.35f, 0.86f, 1f, 1f),
        _ => new Color(0.72f, 1f, 0.55f, 1f)
    };

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }
}

public enum QuestCategory
{
    Main = 0,
    Side = 1,
    Daily = 2
}

public readonly struct TrackedQuestView
{
    public TrackedQuestView(string questId, string title, string shortDescription, QuestCategory category)
    {
        QuestId = questId;
        Title = title;
        ShortDescription = shortDescription;
        Category = category;
    }

    public string QuestId { get; }
    public string Title { get; }
    public string ShortDescription { get; }
    public QuestCategory Category { get; }
}

public static class QuestTrackerOrdering
{
    public static int Compare(TrackedQuestView left, TrackedQuestView right)
    {
        int category = left.Category.CompareTo(right.Category);
        return category != 0
            ? category
            : string.Compare(left.Title, right.Title, StringComparison.CurrentCultureIgnoreCase);
    }
}
