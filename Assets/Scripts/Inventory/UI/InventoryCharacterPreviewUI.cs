using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RawImage))]
public sealed class InventoryCharacterPreviewUI : MonoBehaviour
{
    private const int PreviewLayer = 30;
    private RawImage _image;
    private RenderTexture _texture;
    private Camera _camera;
    private Player _player;
    private Animator _playerAnimator;
    private SpriteRenderer[] _previewRenderers;
    private AnimatorUpdateMode _originalUpdateMode;
    private readonly Dictionary<GameObject, int> _originalLayers = new();
    private readonly Dictionary<Camera, int> _originalCameraMasks = new();

    private void OnEnable()
    {
        _image = GetComponent<RawImage>();
        BuildPreview();
    }

    private void LateUpdate()
    {
        if (_camera == null || _player == null) return;
        Vector3 previewCenter = GetPreviewCenter();
        _camera.transform.position = new Vector3(previewCenter.x, previewCenter.y, -10f);
    }

    private void OnDisable() => Cleanup();

    private void BuildPreview()
    {
        Cleanup();
        _player = FindAnyObjectByType<Player>();
        if (_player == null) return;

        _playerAnimator = _player.GetComponent<Animator>();
        if (_playerAnimator != null)
        {
            _originalUpdateMode = _playerAnimator.updateMode;
            _playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        MoveVisualToPreviewLayer("Body");
        MoveVisualToPreviewLayer("Head");
        MoveVisualToPreviewLayer("Weapon");
        _previewRenderers = _player.GetComponentsInChildren<SpriteRenderer>(true);

        // The preview uses the real animated Player. Hide its visual-only layer from
        // gameplay cameras while the inventory is open so it cannot show through the
        // translucent inventory panel; the dedicated preview camera still renders it.
        Camera[] sceneCameras = FindObjectsByType<Camera>();
        foreach (Camera sceneCamera in sceneCameras)
        {
            if (sceneCamera == null) continue;
            _originalCameraMasks[sceneCamera] = sceneCamera.cullingMask;
            sceneCamera.cullingMask &= ~(1 << PreviewLayer);
        }

        _texture = new RenderTexture(128, 192, 24, RenderTextureFormat.ARGB32)
        {
            name = "InventoryPlayerPreviewRT",
            filterMode = FilterMode.Point,
            antiAliasing = 1
        };
        _texture.Create();
        _image.texture = _texture;

        GameObject cameraObject = new("InventoryPlayerPreviewCamera", typeof(Camera));
        _camera = cameraObject.GetComponent<Camera>();
        Vector3 previewCenter = GetPreviewCenter();
        _camera.transform.position = new Vector3(previewCenter.x, previewCenter.y, -10f);
        _camera.orthographic = true;
        _camera.orthographicSize = 1.75f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.cullingMask = 1 << PreviewLayer;
        _camera.targetTexture = _texture;
    }

    private void Cleanup()
    {
        if (_playerAnimator != null) _playerAnimator.updateMode = _originalUpdateMode;
        foreach (KeyValuePair<GameObject, int> entry in _originalLayers)
        {
            if (entry.Key != null) entry.Key.layer = entry.Value;
        }
        _originalLayers.Clear();
        foreach (KeyValuePair<Camera, int> entry in _originalCameraMasks)
        {
            if (entry.Key != null) entry.Key.cullingMask = entry.Value;
        }
        _originalCameraMasks.Clear();
        if (_camera != null) Destroy(_camera.gameObject);
        if (_texture != null)
        {
            _texture.Release();
            Destroy(_texture);
        }
        _player = null;
        _playerAnimator = null;
        _previewRenderers = null;
        _camera = null;
        _texture = null;
        if (_image != null) _image.texture = null;
    }

    private Vector3 GetPreviewCenter()
    {
        bool hasBounds = false;
        Bounds combined = default;
        if (_previewRenderers != null)
        {
            foreach (SpriteRenderer renderer in _previewRenderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!hasBounds)
                {
                    combined = renderer.bounds;
                    hasBounds = true;
                }
                else combined.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? combined.center : _player.transform.position;
    }

    private void MoveVisualToPreviewLayer(string childName)
    {
        Transform visual = _player.transform.Find(childName);
        if (visual == null) return;
        MoveToPreviewLayerRecursively(visual);
    }

    private void MoveToPreviewLayerRecursively(Transform target)
    {
        _originalLayers[target.gameObject] = target.gameObject.layer;
        target.gameObject.layer = PreviewLayer;
        foreach (Transform child in target) MoveToPreviewLayerRecursively(child);
    }
}
