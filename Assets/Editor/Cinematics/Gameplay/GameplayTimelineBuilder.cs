using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public static class GameplayTimelineBuilder
{
    private const string TimelinePath = "Assets/Timeline/IntroTimeline.playable";
    private const string AnimationFolder = "Assets/Timeline/Animations";
    private const string MovementClipPath = AnimationFolder + "/Scene01_PlayerCrossBridge.anim";
    private const string WalkRightClipPath = "Assets/Animations/PlayerAnimations/WalkRight.anim";
    private const string MapNhatPath = "Assets/Scenes/MapNhat.unity";
    private const string SetupVersion = "GameplayScene01:v4-native-animation-track";
    private static readonly Vector3 BridgeLeft = new(-27.88571f, -5.71325f, 0f);
    private static readonly Vector3 BridgeRight = new(-24.25f, -6.42f, 0f);

    [InitializeOnLoadMethod]
    private static void InstallRequestedSceneAfterCompile()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || HasScene01Timeline())
            {
                return;
            }

            CreateScene01();
        };
    }

    private static bool HasScene01Timeline()
    {
        AssetImporter importer = AssetImporter.GetAtPath(TimelinePath);
        return importer != null && importer.userData == SetupVersion;
    }

    [MenuItem("Tools/Project Game 2D/Cinematics/Gameplay/Create Scene 01 - Cross Bridge")]
    public static void CreateScene01()
    {
        TimelineAsset timeline = CreateTimeline();
        InstallInMapNhat(timeline);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static TimelineAsset CreateTimeline()
    {
        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
        if (timeline == null)
        {
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "IntroTimeline";
            AssetDatabase.CreateAsset(timeline, TimelinePath);
        }
        else
        {
            TrackAsset[] existingTracks = new System.Collections.Generic.List<TrackAsset>(timeline.GetRootTracks()).ToArray();
            foreach (TrackAsset track in existingTracks)
                timeline.DeleteTrack(track);
        }

        AnimationClip movementClip = CreateMovementAnimation();
        GroupTrack sceneGroup = timeline.CreateTrack<GroupTrack>(null, "SCENE 01 — CROSS THE BRIDGE");
        AnimationTrack movement = timeline.CreateTrack<AnimationTrack>(sceneGroup, "PLAYER — Position + WalkRight Keyframes");
        TimelineClip clip = movement.CreateClip(movementClip);
        clip.displayName = "Cross Bridge (4 seconds)";
        clip.start = 0d;
        clip.duration = 4d;

        timeline.fixedDuration = 4d;
        EditorUtility.SetDirty(timeline);
        return timeline;
    }

    private static AnimationClip CreateMovementAnimation()
    {
        if (!AssetDatabase.IsValidFolder(AnimationFolder))
            AssetDatabase.CreateFolder("Assets/Timeline", "Animations");

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MovementClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Scene01_PlayerCrossBridge" };
            AssetDatabase.CreateAsset(clip, MovementClipPath);
        }
        else
        {
            clip.ClearCurves();
        }

        clip.frameRate = 60f;
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.x",
            AnimationCurve.Linear(0f, BridgeLeft.x, 4f, BridgeRight.x));
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.y",
            AnimationCurve.Linear(0f, BridgeLeft.y, 4f, BridgeRight.y));
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.z",
            AnimationCurve.Constant(0f, 4f, 0f));

        AnimationClip walkRight = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkRightClipPath);
        if (walkRight == null)
        {
            Debug.LogError($"Missing Player walk animation: {WalkRightClipPath}");
            return clip;
        }

        float cycleDuration = Mathf.Max(1f / walkRight.frameRate, walkRight.length);
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(walkRight))
        {
            ObjectReferenceKeyframe[] sourceFrames = AnimationUtility.GetObjectReferenceCurve(walkRight, binding);
            var repeatedFrames = new System.Collections.Generic.List<ObjectReferenceKeyframe>();
            for (float offset = 0f; offset < 4f; offset += cycleDuration)
            {
                foreach (ObjectReferenceKeyframe sourceFrame in sourceFrames)
                {
                    float time = offset + sourceFrame.time;
                    if (time >= 4f)
                        break;
                    repeatedFrames.Add(new ObjectReferenceKeyframe { time = time, value = sourceFrame.value });
                }
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, repeatedFrames.ToArray());
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void InstallInMapNhat(TimelineAsset timeline)
    {
        Scene scene = SceneManager.GetSceneByPath(MapNhatPath);
        bool openedForInstall = !scene.isLoaded;
        if (openedForInstall)
            scene = EditorSceneManager.OpenScene(MapNhatPath, OpenSceneMode.Additive);

        Player player = FindInScene<Player>(scene);
        IntroCutsceneController intro = FindInScene<IntroCutsceneController>(scene);
        if (player == null || intro == null)
        {
            Debug.LogError("Scene 01 requires both Player and IntroCutsceneController in MapNhat.");
            if (openedForInstall)
                EditorSceneManager.CloseScene(scene, true);
            return;
        }

        GameObject root = FindRoot(scene, "Intro");
        if (root == null)
        {
            Debug.LogError("MapNhat requires the existing root GameObject named 'Intro'.");
            if (openedForInstall)
                EditorSceneManager.CloseScene(scene, true);
            return;
        }

        PlayableDirector director = root.GetComponent<PlayableDirector>() ?? root.AddComponent<PlayableDirector>();
        GameplayTimelineController controller = root.GetComponent<GameplayTimelineController>() ?? root.AddComponent<GameplayTimelineController>();

        director.playableAsset = timeline;
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.Hold;
        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            if (track is AnimationTrack)
                director.SetGenericBinding(track, player.GetComponent<Animator>());
        }

        SerializedObject serialized = new(controller);
        serialized.FindProperty("_introCutscene").objectReferenceValue = intro;
        serialized.FindProperty("_director").objectReferenceValue = director;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        player.transform.position = BridgeLeft;
        Selection.activeObject = root;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetImporter importer = AssetImporter.GetAtPath(TimelinePath);
        importer.userData = SetupVersion;
        importer.SaveAndReimport();
        if (openedForInstall)
            EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Gameplay Timeline Scene 01 is installed. Open Window > Sequencing > Timeline to edit the bridge shot.");
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null)
                return result;
        }

        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }
}
