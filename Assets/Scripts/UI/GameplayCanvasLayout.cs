using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class GameplayCanvasLayout : MonoBehaviour
{
    // The HUD and older overlays were authored at an 800-unit canvas width.
    private const float LegacyScale = 1920f / 800f;
    private RectTransform _canvasRect;
    private RectTransform _content;

    private void Awake()
    {
        _canvasRect = (RectTransform)transform;
        var content = new GameObject("GameplayContent", typeof(RectTransform));
        _content = (RectTransform)content.transform;
        _content.SetParent(_canvasRect, false);
        _content.anchorMin = _content.anchorMax = new Vector2(0.5f, 0.5f);
        _content.localScale = new Vector3(LegacyScale, LegacyScale, 1f);
        ResizeContent();

        // Preserve sibling order and local layout. These objects are still authored directly
        // under GameplayUIRoot so existing prefab builders and scene overrides remain valid.
        while (_canvasRect.childCount > 1)
            _canvasRect.GetChild(0).SetParent(_content, false);
    }

    private void OnRectTransformDimensionsChange() => ResizeContent();

    private void ResizeContent()
    {
        if (_content == null) return;
        _content.sizeDelta = _canvasRect.rect.size / LegacyScale;
    }
}
