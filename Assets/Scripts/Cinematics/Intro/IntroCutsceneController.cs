using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Owns the presentation flow for the generated intro videos. The definition supplies immutable
/// content; this component only keeps temporary scene state and never changes tutorial or save progress.
/// </summary>
[DefaultExecutionOrder(-850)]
public sealed class IntroCutsceneController : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private IntroCutsceneDefinition _definition;
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private bool _playInDevelopment = true;

    [Header("Video")]
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RawImage _videoSurface;
    [SerializeField] private Vector2Int _renderResolution = new(1920, 1080);

    [Header("Dialogue Presentation")]
    [SerializeField] private GameObject _root;
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private GameObject _dialogueParchment;
    [SerializeField] private TMP_Text _speakerText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _skipSceneButton;
    [SerializeField] private Button _skipIntroButton;
    [SerializeField] private Image _fadeOverlay;
    [SerializeField, Min(0.05f)] private float _outroFadeDuration = 2f;
    [SerializeField, Min(0f)] private float _outroBlackHoldDuration = 0.15f;

    [Header("Scene Handoff")]
    [Tooltip("Fallback scene to load once the video ends. Only used when nothing is listening "
        + "to Completed (e.g. the in-engine Timeline Gameplay object is inactive) -- normally "
        + "GameplayTimelineController owns the transition after its Timeline finishes.")]
    [SerializeField] private string _fallbackNextSceneName = "MapNhat";

    private RenderTexture _renderTexture;
    private bool _ownsRenderTexture;
    private bool _isPlaying;
    private bool _isEnding;
    private bool _videoPrepared;
    private int _segmentIndex = -1;
    private int _lineIndex;

    public bool IsPlaying => _isPlaying;
    public event Action Completed;

    private void Awake()
    {
        // GameBootstrap (-900) has created the Development session by this point. Suppress music
        // before default-order MusicManager instances initialize and play their AudioSource.
        if (ShouldAutoPlay())
            MusicManager.SuppressBackgroundMusic();

        if (_videoPlayer != null)
        {
            _videoPlayer.playOnAwake = false;
            _videoPlayer.waitForFirstFrame = true;
            // Keep each generated clip's own ambience and effects. Gameplay background music is
            // suppressed separately by MusicManager until the Outro hand-off completes.
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _videoPlayer.prepareCompleted += HandleVideoPrepared;
            _videoPlayer.loopPointReached += HandleVideoLoopPoint;
            EnsureRenderTexture();
        }

        if (_nextButton != null)
            _nextButton.onClick.AddListener(Advance);
        if (_skipSceneButton != null)
            _skipSceneButton.onClick.AddListener(SkipScene);
        if (_skipIntroButton != null)
            _skipIntroButton.onClick.AddListener(SkipIntro);

        if (_root != null)
            _root.SetActive(false);
        SetFadeAlpha(0f);
    }

    private IEnumerator Start()
    {
        if (!ShouldAutoPlay())
            yield break;

        // Reached via SceneFlowService (MainMenu -> intro scene), GameState only flips
        // Loading -> Playing once GameplayReadinessGate finishes restoring the session
        // asynchronously, which can take more than one frame. Wait for it rather than a
        // fixed one-frame delay, or the cutscene can silently be skipped (PlayIntro() no-ops
        // unless CurrentState == Playing). Reached via GameBootstrap's DevelopmentGameplay
        // path, state is already Playing by the next frame, so this resolves immediately.
        float deadline = Time.unscaledTime + 10f;
        while (GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing)
        {
            if (Time.unscaledTime >= deadline)
                yield break;
            yield return null;
        }

        PlayIntro();
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

        bool nextPressed = Keyboard.current != null
            && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        if (nextPressed)
            Advance();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SkipScene();

        UpdateOutroFade();
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted -= HandleVideoPrepared;
            _videoPlayer.loopPointReached -= HandleVideoLoopPoint;
        }

        if (_nextButton != null)
            _nextButton.onClick.RemoveListener(Advance);
        if (_skipSceneButton != null)
            _skipSceneButton.onClick.RemoveListener(SkipScene);
        if (_skipIntroButton != null)
            _skipIntroButton.onClick.RemoveListener(SkipIntro);

        if (_ownsRenderTexture && _renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        if (_isPlaying)
            MusicManager.ResumeBackgroundMusic();
    }

    public void PlayIntro()
    {
        if (_isPlaying || _definition == null || _definition.Segments.Count == 0 || _videoPlayer == null)
            return;

        if (GameStateManager.Instance == null || GameStateManager.Instance.CurrentState != GameState.Playing)
            return;

        _isPlaying = true;
        _isEnding = false;
        GameStateManager.Instance.PushState(GameState.Cutscene);
        if (_root != null)
            _root.SetActive(true);
        // Start opaque, not transparent: VideoPlayer.Prepare() below is async and can take a
        // real chunk of time (codec startup), and until the first segment's video frame is
        // ready there is nothing in the RawImage yet -- a transparent overlay would let the
        // scene's own camera view (the local map/Player behind the cutscene canvas) show
        // through for that whole window. HandleVideoPrepared reveals the video once it
        // actually has a frame to show (waitForFirstFrame = true guarantees one is ready).
        SetFadeAlpha(1f);

        if (_director != null)
        {
            _director.time = 0d;
            _director.Evaluate();
            _director.Pause();
        }

        BeginSegment(0);
    }

    /// <summary>Called by the Intro Cue track when a Timeline editor preview enters a segment.</summary>
    public void BeginSegmentFromTimeline(int segmentIndex)
    {
        if (_isPlaying)
            BeginSegment(segmentIndex);
    }

    public void Advance()
    {
        if (!_isPlaying || !_definition.TryGetSegment(_segmentIndex, out IntroCutsceneSegment segment))
            return;

        if (!segment.RequiresPlayerAdvance)
            return;

        if (_lineIndex + 1 < segment.Lines.Count)
        {
            _lineIndex++;
            RefreshDialogue();
            return;
        }

        MoveToNextSegment();
    }

    public void SkipScene()
    {
        if (_isPlaying)
            MoveToNextSegment();
    }

    public void SkipIntro()
    {
        if (_isPlaying)
            Finish();
    }

    private bool ShouldAutoPlay()
    {
        GameSessionManager sessionManager = GameSessionManager.Instance;
        if (sessionManager == null)
            return false;

        GameSessionKind kind = sessionManager.Current.Kind;
        return kind == GameSessionKind.NewGame || (_playInDevelopment && kind == GameSessionKind.Development);
    }

    private void BeginSegment(int segmentIndex)
    {
        if (!_definition.TryGetSegment(segmentIndex, out IntroCutsceneSegment segment))
        {
            Finish();
            return;
        }

        _segmentIndex = segmentIndex;
        _lineIndex = 0;
        _videoPrepared = false;
        _videoPlayer.Stop();
        _videoPlayer.clip = segment.Video;
        _videoPlayer.isLooping = segment.RequiresPlayerAdvance;
        _videoPlayer.Prepare();
        RefreshDialogue();
    }

    private void MoveToNextSegment()
    {
        int nextIndex = _segmentIndex + 1;
        if (nextIndex >= _definition.Segments.Count)
        {
            Finish();
            return;
        }

        if (_director != null)
        {
            _director.time = nextIndex * 10d;
            _director.Evaluate();
            _director.Pause();
        }

        BeginSegment(nextIndex);
    }

    private void RefreshDialogue()
    {
        bool hasLine = _definition.TryGetSegment(_segmentIndex, out IntroCutsceneSegment segment)
            && segment.RequiresPlayerAdvance
            && _lineIndex >= 0
            && _lineIndex < segment.Lines.Count;

        bool showDialogue = hasLine
            && _segmentIndex > 0
            && _segmentIndex < _definition.Segments.Count - 1;

        if (_dialoguePanel != null)
            _dialoguePanel.SetActive(showDialogue);
        if (_dialogueParchment != null)
            _dialogueParchment.SetActive(showDialogue);

        if (!hasLine)
            return;

        IntroCutsceneLine line = segment.Lines[_lineIndex];
        if (_speakerText != null)
            _speakerText.text = line.SpeakerName;
        if (_bodyText != null)
            _bodyText.text = line.Text;
    }

    private void HandleVideoPrepared(VideoPlayer player)
    {
        _videoPrepared = true;
        if (!_isPlaying || player != _videoPlayer)
            return;

        for (ushort track = 0; track < player.audioTrackCount; track++)
        {
            player.EnableAudioTrack(track, true);
            player.SetDirectAudioVolume(track, 1f);
        }

        player.Play();

        // Only the very first segment needs this: later segments' Prepare() gaps stay covered
        // by the previous segment's last rendered frame, so this is a no-op past segment 0 (already 0).
        SetFadeAlpha(0f);
    }

    private void HandleVideoLoopPoint(VideoPlayer player)
    {
        if (!_isPlaying || !_videoPrepared || player != _videoPlayer)
            return;

        if (_definition.TryGetSegment(_segmentIndex, out IntroCutsceneSegment segment)
            && !segment.RequiresPlayerAdvance)
        {
            if (_segmentIndex == _definition.Segments.Count - 1)
            {
                StartCoroutine(HoldBlackThenFinish());
                return;
            }

            MoveToNextSegment();
        }
    }

    private void UpdateOutroFade()
    {
        if (_isEnding || _videoPlayer == null || !_videoPlayer.isPlaying || _segmentIndex != _definition.Segments.Count - 1)
            return;

        double remainingSeconds = GetOutroRemainingSeconds();
        if (remainingSeconds > _outroFadeDuration)
            return;

        SetFadeAlpha(1f - Mathf.Clamp01((float)(remainingSeconds / _outroFadeDuration)));
    }

    private double GetOutroRemainingSeconds()
    {
        if (_videoPlayer.frame >= 0 && _videoPlayer.frameCount > 0 && _videoPlayer.frameRate > 0d)
            return Math.Max(0d, ((double)_videoPlayer.frameCount - _videoPlayer.frame) / _videoPlayer.frameRate);

        return Math.Max(0d, _videoPlayer.length - _videoPlayer.time);
    }

    private IEnumerator HoldBlackThenFinish()
    {
        if (_isEnding)
            yield break;

        _isEnding = true;
        SetFadeAlpha(1f);

        if (_outroBlackHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(_outroBlackHoldDuration);

        Finish();
    }

    private void Finish()
    {
        _isPlaying = false;
        _isEnding = false;
        _videoPlayer?.Stop();
        if (_root != null)
            _root.SetActive(false);
        GameStateManager.Instance?.ResetToPlaying();
        MusicManager.ResumeBackgroundMusic();

        // Capture before invoking: if nobody subscribed (the Timeline Gameplay object was
        // toggled inactive for isolated testing), it will never call
        // SceneFlowService.TryLoadGameplay, so do it here instead of leaving the game stuck.
        bool hasSubscriber = Completed != null;
        Completed?.Invoke();

        if (!hasSubscriber
            && !string.IsNullOrWhiteSpace(_fallbackNextSceneName)
            && SceneFlowService.Instance != null)
        {
            SceneFlowService.Instance.TryLoadGameplay(_fallbackNextSceneName);
        }
    }

    private void SetFadeAlpha(float alpha)
    {
        if (_fadeOverlay == null)
            return;

        Color color = _fadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        _fadeOverlay.color = color;
    }

    private void EnsureRenderTexture()
    {
        if (_videoPlayer == null)
            return;

        _renderTexture = new RenderTexture(_renderResolution.x, _renderResolution.y, 0, RenderTextureFormat.ARGB32)
        {
            name = "RuntimeIntroCutsceneRenderTexture"
        };
        _renderTexture.Create();
        _ownsRenderTexture = true;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _renderTexture;
        if (_videoSurface != null)
            _videoSurface.texture = _renderTexture;
    }
}
