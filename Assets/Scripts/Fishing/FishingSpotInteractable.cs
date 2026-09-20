using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class FishingSpotInteractable : MonoBehaviour
{
    [SerializeField] private FishingSpotDefinition _definition;
    [SerializeField] private FishingMinigameController _controller;

    public bool IsAvailable => _definition != null
        && (_controller != null || FishingMinigameController.Instance != null)
        && (FishingMinigameController.Instance == null || !FishingMinigameController.Instance.IsSessionActive);

    public bool TryBeginFishing()
    {
        FishingMinigameController controller = _controller != null
            ? _controller
            : FishingMinigameController.Instance;
        return controller != null && controller.TryBeginFishing(this, _definition);
    }

    private void Awake()
    {
        Collider2D interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        if (_controller == null)
            _controller = FindAnyObjectByType<FishingMinigameController>();
    }
}
