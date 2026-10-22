using UnityEngine;
using UnityEngine.UI;

// Keeps the existing Icon Image/reference as the data source; the motif is an independent layer.
[RequireComponent(typeof(Image))]
public sealed class SkillIconView : MonoBehaviour
{
    public enum SocketShape { Diamond, Hexagon }

    [SerializeField] private SkillIconVisualCatalog _visuals;
    [SerializeField] private Image _foreground;
    [SerializeField] private SocketShape _shape;
    [SerializeField, Range(.5f, .98f)] private float _safeFill = .90f;
    private Image _source;
    private Sprite _lastSprite;
    private Vector2 _lastSize;
    private SkillIconVisualCatalog.Entry _entry;

    public Sprite ForegroundSprite => _entry?.foreground;

    public void Configure(SkillIconVisualCatalog visuals, Image foreground, SocketShape shape)
    {
        _visuals = visuals;
        _foreground = foreground;
        _shape = shape;
        _lastSprite = null;
        Refresh();
    }

    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (_source == null) _source = GetComponent<Image>();
        if (_foreground == null || _visuals == null) return;
        Vector2 size = _source.rectTransform.rect.size;
        if (_lastSprite != _source.sprite || _lastSize != size || _entry == null)
        {
            _lastSprite = _source.sprite;
            _lastSize = size;
            _entry = _visuals.Resolve(_lastSprite);
            _foreground.sprite = _entry?.foreground;
            if (_entry != null && size.x > 0f && size.y > 0f) Fit(size);
        }
        _foreground.enabled = _source.enabled && _entry?.foreground != null;
        _foreground.color = _source.color;
    }

    private void Fit(Vector2 size)
    {
        float metric = 0f;
        // Hull coordinates are relative to the cropped sprite center, divided by its max edge.
        // Test every hull point against the actual socket polygon, including diagonal trails.
        foreach (Vector2 point in _entry.hull)
        {
            float x = Mathf.Abs(point.x) * 2f / size.x;
            float y = Mathf.Abs(point.y) * 2f / size.y;
            float value = _shape == SocketShape.Diamond ? x + y : Mathf.Max(y, x + .5f * y);
            metric = Mathf.Max(metric, value);
        }
        if (metric <= 0f) return;
        Vector2 spriteSize = _entry.foreground.rect.size;
        float extent = Mathf.Max(spriteSize.x, spriteSize.y);
        if (extent <= 0f) return;
        RectTransform rect = _foreground.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = spriteSize / extent * (_safeFill / metric);
    }
}
