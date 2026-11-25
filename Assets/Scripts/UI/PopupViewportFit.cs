using UnityEngine;
using UnityEngine.UI;

/// <summary>Fits a fixed-layout popup into the shared reference frame without changing its child layout.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class PopupViewportFit : MonoBehaviour
{
    private static readonly Vector2 ReferenceSize = new(1920f, 1080f);
    private const float SafePadding = 24f;

    [SerializeField] private Vector2 _targetSize = new(1600f, 900f);

    [SerializeField, Range(0f, 1f)] private float _verticalAlignment = 0.5f;

    [SerializeField, Range(0f, 1f)] private float _horizontalAlignment = 0.5f;

    public static void Configure(GameObject board, Vector2 size, float verticalAlignment = 0.5f,
        float horizontalAlignment = 0.5f)
    {
        var fit = board.GetComponent<PopupViewportFit>() ?? board.AddComponent<PopupViewportFit>();
        fit._targetSize = size;
        fit._verticalAlignment = verticalAlignment;
        fit._horizontalAlignment = horizontalAlignment;
    }

    public static void ConfigureCanvas(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }

    internal Vector2 TargetSize => _targetSize;

    private RectTransform _board;
    private Canvas _canvas;

    private void OnEnable()
    {
        _board = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        Fit();
    }

    private void LateUpdate() => Fit();

    private void Fit()
    {
        if (_canvas == null || _board.parent is not RectTransform parent ||
            _canvas.renderMode == RenderMode.WorldSpace)
            return;

        Rect viewport = _canvas.pixelRect;
        float referenceScale = Mathf.Min(viewport.width / ReferenceSize.x, viewport.height / ReferenceSize.y);
        Rect safe = Screen.safeArea;
        Vector2 safeMin = Vector2.Max(viewport.min, safe.min);
        Vector2 safeMax = Vector2.Min(viewport.max, safe.max);
        Vector2 available = safeMax - safeMin - Vector2.one * (2f * SafePadding * referenceScale);
        Vector2 frameSize = Vector2.Min(available, _targetSize * referenceScale);
        Vector2 boardSize = _board.rect.size;
        if (frameSize.x <= 0f || frameSize.y <= 0f || boardSize.x <= 0f || boardSize.y <= 0f)
            return;

        float pixelScale = Mathf.Min(frameSize.x / boardSize.x, frameSize.y / boardSize.y);
        Vector2 halfSize = boardSize * (pixelScale * 0.5f);
        Vector2 inset = Vector2.one * (SafePadding * referenceScale) + halfSize;
        Vector2 center = new(
            Mathf.Lerp(safeMin.x + inset.x, safeMax.x - inset.x, _horizontalAlignment),
            Mathf.Lerp(safeMin.y + inset.y, safeMax.y - inset.y, _verticalAlignment));
        Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, center - halfSize, camera, out Vector2 lower) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, center + halfSize, camera, out Vector2 upper))
            return;

        Vector2 localSize = upper - lower;
        float scale = Mathf.Min(localSize.x / boardSize.x, localSize.y / boardSize.y);
        if (scale <= 0f) return;

        Vector3 nextScale = new(scale, scale, _board.localScale.z);
        Vector2 pivotPosition = (lower + upper) * 0.5f - _board.rect.center * scale;
        Vector3 nextPosition = new(pivotPosition.x, pivotPosition.y, _board.localPosition.z);
        if (_board.localScale != nextScale) _board.localScale = nextScale;
        if (_board.localPosition != nextPosition) _board.localPosition = nextPosition;
    }
}
