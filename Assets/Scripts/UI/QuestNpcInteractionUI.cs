using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Scene NPC capability that routes quest actions through QuestNpcInteractionService, and
/// also opens Shop/Crafting/Storage when a dialogue choice resolves to one of those outcomes (same
/// outcome ids as TraderNpcInteractionUI) -- so an NPC that both gives/turns in quests and runs a
/// shop (e.g. Cuthbert the Mixologist selling the drinks a quest asks for) needs only this one
/// interaction component instead of two competing for the same click/trigger.</summary>
public sealed class QuestNpcInteractionUI : MonoBehaviour
{
    private const string AvailableMarkerResource = "UI/Quest/Markers/quest_available_marker";
    private const string TurnInMarkerResource = "UI/Quest/Markers/quest_turnin_marker";
    private const string MarkerImageObjectName = "QuestMarkerImage";
    private const float MarkerImageVerticalOffset = -24f;
    private static readonly Vector2 MarkerSize = new(96f, 96f);

    [SerializeField] private string _npcId;

    [Tooltip("Required station tag for the Crafting commerce outcome (CraftingOutcomeId); unused "
        + "for NPCs whose dialogue never branches into crafting.")]
    [SerializeField] private string _stationTag;

    [Tooltip("Optional questId that must be Completed before the Crafting outcome opens this NPC's "
        + "station (Act I: Merric/Smithing after quest.act1.ring_of_forge, Agnes/Black Arts after "
        + "quest.act1.far_from_your_kind). Empty = always open.")]
    [SerializeField] private string _craftingUnlockQuestId;
    [Tooltip("Shown instead of the crafting window while _craftingUnlockQuestId is not Completed.")]
    [SerializeField] private DialogueDefinition _lockedCraftingDialogue;

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
        [Tooltip("Shown while the quest is Available here (the offer).")]
        public DialogueDefinition dialogue;
        [Tooltip("Optional: shown while the quest is ReadyToTurnIn here. Falls back to dialogue.")]
        public DialogueDefinition turnInDialogue;
    }

    [Tooltip("Per-quest dialogue override: an NPC met across several quest stages (e.g. first "
        + "meeting vs. a later quest offer) can show different lines at each stage instead of one "
        + "flat line every time. Matched against whichever quest is currently offered/ready to turn "
        + "in at this NPC; falls back to _dialogue if nothing matches.")]
    [SerializeField] private QuestDialogueEntry[] _questDialogues;

    public string NpcId => _npcId;

    private readonly HashSet<Collider2D> _playerColliders = new();
    private PlayerInput _playerInput;
    private QuestManager _questManager;
    private QuestNpcInteractionService _service;
    private Image _markerImage;
    private Sprite _availableMarker;
    private Sprite _turnInMarker;

    private void OnEnable()
    {
        InitializeMarkerPresentation();
        HideFeedbackText();
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
        _playerInput = playerInput;
        Refresh();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerColliders.Remove(other))
            return;

        if (_playerColliders.Count == 0)
            _playerInput = null;
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

        bool canTurnIn = _service.TryGetTurnInQuest(_npcId, out QuestDefinition turnIn);
        QuestDefinition offered = null;
        bool canOffer = !canTurnIn && _service.TryGetOfferedQuest(_npcId, out offered);

        string contextQuestId = canTurnIn ? turnIn.QuestId : canOffer ? offered.QuestId : null;
        DialogueDefinition dialogue = ResolveDialogueFor(contextQuestId, canTurnIn);

        // Neither an offered quest's acceptance nor a ready quest's turn-in happens until the
        // dialogue actually closes (CompleteDialogueInteraction) -- an offer then hands off to
        // QuestAcceptPopupUI for an explicit Accept/Decline instead of committing immediately.
        // NPCs with no dialogue configured for this context fall back to the instant behavior below.
        if (dialogue != null && DialogueUI.Instance != null
            && DialogueUI.Instance.Open(dialogue, outcomeId => CompleteDialogueInteraction(outcomeId, offered)))
        {
            _promptRoot.SetActive(false);
            return;
        }

        if (!TryPerformTurnIn())
            TryPerformAccept(offered);
        Refresh();
    }

    private DialogueDefinition ResolveDialogueFor(string questId, bool turnIn)
    {
        if (!string.IsNullOrEmpty(questId) && _questDialogues != null)
        {
            foreach (QuestDialogueEntry entry in _questDialogues)
            {
                if (!string.Equals(entry.questId, questId, System.StringComparison.Ordinal))
                    continue;
                if (turnIn && entry.turnInDialogue != null)
                    return entry.turnInDialogue;
                if (entry.dialogue != null)
                    return entry.dialogue;
            }
        }
        return _dialogue;
    }

    /// <summary>True when the Crafting outcome would currently open this NPC's station.</summary>
    public bool IsCraftingUnlocked =>
        string.IsNullOrEmpty(_craftingUnlockQuestId)
        || (_questManager != null && _questManager.GetStatus(_craftingUnlockQuestId) == QuestStatus.Completed);

    private void CompleteDialogueInteraction(string outcomeId, QuestDefinition offered)
    {
        if (TryHandleServiceOutcome(outcomeId))
        {
            Refresh();
            return;
        }

        if (offered != null && QuestAcceptPopupUI.Instance != null
            && QuestAcceptPopupUI.Instance.Open(offered, accepted => HandleQuestDecision(offered, accepted, outcomeId)))
        {
            return;
        }

        // No popup available (not wired into this scene yet): fall back to accepting immediately,
        // same as before the popup existed.
        if (offered != null)
            TryPerformAccept(offered);

        _service?.ReportConversation(_npcId, outcomeId);

        // Turning in is deliberately NOT chained onto the same interaction that just accepted
        // offered -- every giver quest must have its own "come back and hand it in" visit, even
        // one whose only objective is talking to this same NPC (e.g. new_journey): accepting can
        // still satisfy that Talk objective in this conversation (ReadyToTurnIn), but the reward
        // only grants on a later, separate TryInteract call where offered is null. Quests with no
        // giver (e.g. back_to_fields) are unaffected -- they rely on QuestManager's own AutoTurnIn
        // completion instead of ever reaching this method.
        if (offered == null)
            TryPerformTurnIn();
        Refresh();
    }

    private void HandleQuestDecision(QuestDefinition offered, bool accepted, string outcomeId)
    {
        if (TryHandleServiceOutcome(outcomeId))
        {
            Refresh();
            return;
        }

        // Accept before reporting the conversation, not after: a fresh single-Talk-objective quest
        // (e.g. "talk to the trainer") needs to already be Active for this same conversation to
        // satisfy it, so its greeting dialogue completes the quest in one visit instead of needing
        // a second identical conversation.
        if (accepted)
            TryPerformAccept(offered);

        _service?.ReportConversation(_npcId, outcomeId);

        // See the matching comment in CompleteDialogueInteraction: skip turn-in here so accepting
        // and turning in are always two distinct NPC interactions, never one.
        if (offered == null)
            TryPerformTurnIn();
        Refresh();
    }

    // Mirrors TraderNpcInteractionUI.HandleOutcome -- a dialogue choice can open a service UI instead of reporting quest talk.
    private bool TryHandleServiceOutcome(string outcomeId)
    {
        if (_playerInput == null)
            return false;

        if (outcomeId == TraderNpcInteractionUI.ShopOutcomeId)
            ShopCraftingUI.Instance?.OpenShop(_npcId, _playerInput);
        else if (outcomeId == TraderNpcInteractionUI.CraftingOutcomeId)
        {
            // A locked station must never fall through silently: show the authored locked line, or
            // if none is wired, the NPC feedback text, so an early attempt always gets a reason.
            if (IsCraftingUnlocked)
                ShopCraftingUI.Instance?.OpenCrafting(_npcId, _stationTag, _playerInput);
            else if (_lockedCraftingDialogue == null || DialogueUI.Instance == null
                     || !DialogueUI.Instance.Open(_lockedCraftingDialogue, _ => Refresh()))
                Debug.LogWarning($"{name}: crafting is locked until '{_craftingUnlockQuestId}' is completed, "
                    + "but no locked-crafting dialogue could be shown.", this);
        }
        else if (outcomeId == TraderNpcInteractionUI.StorageOutcomeId)
            FarmStorageUI.Instance?.Open(_playerInput);
        else if (outcomeId == SkillPanelUI.TrainingOutcomeId)
            SkillPanelUI.OpenFromTrainer();
        else
            return false;

        return true;
    }

    private bool TryPerformTurnIn()
    {
        if (_service == null || !_service.TryGetTurnInQuest(_npcId, out QuestDefinition turnIn))
            return false;

        _service.TryTurnIn(_npcId, turnIn.QuestId, out _);
        return true;
    }

    private void TryPerformAccept(QuestDefinition offered)
    {
        if (_service == null || offered == null)
            return;

        _service.TryAcceptQuest(_npcId, offered.QuestId);
    }

    private void Refresh()
    {
        if (_service == null)
        {
            SetMarker(null, string.Empty);
            _promptRoot.SetActive(false);
            return;
        }

        bool canTurnIn = _service.TryGetTurnInQuest(_npcId, out QuestDefinition turnIn);
        QuestDefinition offered = null;
        bool canOffer = !canTurnIn && _service.TryGetOfferedQuest(_npcId, out offered);
        SetMarker(
            canTurnIn ? _turnInMarker : canOffer ? _availableMarker : null,
            canTurnIn ? "?" : canOffer ? "!" : string.Empty);

        // The "LEFT CLICK -- ACCEPT/TURN IN <quest>" prompt bubble no longer shows (same call as
        // TraderNpcInteractionUI.Refresh(), for the same reason: the Talk cursor + HoverOutline
        // (D-030/D-055) already say this NPC is interactable). The "?"/"!" marker above their head
        // stays -- it's the always-visible quest-availability indicator, not a hover prompt.
        _promptRoot.SetActive(false);
    }

    private void InitializeMarkerPresentation()
    {
        if (_markerText == null)
            return;

        _markerText.rectTransform.sizeDelta = MarkerSize;
        _markerText.fontSize = 56f;
        _markerText.alignment = TextAlignmentOptions.Center;

        Transform imageTransform = _markerText.transform.Find(MarkerImageObjectName);
        if (imageTransform == null)
        {
            var imageObject = new GameObject(
                MarkerImageObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageTransform = imageObject.transform;
            imageTransform.SetParent(_markerText.transform, false);
        }

        RectTransform imageRect = (RectTransform)imageTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        imageRect.anchoredPosition = new Vector2(0f, MarkerImageVerticalOffset);

        _markerImage = imageTransform.GetComponent<Image>();

        _markerImage.raycastTarget = false;
        _markerImage.preserveAspect = true;
        _availableMarker = Resources.Load<Sprite>(AvailableMarkerResource);
        _turnInMarker = Resources.Load<Sprite>(TurnInMarkerResource);
    }

    private void SetMarker(Sprite sprite, string fallbackText)
    {
        bool useSprite = sprite != null;
        if (_markerImage != null)
        {
            _markerImage.sprite = sprite;
            _markerImage.enabled = useSprite;
        }

        if (_markerText != null)
        {
            _markerText.text = useSprite ? string.Empty : fallbackText;
            _markerText.enabled = !useSprite && !string.IsNullOrEmpty(fallbackText);
        }
    }

    private void HideFeedbackText()
    {
        if (_feedbackText != null)
        {
            _feedbackText.text = string.Empty;
            _feedbackText.gameObject.SetActive(false);
        }
    }
}
