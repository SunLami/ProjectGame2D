using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Cinemachine;

// Presentation only, opened/closed by UnifiedGameplayHudController via GameplayMenuPage.Map.
// Supported scenes use an orthographic live feed. Other maps use the authored snapshot.
[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
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
    private LiveMapCamera _liveMap;
    private Camera _liveSource;
    private RawImage _liveImage;
    private Vector2 _liveCenter;
    private Vector2 _liveViewportSize;

    // Scene overlays and popup canvases can sort above the shared gameplay canvas.
    // Give only the fullscreen map its own draw layer.
    public const int FullMapSortingOrder = 2000;

    private void Awake()
    {
        if (!TryGetComponent(out _canvas))
            _canvas = gameObject.AddComponent<Canvas>();
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = FullMapSortingOrder;
        if (!TryGetComponent<GraphicRaycaster>(out _))
            gameObject.AddComponent<GraphicRaycaster>();
    }

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

    private void OnDisable()
    {
        ReleaseLiveMap();
        _liveSource = null;
    }

    private void LateUpdate()
    {
        if (_player == null)
            _player = FindAnyObjectByType<Player>();

        if (_mapImage != null && _viewport != null
            && LiveMapCamera.TryGetSource(out Camera source, out Bounds bounds))
        {
            UpdateLiveMap(source, bounds);
            return;
        }
        if (_liveMap != null)
        {
            ReleaseLiveMap();
            FitToViewport();
            ApplyZoom();
        }
        if (!_hasBounds)
            _hasBounds = MapWorldBounds.TryGet(out _mapBounds);
        UpdateMarker();
    }

    private void UpdateLiveMap(Camera source, Bounds bounds)
    {
        Vector2 size = _viewport.rect.size;
        if (size.x <= 0f || size.y <= 0f) return;
        if (_liveSource != source)
        {
            ReleaseLiveMap();
            _liveSource = source;
        }
        if (_liveMap == null || size != _liveViewportSize)
        {
            bool firstFrame = _liveMap == null;
            ReleaseLiveMap();
            _liveViewportSize = size;
            int width = Mathf.Clamp(Mathf.RoundToInt(576f * size.x / size.y), 256, 1920);
            _liveMap = new LiveMapCamera(source, OutputChannels.Channel15, width, 576);
            if (firstFrame)
                _liveCenter = bounds.center;

            var surface = new GameObject("LiveMapView", typeof(RectTransform), typeof(RawImage));
            _liveImage = surface.GetComponent<RawImage>();
            _liveImage.texture = _liveMap.Texture;
            _liveImage.raycastTarget = false;
            var rect = _liveImage.rectTransform;
            rect.SetParent(_mapImage.rectTransform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            _mapImage.enabled = false;
            var mapRect = _mapImage.rectTransform;
            mapRect.sizeDelta = size;
            mapRect.localScale = Vector3.one;
            mapRect.anchoredPosition = Vector2.zero;
        }
        _mapBounds = bounds;
        _hasBounds = true;
        float ortho = LiveOrthographicSize();
        _liveCenter = LiveMapCamera.ClampCenter(_liveCenter, bounds, ortho, _liveMap.Aspect);
        _liveMap.Frame(_liveCenter, ortho, source.transform.position.z);
        if (_player != null)
            PositionMarker(_liveMap.Project(_player.transform.position));
    }

    private float LiveOrthographicSize() =>
        Mathf.Max(_mapBounds.extents.y, _mapBounds.extents.x / _liveMap.Aspect) / _zoom;

    private void ReleaseLiveMap()
    {
        if (_liveMap == null) return;
        if (_liveImage != null)
        {
            _liveImage.gameObject.SetActive(false);
            Destroy(_liveImage.gameObject);
        }
        _liveMap.Dispose();
        _liveMap = null;
        if (_mapImage != null)
            _mapImage.enabled = true;
    }

    public void OnScroll(PointerEventData eventData)
    {
        float delta = eventData.scrollDelta.y;
        if (Mathf.Approximately(delta, 0f)) return;

        float factor = 1f + Mathf.Sign(delta) * _zoomStep;
        _zoom = Mathf.Clamp(_zoom * factor, 1f, _maxZoom);
        if (_liveMap == null)
            ApplyZoom();
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (_mapImage == null)
            return;

        Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, camera, out Vector2 current) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position - eventData.delta, camera, out Vector2 previous))
            return;

        Vector2 delta = current - previous;
        if (_liveMap != null)
        {
            float size = LiveOrthographicSize();
            _liveCenter -= new Vector2(delta.x / _liveViewportSize.x * size * _liveMap.Aspect,
                delta.y / _liveViewportSize.y * size) * 2f;
            return;
        }

        _mapImage.rectTransform.anchoredPosition =
            ClampPan(_mapImage.rectTransform.anchoredPosition + delta);
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

        PositionMarker(MapWorldBounds.WorldToNormalized(_mapBounds, _player.transform.position));
    }

    private void PositionMarker(Vector2 normalized)
    {
        if (_playerMarker == null) return;

        Vector2 size = _mapImage.rectTransform.rect.size;
        _playerMarker.anchoredPosition = new Vector2(
            (normalized.x - 0.5f) * size.x,
            (normalized.y - 0.5f) * size.y);
        _playerMarker.localEulerAngles = new Vector3(0f, 0f,
            MapWorldBounds.FacingToMarkerRotationZ(_player.FacingDirection));
    }
}
