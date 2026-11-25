using UnityEngine;

internal static class PopupLayoutAuthoring
{
    /// <summary>Changes anchors while preserving the authored rectangle at the current parent size.</summary>
    public static void Reanchor(Transform child, Vector2 min, Vector2 max)
    {
        var rect = (RectTransform)child;
        var parent = (RectTransform)rect.parent;
        Vector2 oldMin = rect.offsetMin + Vector2.Scale(parent.rect.size, rect.anchorMin);
        Vector2 oldMax = rect.offsetMax + Vector2.Scale(parent.rect.size, rect.anchorMax);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = oldMin - Vector2.Scale(parent.rect.size, min);
        rect.offsetMax = oldMax - Vector2.Scale(parent.rect.size, max);
    }
}
