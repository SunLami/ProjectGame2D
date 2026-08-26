#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ExperienceBarAuthoring
{
    private const string PrefabPath = "Assets/Resources/UI/Gameplay/HUD/PlayerExperienceBar.prefab";
    private const string ScenePath = "Assets/Scenes/DemoScene.unity";

    [MenuItem("Tools/ProjectGame2D/UI/Build Player Experience Bar")]
    public static void Build()
    {
        Sprite fillSprite = LoadSprite("Assets/Resources/UI/HUD/ExperienceBar/experience_bar_fill_blue_v2.png");
        Sprite frameSprite = LoadSprite("Assets/Resources/UI/HUD/ExperienceBar/experience_bar_frame_light_fantasy.png");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");

        if (fillSprite == null || frameSprite == null || font == null)
            throw new System.InvalidOperationException("Experience bar assets or Digital Disco font could not be loaded.");

        GameObject root = new("PlayerExperienceBar", typeof(RectTransform), typeof(PlayerExperienceBarController));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            // Sink the decorative alpha edge slightly below the canvas so the visible frame
            // touches the bottom screen edge instead of leaving a transparent-looking gap.
            rootRect.anchoredPosition = new Vector2(0f, -6f);
            rootRect.sizeDelta = new Vector2(0f, 24f);

            Image fill = CreateImage("Fill", rootRect, fillSprite);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 0f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.anchoredPosition = new Vector2(0f, 7.5f);
            fillRect.sizeDelta = new Vector2(-48f, 8f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            Image frame = CreateImage("Frame", rootRect, frameSprite);
            Stretch(frame.rectTransform);

            GameObject textObject = new("ExperienceText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rootRect, false);
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 6f);
            textRect.sizeDelta = new Vector2(-72f, 11f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = "0 / 100";
            text.fontSize = 8f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.95f, 0.72f, 1f);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontStyle = FontStyles.Bold;

            SerializedObject controller = new(root.GetComponent<PlayerExperienceBarController>());
            controller.FindProperty("_experienceFill").objectReferenceValue = fill;
            controller.FindProperty("_experienceText").objectReferenceValue = text;
            controller.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject canvas = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
            .FirstOrDefault(c => c.gameObject.name == "UICanvas")?.gameObject;
        if (canvas == null)
            throw new System.InvalidOperationException("DemoScene UICanvas was not found.");

        Transform existing = canvas.transform.Find("PlayerExperienceBar");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        instance.name = "PlayerExperienceBar";
        instance.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ExperienceBarAuthoring: prefab and DemoScene integration completed.");
    }

    private static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
