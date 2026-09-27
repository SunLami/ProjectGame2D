using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation only, opened/closed by UnifiedGameplayHudController via GameplayMenuPage.Map.
// Shows the static map snapshot baked by MapSnapshotBaker (Tools/ProjectGame2D/UI/Bake Map
// Snapshot) full-screen, scaled to "cover" the viewport (fills edge-to-edge, no letterbox bars,
// no distortion) with a Player icon overlay -- no live camera/RenderTexture. Zoom scales the
// image's RectTransform around its own center; a RectMask2D on the viewport clips the overflow.
public sealed class FullMapController : MonoBehaviour, IScrollHandler
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

    private void OnEnable()
    {
        _hasBounds = MapWorldBounds.TryGet(out _mapBounds);
        FitToViewport();
        _zoom = 1f;
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

    private void ApplyZoom()
    {
        if (_mapImage != null)
            _mapImage.rectTransform.localScale = Vector3.one * _zoom;
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
