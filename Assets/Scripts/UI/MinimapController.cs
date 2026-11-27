using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

// Supported scenes use a live orthographic feed. Other scenes retain the snapshot and marker.
public sealed class MinimapController : MonoBehaviour
{
    [SerializeField] private RawImage _mapImage;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private TMP_Text _zoneText;
    [SerializeField] private TMP_Text _dateText;
    [SerializeField] private Image _frameImage;
    [SerializeField] private Image _maskImage;
    [SerializeField, Range(0.05f, 1f)] private float _viewFraction = 0.22f;

    private LiveMapCamera _liveMap;
    private Texture _snapshot;
    private Camera _liveSource;
    private Scene _scene;
    private Vector2 _smoothedFocus;
    private Vector2 _followVelocity;

    private Player _player;
    private Bounds _mapBounds;
    private bool _hasBounds;
    private MapZoneManager _zoneManager;
    private int _lastFormattedDay = -1;

    private void Awake()
    {
        if (_mapImage != null) _snapshot = _mapImage.texture;
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
        RefreshDateText(force: true);
    }

    private void OnDisable()
    {
        ReleaseLiveMap();
        UnbindZoneManager();
    }

    private void ReleaseLiveMap()
    {
        _liveMap?.Dispose();
        _liveMap = null;
        _liveSource = null;
        if (_mapImage != null)
        {
            bool useSnapshot = !LiveMapCamera.SupportsScene(_scene);
            _mapImage.texture = useSnapshot ? _snapshot : null;
            _mapImage.enabled = useSnapshot;
            if (_playerMarker != null)
                _playerMarker.gameObject.SetActive(useSnapshot);
        }
    }

    private void LateUpdate()
    {
        Scene scene = LiveMapCamera.ResolveWorldScene();
        if (_scene != scene)
        {
            _scene = scene;
            _hasBounds = false;
            _player = null;
            ReleaseLiveMap();
        }
        BindZoneManager(scene);
        if (_player == null)
            _player = FindAnyObjectByType<Player>();
        if (_player == null) return;

        if (_mapImage != null && LiveMapCamera.TryGetSource(scene, out Camera source, out Bounds bounds))
            UpdateLiveViewport(source, bounds);
        else
        {
            ReleaseLiveMap();
            if (LiveMapCamera.SupportsScene(scene))
                return;
            if (!_hasBounds)
                _hasBounds = MapWorldBounds.TryGet(out _mapBounds);
            if (_hasBounds)
                UpdateViewport(_player.transform.position);
        }
        RefreshDateText(force: false);
    }

    private void UpdateLiveViewport(Camera source, Bounds bounds)
    {
        Vector2 position = _player.transform.position;
        if (_liveMap == null || _liveSource != source)
        {
            ReleaseLiveMap();
            _liveSource = source;
            _liveMap = new LiveMapCamera(source, OutputChannels.Channel14, 256, 256);
            _mapImage.texture = _liveMap.Texture;
            _mapImage.enabled = true;
            if (_playerMarker != null)
                _playerMarker.gameObject.SetActive(true);
            _mapImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            _smoothedFocus = position;
            _followVelocity = Vector2.zero;
        }

        _smoothedFocus = Vector2.SmoothDamp(_smoothedFocus, position, ref _followVelocity,
            0.10f, Mathf.Infinity, Time.unscaledDeltaTime);
        float size = Mathf.Min(bounds.extents.x, bounds.extents.y) * _viewFraction;
        Vector2 center = LiveMapCamera.ClampCenter(_smoothedFocus, bounds, size, _liveMap.Aspect);
        _liveMap.Frame(center, size, source.transform.position.z);
        PositionMarker(_liveMap.Project(_player.transform.position));
    }

    private void PositionMarker(Vector2 normalized)
    {
        if (_playerMarker == null || _mapImage == null) return;
        _playerMarker.anchoredPosition = Vector2.Scale(normalized - Vector2.one * 0.5f, _mapImage.rectTransform.rect.size);
        _playerMarker.localEulerAngles = new Vector3(0f, 0f,
            MapWorldBounds.FacingToMarkerRotationZ(_player.FacingDirection));
    }

    private void UpdateViewport(Vector3 worldPosition)
    {
        Vector2 normalized = MapWorldBounds.WorldToNormalized(_mapBounds, worldPosition);
        float half = _viewFraction * 0.5f;
        float u0 = Mathf.Clamp(normalized.x - half, 0f, 1f - _viewFraction);
        float v0 = Mathf.Clamp(normalized.y - half, 0f, 1f - _viewFraction);

        if (_mapImage != null)
            _mapImage.uvRect = new Rect(u0, v0, _viewFraction, _viewFraction);

        PositionMarker(new Vector2((normalized.x - u0) / _viewFraction, (normalized.y - v0) / _viewFraction));
    }

    private void BindZoneManager(Scene scene)
    {
        MapZoneManager manager = MapZoneManager.Instance;
        if (manager != null && manager.gameObject.scene != scene)
            manager = null;
        if (ReferenceEquals(_zoneManager, manager))
            return;

        UnbindZoneManager();
        _zoneManager = manager;
        if (_zoneManager != null)
            _zoneManager.OnZoneChanged += RefreshZoneText;
        RefreshZoneText(_zoneManager != null ? _zoneManager.CurrentZoneName : string.Empty);
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
