using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Creates the reusable intro definition, Timeline and presentation prefab from approved clips.</summary>
public static class IntroCutsceneBuilder
{
    private const string DefinitionPath = "Assets/Cinematics/Intro/Definitions/IntroCutsceneDefinition.asset";
    private const string TimelinePath = "Assets/Cinematics/Intro/Timelines/IntroCutsceneTimeline.playable";
    private const string PrefabPath = "Assets/Prefabs/Cinematics/IntroCutscene.prefab";
    private const string DialogueFramePath = "Assets/Resources/UI/Dialogue/DarkInventoryStyle/dialogue_frame_v4.png";
    private const string DialogueButtonPath = "Assets/Resources/UI/Dialogue/DarkInventoryStyle/dialogue_action_button_v1.png";

    [MenuItem("Tools/Project Game 2D/Cinematics/Create Or Update Intro Cutscene")]
    public static void CreateOrUpdate()
    {
        IntroCutsceneDefinition definition = CreateDefinition();
        TimelineAsset timeline = CreateTimeline();
        GameObject prefab = CreatePrefab(definition, timeline);
        Selection.activeObject = prefab;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Intro cutscene assets are ready. Use 'Install Intro Cutscene In Active Scene' to place the prefab.");
    }

    [MenuItem("Tools/Project Game 2D/Cinematics/Install Intro Cutscene In Active Scene")]
    public static void InstallInActiveScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("Create the Intro Cutscene assets before installing them in a scene.");
            return;
        }

        IntroCutsceneController existing = UnityEngine.Object.FindAnyObjectByType<IntroCutsceneController>();
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            prefab, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        IntroCutsceneController installedIntro = instance.GetComponent<IntroCutsceneController>();

        // Replacing the intro prefab invalidates scene references held by the six-scene gameplay
        // Timeline. Restore that handoff so Completed starts the Timeline instead of using the
        // fallback scene load.
        foreach (GameplayTimelineController timelineController in UnityEngine.Object.FindObjectsByType<GameplayTimelineController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (timelineController.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                continue;

            SerializedObject timelineSerialized = new(timelineController);
            timelineSerialized.FindProperty("_introCutscene").objectReferenceValue = installedIntro;
            timelineSerialized.FindProperty("_skipSceneButtonRoot").objectReferenceValue =
                CreateTimelineSkipButton(timelineController.transform);
            timelineSerialized.ApplyModifiedPropertiesWithoutUndo();
        }
        // GameBootstrap owns the persistent EventSystem. Keeping another scene-local instance
        // produces duplicate-event-system errors when the Intro scene starts.
        foreach (EventSystem eventSystem in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (eventSystem.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Intro Cutscene installed in the active scene.");
    }

    private static GameObject CreateTimelineSkipButton(Transform timelineRoot)
    {
        Transform existing = timelineRoot.Find("GameplayTimelineUI");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject canvasObject = new("GameplayTimelineUI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(timelineRoot, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Sprite sprite = LoadUiSprite(DialogueButtonPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
        Button button = CreateButton("TimelineSkipSceneButton", canvasObject.transform, "SKIP SCENE",
            sprite, font, new Vector2(760f, -445f));
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 120f);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.fontSizeMin = 18f;
        label.fontSizeMax = 28f;
        button.gameObject.SetActive(false);
        return button.gameObject;
    }

    private static IntroCutsceneDefinition CreateDefinition()
    {
        IntroCutsceneDefinition definition = AssetDatabase.LoadAssetAtPath<IntroCutsceneDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<IntroCutsceneDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }

        SegmentSeed[] seeds =
        {
            new("intro.logo", "Logo Intro", "LogoIntro", Array.Empty<LineSeed>()),
            new("intro.story.firelight", "The First Spark", "IntroScene1", new[]
            {
                new LineSeed("Storyteller", "Long ago, a little girl carried a tiny lantern through a forest so dark that every tree seemed to whisper her name. She was afraid, but she never turned back."),
                new LineSeed("Young Hero", "Did she become a famous adventurer? Did anyone sing songs about her? I want to hear one someday... and maybe have a song of my own too."),
                new LineSeed("Storyteller", "Ha! That is a grand dream. Remember this: people do not remember heroes for their names, but for the moment they chose not to turn away.")
            }),
            new("intro.story.departure", "A Promise at Dawn", "IntroScene2", new[]
            {
                new LineSeed("Hero", "Am I really leaving? One step beyond this door, and everything changes. My heart is racing so fast that I cannot tell whether it is excitement or fear."),
                new LineSeed("A Memory", "It is all right to be afraid. It means you understand this journey matters. Just do not let fear choose your path for you. Promise me that."),
                new LineSeed("Hero", "I promise. I cannot promise to be brave every moment, but I will not give up easily. And I will come home with more stories than I can carry.")
            }),
            new("intro.story.open-road", "The Road Calls", "IntroScene3", new[]
            {
                new LineSeed("Hero", "Wow... the world is wider than every map I ever studied. Look at that road. It feels like it is calling my name. Or maybe that is just my stomach."),
                new LineSeed("Hero", "I do not know what waits ahead: treasure, monsters, or rain cruel enough to soak my pack. Thinking about it only makes me want to take another step."),
                new LineSeed("Hero", "That is fine. I have no title, no grand victory, and no one waiting to tell my story. Every famous adventurer was once a newcomer, too.")
            }),
            new("intro.story.village", "The Village of Beginnings", "IntroScene4", new[]
            {
                new LineSeed("Hero", "I finally made it... This village is even brighter than I imagined. A training yard, a market, farms, a windmill... there must always be something waiting to be done."),
                new LineSeed("Hero", "They say many adventurers began here. Some became heroes, some opened shops, and some simply found a home worth returning to."),
                new LineSeed("Hero", "I want to travel far, but first I should learn how to stand on my own here. Maybe this little village is the first page of my real adventure.")
            }),
            new("intro.story.training-yard", "A Guide Appears", "IntroScene5", new[]
            {
                new LineSeed("Guide", "You are new here, right? I can spot that look from a mile away: eager, confident... and completely unaware of how tiring a real adventure can be."),
                new LineSeed("Hero", "I want to become a famous adventurer! I want to see the world, do something I can be proud of, and someday hear people sing about the places I have been."),
                new LineSeed("Guide", "That is a fine dream. Before the whole world knows your name, let this village learn who you are. Come on. I will show you where an adventurer begins.")
            }),
            new("intro.outro.transition", "Into the Village", "OutroTransition", Array.Empty<LineSeed>())
        };

        SerializedObject serialized = new(definition);
        serialized.FindProperty("_cutsceneId").stringValue = "cutscene.intro.orynthals";
        SerializedProperty segments = serialized.FindProperty("_segments");
        segments.arraySize = seeds.Length;
        for (int index = 0; index < seeds.Length; index++)
        {
            SegmentSeed seed = seeds[index];
            SerializedProperty element = segments.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("_segmentId").stringValue = seed.Id;
            element.FindPropertyRelative("_displayName").stringValue = seed.Name;
            element.FindPropertyRelative("_video").objectReferenceValue = LoadVideo(seed.VideoName);
            SerializedProperty lines = element.FindPropertyRelative("_lines");
            lines.arraySize = seed.Lines.Length;
            for (int lineIndex = 0; lineIndex < seed.Lines.Length; lineIndex++)
            {
                lines.GetArrayElementAtIndex(lineIndex).FindPropertyRelative("_speakerName").stringValue = seed.Lines[lineIndex].Speaker;
                lines.GetArrayElementAtIndex(lineIndex).FindPropertyRelative("_text").stringValue = seed.Lines[lineIndex].Text;
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static TimelineAsset CreateTimeline()
    {
        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
        if (timeline != null)
            AssetDatabase.DeleteAsset(TimelinePath);

        timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, TimelinePath);
        IntroCutsceneCueTrack track = timeline.CreateTrack<IntroCutsceneCueTrack>(null, "Intro Cutscene Flow");
        string[] names = { "Logo Intro", "The First Spark", "A Promise at Dawn", "The Road Calls", "The Village of Beginnings", "A Guide Appears", "Into the Village" };
        for (int index = 0; index < names.Length; index++)
        {
            TimelineClip clip = track.CreateClip<IntroCutsceneCueClip>();
            clip.displayName = names[index];
            clip.start = index * 10d;
            clip.duration = 10d;
            ((IntroCutsceneCueClip)clip.asset).SegmentIndex = index;
        }
        EditorUtility.SetDirty(timeline);
        return timeline;
    }

    private static GameObject CreatePrefab(IntroCutsceneDefinition definition, TimelineAsset timeline)
    {
        GameObject root = new("IntroCutscene", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayableDirector), typeof(IntroCutsceneController));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // The controller stays active while this child is hidden between runs, so Start can
        // still wait for GameBootstrap and begin the cinematic.
        GameObject presentationRoot = new("PresentationRoot", typeof(RectTransform));
        presentationRoot.transform.SetParent(root.transform, false);
        RectTransform presentationRect = presentationRoot.GetComponent<RectTransform>();
        presentationRect.anchorMin = Vector2.zero;
        presentationRect.anchorMax = Vector2.one;
        presentationRect.offsetMin = Vector2.zero;
        presentationRect.offsetMax = Vector2.zero;
        // Keep the Game view clean while authoring. IntroCutsceneController remains active on
        // the prefab root and enables this presentation only after Play Mode starts the intro.
        presentationRoot.SetActive(false);

        GameObject videoObject = CreateRawImage("VideoSurface", presentationRoot.transform);
        RawImage surface = videoObject.GetComponent<RawImage>();
        VideoPlayer video = videoObject.AddComponent<VideoPlayer>();

        Sprite dialogueFrame = LoadUiSprite(DialogueFramePath);
        Sprite dialogueButton = LoadUiSprite(DialogueButtonPath);
        TMP_FontAsset dialogueFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");

        GameObject panel = CreatePanel("DialoguePanel", presentationRoot.transform, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 28f);
        panelRect.sizeDelta = new Vector2(1000f, 333f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = dialogueFrame;
        panelImage.type = Image.Type.Simple;
        panelImage.preserveAspect = true;

        TMP_Text speaker = CreateText("SpeakerText", panel.transform, 16, new Color32(244, 220, 166, 255),
            TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, "");
        SetRect(speaker.rectTransform, new Vector2(330f, -142f), new Vector2(210f, 24f));
        speaker.font = dialogueFont;
        speaker.enableAutoSizing = true;
        speaker.fontSizeMin = 11f;
        speaker.fontSizeMax = 16f;
        speaker.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text body = CreateText("BodyText", panel.transform, 18, new Color32(244, 232, 200, 255),
            TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.zero, "");
        // The frame's visible inner border sits farther in than the texture bounds. Keep the
        // dialogue inside that artwork safe area instead of merely inside the panel RectTransform.
        SetRect(body.rectTransform, new Vector2(0f, 38f), new Vector2(760f, 110f));
        body.font = dialogueFont;
        body.enableAutoSizing = true;
        body.fontSizeMin = 13f;
        body.fontSizeMax = 18f;
        body.overflowMode = TextOverflowModes.Ellipsis;

        Button next = CreateButton("NextButton", panel.transform, "NEXT", dialogueButton, dialogueFont, new Vector2(-300f, -108f));
        Button skipScene = CreateButton("SkipSceneButton", panel.transform, "SKIP SCENE", dialogueButton, dialogueFont, new Vector2(-105f, -108f));
        Button skipIntro = CreateButton("SkipIntroButton", panel.transform, "SKIP INTRO", dialogueButton, dialogueFont, new Vector2(90f, -108f));
        GameObject fadeObject = CreatePanel("FadeOverlay", presentationRoot.transform, new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image fadeOverlay = fadeObject.GetComponent<Image>();
        fadeOverlay.raycastTarget = false;

        PlayableDirector director = root.GetComponent<PlayableDirector>();
        director.playableAsset = timeline;
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
        IntroCutsceneController controller = root.GetComponent<IntroCutsceneController>();
        foreach (TrackAsset track in timeline.GetOutputTracks())
            director.SetGenericBinding(track, controller);

        SerializedObject serialized = new(controller);
        serialized.FindProperty("_definition").objectReferenceValue = definition;
        serialized.FindProperty("_director").objectReferenceValue = director;
        serialized.FindProperty("_videoPlayer").objectReferenceValue = video;
        serialized.FindProperty("_videoSurface").objectReferenceValue = surface;
        serialized.FindProperty("_root").objectReferenceValue = presentationRoot;
        serialized.FindProperty("_dialoguePanel").objectReferenceValue = panel;
        serialized.FindProperty("_speakerText").objectReferenceValue = speaker;
        serialized.FindProperty("_bodyText").objectReferenceValue = body;
        serialized.FindProperty("_nextButton").objectReferenceValue = next;
        serialized.FindProperty("_skipSceneButton").objectReferenceValue = skipScene;
        serialized.FindProperty("_skipIntroButton").objectReferenceValue = skipIntro;
        serialized.FindProperty("_fadeOverlay").objectReferenceValue = fadeOverlay;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static GameObject CreateRawImage(string name, Transform parent)
    {
        GameObject surface = new(name, typeof(RectTransform), typeof(RawImage));
        surface.transform.SetParent(parent, false);
        RectTransform rect = surface.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        surface.GetComponent<RawImage>().color = Color.white;
        return surface;
    }

    private static TMP_Text CreateText(string name, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, string value)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.text = value;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Sprite sprite, TMP_FontAsset font, Vector2 position)
    {
        GameObject buttonObject = CreatePanel(name, parent, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(168f, 56f));
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(255, 242, 190, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color32(190, 165, 115, 255);
        button.colors = colors;
        TMP_Text text = CreateText("Label", buttonObject.transform, 15, new Color32(244, 232, 200, 255), TextAlignmentOptions.Center, Vector2.zero, Vector2.one, label);
        text.font = font;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 15f;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Sprite LoadUiSprite(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new MissingReferenceException($"Intro cutscene UI sprite is missing: {path}");
        return sprite;
    }

    private static VideoClip LoadVideo(string fileName)
    {
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>($"Assets/Cinematics/Intro/Videos/{fileName}.mp4");
        if (clip == null)
            Debug.LogError($"Missing intro video: {fileName}.mp4");
        return clip;
    }

    private readonly struct SegmentSeed
    {
        public SegmentSeed(string id, string name, string videoName, LineSeed[] lines) { Id = id; Name = name; VideoName = videoName; Lines = lines; }
        public string Id { get; }
        public string Name { get; }
        public string VideoName { get; }
        public LineSeed[] Lines { get; }
    }

    private readonly struct LineSeed
    {
        public LineSeed(string speaker, string text) { Speaker = speaker; Text = text; }
        public string Speaker { get; }
        public string Text { get; }
    }
}
