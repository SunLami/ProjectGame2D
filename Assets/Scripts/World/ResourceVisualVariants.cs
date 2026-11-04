using UnityEngine;

/// <summary>Presentation only; changing the tree sprite never changes its persistent identity or loot.</summary>
public sealed class ResourceVisualVariants : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite[] _variants;
    private void Awake()
    {
        if (_renderer != null && _variants != null && _variants.Length > 0)
            _renderer.sprite = _variants[Random.Range(0, _variants.Length)];
    }
}
