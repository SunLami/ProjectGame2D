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

    private readonly List<GameObject> _rows = new();
    private QuestManager _questManager;
    private InputAction _questLogAction;
    private string _selectedQuestId;

    private void Awake()
    {
        if (_playerInput == null)
            _playerInput = Object.FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);

        _questLogAction = _playerInput != null
            ? _playerInput.actions.FindAction("Gameplay/QuestLog", false)
            : null;
    }

    private void OnEnable()
    {
        _closeButton.onClick.AddListener(CloseQuestLog);
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
        _closeButton.onClick.RemoveListener(CloseQuestLog);
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

    private void HandleGameStateChanged(GameStateChange change)
    {
        RefreshVisibility();
        if (IsQuestLogOpen())
            SelectDefault();
    }

    private void RefreshVisibility()
    {
        bool logOpen = IsQuestLogOpen();
        _logRoot.SetActive(logOpen);
        if (_trackerRoot != null)
            _trackerRoot.SetActive(!logOpen && FindTrackedQuest() != null);
    }

    private bool IsQuestLogOpen() =>
        GameStateManager.Instance != null
        && GameStateManager.Instance.CurrentState == GameState.GameplayMenu
        && GameStateManager.Instance.CurrentMenuPage == GameplayMenuPage.QuestLog;

    private void RefreshQuestPresentation()
    {
        QuestDefinition tracked = FindTrackedQuest();
        if (tracked != null)
        {
            _trackerTitle.text = tracked.DisplayName;
            _trackerObjectives.text = BuildObjectiveText(tracked, compact: true);
        }

        RebuildList();
        RefreshDetails();
        RefreshVisibility();
    }

    private QuestDefinition FindTrackedQuest()
    {
        if (_questManager?.Catalog == null)
            return null;

        QuestDefinition ready = null;
        foreach (QuestDefinition quest in _questManager.Catalog.AllQuests)
        {
            QuestStatus status = _questManager.GetStatus(quest.QuestId);
            if (status == QuestStatus.Active)
                return quest;
            if (status == QuestStatus.ReadyToTurnIn)
                ready = quest;
        }
        return ready;
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

            first ??= quest;
            GameObject row = Instantiate(_rowTemplate, _listContent);
            row.name = $"QuestRow_{quest.QuestId}";
            row.SetActive(true);
            row.transform.Find("Title").GetComponent<TMP_Text>().text = quest.DisplayName;
            row.transform.Find("Status").GetComponent<TMP_Text>().text = FormatStatus(status);
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

    private void RefreshDetails()
    {
        if (_questManager?.Catalog == null
            || string.IsNullOrEmpty(_selectedQuestId)
            || !_questManager.Catalog.TryResolve(_selectedQuestId, out QuestDefinition quest))
        {
            _detailTitle.text = "QUEST";
            _detailStatus.text = string.Empty;
            _detailObjectives.text = "No accepted quests.";
            return;
        }

        _detailTitle.text = quest.DisplayName;
        _detailStatus.text = FormatStatus(_questManager.GetStatus(quest.QuestId));
        _detailObjectives.text = BuildObjectiveText(quest, compact: false);
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
            AppendObjective(builder, quest, snapshot, compactIndex, showCounter: true);
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
                showCounter: hasProgress && i == currentIndex);
        }
        return builder.ToString();
    }

    private static void AppendObjective(
        StringBuilder builder,
        QuestDefinition quest,
        QuestProgressSnapshot snapshot,
        int index,
        bool showCounter)
    {
        QuestObjectiveDefinition objective = quest.Objectives[index];
        builder.Append("- ").Append(objective.Description);

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
