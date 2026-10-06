using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation only, opened/closed by UnifiedGameplayHudController via GameplayMenuPage.Map.
// Shows the static map snapshot baked by MapSnapshotBaker (Tools/ProjectGame2D/UI/Bake Map
// Snapshot) full-screen, scaled to "cover" the viewport (fills edge-to-edge, no letterbox bars,
// no distortion) with a Player icon overlay -- no live camera/RenderTexture. Zoom scales the
// image's RectTransform around its own center; a RectMask2D on the viewport clips the overflow.
// Once zoomed in, the scaled image is bigger than the viewport, so it also needs to be draggable
// (IDragHandler) to reach corners the zoom pushed outside the visible area -- panning is clamped
// so the image can never reveal empty space past its own edge.
public sealed class FullMapController : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private Image _mapImage;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private float _zoomStep = 0.12f;
    [SerializeField] private float _maxZoom = 3f;

    private Player _player;
    private Bounds _mapBounds;
    private bool _hasBounds;
    private float _zoom = 1f;
    private Canvas _canvas;

    private void OnEnable()
    {
        _hasBounds = MapWorldBounds.TryGet(out _mapBounds);
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        FitToViewport();
        _zoom = 1f;
        if (_mapImage != null)
            _mapImage.rectTransform.anchoredPosition = Vector2.zero;
        ApplyZoom();
        UpdateMarker();
    }

    private void Update()
    {
        if (!_hasBounds)
            _hasBounds = MapWorldBounds.TryGet(out _mapBounds);
        if (_player == null)
            _player = FindAnyObjectByType<Player>();
        UpdateMarker();
    }

    public void OnScroll(PointerEventData eventData)
    {
        float delta = eventData.scrollDelta.y;
        if (Mathf.Approximately(delta, 0f)) return;

        float factor = 1f + Mathf.Sign(delta) * _zoomStep;
        _zoom = Mathf.Clamp(_zoom * factor, 1f, _maxZoom);
        ApplyZoom();
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (_mapImage == null)
            return;

        float scaleFactor = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
        Vector2 delta = eventData.delta / scaleFactor;
        _mapImage.rectTransform.anchoredPosition = ClampPan(_mapImage.rectTransform.anchoredPosition + delta);
    }

    private void ApplyZoom()
    {
        if (_mapImage == null)
            return;

        _mapImage.rectTransform.localScale = Vector3.one * _zoom;
        // Zooming out can leave a previously valid pan position revealing empty space past the
        // image's (now smaller) edge -- re-clamp every time zoom changes, not just while dragging.
        _mapImage.rectTransform.anchoredPosition = ClampPan(_mapImage.rectTransform.anchoredPosition);
    }

    // The map image is scaled (not resized) around its own center, so its on-screen size is its
    // unscaled rect size times the current zoom. Panning past half the difference between that
    // scaled size and the viewport would pull the image's edge inside the viewport, showing empty
    // space -- clamp to keep the image edge-to-edge or beyond on every side.
    private Vector2 ClampPan(Vector2 position)
    {
        if (_mapImage == null || _viewport == null)
            return position;

        Vector2 imageSize = _mapImage.rectTransform.rect.size * _zoom;
        Vector2 viewportSize = _viewport.rect.size;
        Vector2 maxOffset = Vector2.Max(Vector2.zero, (imageSize - viewportSize) * 0.5f);
        return new Vector2(
            Mathf.Clamp(position.x, -maxOffset.x, maxOffset.x),
            Mathf.Clamp(position.y, -maxOffset.y, maxOffset.y));
    }

    private void FitToViewport()
    {
        // Cover: scale the sprite's native pixel size so it fills the viewport with no gaps on
        // either axis, cropping whichever axis has the smaller required scale.
        if (_mapImage == null || _mapImage.sprite == null || _viewport == null)
            return;

        Vector2 spriteSize = _mapImage.sprite.rect.size;
        Vector2 viewportSize = _viewport.rect.size;
        float scale = Mathf.Max(viewportSize.x / spriteSize.x, viewportSize.y / spriteSize.y);
        _mapImage.rectTransform.sizeDelta = spriteSize * scale;
    }

    private void UpdateMarker()
    {
        if (_player == null || !_hasBounds || _playerMarker == null || _mapImage == null)
            return;

        Vector2 normalized = MapWorldBounds.WorldToNormalized(_mapBounds, _player.transform.position);
        Vector2 size = _mapImage.rectTransform.rect.size;
        _playerMarker.anchoredPosition = new Vector2(
            (normalized.x - 0.5f) * size.x,
            (normalized.y - 0.5f) * size.y);
        _playerMarker.localEulerAngles = new Vector3(0f, 0f,
            MapWorldBounds.FacingToMarkerRotationZ(_player.FacingDirection));
    }
}
