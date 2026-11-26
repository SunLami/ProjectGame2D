using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>A dedicated orthographic view of the active map’s XY tile plane.</summary>
internal sealed class LiveMapCamera : IDisposable
{
    private static Collider2D _mapBorder;
    private static Scene _mapScene;

    private readonly Camera _output;
    private readonly CinemachineCamera _view;
    private readonly CinemachineBrain _brain;
    private bool _disposed;

    public RenderTexture Texture { get; }
    public float Aspect => (float)Texture.width / Texture.height;

    public static bool TryGetSource(out Camera source, out Bounds bounds)
    {
        var scene = SceneManager.GetActiveScene();
        bool supportsLiveMap = scene.name is "MapNhat" or "MapDuy" or "BossArena_Earth"
            or "BossArena_Water" or "BossArena_Wind";
        source = supportsLiveMap ? Camera.main : null;
        bounds = default;
        if (source == null || source.gameObject.scene != scene)
            return false;

        if (_mapBorder == null || _mapScene != scene)
        {
            _mapScene = scene;
            _mapBorder = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "BorderMap") continue;
                _mapBorder = root.GetComponent<Collider2D>();
                break;
            }
        }
        if (_mapBorder == null) return false;

        bounds = _mapBorder.bounds;
        return bounds.size.x > 0f && bounds.size.y > 0f;
    }

    public LiveMapCamera(Camera source, OutputChannels channel, int width, int height)
    {
        Texture = new RenderTexture(width, height, 24)
        {
            name = $"{source.gameObject.scene.name} {channel} View",
            filterMode = FilterMode.Point
        };
        Texture.Create();

        var output = new GameObject($"{source.gameObject.scene.name} {channel} Output");
        output.SetActive(false);
        _output = output.AddComponent<Camera>();
        _output.orthographic = true;
        _output.aspect = Aspect;
        _output.targetTexture = Texture;
        _output.allowMSAA = false;
        _output.allowHDR = false;
        _output.clearFlags = CameraClearFlags.SolidColor;
        _output.backgroundColor = new Color32(31, 40, 44, 255);
        _output.cullingMask = source.cullingMask & ~LayerMask.GetMask("UI");
        _brain = output.AddComponent<CinemachineBrain>();
        _brain.ChannelMask = channel;
        _brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
        _brain.IgnoreTimeScale = true;
        _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        var view = new GameObject($"{source.gameObject.scene.name} {channel} Cinemachine Camera");
        view.SetActive(false);
        _view = view.AddComponent<CinemachineCamera>();
        _view.OutputChannel = channel;
        view.SetActive(true);
        output.SetActive(true);
    }

    public void Frame(Vector2 center, float size, float depth)
    {
        _view.transform.SetPositionAndRotation(new Vector3(center.x, center.y, depth), Quaternion.identity);
        LensSettings lens = _view.Lens;
        lens.OrthographicSize = size;
        _view.Lens = lens;
        _brain.ManualUpdate();
    }

    public Vector2 Project(Vector3 position) => _output.WorldToViewportPoint(position);

    public static Vector2 ClampCenter(Vector2 center, Bounds bounds, float size, float aspect)
    {
        Vector2 travel = Vector2.Max(Vector2.zero, (Vector2)bounds.extents - new Vector2(size * aspect, size));
        Vector2 midpoint = bounds.center;
        return new Vector2(
            Mathf.Clamp(center.x, midpoint.x - travel.x, midpoint.x + travel.x),
            Mathf.Clamp(center.y, midpoint.y - travel.y, midpoint.y + travel.y));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_view != null)
        {
            _view.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_view.gameObject);
        }

        if (_output != null)
        {
            _output.gameObject.SetActive(false);
            _output.targetTexture = null;
            UnityEngine.Object.Destroy(_output.gameObject);
        }

        if (Texture != null)
        {
            Texture.Release();
            UnityEngine.Object.Destroy(Texture);
        }
    }
}
