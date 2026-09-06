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

        PrefabUtility.InstantiatePrefab(prefab, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            CreateEventSystem();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Intro Cutscene installed in the active scene.");
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

        GameObject videoObject = CreateRawImage("VideoSurface", presentationRoot.transform);
        RawImage surface = videoObject.GetComponent<RawImage>();
        VideoPlayer video = videoObject.AddComponent<VideoPlayer>();

        GameObject panel = CreatePanel("DialoguePanel", presentationRoot.transform, new Color(0.025f, 0.055f, 0.09f, 0.9f), new Vector2(0.05f, 0.035f), new Vector2(0.95f, 0.29f), Vector2.zero, Vector2.zero);
        TMP_Text speaker = CreateText("SpeakerText", panel.transform, 38, new Color(1f, 0.78f, 0.28f), TextAlignmentOptions.Left, new Vector2(0.035f, 0.66f), new Vector2(0.65f, 0.95f), "");
        TMP_Text body = CreateText("BodyText", panel.transform, 29, Color.white, TextAlignmentOptions.TopLeft, new Vector2(0.035f, 0.12f), new Vector2(0.94f, 0.7f), "");
        Button next = CreateButton("NextButton", panel.transform, "NEXT", new Vector2(0.79f, 0.69f), new Vector2(0.96f, 0.93f));
        Button skipScene = CreateButton("SkipSceneButton", panel.transform, "SKIP SCENE", new Vector2(0.72f, 0.41f), new Vector2(0.96f, 0.63f));
        Button skipIntro = CreateButton("SkipIntroButton", panel.transform, "SKIP INTRO", new Vector2(0.72f, 0.15f), new Vector2(0.96f, 0.35f));

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

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.12f, 0.27f, 0.38f, 0.96f), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        TMP_Text text = CreateText("Label", buttonObject.transform, 22, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, label);
        text.raycastTarget = false;
        return button;
    }

    private static VideoClip LoadVideo(string fileName)
    {
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>($"Assets/Cinematics/Intro/Videos/{fileName}.mp4");
        if (clip == null)
            Debug.LogError($"Missing intro video: {fileName}.mp4");
        return clip;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
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
