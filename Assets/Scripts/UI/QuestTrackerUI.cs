using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Background-free quest HUD with a header-controlled dropdown list.</summary>
public sealed class QuestTrackerUI : MonoBehaviour
{
    private const string SpriteRoot = "UI/Quest/Tracker1920/";
    private static readonly Color Cream = new(1f, 0.94f, 0.78f, 1f);
    private static readonly Color Gold = new(0.96f, 0.69f, 0.20f, 1f);

    private readonly List<GameObject> _rows = new();
    private Image _chevron;
    private ScrollRect _scrollRect;
    private RectTransform _content;
    private RectTransform _viewport;
    private GameObject _rowTemplate;
    private TMP_Text _titleStyle;
    private TMP_Text _descriptionStyle;
    private bool _isOpen = true;

    public bool IsInitialized => _scrollRect != null;

    public void Initialize(TMP_Text titleStyle, TMP_Text descriptionStyle)
    {
        if (_scrollRect != null)
            return;

        _titleStyle = titleStyle;
        _descriptionStyle = descriptionStyle;
        if (_titleStyle != null) _titleStyle.gameObject.SetActive(false);
        if (_descriptionStyle != null) _descriptionStyle.gameObject.SetActive(false);
        Transform legacyBanner = transform.Find("SkinTrackerQuestBanner");
        if (legacyBanner != null) legacyBanner.gameObject.SetActive(false);

        RectTransform root = (RectTransform)transform;
        root.sizeDelta = new Vector2(155f, 190f); // 372x456 physical pixels at 1920x1080.
        Image rootImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        rootImage.sprite = null;
        rootImage.color = Color.clear;
        rootImage.raycastTarget = false;

        BuildHeader();
        BuildViewport();
        SetOpen(true);
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
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildHeader()
    {
        Transform authoredHeader = transform.Find("Header");
        if (authoredHeader != null)
        {
            _chevron = authoredHeader.Find("Chevron")?.GetComponent<Image>();
            if (_chevron != null && _chevron.sprite == null)
                _chevron.sprite = LoadSprite(_isOpen ? "chevron_down" : "chevron_right");

            Image questIcon = authoredHeader.Find("QuestIcon")?.GetComponent<Image>();
            if (questIcon != null && questIcon.sprite == null)
                questIcon.sprite = LoadSprite("quest_scroll");

            Button authoredButton = authoredHeader.GetComponent<Button>();
            if (authoredButton != null)
            {
                authoredButton.onClick.RemoveAllListeners();
                authoredButton.onClick.AddListener(() => SetOpen(!_isOpen));
            }
            return;
        }

        GameObject header = new("Header", typeof(RectTransform), typeof(Image), typeof(Button));
        header.transform.SetParent(transform, false);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 20f);
        Image headerImage = header.GetComponent<Image>();
        headerImage.color = new Color(0f, 0f, 0f, 0.001f);

        _chevron = CreateIcon("Chevron", header.transform, "chevron_down", new Vector2(1f, -3f), new Vector2(14f, 14f));
        CreateIcon("QuestIcon", header.transform, "quest_scroll", new Vector2(17f, -2f), new Vector2(16f, 16f));

        TMP_Text title = CreateText("QuestHeader", header.transform, _titleStyle);
        SetTopLeft(title.rectTransform, new Vector2(35f, -4f), new Vector2(48f, 15f));
        title.text = "QUESTS";
        title.fontSize = 8f;
        title.fontStyle = FontStyles.Bold;
        title.color = Cream;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        AddShadow(title.gameObject);

        GameObject line = new("GoldDivider", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(header.transform, false);
        RectTransform lineRect = line.GetComponent<RectTransform>();
        lineRect.anchorMin = lineRect.anchorMax = new Vector2(0f, 1f);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = new Vector2(82f, -10f);
        lineRect.sizeDelta = new Vector2(68f, 1f);
        Image lineImage = line.GetComponent<Image>();
        lineImage.color = Gold;
        lineImage.raycastTarget = false;

        Button button = header.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = headerImage;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.2f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => SetOpen(!_isOpen));
    }

