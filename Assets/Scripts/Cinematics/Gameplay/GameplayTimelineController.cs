using System.Collections;
using System.Collections.Generic;
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

    [Header("Timeline Dialogue Cues")]
    [Tooltip("Wired to a Signal Emitter (via Signal Receiver) once Player has walked into the "
        + "village alone, before TruongLang appears. Pauses the director for this solo line, "
        + "then resumes -- TruongLang's entrance only starts once this closes.")]
    [SerializeField] private DialogueDefinition _scene1PlayerArrivalDialogue;

    [Tooltip("Wired to a Signal Emitter at the moment TruongLang reaches Player. Pauses the "
        + "director, shows the greeting, and resumes playback once closed.")]
    [SerializeField] private DialogueDefinition _scene1Dialogue;

    [System.Serializable]
    private struct ActorIdlePose
    {
        public GameObject actor;
        public AnimationClip idleClip;
    }

    [Tooltip("Force-applied the instant the Player-arrival dialogue pauses the Timeline -- "
        + "Player only, since TruongLang hasn't entered yet. See _scene1GreetingIdlePoses for "
        + "why this force-sample exists (stale clip-boundary frame on Pause()).")]
    [SerializeField] private List<ActorIdlePose> _scene1ArrivalIdlePoses = new();

    [Tooltip("Force-applied the instant the greeting dialogue pauses the Timeline. AnimationTrack "
        + "clip switches (e.g. walk -> idle-facing) can land one frame stale when Pause() lands "
        + "mid-frame, before that clip boundary's track evaluation runs -- for a looping "
        + "sprite-swap clip the stale frame can read as a completely different, wrong pose "
        + "(e.g. idle-down instead of idle-right) since frames aren't visually interpolatable. "
        + "Sampling the intended idle clip directly guarantees the correct held pose regardless.")]
    [SerializeField] private List<ActorIdlePose> _scene1GreetingIdlePoses = new();

    [Header("Actor Doubles")]
    [Tooltip("Real gameplay objects (Player, NPCs) stood in for by dedicated cutscene actor "
        + "clones. Hidden for the duration of the Timeline, restored when it stops -- an "
        + "ActivationTrack could do this instead, but its active/inactive window only applies "
        + "reliably during an actual Play, not Timeline preview scrubbing in the Editor, which "
        + "made it hard to verify; this direct toggle is unambiguous either way.")]
    [SerializeField] private List<GameObject> _actorDoublesToHide = new();
    private readonly List<bool> _actorDoublesPreviousActive = new();

    [Tooltip("Name of the persistent gameplay HUD root (lives in the Bootstrap scene, so it "
        + "can't be dragged in here from this scene) to hide for the duration of the Timeline.")]
    [SerializeField] private string _hudRootName = "GameplayUIRoot";
    private GameObject _hudRoot;
    private bool _hudRootPreviousActive;

    private void Awake()
    {
        if (_director == null)
            _director = GetComponent<PlayableDirector>();

        if (_director != null)
        {
            _director.playOnAwake = false;
            _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            // Hold (the project default) makes the director sit at its last frame forever
            // instead of stopping when it reaches the end -- which means `stopped` never fires,
            // so nothing here ever restores the HUD/actor doubles or hands off to MapNhat. This
            // Timeline is single-use for this cutscene, so auto-stopping at the end is correct.
            _director.extrapolationMode = DirectorWrapMode.None;
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
        _director.extrapolationMode = DirectorWrapMode.None;

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
        HideActorDoubles();
        HideHud();
        // Each ActorDialogueTrack's "already shown this line" bookkeeping lives outside the
        // PlayableGraph (see ActorDialogueMixerBehaviour) specifically so it survives the
        // Pause()/Play() cycle every dialogue line goes through mid-cutscene. Clear it only here,
        // where a cutscene genuinely (re)starts from the top.
        ActorDialogueMixerBehaviour.ResetAll();
        _director.time = 0d;
        _director.Play();
    }

    private void HideHud()
    {
        _hudRoot = GameObject.Find(_hudRootName);
        if (_hudRoot == null)
            return;
        _hudRootPreviousActive = _hudRoot.activeSelf;
        _hudRoot.SetActive(false);
    }

    private void RestoreHud()
    {
        if (_hudRoot != null)
            _hudRoot.SetActive(_hudRootPreviousActive);
        _hudRoot = null;
    }

    private void HideActorDoubles()
    {
        _actorDoublesPreviousActive.Clear();
        foreach (GameObject actor in _actorDoublesToHide)
        {
            if (actor == null)
                continue;
            _actorDoublesPreviousActive.Add(actor.activeSelf);
            actor.SetActive(false);
        }
    }

    private void RestoreActorDoubles()
    {
        for (int i = 0; i < _actorDoublesToHide.Count && i < _actorDoublesPreviousActive.Count; i++)
        {
            if (_actorDoublesToHide[i] != null)
                _actorDoublesToHide[i].SetActive(_actorDoublesPreviousActive[i]);
        }
    }

    private bool ShouldPlay()
    {
        GameSessionManager session = GameSessionManager.Instance;
        if (session == null)
            return false;

        return session.Current.Kind == GameSessionKind.NewGame
            || (_playInDevelopment && session.Current.Kind == GameSessionKind.Development);
    }

    /// <summary>Called by a Signal Emitter/Receiver pair once Player has walked into the
    /// village alone. Pauses the Timeline for this solo line; TruongLang's entrance only
    /// starts once it closes and playback resumes.</summary>
    public void PlayScene1PlayerArrival()
    {
        PlayDialogueAndResume(_scene1PlayerArrivalDialogue, _scene1ArrivalIdlePoses);
    }

    /// <summary>Called by a Signal Emitter/Receiver pair at the point in Scene 1 where TruongLang
    /// reaches Player. Pauses the Timeline for as long as the player takes to read the dialogue,
    /// then resumes playback from the same point.</summary>
    public void PlayScene1Dialogue()
    {
        PlayDialogueAndResume(_scene1Dialogue, _scene1GreetingIdlePoses);
    }

    private void PlayDialogueAndResume(DialogueDefinition definition, List<ActorIdlePose> idlePoses)
    {
        if (_director == null || definition == null || DialogueUI.Instance == null)
            return;

        _director.Pause();
        _director.Evaluate();
        ForceIdlePoses(idlePoses);
        if (!DialogueUI.Instance.Open(definition, _ => ResumeFromDialogue(idlePoses)))
            ResumeFromDialogue(idlePoses);
    }

    private void ResumeFromDialogue(List<ActorIdlePose> idlePoses)
    {
        SetActorAnimatorsEnabled(idlePoses, true);
        _director.Play();
    }

    private void ForceIdlePoses(List<ActorIdlePose> idlePoses)
    {
        // A PlayableDirector that is Paused (not Stopped) keeps its graph connected, so the
        // bound Animators re-run their own Update every frame and immediately overwrite this
        // forced pose with whatever the Timeline graph is evaluating at the clip boundary --
        // which, for a hash-curve-driven sprite swap, can land on a blended, meaningless frame
        // (e.g. a walk-cycle frame instead of the idle-facing pose). Disabling the Animators
        // stops that re-drive so the sampled pose actually holds; ResumeFromDialogue turns them
        // back on before resuming playback.
        SetActorAnimatorsEnabled(idlePoses, false);
        foreach (ActorIdlePose pose in idlePoses)
        {
            if (pose.actor != null && pose.idleClip != null)
                pose.idleClip.SampleAnimation(pose.actor, 0f);
        }
    }

    private void SetActorAnimatorsEnabled(List<ActorIdlePose> idlePoses, bool enabled)
    {
        foreach (ActorIdlePose pose in idlePoses)
        {
            if (pose.actor == null)
                continue;
            Animator animator = pose.actor.GetComponent<Animator>();
            if (animator != null)
                animator.enabled = enabled;
        }
    }

    private void HandleStopped(PlayableDirector director)
    {
        RestoreActorDoubles();
        RestoreHud();

        if (string.IsNullOrWhiteSpace(_nextSceneName)
            || SceneFlowService.Instance == null
            || !SceneFlowService.Instance.TryLoadGameplay(_nextSceneName))
        {
            GameStateManager.Instance?.ResetToPlaying();
        }
    }
}
