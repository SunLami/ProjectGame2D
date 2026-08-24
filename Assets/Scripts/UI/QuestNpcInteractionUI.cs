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

    private readonly HashSet<Collider2D> _playerColliders = new();
    private QuestManager _questManager;
    private QuestNpcInteractionService _service;
    private InputAction _interactAction;

    private void OnEnable()
    {
        _interactionButton.onClick.AddListener(TryInteract);
        BindQuestManager();
    }

    private void Start()
    {
        if (_service == null)
            BindQuestManager();
    }

    private void OnDisable()
    {
        _interactionButton.onClick.RemoveListener(TryInteract);
        SetPlayerInput(null);
        UnbindQuestManager();
        _playerColliders.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInput playerInput = other.GetComponentInParent<PlayerInput>();
        if (playerInput == null)
            return;

        _playerColliders.Add(other);
        SetPlayerInput(playerInput);
        Refresh();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerColliders.Remove(other))
            return;

        if (_playerColliders.Count == 0)
            SetPlayerInput(null);
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

    private void SetPlayerInput(PlayerInput playerInput)
    {
        if (_interactAction != null)
            _interactAction.performed -= HandleInteractPerformed;

        _interactAction = playerInput != null
            ? playerInput.actions.FindAction("Gameplay/Interact", false)
            : null;

        if (_interactAction != null)
            _interactAction.performed += HandleInteractPerformed;
    }

    private void HandleInteractPerformed(InputAction.CallbackContext context) => TryInteract();
    private void HandleQuestChanged(string questId) => Refresh();
    private void HandleMainQuestUnlocked() => Refresh();

    public void TryInteract()
    {
        if (_service == null || _playerColliders.Count == 0)
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

        bool showPrompt = _playerColliders.Count > 0 && (canTurnIn || canOffer);
        _promptRoot.SetActive(showPrompt);
        if (showPrompt)
        {
            QuestDefinition quest = canTurnIn ? turnIn : offered;
            _promptText.text = canTurnIn
                ? $"HOLD E — TURN IN {quest.DisplayName}"
                : $"HOLD E — ACCEPT {quest.DisplayName}";
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
