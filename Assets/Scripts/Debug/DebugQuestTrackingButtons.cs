using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates three development buttons that accept authored debug quests through QuestManager.</summary>
public sealed class DebugQuestTrackingButtons : MonoBehaviour
{
    private const string MainQuestId = "quest.debug.main.001";
    private const string SideQuestId = "quest.debug.side.001";
    private const string DailyQuestId = "quest.debug.daily.001";

    private readonly List<DebugQuestButton> _buttons = new();
    private QuestManager _questManager;

    private void Start()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        enabled = false;
        return;
#else
        Button template = GetComponent<Button>();
        TMP_Text templateLabel = GetComponentInChildren<TMP_Text>(true);
        if (template == null || templateLabel == null)
            return;

        CreateButton(template, templateLabel, MainQuestId, "TRACK MAIN QUEST", new Color(0.48f, 0.25f, 0.04f, 0.96f), 1);
        CreateButton(template, templateLabel, SideQuestId, "TRACK SIDE QUEST", new Color(0.12f, 0.34f, 0.12f, 0.96f), 2);
        CreateButton(template, templateLabel, DailyQuestId, "TRACK DAILY QUEST", new Color(0.05f, 0.28f, 0.43f, 0.96f), 3);
        BindQuestManager();
        RefreshButtons();
#endif
    }

    private void OnDestroy() => UnbindQuestManager();

    private void CreateButton(
        Button template,
        TMP_Text templateLabel,
        string questId,
        string idleLabel,
        Color backgroundColor,
        int row)
    {
        GameObject buttonObject = new(
            $"DebugTrackQuestButton_{row}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(transform.parent, false);
        buttonObject.transform.SetSiblingIndex(transform.GetSiblingIndex() + row);

        RectTransform sourceRect = (RectTransform)transform;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = sourceRect.anchorMin;
        rect.anchorMax = sourceRect.anchorMax;
        rect.pivot = sourceRect.pivot;
        rect.sizeDelta = new Vector2(sourceRect.sizeDelta.x, 32f);
        rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -42f * row);

        Image sourceImage = template.targetGraphic as Image;
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sourceImage != null ? sourceImage.sprite : null;
        image.type = sourceImage != null ? sourceImage.type : Image.Type.Simple;
        image.color = backgroundColor;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = template.transition;
        button.colors = template.colors;
        button.spriteState = template.spriteState;
        button.targetGraphic = image;

        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 2f);
        labelRect.offsetMax = new Vector2(-4f, -2f);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.font = templateLabel.font;
        label.fontSharedMaterial = templateLabel.fontSharedMaterial;
        label.fontSize = 10f;
        label.color = templateLabel.color;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        button.onClick.AddListener(() => TrackQuest(questId));
        _buttons.Add(new DebugQuestButton(questId, idleLabel, button, label));
    }

    private void BindQuestManager()
    {
        _questManager = QuestManager.Instance;
        if (_questManager == null)
            return;

        _questManager.QuestAccepted += HandleQuestChanged;
        _questManager.QuestProgressChanged += HandleQuestChanged;
        _questManager.QuestCompleted += HandleQuestChanged;
    }

    private void UnbindQuestManager()
    {
        if (_questManager == null)
            return;

        _questManager.QuestAccepted -= HandleQuestChanged;
        _questManager.QuestProgressChanged -= HandleQuestChanged;
        _questManager.QuestCompleted -= HandleQuestChanged;
        _questManager = null;
    }

    private void TrackQuest(string questId)
    {
        if (_questManager == null)
            BindQuestManager();

        _questManager?.TryAcceptQuest(questId);
        RefreshButtons();
    }

    private void HandleQuestChanged(string questId) => RefreshButtons();

    private void RefreshButtons()
    {
        foreach (DebugQuestButton entry in _buttons)
        {
            QuestStatus status = _questManager != null
                ? _questManager.GetStatus(entry.QuestId)
                : QuestStatus.Locked;
            entry.Button.interactable = status == QuestStatus.Available;
            entry.Label.text = status switch
            {
                QuestStatus.Active or QuestStatus.ReadyToTurnIn => "TRACKED",
                QuestStatus.Completed => "COMPLETED",
                QuestStatus.Locked => "LOCKED",
                _ => entry.IdleLabel
            };
        }
    }

    private sealed class DebugQuestButton
    {
        public DebugQuestButton(string questId, string idleLabel, Button button, TMP_Text label)
        {
            QuestId = questId;
            IdleLabel = idleLabel;
            Button = button;
            Label = label;
        }

        public string QuestId { get; }
        public string IdleLabel { get; }
        public Button Button { get; }
        public TMP_Text Label { get; }
    }
}
