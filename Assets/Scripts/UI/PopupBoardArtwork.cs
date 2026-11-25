using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>Stretches the panel surfaces while keeping the original frame and dividers at fixed thickness.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class PopupBoardArtwork : BaseMeshEffect
{
    [SerializeField] private Vector2 _authoredSize;
    [SerializeField] private float[] _dividers = System.Array.Empty<float>();
    [SerializeField] private Vector2 _verticalBorders = new(0.12f, 0.12f);

    public static void Configure(Image image, Vector2 authoredSize, Vector2 verticalBorders, params float[] dividers)
    {
        var artwork = image.GetComponent<PopupBoardArtwork>() ?? image.gameObject.AddComponent<PopupBoardArtwork>();
        artwork._authoredSize = authoredSize;
        artwork._verticalBorders = verticalBorders;
        artwork._dividers = dividers;
        image.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive() || graphic is not Image image || image.sprite == null ||
            _authoredSize.x <= 0f || _authoredSize.y <= 0f)
            return;

        Rect rect = graphic.GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f ||
            (rect.size - _authoredSize).sqrMagnitude < 0.01f)
            return;

        const float edge = 0.045f;
        const float halfDivider = 0.012f;
        int columns = 4 + 2 * _dividers.Length;
        var sourceX = new float[columns];
        var targetX = new float[columns];
        sourceX[1] = edge;
        sourceX[columns - 2] = 1f - edge;
        sourceX[columns - 1] = 1f;
        float fixedX = Mathf.Min(1f, rect.width / _authoredSize.x) * _authoredSize.x;
        targetX[1] = edge * fixedX;
        for (int i = 0; i < _dividers.Length; i++)
        {
            int index = 2 + i * 2;
            sourceX[index] = _dividers[i] - halfDivider;
            sourceX[index + 1] = _dividers[i] + halfDivider;
            targetX[index] = rect.width * _dividers[i] - fixedX * halfDivider;
            targetX[index + 1] = rect.width * _dividers[i] + fixedX * halfDivider;
        }
        targetX[columns - 2] = rect.width - edge * fixedX;
        targetX[columns - 1] = rect.width;

        float fixedY = Mathf.Min(rect.height, _authoredSize.y);
        float[] sourceY = { 0f, _verticalBorders.x, 1f - _verticalBorders.y, 1f };
        float[] targetY = { 0f, _verticalBorders.x * fixedY,
            rect.height - _verticalBorders.y * fixedY, rect.height };

        Vector4 uv = DataUtility.GetOuterUV(image.sprite);
        vertices.Clear();
        for (int row = 0; row < sourceY.Length; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                vertices.AddVert(new Vector3(rect.xMin + targetX[col], rect.yMin + targetY[row]),
                    image.color, new Vector2(Mathf.Lerp(uv.x, uv.z, sourceX[col]),
                        Mathf.Lerp(uv.y, uv.w, sourceY[row])));
                if (row == 0 || col == 0) continue;
                int corner = row * columns + col;
                vertices.AddTriangle(corner - columns - 1, corner - 1, corner);
                vertices.AddTriangle(corner - columns - 1, corner, corner - columns);
            }
        }
    }
}
