using UnityEngine;
using UnityEngine.UI;

/// <summary>Projects the shared item grid dimensions into a legacy popup's local layout units.</summary>
internal static class PopupItemGridLayout
{
    private const float ReferenceCell = 96f;
    private const float ReferenceGap = 16f;
    private const int MaxColumns = 6;
    private const float MinimumCellRatio = 0.95f;

    public static void Apply(GridLayoutGroup grid, RectTransform viewport, TextAnchor alignment = TextAnchor.UpperCenter)
    {
        if (grid == null || viewport == null) return;

        var popup = grid.GetComponentInParent<PopupViewportFit>(true);
        if (popup == null) return;

        var board = (RectTransform)popup.transform;
        Vector2 boardSize = board.rect.size;
        if (boardSize.x <= 0f || boardSize.y <= 0f) return;

        float boardFit = Mathf.Min(popup.TargetSize.x / boardSize.x, popup.TargetSize.y / boardSize.y);
        if (boardFit <= 0f) return;

        float width = Mathf.Min(((RectTransform)grid.transform).rect.width, viewport.rect.width)
            - grid.padding.horizontal;
        if (width <= 0f) return;

        float cell = ReferenceCell / boardFit;
        float spacing = ReferenceGap / boardFit;
        int columns = Mathf.Clamp(Mathf.FloorToInt((width + spacing) / (cell * MinimumCellRatio + spacing)),
            1, MaxColumns);
        cell = Mathf.Min(cell, (width - (columns - 1) * spacing) / columns);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = Vector2.one * cell;
        grid.spacing = Vector2.one * spacing;
        grid.childAlignment = alignment;
    }
}