    private void BuildViewport()
    {
        Transform existing = transform.Find("Viewport");
        _viewport = existing != null ? existing.GetComponent<RectTransform>() : null;
        if (_viewport == null)
        {
            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(transform, false);
            _viewport = viewportObject.GetComponent<RectTransform>();
        }

        _viewport.anchorMin = Vector2.zero;
        _viewport.anchorMax = Vector2.one;
        _viewport.offsetMin = Vector2.zero;
        _viewport.offsetMax = new Vector2(0f, -22f);
        if (_viewport.GetComponent<RectMask2D>() == null) _viewport.gameObject.AddComponent<RectMask2D>();
        Image viewportImage = _viewport.GetComponent<Image>() ?? _viewport.gameObject.AddComponent<Image>();
        viewportImage.sprite = null;
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;

        Transform existingContent = _viewport.Find("Content");
        GameObject contentObject = existingContent != null
            ? existingContent.gameObject
            : new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        if (existingContent == null) contentObject.transform.SetParent(_viewport, false);
        _content = contentObject.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = Vector2.one;
        _content.pivot = new Vector2(0.5f, 1f);
        _content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>() ?? contentObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(2, 2, 2, 2);
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>() ?? contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Transform authoredTemplate = _content.Find("QuestRowTemplate");
        _rowTemplate = authoredTemplate != null ? authoredTemplate.gameObject : null;
        if (_rowTemplate != null) _rowTemplate.SetActive(false);
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            if (_content.GetChild(i).gameObject != _rowTemplate)
                Destroy(_content.GetChild(i).gameObject);
        }

