using UnityEngine;

/// <summary>Generic hover-highlight for any interactable: swaps its SpriteRenderer's material to
/// HoverOutline.shader (a thin white silhouette outline, sprite pixels otherwise untouched) while
/// GameCursorManager reports the pointer over it, same swap-a-material pattern as
/// ResourceNodeInteractable's white-flash hit feedback. Presentation only -- owns no interaction
/// logic. Add this component to any interactable's sprite GameObject (NPC, fishing spot, resource
/// node, farm plot, chest, pickup...) and GameCursorManager picks it up automatically via
/// GetComponentInParent, no code changes needed there.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class HoverOutline : MonoBehaviour
{
    private static Material _sharedOutlineMaterial;

    private SpriteRenderer _renderer;
    private Material _originalMaterial;
    private bool _highlighted;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _originalMaterial = _renderer.sharedMaterial;
        _sharedOutlineMaterial ??= Resources.Load<Material>("World/HoverOutline");
    }

    public void SetHighlighted(bool highlighted)
    {
        if (_highlighted == highlighted || _renderer == null)
            return;

        _highlighted = highlighted;
        _renderer.sharedMaterial = highlighted && _sharedOutlineMaterial != null
            ? _sharedOutlineMaterial
            : _originalMaterial;
    }

    private void OnDisable() => SetHighlighted(false);
}
