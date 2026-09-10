using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[DefaultExecutionOrder(-800)]
public sealed class GameplayTimelineController : MonoBehaviour
{
    [SerializeField] private IntroCutsceneController _introCutscene;
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private bool _playInDevelopment = true;
    [SerializeField] private string _nextSceneName = "MapNhat";

    private void Awake()
    {
        if (_director == null)
            _director = GetComponent<PlayableDirector>();

        if (_director != null)
        {
            _director.playOnAwake = false;
            _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            _director.stopped += HandleStopped;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnIntroObject()
    {
        GameObject introObject = GameObject.Find("Intro");
        Player player = FindAnyObjectByType<Player>();
        IntroCutsceneController introCutscene = FindAnyObjectByType<IntroCutsceneController>();
        if (introObject == null || player == null || introCutscene == null)
            return;

        PlayableDirector director = introObject.GetComponent<PlayableDirector>();
        if (director == null || director.playableAsset == null)
            return;

        GameplayTimelineController controller = introObject.GetComponent<GameplayTimelineController>()
            ?? introObject.AddComponent<GameplayTimelineController>();
        controller.Configure(introCutscene, director, player.transform);
    }

    private void Configure(IntroCutsceneController introCutscene, PlayableDirector director, Transform player)
    {
        if (_introCutscene != null)
            _introCutscene.Completed -= Play;

        _introCutscene = introCutscene;
        _director = director;
        _director.playOnAwake = false;
        _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

        if (_director.playableAsset is TimelineAsset timeline)
        {
            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                if (track is AnimationTrack)
                    _director.SetGenericBinding(track, player.GetComponent<Animator>());
            }
        }

        _introCutscene.Completed += Play;
    }

    private void Start()
    {
        // _introCutscene is a scene reference, not a live subscription check: even when its
        // GameObject is active, only re-subscribe here -- Awake/Configure already wired this
        // once, and this just guards against double calls to Play. When the IntroCutscene
        // object is inactive (author toggled it off in the Hierarchy for isolated testing),
        // its Completed event never fires, so this Timeline would otherwise never play. Fall
        // back to driving it directly in that case.
        if (_introCutscene != null && _introCutscene.isActiveAndEnabled)
        {
            _introCutscene.Completed -= Play;
            _introCutscene.Completed += Play;
            return;
        }

        StartCoroutine(PlayWhenReady());
    }

    private IEnumerator PlayWhenReady()
    {
        float deadline = Time.unscaledTime + 10f;
        while (GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing)
        {
            if (Time.unscaledTime >= deadline)
                yield break;
            yield return null;
        }

        Play();
    }

    private void OnDestroy()
    {
        if (_introCutscene != null)
            _introCutscene.Completed -= Play;
        if (_director != null)
            _director.stopped -= HandleStopped;
    }

    public void Play()
    {
        if (_director == null || _director.playableAsset == null || !ShouldPlay())
            return;

        GameStateManager.Instance?.PushState(GameState.Cutscene);
        _director.time = 0d;
        _director.Play();
    }

    private bool ShouldPlay()
    {
        GameSessionManager session = GameSessionManager.Instance;
        if (session == null)
            return false;

        return session.Current.Kind == GameSessionKind.NewGame
            || (_playInDevelopment && session.Current.Kind == GameSessionKind.Development);
    }

    private void HandleStopped(PlayableDirector director)
    {
        if (string.IsNullOrWhiteSpace(_nextSceneName)
            || SceneFlowService.Instance == null
            || !SceneFlowService.Instance.TryLoadGameplay(_nextSceneName))
        {
            GameStateManager.Instance?.ResetToPlaying();
        }
    }
}
