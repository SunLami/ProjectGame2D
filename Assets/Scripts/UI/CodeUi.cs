using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small helpers for UI that is built in code (boss teleport/shrine panels, D-089/D-090) in the
/// Dark Light Fantasy palette. Presentation only.</summary>
public static class CodeUi
{
    public static readonly Color Charcoal = new Color(0.09f, 0.08f, 0.07f, 0.97f);
    public static readonly Color Walnut = new Color(0.17f, 0.12f, 0.09f, 1f);
    public static readonly Color Gold = new Color(0.78f, 0.62f, 0.28f, 1f);
    public static readonly Color GoldDim = new Color(0.78f, 0.62f, 0.28f, 0.35f);
    public static readonly Color Ivory = new Color(0.96f, 0.92f, 0.82f, 1f);
    public static readonly Color WarmGray = new Color(0.66f, 0.62f, 0.56f, 1f);
    public static readonly Color Sapphire = new Color(0.12f, 0.22f, 0.5f, 1f);
    public static readonly Color Disabled = new Color(0.2f, 0.2f, 0.22f, 1f);
    public static readonly Color Positive = new Color(0.45f, 0.85f, 0.5f, 1f);

    public static Canvas NewOverlayCanvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static TMP_Text NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal; // code-created TMP defaults to NoWrap in this version
        label.raycastTarget = false;
        return label;
    }

    public static void Inset(RectTransform rect, float amount)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(amount, amount);
        rect.offsetMax = new Vector2(-amount, -amount);
    }

    public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    /// <summary>A walnut button with a centred label; returns the Button (its Image is on the same object).</summary>
    public static Button NewButton(string name, Transform parent, string label, float fontSize, Color background, Color labelColor, out TMP_Text text)
    {
        Image image = NewImage(name, parent, background);
        text = NewText("Label", image.transform, label, fontSize, labelColor, TextAlignmentOptions.Center);
        Inset(text.rectTransform, 0f);
        return image.gameObject.AddComponent<Button>();
    }
}
