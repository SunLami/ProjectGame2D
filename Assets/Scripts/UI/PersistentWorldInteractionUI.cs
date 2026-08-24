using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Presentation and proximity input for one persistent chest, pickup, or resource node.</summary>
public sealed class PersistentWorldInteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject _promptRoot;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private TMP_Text _feedbackText;
    [SerializeField] private GameObject _availableVisual;

    private readonly HashSet<Collider2D> _playerColliders = new();
    private ChestInteractable _chest;
    private UniquePickupInteractable _pickup;
    private ResourceNodeInteractable _resource;
    private InputAction _interactAction;
    private bool? _lastAvailable;

    private void Awake()
    {
        _chest = GetComponent<ChestInteractable>();
        _pickup = GetComponent<UniquePickupInteractable>();
        _resource = GetComponent<ResourceNodeInteractable>();
    }

    private void OnEnable() => Refresh();

    private void Update()
    {
        bool available = IsAvailable();
        if (_lastAvailable != available)
            Refresh();
    }

    private void OnDisable()
    {
        SetPlayerInput(null);
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

    public void TryInteract()
    {
        if (_playerColliders.Count == 0)
            return;

        bool succeeded;
        bool granted;
        if (_chest != null)
        {
            succeeded = _chest.TryOpen(out granted);
            _feedbackText.text = succeeded && granted ? "Chest opened." : "Chest cannot be opened.";
        }
        else if (_pickup != null)
        {
            succeeded = _pickup.TryCollect(out granted);
            if (!succeeded || !granted)
                _feedbackText.text = "Inventory is full.";
        }
        else if (_resource != null)
        {
            succeeded = _resource.TryHarvest(out granted);
            _feedbackText.text = succeeded && granted ? "Resources gathered." : "Resource is depleted.";
        }

        Refresh();
    }

    private void SetPlayerInput(PlayerInput playerInput)
    {
        if (_interactAction != null)
            _interactAction.performed -= HandleInteract;

        _interactAction = playerInput != null
            ? playerInput.actions.FindAction("Gameplay/Interact", false)
            : null;

        if (_interactAction != null)
            _interactAction.performed += HandleInteract;
    }

    private void HandleInteract(InputAction.CallbackContext context) => TryInteract();

    private void Refresh()
    {
        bool available = IsAvailable();
        _lastAvailable = available;

        if (_availableVisual != null)
            _availableVisual.SetActive(available);

        bool showPrompt = available && _playerColliders.Count > 0;
        if (_promptRoot != null)
            _promptRoot.SetActive(showPrompt);
        if (showPrompt && _promptText != null)
            _promptText.text = _chest != null ? "E / A  OPEN CHEST"
                : _pickup != null ? "E / A  COLLECT RELIC"
                : "E / A  HARVEST WOOD";
    }

    private bool IsAvailable() => _chest != null ? !_chest.IsOpened
        : _pickup != null ? !_pickup.IsCollected
        : _resource != null && _resource.IsAvailable;
}
