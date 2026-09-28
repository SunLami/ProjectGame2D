using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class QuestTrackerPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/QuestTracker.prefab";
    private const string SpriteFolder = "Assets/Resources/UI/Quest/Tracker1920/";

    [MenuItem("Tools/ProjectGame2D/UI/Build Quest Tracker 1920 HUD")]
    public static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            TMP_Text titleStyle = root.transform.Find("Title")?.GetComponent<TMP_Text>();
            TMP_Text objectiveStyle = root.transform.Find("Objectives")?.GetComponent<TMP_Text>();
            RemoveIfPresent(root.transform, "Header");
            RemoveIfPresent(root.transform, "Viewport");

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.anchoredPosition = new Vector2(16f, 0f);
            rootRect.sizeDelta = new Vector2(155f, 190f);
            Image rootImage = root.GetComponent<Image>() ?? root.AddComponent<Image>();
            rootImage.sprite = null;
            rootImage.color = Color.clear;
            rootImage.raycastTarget = false;
            if (titleStyle != null) titleStyle.gameObject.SetActive(false);
            if (objectiveStyle != null) objectiveStyle.gameObject.SetActive(false);

            BuildHeader(root.transform, titleStyle);
            BuildViewport(root.transform, titleStyle, objectiveStyle);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("QuestTracker prefab rebuilt as an editable, background-free 1920x1080 HUD.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildHeader(Transform parent, TMP_Text style)
    {
        GameObject header = UI("Header", parent, typeof(Image), typeof(Button));
        Rect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 20f));
        Image hitArea = header.GetComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0.001f);

        Image chevron = Icon("Chevron", header.transform, "chevron_down", new Vector2(1f, -3f), new Vector2(14f, 14f));
        Icon("QuestIcon", header.transform, "quest_scroll", new Vector2(17f, -2f), new Vector2(16f, 16f));
        TMP_Text title = Text("QuestHeader", header.transform, style, "QUESTS", 8f, new Vector2(35f, -4f), new Vector2(48f, 15f));
        title.fontStyle = FontStyles.Bold;

        GameObject divider = UI("GoldDivider", header.transform, typeof(Image));
        Rect(divider, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(82f, -10f), new Vector2(68f, 1f));
        divider.GetComponent<Image>().color = new Color(0.96f, 0.69f, 0.2f, 1f);
        divider.GetComponent<Image>().raycastTarget = false;

        Button button = header.GetComponent<Button>();
        button.targetGraphic = hitArea;
        button.transition = Selectable.Transition.ColorTint;
        chevron.raycastTarget = false;
    }

    private static void BuildViewport(Transform parent, TMP_Text titleStyle, TMP_Text objectiveStyle)
    {
        GameObject viewport = UI("Viewport", parent, typeof(Image), typeof(RectMask2D));
        Rect(viewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -11f), new Vector2(0f, -22f));
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;

        GameObject content = UI("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Rect(content, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(2, 2, 2, 2);
        contentLayout.spacing = 3f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject template = UI("QuestRowTemplate", content.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        VerticalLayoutGroup rowLayout = template.GetComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 0f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        template.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        BuildLine(template.transform, "QuestTitle", "category_main", "A Call to Adventure", titleStyle, 7.5f, 16f, false);
        BuildLine(template.transform, "Objective", "objective_kill", "Defeat forest wolves", objectiveStyle, 6.5f, 15f, true);
        template.SetActive(false);

        ScrollRect scroll = parent.GetComponent<ScrollRect>() ?? parent.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content.GetComponent<RectTransform>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
    }

    private static void BuildLine(Transform parent, string name, string iconName, string labelValue,
        TMP_Text style, float fontSize, float height, bool withProgress)
    {
        GameObject line = UI(name, parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        line.GetComponent<LayoutElement>().preferredHeight = height;
        HorizontalLayoutGroup layout = line.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject icon = UI("Icon", line.transform, typeof(Image), typeof(LayoutElement));
        icon.GetComponent<Image>().sprite = Sprite(iconName);
        icon.GetComponent<Image>().preserveAspect = true;
        icon.GetComponent<Image>().raycastTarget = false;
        icon.GetComponent<LayoutElement>().preferredWidth = 14f;
        icon.GetComponent<LayoutElement>().preferredHeight = 14f;

        TMP_Text label = LayoutText("Label", line.transform, style, labelValue, fontSize);
        label.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;
        if (!withProgress) label.fontStyle = FontStyles.Bold;

        if (withProgress)
        {
            TMP_Text progress = LayoutText("Progress", line.transform, style, "3/5", 6.5f);
            progress.alignment = TextAlignmentOptions.MidlineRight;
            progress.gameObject.GetComponent<LayoutElement>().preferredWidth = 27f;
        }
    }

    private static TMP_Text LayoutText(string name, Transform parent, TMP_Text style, string value, float size)
    {
        GameObject go = UI(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement), typeof(Shadow));
        TMP_Text text = go.GetComponent<TMP_Text>();
        CopyFont(style, text);
        text.text = value;
        text.fontSize = size;
        text.color = new Color(1f, 0.94f, 0.78f, 1f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1.2f, -1.2f);
        return text;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_Text style, string value, float size, Vector2 position, Vector2 rectSize)
    {
        TMP_Text text = LayoutText(name, parent, style, value, size);
        Rect(text.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, rectSize);
        return text;
    }

    private static Image Icon(string name, Transform parent, string spriteName, Vector2 position, Vector2 size)
    {
        GameObject go = UI(name, parent, typeof(Image));
        Rect(go, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        Image image = go.GetComponent<Image>();
        image.sprite = Sprite(spriteName);
        image.preserveAspect = true;
        return image;
    }

    private static Sprite Sprite(string name)
    {
        string path = SpriteFolder + name + ".png";
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static void CopyFont(TMP_Text source, TMP_Text target)
    {
        if (source == null) return;
        target.font = source.font;
        target.fontSharedMaterial = source.fontSharedMaterial;
    }

    private static GameObject UI(string name, Transform parent, params Type[] components)
    {
        Type[] all = new[] { typeof(RectTransform) }.Concat(components).ToArray();
        GameObject go = new(name, all);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Rect(GameObject go, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void RemoveIfPresent(Transform root, string name)
    {
        Transform child = root.Find(name);
        if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }
}
