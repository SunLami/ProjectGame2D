using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Event-driven presentation for the quest tracker and Quest Log gameplay menu.</summary>
public sealed class QuestLogUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private PlayerInput _playerInput;

    [Header("Tracker")]
    [SerializeField] private GameObject _trackerRoot;
    [SerializeField] private TMP_Text _trackerTitle;
    [SerializeField] private TMP_Text _trackerObjectives;

    [Header("Quest Log")]
    [SerializeField] private GameObject _logRoot;
    [SerializeField] private Transform _listContent;
    [SerializeField] private GameObject _rowTemplate;
    [SerializeField] private TMP_Text _emptyText;
    [SerializeField] private TMP_Text _detailTitle;
    [SerializeField] private TMP_Text _detailStatus;
    [SerializeField] private TMP_Text _detailObjectives;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _trackButton;
    [SerializeField] private TMP_Text _trackButtonLabel;
    [SerializeField] private Button _abandonButton;
    [SerializeField] private GameObject _abandonConfirmationRoot;
    [SerializeField] private TMP_Text _abandonConfirmationMessage;
    [SerializeField] private Button _confirmAbandonButton;
    [SerializeField] private Button _cancelAbandonButton;

    private readonly List<GameObject> _rows = new();
    private readonly List<TrackedQuestView> _trackedQuests = new();
    private QuestManager _questManager;
    private QuestTrackerUI _tracker;
    private InputAction _questLogAction;
    private string _selectedQuestId;
    private QuestCategory? _activeFilter;
    private Button[] _filterButtons;

    private void Awake()
    {
        ResolveQuestLogAction();
        ResolveQuestLogPresentation();

        if (_trackerRoot != null)
        {
            _tracker = _trackerRoot.GetComponent<QuestTrackerUI>();
            if (_tracker == null)
                _tracker = _trackerRoot.AddComponent<QuestTrackerUI>();
            _tracker.Initialize(_trackerTitle, _trackerObjectives);
        }
    }

    private void OnEnable()
    {
        _closeButton?.onClick.AddListener(CloseQuestLog);
        _trackButton?.onClick.AddListener(ToggleSelectedQuestTracking);
        _abandonButton?.onClick.AddListener(OpenAbandonConfirmation);
        _confirmAbandonButton?.onClick.AddListener(ConfirmAbandonQuest);
        _cancelAbandonButton?.onClick.AddListener(CloseAbandonConfirmation);
        BindFilterButtons();
        if (_questLogAction != null)
            _questLogAction.performed += HandleQuestLogPerformed;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged += HandleGameStateChanged;

        BindQuestManager();
        RefreshVisibility();
    }

    private void Start()
    {
        if (_questManager == null)
            BindQuestManager();
    }

    private void OnDisable()
    {
        _closeButton?.onClick.RemoveListener(CloseQuestLog);
        _trackButton?.onClick.RemoveListener(ToggleSelectedQuestTracking);
        _abandonButton?.onClick.RemoveListener(OpenAbandonConfirmation);
        _confirmAbandonButton?.onClick.RemoveListener(ConfirmAbandonQuest);
        _cancelAbandonButton?.onClick.RemoveListener(CloseAbandonConfirmation);
        UnbindFilterButtons();
        if (_questLogAction != null)
            _questLogAction.performed -= HandleQuestLogPerformed;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= HandleGameStateChanged;

        UnbindQuestManager();
    }

    public void OpenQuestLog()
    {
        if (GameStateManager.Instance == null || GameStateManager.Instance.CurrentState != GameState.Playing)
            return;

        GameStateManager.Instance.OpenMenu(GameplayMenuPage.QuestLog);
    }

    public void CloseQuestLog()
    {
        if (IsQuestLogOpen())
            GameStateManager.Instance.ReturnToPreviousState();
    }

    private void HandleQuestLogPerformed(InputAction.CallbackContext context) => OpenQuestLog();

    private void BindQuestManager()
    {
        QuestManager manager = QuestManager.Instance;
        if (_questManager != manager)
        {
            UnbindQuestManager();
            _questManager = manager;
            if (_questManager != null)
            {
                _questManager.QuestAccepted += HandleQuestChanged;
                _questManager.QuestProgressChanged += HandleQuestChanged;
                _questManager.QuestCompleted += HandleQuestChanged;
                _questManager.QuestTrackingChanged += HandleQuestChanged;
                _questManager.QuestAbandoned += HandleQuestAbandoned;
                _questManager.MainQuestUnlocked += HandleMainQuestUnlocked;
            }
        }

        RefreshQuestPresentation();
    }

    private void UnbindQuestManager()
    {
        if (_questManager == null)
            return;

        _questManager.QuestAccepted -= HandleQuestChanged;
        _questManager.QuestProgressChanged -= HandleQuestChanged;
        _questManager.QuestCompleted -= HandleQuestChanged;
        _questManager.QuestTrackingChanged -= HandleQuestChanged;
        _questManager.QuestAbandoned -= HandleQuestAbandoned;
        _questManager.MainQuestUnlocked -= HandleMainQuestUnlocked;
        _questManager = null;
    }

    private void HandleQuestChanged(string questId)
    {
        if (string.IsNullOrEmpty(_selectedQuestId))
            _selectedQuestId = questId;
        RefreshQuestPresentation();
    }

    private void HandleMainQuestUnlocked() => RefreshQuestPresentation();

    private void HandleQuestAbandoned(string questId)
    {
        if (_selectedQuestId == questId)
            _selectedQuestId = null;
        CloseAbandonConfirmation();
        RefreshQuestPresentation();
    }

    private void HandleGameStateChanged(GameStateChange change)
    {
        ResolveQuestLogAction();
        RefreshVisibility();
        if (IsQuestLogOpen())
            SelectDefault();
    }

    // This UI lives in the persistent Bootstrap scene and resolves its PlayerInput by scene
    // search, but Awake() here runs before the gameplay scene (and its Player) is loaded, so the
    // first lookup always fails. Retry on every GameState change (never assigned yet, or the
    // previous gameplay scene's Player was destroyed) so the QuestLog hotkey (J) starts working
    // once a real gameplay scene is actually loaded.
    private void ResolveQuestLogAction()
    {
        if (_questLogAction != null)
            return;

        if (_playerInput == null)
            _playerInput = Object.FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);

        InputAction found = _playerInput != null
            ? _playerInput.actions.FindAction("Gameplay/QuestLog", false)
            : null;
        if (found == null)
            return;

        _questLogAction = found;
        if (isActiveAndEnabled)
            _questLogAction.performed += HandleQuestLogPerformed;
    }

    private void RefreshVisibility()
    {
        bool logOpen = IsQuestLogOpen();
        _logRoot.SetActive(logOpen);
        if (_trackerRoot != null)
            _trackerRoot.SetActive(!logOpen && _trackedQuests.Count > 0);
    }

    private bool IsQuestLogOpen() =>
        GameStateManager.Instance != null
        && GameStateManager.Instance.CurrentState == GameState.GameplayMenu
        && GameStateManager.Instance.CurrentMenuPage == GameplayMenuPage.QuestLog;

    private void RefreshQuestPresentation()
    {
        RebuildTracker();

        RebuildList();
        RefreshDetails();
        RefreshVisibility();
    }

    private void RebuildTracker()
    {
        _trackedQuests.Clear();
        if (_questManager?.Catalog == null)
        {
            _tracker?.SetQuests(_trackedQuests);
            return;
        }

        foreach (QuestDefinition quest in _questManager.Catalog.AllQuests)
        {
            if (!_questManager.IsTracked(quest.QuestId))
                continue;

            QuestStatus status = _questManager.GetStatus(quest.QuestId);
            if (status != QuestStatus.Active && status != QuestStatus.ReadyToTurnIn)
                continue;

            _questManager.TryGetProgress(quest.QuestId, out QuestProgressSnapshot snapshot);
            int objectiveIndex = Mathf.Clamp(snapshot.CurrentObjectiveIndex, 0, Mathf.Max(0, quest.Objectives.Count - 1));
            QuestObjectiveDefinition objective = quest.Objectives.Count > 0 ? quest.Objectives[objectiveIndex] : null;
            int currentCount = objective != null && objectiveIndex < snapshot.ObjectiveCounters.Count
                ? snapshot.ObjectiveCounters[objectiveIndex]
                : 0;
            _trackedQuests.Add(new TrackedQuestView(
                quest.QuestId,
                quest.DisplayName,
                objective != null ? objective.Description : "Return to the quest giver",
                CategoryOf(quest),
                objective != null ? objective.Type : QuestObjectiveType.Talk,
                currentCount,
                objective != null ? objective.TargetCount : 1,
                status == QuestStatus.ReadyToTurnIn));
        }

        _trackedQuests.Sort(QuestTrackerOrdering.Compare);
        _tracker?.SetQuests(_trackedQuests);
    }

    private static QuestCategory CategoryOf(QuestDefinition quest)
    {
        if (quest.IsMainQuest)
            return QuestCategory.Main;
        return quest.IsDailyQuest ? QuestCategory.Daily : QuestCategory.Side;
    }

    private void RebuildList()
    {
        foreach (GameObject row in _rows)
            Destroy(row);
        _rows.Clear();

        if (_questManager?.Catalog == null)
        {
            _emptyText.gameObject.SetActive(true);
            return;
        }

        QuestDefinition first = null;
        foreach (QuestDefinition quest in _questManager.Catalog.AllQuests)
        {
            QuestStatus status = _questManager.GetStatus(quest.QuestId);
            if (status != QuestStatus.Active
                && status != QuestStatus.ReadyToTurnIn
                && status != QuestStatus.Completed)
            {
                continue;
            }

            QuestCategory category = CategoryOf(quest);
            if (_activeFilter.HasValue && category != _activeFilter.Value)
                continue;

            first ??= quest;
            GameObject row = Instantiate(_rowTemplate, _listContent);
            row.name = $"QuestRow_{quest.QuestId}";
            row.SetActive(true);
            row.transform.Find("Title").GetComponent<TMP_Text>().text = quest.DisplayName;
            row.transform.Find("Status").GetComponent<TMP_Text>().text = FormatStatus(status);
            TMP_Text categoryLabel = row.transform.Find("Category")?.GetComponent<TMP_Text>();
            if (categoryLabel != null)
                categoryLabel.text = category.ToString().ToUpperInvariant();
            Image categoryIcon = row.transform.Find("CategoryIcon")?.GetComponent<Image>();
            if (categoryIcon != null)
                categoryIcon.sprite = LoadCategorySprite(category);
            Transform pin = row.transform.Find("TrackedPin");
            if (pin != null)
                pin.gameObject.SetActive(_questManager.IsTracked(quest.QuestId));
            string questId = quest.QuestId;
            row.GetComponent<Button>().onClick.AddListener(() => SelectQuest(questId));
            _rows.Add(row);
        }

        _emptyText.gameObject.SetActive(_rows.Count == 0);
        if (string.IsNullOrEmpty(_selectedQuestId) && first != null)
            _selectedQuestId = first.QuestId;
    }

    private void SelectQuest(string questId)
    {
        _selectedQuestId = questId;
        RefreshDetails();
    }

    private void ResolveQuestLogPresentation()
    {
        Transform filters = _logRoot != null
            ? _logRoot.transform.Find("Window/QuestListPanel/Filters")
            : null;
        if (filters == null)
            return;

        _filterButtons = new[]
        {
            filters.Find("All")?.GetComponent<Button>(),
            filters.Find("Main")?.GetComponent<Button>(),
            filters.Find("Side")?.GetComponent<Button>(),
            filters.Find("Daily")?.GetComponent<Button>()
        };
    }

    private void BindFilterButtons()
    {
        if (_filterButtons == null) return;
        _filterButtons[0]?.onClick.AddListener(FilterAll);
        _filterButtons[1]?.onClick.AddListener(FilterMain);
        _filterButtons[2]?.onClick.AddListener(FilterSide);
        _filterButtons[3]?.onClick.AddListener(FilterDaily);
    }

    private void UnbindFilterButtons()
    {
        if (_filterButtons == null) return;
        _filterButtons[0]?.onClick.RemoveListener(FilterAll);
        _filterButtons[1]?.onClick.RemoveListener(FilterMain);
        _filterButtons[2]?.onClick.RemoveListener(FilterSide);
        _filterButtons[3]?.onClick.RemoveListener(FilterDaily);
    }

    private void FilterAll() => SetFilter(null);
    private void FilterMain() => SetFilter(QuestCategory.Main);
    private void FilterSide() => SetFilter(QuestCategory.Side);
    private void FilterDaily() => SetFilter(QuestCategory.Daily);

    private void SetFilter(QuestCategory? filter)
    {
        _activeFilter = filter;
        _selectedQuestId = null;
        RebuildList();
        RefreshDetails();
        SelectDefault();
    }

    private static Sprite LoadCategorySprite(QuestCategory category)
    {
        string path = category switch
        {
            QuestCategory.Main => "UI/Quest/Tracker1920/category_main",
            QuestCategory.Daily => "UI/Quest/Tracker1920/category_daily",
            _ => "UI/Quest/Tracker1920/category_side"
        };
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private void RefreshDetails()
    {
        if (_questManager?.Catalog == null
            || string.IsNullOrEmpty(_selectedQuestId)
            || !_questManager.Catalog.TryResolve(_selectedQuestId, out QuestDefinition quest))
        {
            _detailTitle.text = "QUEST";
            _detailStatus.text = string.Empty;
            _detailObjectives.text = "No accepted quests.";
            SetQuestActionVisibility(false, false);
            return;
        }

        QuestStatus status = _questManager.GetStatus(quest.QuestId);
        _detailTitle.text = quest.DisplayName;
        _detailStatus.text = FormatStatus(status);
        _detailObjectives.text = BuildObjectiveText(quest, compact: false);

        Transform detail = _detailTitle.transform.parent;
        Image categoryIcon = detail.Find("CategoryIcon")?.GetComponent<Image>();
        if (categoryIcon != null)
            categoryIcon.sprite = LoadCategorySprite(CategoryOf(quest));
        TMP_Text categoryLabel = detail.Find("CategoryLabel")?.GetComponent<TMP_Text>();
        if (categoryLabel != null)
            categoryLabel.text = CategoryOf(quest).ToString().ToUpperInvariant() + " QUEST";
        TMP_Text rewards = detail.Find("Rewards/RewardSummary")?.GetComponent<TMP_Text>();
        if (rewards != null)
            rewards.text = FormatRewards(quest.Rewards);
        Image objectiveIcon = detail.Find("ObjectiveIcon")?.GetComponent<Image>();
        if (objectiveIcon != null && quest.Objectives.Count > 0)
        {
            _questManager.TryGetProgress(quest.QuestId, out QuestProgressSnapshot progress);
            int index = Mathf.Clamp(progress.CurrentObjectiveIndex, 0, quest.Objectives.Count - 1);
            objectiveIcon.sprite = LoadObjectiveSprite(quest.Objectives[index].Type);
        }

        bool actionable = status == QuestStatus.Active || status == QuestStatus.ReadyToTurnIn;
        SetQuestActionVisibility(actionable, actionable && !string.IsNullOrEmpty(quest.GiverNpcId));
        if (_trackButtonLabel != null)
            _trackButtonLabel.text = _questManager.IsTracked(quest.QuestId) ? "UNTRACK QUEST" : "TRACK QUEST";
    }

    private static string FormatRewards(QuestRewardDefinition rewards)
    {
        if (rewards == null) return "No rewards";

        var parts = new List<string>();
        if (rewards.Gold > 0) parts.Add($"{rewards.Gold} GOLD");
        if (rewards.Experience > 0) parts.Add($"{rewards.Experience} EXP");
        foreach (QuestRewardItemEntry item in rewards.Items)
            parts.Add($"{item.Quantity}x {item.ItemId}");
        return parts.Count > 0 ? string.Join("     ", parts) : "No rewards";
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
        string path = "UI/Quest/Tracker1920/" + name;
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private void SetQuestActionVisibility(bool showTrack, bool showAbandon)
    {
        if (_trackButton != null)
            _trackButton.gameObject.SetActive(showTrack);
        if (_abandonButton != null)
            _abandonButton.gameObject.SetActive(showAbandon);
    }

    private void ToggleSelectedQuestTracking()
    {
        if (_questManager == null || string.IsNullOrEmpty(_selectedQuestId))
            return;

        if (_questManager.IsTracked(_selectedQuestId))
            _questManager.TryUntrackQuest(_selectedQuestId);
        else
            _questManager.TryTrackQuest(_selectedQuestId);
    }

    private void OpenAbandonConfirmation()
    {
        if (_questManager?.Catalog == null
            || string.IsNullOrEmpty(_selectedQuestId)
            || !_questManager.Catalog.TryResolve(_selectedQuestId, out QuestDefinition quest)
            || string.IsNullOrEmpty(quest.GiverNpcId))
        {
            return;
        }

        if (_abandonConfirmationMessage != null)
        {
            _abandonConfirmationMessage.text =
                $"Abandon {quest.DisplayName}?\nProgress will be lost. Return to the quest giver to accept it again.";
        }
        _abandonConfirmationRoot?.SetActive(true);
        if (EventSystem.current != null && _confirmAbandonButton != null)
            EventSystem.current.SetSelectedGameObject(_confirmAbandonButton.gameObject);
    }

    private void ConfirmAbandonQuest()
    {
        if (_questManager != null && !string.IsNullOrEmpty(_selectedQuestId))
            _questManager.TryAbandonQuest(_selectedQuestId);
    }

    private void CloseAbandonConfirmation()
    {
        _abandonConfirmationRoot?.SetActive(false);
    }

    private string BuildObjectiveText(QuestDefinition quest, bool compact)
    {
        bool hasProgress = _questManager.TryGetProgress(
            quest.QuestId,
            out QuestProgressSnapshot snapshot);
        int currentIndex = hasProgress ? snapshot.CurrentObjectiveIndex : -1;

        var builder = new StringBuilder();
        int compactIndex = currentIndex;
        if (compact
            && hasProgress
            && snapshot.Status == QuestStatus.ReadyToTurnIn
            && compactIndex == quest.Objectives.Count)
        {
            compactIndex--;
        }

        if (compact && compactIndex >= 0 && compactIndex < quest.Objectives.Count)
        {
            AppendObjective(builder, quest, snapshot, compactIndex, showCounter: true, showBullet: true);
            return builder.ToString();
        }

        for (int i = 0; i < quest.Objectives.Count; i++)
        {
            if (builder.Length > 0)
                builder.AppendLine();

            AppendObjective(
                builder,
                quest,
                snapshot,
                i,
                showCounter: hasProgress && i == currentIndex,
                showBullet: false);
        }
        return builder.ToString();
    }

    private static void AppendObjective(
        StringBuilder builder,
        QuestDefinition quest,
        QuestProgressSnapshot snapshot,
        int index,
        bool showCounter,
        bool showBullet)
    {
        QuestObjectiveDefinition objective = quest.Objectives[index];
        if (showBullet)
            builder.Append("- ");
        builder.Append(objective.Description);

        if (!showCounter || index >= snapshot.ObjectiveCounters.Count)
            return;

        builder.Append("  ")
            .Append(snapshot.ObjectiveCounters[index])
            .Append(" / ")
            .Append(objective.TargetCount);
    }

    private static string FormatStatus(QuestStatus status) => status switch
    {
        QuestStatus.ReadyToTurnIn => "READY TO TURN IN",
        QuestStatus.Completed => "COMPLETED",
        QuestStatus.Active => "IN PROGRESS",
        _ => status.ToString().ToUpperInvariant()
    };

    private void SelectDefault()
    {
        if (EventSystem.current == null)
            return;

        GameObject target = _rows.Count > 0 ? _rows[0] : _closeButton.gameObject;
        EventSystem.current.SetSelectedGameObject(target);
    }
}
