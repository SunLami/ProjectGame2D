using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class StudioIntroController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    [SerializeField] private VideoClip _videoClip;
    [SerializeField, Min(1f)] private float _prepareTimeoutSeconds = 10f;
    [SerializeField, Min(0f)] private float _minimumPlaybackSeconds = 8f;

    private VideoPlayer _videoPlayer;
    private AudioSource _audioSource;
    private AudioListener _introAudioListener;
    private Camera _introCamera;
    private RenderTexture _renderTexture;
    private RawImage _videoSurface;
    private float _playbackStartedAt;
    private bool _leaving;

    private void Awake()
    {
        MusicManager.SuppressBackgroundMusic();
        CreatePresentation();

        if (_videoClip == null)
        {
            Debug.LogError("Studio intro VideoClip is missing. Continuing to MainMenu.", this);
            StartCoroutine(LoadMainMenu());
            return;
        }

        ConfigureVideoPlayer();
        ApplySfxVolume();
        if (SettingsService.Instance != null)
            SettingsService.Instance.Changed += HandleSettingsChanged;

        _videoPlayer.Prepare();
        StartCoroutine(PrepareTimeout());
    }

    private void OnDestroy()
    {
        if (SettingsService.Instance != null)
            SettingsService.Instance.Changed -= HandleSettingsChanged;

        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted -= HandlePrepared;
            _videoPlayer.loopPointReached -= HandleFinished;
            _videoPlayer.errorReceived -= HandleVideoError;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }

    private void CreatePresentation()
    {
        GameObject cameraObject = new("StudioIntroCamera", typeof(Camera));
        _introCamera = cameraObject.GetComponent<Camera>();
        _introCamera.clearFlags = CameraClearFlags.SolidColor;
        _introCamera.backgroundColor = Color.black;
        _introCamera.cullingMask = 0;
        _introCamera.depth = -100f;

        GameObject canvasObject = new("StudioIntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject backgroundObject = new("BlackBackground", typeof(RectTransform), typeof(Image));
        RectTransform background = backgroundObject.GetComponent<RectTransform>();
        background.SetParent(canvasObject.transform, false);
        StretchToParent(background);
        backgroundObject.GetComponent<Image>().color = Color.black;

        GameObject surfaceObject = new("VideoSurface", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        RectTransform surface = surfaceObject.GetComponent<RectTransform>();
        surface.SetParent(canvasObject.transform, false);
        surface.anchorMin = new Vector2(0.5f, 0.5f);
        surface.anchorMax = new Vector2(0.5f, 0.5f);
        surface.anchoredPosition = Vector2.zero;
        surface.sizeDelta = Vector2.one;

        _videoSurface = surfaceObject.GetComponent<RawImage>();
        _videoSurface.raycastTarget = false;
        _videoSurface.enabled = false;
    }

    private void ConfigureVideoPlayer()
    {
        int width = _videoClip.width > 0 ? (int)_videoClip.width : 1920;
        int height = _videoClip.height > 0 ? (int)_videoClip.height : 1080;
        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "StudioIntroVideo"
        };
        _renderTexture.Create();

        _videoSurface.texture = _renderTexture;
        AspectRatioFitter fitter = _videoSurface.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)width / height;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;

        if (FindAnyObjectByType<AudioListener>() == null)
            _introAudioListener = gameObject.AddComponent<AudioListener>();

        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.isLooping = false;
        _videoPlayer.waitForFirstFrame = true;
        _videoPlayer.skipOnDrop = false;
        _videoPlayer.source = VideoSource.VideoClip;
        _videoPlayer.clip = _videoClip;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _renderTexture;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _videoPlayer.controlledAudioTrackCount = 1;
        _videoPlayer.EnableAudioTrack(0, true);
        _videoPlayer.SetTargetAudioSource(0, _audioSource);
        _videoPlayer.prepareCompleted += HandlePrepared;
        _videoPlayer.loopPointReached += HandleFinished;
        _videoPlayer.errorReceived += HandleVideoError;
    }

    private IEnumerator PrepareTimeout()
    {
        float waited = 0f;
        while (!_leaving && _videoPlayer != null && !_videoPlayer.isPrepared && waited < _prepareTimeoutSeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_leaving && _videoPlayer != null && !_videoPlayer.isPrepared)
        {
            Debug.LogError("Studio intro video preparation timed out. Continuing to MainMenu.", this);
            yield return LoadMainMenu();
        }
    }

    private void HandlePrepared(VideoPlayer player)
    {
        _videoSurface.enabled = true;
        _playbackStartedAt = Time.unscaledTime;
        player.Play();
    }

    private void HandleFinished(VideoPlayer player)
    {
        if (!_leaving)
            StartCoroutine(CompleteAfterMinimumPlayback());
    }

    private void HandleVideoError(VideoPlayer player, string message)
    {
        Debug.LogError($"Studio intro video could not be played: {message}. Continuing to MainMenu.", this);
        if (!_leaving)
            StartCoroutine(LoadMainMenu());
    }

    private IEnumerator CompleteAfterMinimumPlayback()
    {
        float remaining = _minimumPlaybackSeconds - (Time.unscaledTime - _playbackStartedAt);
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        yield return LoadMainMenu();
    }

    private IEnumerator LoadMainMenu()
    {
        if (_leaving)
            yield break;

        _leaving = true;
        if (_videoPlayer != null)
            _videoPlayer.Stop();
        if (_introAudioListener != null)
            _introAudioListener.enabled = false;

        AsyncOperation load = SceneManager.LoadSceneAsync(MainMenuSceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError("Could not start loading MainMenu from StudioIntro.", this);
            yield break;
        }

        yield return load;
        Scene mainMenu = SceneManager.GetSceneByName(MainMenuSceneName);
        if (_introCamera != null)
            _introCamera.enabled = false;
        if (mainMenu.IsValid() && mainMenu.isLoaded)
            SceneManager.SetActiveScene(mainMenu);

        MusicManager.ResumeBackgroundMusic();
        yield return SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    private void HandleSettingsChanged(SettingsSnapshot snapshot) => _audioSource.volume = snapshot.SfxVolume;

    private void ApplySfxVolume()
    {
        _audioSource.volume = SettingsService.Instance != null
            ? SettingsService.Instance.Current.SfxVolume
            : 1f;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
