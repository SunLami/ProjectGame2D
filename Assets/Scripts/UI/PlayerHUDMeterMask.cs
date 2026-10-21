using UnityEngine;
using UnityEngine.UI;

// Convex authored meter opening. Keeps the fill behind the slanted frame ends.
public sealed class PlayerHUDMeterMask : MaskableGraphic
{
    [SerializeField] private Vector2[] _points;

    public void Configure(Vector2[] points)
    {
        _points = points;
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        if (_points == null || _points.Length < 3) return;
        Rect bounds = rectTransform.rect;
        for (int i = 0; i < _points.Length; i++)
            helper.AddVert(new Vector3(bounds.xMin + _points[i].x * bounds.width,
                bounds.yMin + _points[i].y * bounds.height), color, Vector2.zero);
        for (int i = 1; i < _points.Length - 1; i++)
            helper.AddTriangle(0, i, i + 1);
    }
}
