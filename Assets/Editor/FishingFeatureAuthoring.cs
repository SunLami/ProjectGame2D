using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
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
    private const string UiFolder = "Assets/Resources/UI/Fishing/DarkInventoryStyle/";
    private const string BobberControllerPath = PrefabFolder + "/FishingBobberBite.controller";

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
        Sprite waitingPanelSprite = LoadSprite(UiFolder + "fishing_waiting_panel_v3.png");
        Sprite bitePromptSprite = LoadSprite(UiFolder + "fishing_bite_prompt_v3.png");
        Sprite minigamePanelSprite = LoadSprite(UiFolder + "fishing_minigame_panel_v3.png");
        Sprite movementTrackSprite = LoadSprite(UiFolder + "fishing_movement_track_v3.png");
        Sprite catchZoneSprite = LoadSprite(UiFolder + "fishing_catch_zone_v3.png");
        Sprite sliderTrackSprite = LoadSprite(UiFolder + "fishing_slider_track_v3.png");
        Sprite sliderFillSprite = LoadSprite(UiFolder + "fishing_slider_fill_v3.png");
        Sprite resultPanelSprite = LoadSprite(UiFolder + "fishing_result_panel_v3.png");
        Sprite bobberFrame0 = LoadSprite(UiFolder + "fishing_bobber_bite_00.png");
        Sprite mysteryFishSprite = TryLoadSprite(UiFolder + "fishing_mystery_fish_icon.png");
        AnimationClip bobberClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UiFolder + "fishing_bobber_bite_loop.anim");
        RuntimeAnimatorController bobberController = GetOrCreateBobberController(bobberClip);

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

        GameObject waiting = CreateSpritePanel("WaitingPanel", uiRoot.transform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(500f, 74f), waitingPanelSprite);
        TMP_Text waitingText = CreateText("WaitingText", waiting.transform, font, "Waiting for a bite...", 30f, Color.white);

        GameObject bite = CreateSpritePanel("BitePrompt", uiRoot.transform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(150f, 150f), bitePromptSprite);
        GameObject bobberObject = CreateRect("BobberIcon", bite.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(48f, 64f));
        Image bobberImage = bobberObject.AddComponent<Image>();
        bobberImage.sprite = bobberFrame0;
        bobberImage.preserveAspect = true;
        bobberImage.color = Color.white;
        bobberImage.raycastTarget = false;
        Animator bobberAnimator = bobberObject.AddComponent<Animator>();
        bobberAnimator.runtimeAnimatorController = bobberController;

        GameObject minigame = CreateSpritePanel("MinigamePanel", uiRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 650f), minigamePanelSprite);
        TMP_Text timer = CreateText("Timer", minigame.transform, font, "18s", 34f, Color.white);
        RectTransform timerRect = timer.rectTransform;
        timerRect.anchorMin = timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0f, -22f);
        timerRect.sizeDelta = new Vector2(180f, 50f);

        GameObject trackObject = CreateSpritePanel("MovementTrack", minigame.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 490f), movementTrackSprite);
        RectTransform track = trackObject.GetComponent<RectTransform>();
        track.anchoredPosition = new Vector2(-75f, -10f);
        GameObject catchObject = CreateSpritePanel("CatchZone", track, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(128f, 120f), catchZoneSprite);
        RectTransform catchZone = catchObject.GetComponent<RectTransform>();
        Image catchZoneImage = catchObject.GetComponent<Image>();
        catchZoneImage.type = Image.Type.Sliced;
        GameObject fishObject = CreateRect("FishIcon", track, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 62f));
        Image fishImage = fishObject.AddComponent<Image>();
        fishImage.preserveAspect = true;
        fishImage.raycastTarget = false;
        RectTransform fishIcon = fishObject.GetComponent<RectTransform>();

        Slider progress = CreateVerticalSlider(minigame.transform, sliderTrackSprite, sliderFillSprite);
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchoredPosition = new Vector2(105f, -10f);

        GameObject result = CreateSpritePanel("ResultPanel", uiRoot.transform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(620f, 180f), resultPanelSprite);
        GameObject resultIconObject = CreateRect("ResultFishIcon", result.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 90f));
        resultIconObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(85f, 0f);
        Image resultFishImage = resultIconObject.AddComponent<Image>();
        resultFishImage.preserveAspect = true;
        resultFishImage.raycastTarget = false;
        resultFishImage.enabled = false;
        TMP_Text resultText = CreateText("ResultText", result.transform, font, string.Empty, 30f, Color.white);
        resultText.rectTransform.offsetMin = new Vector2(140f, 0f);

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
        uiSerialized.FindProperty("_mysteryFishSprite").objectReferenceValue = mysteryFishSprite;
        uiSerialized.FindProperty("_resultFishImage").objectReferenceValue = resultFishImage;
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

    private static Slider CreateVerticalSlider(Transform parent, Sprite trackSprite, Sprite fillSprite)
    {
        GameObject root = CreateRect("CatchProgress", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 490f));
        Image background = root.AddComponent<Image>();
        background.sprite = trackSprite;
        background.type = Image.Type.Simple;
        background.color = Color.white;
        Slider slider = root.AddComponent<Slider>();
        slider.direction = Slider.Direction.BottomToTop;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject fillArea = CreateRect("Fill Area", root.transform, Vector2.zero, Vector2.one, Vector2.zero);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        // Horizontal stays flush (0) so fill uses the full track width; vertical keeps a small inset
        // so the fill doesn't paint over the track sprite's own top/bottom gold end-cap ornament.
        fillAreaRect.offsetMin = new Vector2(0f, 12f);
        fillAreaRect.offsetMax = new Vector2(0f, -12f);
        GameObject fill = CreateRect("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.sprite = fillSprite;
        fillImage.type = Image.Type.Simple;
        fillImage.color = Color.white;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
        slider.interactable = false;
        return slider;
    }

    private static GameObject CreateSpritePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Sprite sprite)
    {
        GameObject panel = CreateRect(name, parent, anchorMin, anchorMax, size);
        Image image = panel.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        return panel;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new System.InvalidOperationException("Fishing UI sprite not found: " + path);
        return sprite;
    }

    private static Sprite TryLoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static RuntimeAnimatorController GetOrCreateBobberController(AnimationClip clip)
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(BobberControllerPath);
        if (existing != null)
            return existing;

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BobberControllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = stateMachine.AddState("BiteLoop");
        state.motion = clip;
        stateMachine.defaultState = state;
        return controller;
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
