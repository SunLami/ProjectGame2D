using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Standalone "accept this quest?" confirmation card shown after an NPC's dialogue closes on an
/// offered quest -- lists objectives and rewards so the player decides before QuestNpcInteractionUI
/// commits to QuestManager.TryAcceptQuest. Presentation-only: never touches QuestManager directly,
/// only reports the player's Accept/Decline choice back through the callback passed to Open().
/// </summary>
public sealed class QuestAcceptPopupUI : MonoBehaviour
{
    public static QuestAcceptPopupUI Instance { get; private set; }

    [SerializeField] private GameObject _root;
    [SerializeField] private TMP_Text _questPromptText;
    [SerializeField] private Transform _objectiveContainer;
    [SerializeField] private GameObject _objectiveRowTemplate;
    [SerializeField] private Transform _rewardContainer;
    [SerializeField] private GameObject _rewardSlotTemplate;
    [SerializeField] private TMP_Text _rewardSummaryText;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _declineButton;

    private readonly List<GameObject> _spawnedObjectiveRows = new();
    private readonly List<GameObject> _spawnedRewardSlots = new();
    private Action<bool> _onDecision;
    private IItemResolver _itemResolver;

    public bool IsOpen => _root != null && _root.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _root.SetActive(false);
        _objectiveRowTemplate.SetActive(false);
        _rewardSlotTemplate.SetActive(false);
    }

    private void OnEnable()
    {
        _acceptButton.onClick.AddListener(HandleAccept);
        _declineButton.onClick.AddListener(HandleDecline);
    }

    private void OnDisable()
    {
        _acceptButton.onClick.RemoveListener(HandleAccept);
        _declineButton.onClick.RemoveListener(HandleDecline);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool Open(QuestDefinition quest, Action<bool> onDecision)
    {
        if (quest == null || GameStateManager.Instance == null)
            return false;

        // Cutscene allowed for the same reason as DialogueUI.Open: a Timeline-driven conversation
        // can still offer a quest without leaving its own state.
        GameState currentState = GameStateManager.Instance.CurrentState;
        if (currentState != GameState.Playing && currentState != GameState.Cutscene)
            return false;

        _onDecision = onDecision;
        _questPromptText.text = quest.DisplayName;
        Transform card = _questPromptText.transform.parent;
        Image categoryIcon = card.Find("QuestCategoryIcon")?.GetComponent<Image>();
        if (categoryIcon != null)
            categoryIcon.sprite = LoadCategorySprite(quest);
        BuildObjectives(quest);
        BuildRewards(quest);
        _root.SetActive(true);
        GameStateManager.Instance.PushState(GameState.Dialogue);
        return true;
    }

    private void BuildObjectives(QuestDefinition quest)
    {
        ClearSpawned(_spawnedObjectiveRows);
        if (_objectiveContainer == null || _objectiveRowTemplate == null)
            return;

        foreach (QuestObjectiveDefinition objective in quest.Objectives)
        {
            GameObject row = Instantiate(_objectiveRowTemplate, _objectiveContainer);
            row.SetActive(true);
            Image icon = row.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
                icon.sprite = LoadObjectiveSprite(objective.Type);
            row.transform.Find("Progress").GetComponent<TMP_Text>().text = $"0/{objective.TargetCount}";
            row.transform.Find("Label").GetComponent<TMP_Text>().text = objective.Description;
            _spawnedObjectiveRows.Add(row);
        }
    }

    private void BuildRewards(QuestDefinition quest)
    {
        ClearSpawned(_spawnedRewardSlots);
        QuestRewardDefinition rewards = quest.Rewards;
        if (rewards == null)
        {
            if (_rewardSummaryText != null)
                _rewardSummaryText.text = string.Empty;
            return;
        }

        if (_rewardContainer != null && _rewardSlotTemplate != null)
        {
            _itemResolver ??= new ResourcesItemResolver();
            int visibleItemCount = 0;
            foreach (QuestRewardItemEntry entry in rewards.Items)
                if (!string.IsNullOrEmpty(entry.ItemId))
                    visibleItemCount++;

            float slotSize = visibleItemCount > 0
                ? Mathf.Clamp((244f - (Mathf.Max(0, visibleItemCount - 1) * 6f)) / visibleItemCount, 24f, 42f)
                : 42f;

            foreach (QuestRewardItemEntry entry in rewards.Items)
            {
                if (string.IsNullOrEmpty(entry.ItemId))
                    continue;

                GameObject slot = Instantiate(_rewardSlotTemplate, _rewardContainer);
                slot.SetActive(true);
                LayoutElement slotLayout = slot.GetComponent<LayoutElement>();
                if (slotLayout != null)
                {
                    slotLayout.preferredWidth = slotSize;
                    slotLayout.preferredHeight = slotSize;
                }
                Image icon = slot.transform.Find("Icon").GetComponent<Image>();
                TMP_Text qty = slot.transform.Find("Qty").GetComponent<TMP_Text>();
                bool resolved = _itemResolver.TryResolve(entry.ItemId, out ItemSO item) && item.icon != null;
                icon.enabled = resolved;
                if (resolved)
                    icon.sprite = item.icon;
                qty.text = $"x{entry.Quantity}";
                _spawnedRewardSlots.Add(slot);
            }
        }

        var extras = new List<string>();
        if (rewards.Gold > 0)
            extras.Add($"{rewards.Gold} Gold");
        if (rewards.Experience > 0)
            extras.Add($"{rewards.Experience} XP");
        if (_rewardSummaryText != null)
            _rewardSummaryText.text = string.Join("     ", extras);
    }

    private static void ClearSpawned(List<GameObject> spawned)
    {
        foreach (GameObject go in spawned)
        {
            if (go != null)
                Destroy(go);
        }
        spawned.Clear();
    }

    private static Sprite LoadCategorySprite(QuestDefinition quest)
    {
        string name = quest.IsMainQuest ? "category_main" : quest.IsDailyQuest ? "category_daily" : "category_side";
        return LoadSprite("UI/Quest/Tracker1920/" + name);
    }

    private static Sprite LoadObjectiveSprite(QuestObjectiveType type)
    {
        string name = type switch
        {
            QuestObjectiveType.Kill => "objective_kill",
            QuestObjectiveType.Gather or QuestObjectiveType.Obtain => "objective_collect",
            QuestObjectiveType.Talk => "objective_talk",
            _ => "objective_location"
        };
        return LoadSprite("UI/Quest/Tracker1920/" + name);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private void HandleAccept() => Close(true);
    private void HandleDecline() => Close(false);

    private void Close(bool accepted)
    {
        if (!IsOpen)
            return;

        _root.SetActive(false);
        GameStateManager.Instance?.ReturnToPreviousState();
        Action<bool> callback = _onDecision;
        _onDecision = null;
        callback?.Invoke(accepted);
    }
}
