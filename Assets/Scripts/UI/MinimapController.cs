using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only. Shows a scrolling crop of the static map snapshot baked by
// MapSnapshotBaker (Tools/ProjectGame2D/UI/Bake Map Snapshot), centered on the Player via
// RawImage.uvRect, with a fixed marker icon repositioned to stay accurate even when the crop
// clamps near a map edge. No live camera/RenderTexture -- matches the standard genre pattern
// (static map art + moving icon) instead of rendering the world every frame.
public sealed class MinimapController : MonoBehaviour
{
    [SerializeField] private RawImage _mapImage;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private TMP_Text _zoneText;
    [SerializeField] private TMP_Text _dateText;
    [SerializeField] private Image _frameImage;
    [SerializeField] private Image _maskImage;
    [SerializeField, Range(0.05f, 1f)] private float _viewFraction = 0.22f;

    private Player _player;
    private Bounds _mapBounds;
    private bool _hasBounds;
    private MapZoneManager _zoneManager;
    private int _lastFormattedDay = -1;

    private void Awake()
    {
        // Dark Inventory Style circular frame/mask art now ships via MapUIAuthoring
        // (minimap_frame_v1 / minimap_mask_v1). Only fall back to the procedurally drawn circle
        // (not an authored bitmap) when a field is wired but its sprite is missing, so this never
        // overwrites a real sprite assigned in the Inspector.
        if (_frameImage != null && _frameImage.sprite == null)
            _frameImage.sprite = RuntimeCircleSprite.Get(256);
        if (_maskImage != null && _maskImage.sprite == null)
            _maskImage.sprite = RuntimeCircleSprite.Get(256);
    }

    private void OnEnable()
    {
        // Not re-fetched here on purpose: Minimap lives in the persistent Bootstrap HUD, which can
        // enable before MapNhat (and its BorderMap) has finished loading additively. Retrying every
        // LateUpdate until found (below) avoids a permanent "never tracks the Player" freeze from
        // that race, instead of a single OnEnable-time check that can lose the race silently.
        BindZoneManager();
        if (_zoneManager != null)
            RefreshZoneText(_zoneManager.CurrentZoneName);
        RefreshDateText(force: true);
    }

    private void OnDisable() => UnbindZoneManager();

    private void LateUpdate()
    {
        if (!_hasBounds)
            _hasBounds = MapWorldBounds.TryGet(out _mapBounds);
        if (_player == null)
            _player = FindAnyObjectByType<Player>();
        if (_player == null || !_hasBounds)
            return;

        UpdateViewport(_player.transform.position);
        RefreshDateText(force: false);
    }

    private void UpdateViewport(Vector3 worldPosition)
    {
        Vector2 normalized = MapWorldBounds.WorldToNormalized(_mapBounds, worldPosition);
        float half = _viewFraction * 0.5f;
        float u0 = Mathf.Clamp(normalized.x - half, 0f, 1f - _viewFraction);
        float v0 = Mathf.Clamp(normalized.y - half, 0f, 1f - _viewFraction);

        if (_mapImage != null)
            _mapImage.uvRect = new Rect(u0, v0, _viewFraction, _viewFraction);

        if (_playerMarker != null && _mapImage != null)
        {
            Vector2 withinView = new(
                (normalized.x - u0) / _viewFraction,
                (normalized.y - v0) / _viewFraction);
            Vector2 size = _mapImage.rectTransform.rect.size;
            _playerMarker.anchoredPosition = new Vector2(
                (withinView.x - 0.5f) * size.x,
                (withinView.y - 0.5f) * size.y);
            _playerMarker.localEulerAngles = new Vector3(0f, 0f,
                MapWorldBounds.FacingToMarkerRotationZ(_player.FacingDirection));
        }
    }

    private void BindZoneManager()
    {
        _zoneManager = MapZoneManager.Instance;
        if (_zoneManager != null)
            _zoneManager.OnZoneChanged += RefreshZoneText;
    }

    private void UnbindZoneManager()
    {
        if (_zoneManager != null)
            _zoneManager.OnZoneChanged -= RefreshZoneText;
        _zoneManager = null;
    }

    private void RefreshZoneText(string zoneName)
    {
        if (_zoneText != null)
            _zoneText.text = zoneName;
    }

    private void RefreshDateText(bool force)
    {
        DateTime now = DateTime.Now;
        if (!force && now.Day == _lastFormattedDay)
            return;
        _lastFormattedDay = now.Day;
        if (_dateText != null)
            _dateText.text = now.ToString("d MMM", CultureInfo.InvariantCulture);
    }
}