        _scrollRect = GetComponent<ScrollRect>() ?? gameObject.AddComponent<ScrollRect>();
        _scrollRect.viewport = _viewport;
        _scrollRect.content = _content;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 24f;
    }

    private void SetOpen(bool open)
    {
        _isOpen = open;
        if (_viewport != null) _viewport.gameObject.SetActive(open);
        if (_chevron != null) _chevron.sprite = LoadSprite(open ? "chevron_down" : "chevron_right");
    }

    private void CreateRow(TrackedQuestView quest)
    {
        if (_rowTemplate != null)
        {
            CreateAuthoredRow(quest);
            return;
        }

        GameObject row = new($"TrackedQuest_{quest.QuestId}", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        row.transform.SetParent(_content, false);
        VerticalLayoutGroup layout = row.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 0f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        row.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateInfoLine(row.transform, "QuestTitle", CategorySprite(quest.Category), quest.Title, string.Empty, 7.5f, CategoryColor(quest.Category));
        string progress = quest.ReadyToTurnIn ? "READY" : quest.TargetCount > 1 ? $"{quest.CurrentCount}/{quest.TargetCount}" : string.Empty;
        Sprite objectiveIcon = quest.ReadyToTurnIn ? LoadSprite("objective_return") : ObjectiveSprite(quest.ObjectiveType);
        CreateInfoLine(row.transform, "Objective", objectiveIcon, quest.ShortDescription, progress, 6.5f, quest.ReadyToTurnIn ? Gold : Cream);
        _rows.Add(row);
    }

    private void CreateAuthoredRow(TrackedQuestView quest)
    {
        GameObject row = Instantiate(_rowTemplate, _content);
        row.name = $"TrackedQuest_{quest.QuestId}";
        row.SetActive(true);

        Transform questLine = row.transform.Find("QuestTitle");
        Image questIcon = questLine.Find("Icon").GetComponent<Image>();
        TMP_Text questLabel = questLine.Find("Label").GetComponent<TMP_Text>();
        questIcon.sprite = CategorySprite(quest.Category);
        questLabel.text = quest.Title;
        questLabel.color = CategoryColor(quest.Category);

        Transform objectiveLine = row.transform.Find("Objective");
        Image objectiveIcon = objectiveLine.Find("Icon").GetComponent<Image>();
        TMP_Text objectiveLabel = objectiveLine.Find("Label").GetComponent<TMP_Text>();
        TMP_Text progress = objectiveLine.Find("Progress").GetComponent<TMP_Text>();
        objectiveIcon.sprite = quest.ReadyToTurnIn ? LoadSprite("objective_return") : ObjectiveSprite(quest.ObjectiveType);
        objectiveLabel.text = quest.ShortDescription;
        objectiveLabel.color = quest.ReadyToTurnIn ? Gold : Cream;
        progress.text = quest.ReadyToTurnIn ? "READY" : quest.TargetCount > 1 ? $"{quest.CurrentCount}/{quest.TargetCount}" : string.Empty;
        progress.color = quest.ReadyToTurnIn ? Gold : Cream;
        _rows.Add(row);
    }

    private void CreateInfoLine(Transform parent, string name, Sprite icon, string label, string trailing, float fontSize, Color color)
    {
        GameObject line = new(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        line.transform.SetParent(parent, false);
        line.GetComponent<LayoutElement>().preferredHeight = name == "QuestTitle" ? 16f : 15f;
        HorizontalLayoutGroup layout = line.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Image image = CreateIcon("Icon", line.transform, null, Vector2.zero, new Vector2(14f, 14f));
        image.sprite = icon;
        image.gameObject.AddComponent<LayoutElement>().preferredWidth = 14f;

        TMP_Text text = CreateText("Label", line.transform, name == "QuestTitle" ? _titleStyle : _descriptionStyle);
        text.text = label;
        text.fontSize = fontSize;
        text.fontStyle = name == "QuestTitle" ? FontStyles.Bold : FontStyles.Normal;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;
        AddShadow(text.gameObject);

        if (!string.IsNullOrEmpty(trailing))
        {
            TMP_Text value = CreateText("Progress", line.transform, _descriptionStyle);
            value.text = trailing;
            value.fontSize = 6.5f;
            value.fontStyle = FontStyles.Bold;
            value.color = trailing == "READY" ? Gold : Cream;
            value.alignment = TextAlignmentOptions.MidlineRight;
            value.gameObject.GetComponent<LayoutElement>().preferredWidth = trailing == "READY" ? 27f : 20f;
            AddShadow(value.gameObject);
        }
    }

    private static Image CreateIcon(string name, Transform parent, string spriteName, Vector2 position, Vector2 size)
    {
        GameObject iconObject = new(name, typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(parent, false);
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = iconObject.GetComponent<Image>();
        image.sprite = string.IsNullOrEmpty(spriteName) ? null : LoadSprite(spriteName);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
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
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void AddShadow(GameObject target)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1.2f, -1.2f);
    }

    private static Sprite CategorySprite(QuestCategory category) => LoadSprite(category switch
    {
        QuestCategory.Main => "category_main",
        QuestCategory.Daily => "category_daily",
        _ => "category_side"
    });

    private static Sprite ObjectiveSprite(QuestObjectiveType type) => LoadSprite(type switch
    {
        QuestObjectiveType.Kill => "objective_kill",
        QuestObjectiveType.Talk => "objective_talk",
        QuestObjectiveType.Gather or QuestObjectiveType.Obtain or QuestObjectiveType.Craft or QuestObjectiveType.Purchase => "objective_collect",
        _ => "objective_location"
    });

    private static Sprite LoadSprite(string name)
    {
        string path = SpriteRoot + name;
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        Sprite[] slicedSprites = Resources.LoadAll<Sprite>(path);
        return slicedSprites.Length > 0 ? slicedSprites[0] : null;
    }

    private static Color CategoryColor(QuestCategory category) => category switch
    {
        QuestCategory.Main => Gold,
        QuestCategory.Daily => new Color(0.44f, 0.86f, 1f, 1f),
        _ => new Color(0.72f, 1f, 0.62f, 1f)
    };

    private void ClearRows()
    {
        foreach (GameObject row in _rows) Destroy(row);
        _rows.Clear();
    }
}

public enum QuestCategory { Main = 0, Side = 1, Daily = 2 }

public readonly struct TrackedQuestView
{
    public TrackedQuestView(string questId, string title, string shortDescription, QuestCategory category)
        : this(questId, title, shortDescription, category, QuestObjectiveType.Talk, 0, 0, false) { }

    public TrackedQuestView(string questId, string title, string shortDescription, QuestCategory category,
        QuestObjectiveType objectiveType, int currentCount, int targetCount, bool readyToTurnIn)
    {
        QuestId = questId;
        Title = title;
        ShortDescription = shortDescription;
        Category = category;
        ObjectiveType = objectiveType;
        CurrentCount = currentCount;
        TargetCount = targetCount;
        ReadyToTurnIn = readyToTurnIn;
    }

    public string QuestId { get; }
    public string Title { get; }
    public string ShortDescription { get; }
    public QuestCategory Category { get; }
    public QuestObjectiveType ObjectiveType { get; }
    public int CurrentCount { get; }
    public int TargetCount { get; }
    public bool ReadyToTurnIn { get; }
}

public static class QuestTrackerOrdering
{
    public static int Compare(TrackedQuestView left, TrackedQuestView right)
    {
        int category = left.Category.CompareTo(right.Category);
        return category != 0 ? category : string.Compare(left.Title, right.Title, StringComparison.CurrentCultureIgnoreCase);
    }
}
