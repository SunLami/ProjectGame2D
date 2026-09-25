using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Dialogue-first NPC capability: talk, then optionally branch into Shop/Crafting via a
/// dialogue choice's outcome. Mirrors QuestNpcInteractionUI's proximity/interaction pattern but
/// routes the outcome to ShopCraftingUI instead of QuestNpcInteractionService.</summary>
public sealed class TraderNpcInteractionUI : MonoBehaviour
{
    public const string ShopOutcomeId = "commerce.shop";
    public const string CraftingOutcomeId = "commerce.crafting";

    [SerializeField] private string _npcId;
    [SerializeField] private string _stationTag;
    [SerializeField] private GameObject _promptRoot;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private Button _interactionButton;
    [SerializeField] private DialogueDefinition _dialogue;

    private readonly HashSet<Collider2D> _playerColliders = new();
    private PlayerInput _playerInput;

    private void OnEnable()
    {
        _interactionButton.onClick.AddListener(TryInteract);
        Refresh();
    }

    private void OnDisable()
    {
        _interactionButton.onClick.RemoveListener(TryInteract);
        _playerColliders.Clear();
        _playerInput = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInput input = other.GetComponentInParent<PlayerInput>();
        if (input == null)
            return;

        _playerColliders.Add(other);
        _playerInput = input;
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

    public void TryInteract()
    {
        if (_playerColliders.Count == 0 || _dialogue == null || DialogueUI.Instance == null)
            return;

        if (DialogueUI.Instance.Open(_dialogue, HandleOutcome))
            _promptRoot.SetActive(false);
    }

    private void HandleOutcome(string outcomeId)
    {
        if (_playerInput != null)
        {
            if (outcomeId == ShopOutcomeId)
                ShopCraftingUI.Instance?.OpenShop(_npcId, _playerInput);
            else if (outcomeId == CraftingOutcomeId)
                ShopCraftingUI.Instance?.OpenCrafting(_npcId, _stationTag, _playerInput);
        }
        Refresh();
    }

    private void Refresh()
    {
        bool visible = _playerColliders.Count > 0
            && GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Playing;
        _promptRoot.SetActive(visible);
        if (visible)
            _promptText.text = "LEFT CLICK — TALK";
    }
}
