using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Minimal scene NPC capability that routes quest actions through QuestNpcInteractionService.</summary>
public sealed class QuestNpcInteractionUI : MonoBehaviour
{
    [SerializeField] private string _npcId;
    [SerializeField] private TMP_Text _markerText;
    [SerializeField] private GameObject _promptRoot;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private TMP_Text _feedbackText;
    [SerializeField] private Button _interactionButton;

    [Tooltip("Fallback dialogue used when no entry in _questDialogues matches the quest currently "
        + "being offered/turned in at this NPC (or when there is none, e.g. all quests exhausted).")]
    [SerializeField] private DialogueDefinition _dialogue;

    [System.Serializable]
    private struct QuestDialogueEntry
    {
        public string questId;
        public DialogueDefinition dialogue;
    }

    [Tooltip("Per-quest dialogue override: an NPC met across several quest stages (e.g. first "
        + "meeting vs. a later quest offer) can show different lines at each stage instead of one "
        + "flat line every time. Matched against whichever quest is currently offered/ready to turn "
        + "in at this NPC; falls back to _dialogue if nothing matches.")]
    [SerializeField] private QuestDialogueEntry[] _questDialogues;

    private readonly HashSet<Collider2D> _playerColliders = new();
    private QuestManager _questManager;
    private QuestNpcInteractionService _service;

    private void OnEnable()
    {
        _interactionButton.onClick.AddListener(TryInteract);
        BindQuestManager();
        QuestNpcRegistry.Register(_npcId, transform);
    }

    private void Start()
    {
        if (_service == null)
            BindQuestManager();
    }

    private void OnDisable()
    {
        _interactionButton.onClick.RemoveListener(TryInteract);
        UnbindQuestManager();
        _playerColliders.Clear();
        QuestNpcRegistry.Unregister(_npcId, transform);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInput playerInput = other.GetComponentInParent<PlayerInput>();
        if (playerInput == null)
            return;

        _playerColliders.Add(other);
        Refresh();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerColliders.Remove(other))
            return;

        Refresh();
    }

    private void BindQuestManager()
    {
        QuestManager manager = QuestManager.Instance;
        if (_questManager != manager)
        {
            UnbindQuestManager();
            _questManager = manager;
            if (_questManager != null)
            {
                _service = new QuestNpcInteractionService(_questManager);
                _questManager.QuestAccepted += HandleQuestChanged;
                _questManager.QuestProgressChanged += HandleQuestChanged;
                _questManager.QuestCompleted += HandleQuestChanged;
                _questManager.MainQuestUnlocked += HandleMainQuestUnlocked;
            }
        }
        Refresh();
    }

    private void UnbindQuestManager()
    {
        if (_questManager != null)
        {
            _questManager.QuestAccepted -= HandleQuestChanged;
            _questManager.QuestProgressChanged -= HandleQuestChanged;
            _questManager.QuestCompleted -= HandleQuestChanged;
            _questManager.MainQuestUnlocked -= HandleMainQuestUnlocked;
        }
        _questManager = null;
        _service = null;
    }

    private void HandleQuestChanged(string questId) => Refresh();
    private void HandleMainQuestUnlocked() => Refresh();

    public void TryInteract()
    {
        if (_service == null || _playerColliders.Count == 0)
            return;

        // Accept a freshly offered quest before opening its dialogue, not after: ReportConversation
        // below (fired once the dialogue closes) only progresses a Talk objective on a quest that
        // is already Active. Reporting first and accepting second -- the previous order -- meant a
        // fresh single-Talk-objective quest's own greeting conversation could never satisfy itself;
        // the player would need to trigger the identical dialogue a second time before the first
        // report actually counted.
        if (_service.TryGetOfferedQuest(_npcId, out QuestDefinition offered)
            && _service.TryAcceptQuest(_npcId, offered.QuestId))
        {
            _feedbackText.text = $"Accepted: {offered.DisplayName}";
        }

        string contextQuestId = _service.TryGetTurnInQuest(_npcId, out QuestDefinition turnIn)
            ? turnIn.QuestId
            : offered != null ? offered.QuestId : null;

        DialogueDefinition dialogue = ResolveDialogueFor(contextQuestId);
        if (dialogue != null && DialogueUI.Instance != null
            && DialogueUI.Instance.Open(dialogue, CompleteDialogueInteraction))
        {
            _promptRoot.SetActive(false);
            return;
        }

        PerformQuestInteraction();
    }

    private DialogueDefinition ResolveDialogueFor(string questId)
    {
        if (!string.IsNullOrEmpty(questId) && _questDialogues != null)
        {
            foreach (QuestDialogueEntry entry in _questDialogues)
            {
                if (entry.dialogue != null && string.Equals(entry.questId, questId, System.StringComparison.Ordinal))
                    return entry.dialogue;
            }
        }
        return _dialogue;
    }

    private void CompleteDialogueInteraction(string outcomeId)
    {
        _service?.ReportConversation(_npcId, outcomeId);
        PerformQuestInteraction();
    }

    private void PerformQuestInteraction()
    {
        if (_service == null)
            return;

        if (_service.TryGetTurnInQuest(_npcId, out QuestDefinition turnIn))
        {
            if (_service.TryTurnIn(_npcId, turnIn.QuestId, out QuestTurnInResult result))
                _feedbackText.text = $"Completed: {turnIn.DisplayName}";
            else
                _feedbackText.text = FormatTurnInFailure(result);
        }
        else if (_service.TryGetOfferedQuest(_npcId, out QuestDefinition offered))
        {
            _feedbackText.text = _service.TryAcceptQuest(_npcId, offered.QuestId)
                ? $"Accepted: {offered.DisplayName}"
                : "Quest is no longer available.";
        }

        Refresh();
    }

    private void Refresh()
    {
        if (_service == null)
        {
            _markerText.text = string.Empty;
            _promptRoot.SetActive(false);
            return;
        }

        bool canTurnIn = _service.TryGetTurnInQuest(_npcId, out QuestDefinition turnIn);
        QuestDefinition offered = null;
        bool canOffer = !canTurnIn && _service.TryGetOfferedQuest(_npcId, out offered);
        _markerText.text = canTurnIn ? "?" : canOffer ? "!" : string.Empty;

        bool showPrompt = _playerColliders.Count > 0
            && GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Playing
            && (canTurnIn || canOffer);
        _promptRoot.SetActive(showPrompt);
        if (showPrompt)
        {
            QuestDefinition quest = canTurnIn ? turnIn : offered;
            _promptText.text = canTurnIn
                ? $"LEFT CLICK — TURN IN {quest.DisplayName}"
                : $"LEFT CLICK — ACCEPT {quest.DisplayName}";
        }
    }

    private static string FormatTurnInFailure(QuestTurnInResult result) => result switch
    {
        QuestTurnInResult.ObjectivesIncomplete => "Objectives are not complete.",
        QuestTurnInResult.InsufficientInventoryCapacity => "Not enough inventory space.",
        QuestTurnInResult.AlreadyCompleted => "Quest was already completed.",
        QuestTurnInResult.QuestNotFound => "This NPC cannot turn in that quest.",
        _ => "Unable to turn in quest."
    };
}
