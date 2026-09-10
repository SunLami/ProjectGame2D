using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[DefaultExecutionOrder(-800)]
public sealed class GameplayTimelineController : MonoBehaviour
{
    [SerializeField] private IntroCutsceneController _introCutscene;
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private bool _playInDevelopment = true;

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
        if (_introCutscene != null)
        {
            _introCutscene.Completed -= Play;
            _introCutscene.Completed += Play;
        }
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

    private static void HandleStopped(PlayableDirector director) =>
        GameStateManager.Instance?.ResetToPlaying();
}
