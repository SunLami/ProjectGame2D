using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FishingFeatureAuthoring
{
    private const string RootFolder = "Assets/Game/Fishing";
    private const string FishFolder = "Assets/Resources/Items/Fish";
    private const string PrefabFolder = "Assets/Prefabs/Fishing";
    private const string FeaturePrefabPath = PrefabFolder + "/FishingFeature.prefab";
    private const string SpotPath = RootFolder + "/Definitions/FishingSpot.River.asset";
    private const string FontPath = "Assets/Fonts/DigitalDisco SDF v3.asset";
    private const string FishSpriteRoot = "Assets/Tiles/Tilesets/Fishing and Gathering Pixel Art RPG Icons/PNG_n_Tiled/";

    [MenuItem("Tools/Project Game/Fishing/Build And Install MapNhat Fishing")]
    public static void BuildAndInstall()
    {
        EnsureFolder("Assets", "Game");
        EnsureFolder("Assets/Game", "Fishing");
        EnsureFolder(RootFolder, "Definitions");
        EnsureFolder("Assets/Resources", "Items");
        EnsureFolder("Assets/Resources/Items", "Fish");
        EnsureFolder("Assets/Prefabs", "Fishing");

        FishDefinitionSO riverMinnow = CreateFish(
            FishFolder + "/RiverMinnow.asset",
            "fish.river.minnow",
            "River Minnow",
            "A small common river fish.",
            FishSpriteRoot + "Fish1.png",
            "Fish1_7",
            180,
            650,
            18);
        FishDefinitionSO blueCarp = CreateFish(
            FishFolder + "/BlueCarp.asset",
            "fish.river.blue_carp",
            "Blue Carp",
            "A sturdy carp found near the fishing dock.",
            FishSpriteRoot + "Fish2.png",
            "Fish2_7",
            700,
            2400,
            32);

        FishingSpotDefinition spot = CreateFishingSpot(riverMinnow, blueCarp);
        GameObject featurePrefab = CreateFeaturePrefab();
        InstallIntoActiveScene(featurePrefab, spot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fishing feature built and installed in the active MapNhat scene.");
    }

    private static FishDefinitionSO CreateFish(
        string assetPath,
        string id,
        string displayName,
        string description,
        string spritePath,
        string spriteName,
        int minimumWeight,
        int maximumWeight,
        int pricePerKilogram)
    {
        FishDefinitionSO fish = AssetDatabase.LoadAssetAtPath<FishDefinitionSO>(assetPath);
        if (fish == null)
        {
            fish = ScriptableObject.CreateInstance<FishDefinitionSO>();
            AssetDatabase.CreateAsset(fish, assetPath);
        }

        fish.itemId = id;
        fish.itemName = displayName;
        fish.description = description;
        fish.icon = AssetDatabase.LoadAllAssetsAtPath(spritePath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName)
            ?? AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>().FirstOrDefault();
        fish.type = ItemType.Material;
        fish.isStackable = false;
        fish.maxStackSize = 1;

        SerializedObject serialized = new(fish);
        serialized.FindProperty("_minimumWeightGrams").intValue = minimumWeight;
        serialized.FindProperty("_maximumWeightGrams").intValue = maximumWeight;
        serialized.FindProperty("_pricePerKilogram").intValue = pricePerKilogram;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fish);
        return fish;
    }

    private static FishingSpotDefinition CreateFishingSpot(FishDefinitionSO common, FishDefinitionSO uncommon)
    {
        FishingSpotDefinition spot = AssetDatabase.LoadAssetAtPath<FishingSpotDefinition>(SpotPath);
        if (spot == null)
        {
            spot = ScriptableObject.CreateInstance<FishingSpotDefinition>();
            AssetDatabase.CreateAsset(spot, SpotPath);
        }

        SerializedObject serialized = new(spot);
        SerializedProperty entries = serialized.FindProperty("_fish");
        entries.arraySize = 2;
        entries.GetArrayElementAtIndex(0).FindPropertyRelative("_fish").objectReferenceValue = common;
        entries.GetArrayElementAtIndex(0).FindPropertyRelative("_weight").floatValue = 3f;
        entries.GetArrayElementAtIndex(1).FindPropertyRelative("_fish").objectReferenceValue = uncommon;
        entries.GetArrayElementAtIndex(1).FindPropertyRelative("_weight").floatValue = 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spot);
        return spot;
    }

    private static GameObject CreateFeaturePrefab()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        GameObject root = new("FishingFeature");
        FishingMinigameController controller = root.AddComponent<FishingMinigameController>();

        GameObject canvasObject = new("FishingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject uiRoot = CreateRect("FishingUI", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero);
        FishingMinigameUI ui = uiRoot.AddComponent<FishingMinigameUI>();

        GameObject waiting = CreatePanel("WaitingPanel", uiRoot.transform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(500f, 74f), new Color(0.08f, 0.13f, 0.18f, 0.92f));
        TMP_Text waitingText = CreateText("WaitingText", waiting.transform, font, "Waiting for a bite...", 30f, Color.white);

        GameObject bite = CreatePanel("BitePrompt", uiRoot.transform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(150f, 150f), new Color(0.08f, 0.13f, 0.18f, 0.92f));
        CreateText("Exclamation", bite.transform, font, "!", 92f, new Color(1f, 0.83f, 0.15f));

        GameObject minigame = CreatePanel("MinigamePanel", uiRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 650f), new Color(0.12f, 0.17f, 0.22f, 0.96f));
        TMP_Text timer = CreateText("Timer", minigame.transform, font, "18s", 34f, Color.white);
        RectTransform timerRect = timer.rectTransform;
        timerRect.anchorMin = timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0f, -22f);
        timerRect.sizeDelta = new Vector2(180f, 50f);

        GameObject trackObject = CreatePanel("MovementTrack", minigame.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 490f), new Color(0.04f, 0.3f, 0.43f, 1f));
        RectTransform track = trackObject.GetComponent<RectTransform>();
        track.anchoredPosition = new Vector2(-75f, -10f);
        GameObject catchObject = CreatePanel("CatchZone", track, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(128f, 120f), new Color(0.25f, 0.78f, 0.34f, 0.72f));
        RectTransform catchZone = catchObject.GetComponent<RectTransform>();
        GameObject fishObject = CreateRect("FishIcon", track, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 62f));
        Image fishImage = fishObject.AddComponent<Image>();
        fishImage.preserveAspect = true;
        fishImage.raycastTarget = false;
        RectTransform fishIcon = fishObject.GetComponent<RectTransform>();

        Slider progress = CreateVerticalSlider(minigame.transform);
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchoredPosition = new Vector2(105f, -10f);

        GameObject result = CreatePanel("ResultPanel", uiRoot.transform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(620f, 180f), new Color(0.08f, 0.13f, 0.18f, 0.96f));
        TMP_Text resultText = CreateText("ResultText", result.transform, font, string.Empty, 30f, Color.white);

        SerializedObject uiSerialized = new(ui);
        uiSerialized.FindProperty("_waitingPanel").objectReferenceValue = waiting;
        uiSerialized.FindProperty("_bitePrompt").objectReferenceValue = bite;
        uiSerialized.FindProperty("_minigamePanel").objectReferenceValue = minigame;
        uiSerialized.FindProperty("_resultPanel").objectReferenceValue = result;
        uiSerialized.FindProperty("_waitingText").objectReferenceValue = waitingText;
        uiSerialized.FindProperty("_timerText").objectReferenceValue = timer;
        uiSerialized.FindProperty("_resultText").objectReferenceValue = resultText;
        uiSerialized.FindProperty("_movementTrack").objectReferenceValue = track;
        uiSerialized.FindProperty("_fishIcon").objectReferenceValue = fishIcon;
        uiSerialized.FindProperty("_catchZone").objectReferenceValue = catchZone;
        uiSerialized.FindProperty("_fishImage").objectReferenceValue = fishImage;
        uiSerialized.FindProperty("_progressSlider").objectReferenceValue = progress;
        uiSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSerialized = new(controller);
        controllerSerialized.FindProperty("_ui").objectReferenceValue = ui;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

        waiting.SetActive(false);
        bite.SetActive(false);
        minigame.SetActive(false);
        result.SetActive(false);
        uiRoot.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FeaturePrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void InstallIntoActiveScene(GameObject prefab, FishingSpotDefinition spot)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "MapNhat")
            throw new System.InvalidOperationException("Open MapNhat before installing the fishing feature.");

        FishingMinigameController controller = Object.FindAnyObjectByType<FishingMinigameController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            controller = instance.GetComponent<FishingMinigameController>();
            Undo.RegisterCreatedObjectUndo(instance, "Install Fishing Feature");
        }

        GameObject rod = GameObject.Find("Wooden_FishingRod");
        if (rod == null)
            throw new System.InvalidOperationException("MapNhat does not contain Wooden_FishingRod.");

        Collider2D collider = rod.GetComponent<Collider2D>();
        if (collider == null)
        {
            BoxCollider2D box = Undo.AddComponent<BoxCollider2D>(rod);
            SpriteRenderer renderer = rod.GetComponent<SpriteRenderer>();
            if (renderer?.sprite != null)
            {
                box.size = renderer.sprite.bounds.size;
                box.offset = renderer.sprite.bounds.center;
            }
            collider = box;
        }
        collider.isTrigger = true;

        FishingSpotInteractable interactable = rod.GetComponent<FishingSpotInteractable>();
        if (interactable == null)
            interactable = Undo.AddComponent<FishingSpotInteractable>(rod);

        SerializedObject serialized = new(interactable);
        serialized.FindProperty("_definition").objectReferenceValue = spot;
        serialized.FindProperty("_controller").objectReferenceValue = controller;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(interactable);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Slider CreateVerticalSlider(Transform parent)
    {
        GameObject root = CreateRect("CatchProgress", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 490f));
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.03f, 0.06f, 0.08f, 1f);
        Slider slider = root.AddComponent<Slider>();
        slider.direction = Slider.Direction.BottomToTop;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject fillArea = CreateRect("Fill Area", root.transform, Vector2.zero, Vector2.one, Vector2.zero);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.offsetMin = new Vector2(5f, 5f);
        fillAreaRect.offsetMax = new Vector2(-5f, -5f);
        GameObject fill = CreateRect("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.97f, 0.73f, 0.16f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
        slider.interactable = false;
        return slider;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Color color)
    {
        GameObject panel = CreateRect(name, parent, anchorMin, anchorMax, size);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color)
    {
        GameObject textObject = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        GameObject result = new(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        RectTransform rect = result.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        return result;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string fullPath = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, name);
    }
}
