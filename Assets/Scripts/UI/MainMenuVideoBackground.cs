using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class MainMenuVideoBackground : MonoBehaviour
{
    [SerializeField] private VideoClip _videoClip;
    [SerializeField] private Image _fallbackImage;

    private VideoPlayer _videoPlayer;
    private RenderTexture _renderTexture;
    private RawImage _videoSurface;

    private void Awake()
    {
        if (_fallbackImage == null)
        {
            _fallbackImage = GetComponent<Image>();
        }

        if (_videoClip == null)
        {
            _videoClip = Resources.Load<VideoClip>("UI/Background/Video/mainmenu_background");
        }

        if (_videoClip == null)
        {
            Debug.LogWarning("Main Menu background VideoClip is missing. Keeping the fallback image.", this);
            return;
        }

        CreateVideoSurface();

        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.isLooping = true;
        _videoPlayer.waitForFirstFrame = true;
        _videoPlayer.skipOnDrop = true;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _renderTexture;
        _videoPlayer.source = VideoSource.VideoClip;
        _videoPlayer.clip = _videoClip;
        _videoPlayer.sendFrameReadyEvents = true;
        _videoPlayer.prepareCompleted += HandlePrepared;
        _videoPlayer.frameReady += HandleFirstFrameReady;
        _videoPlayer.errorReceived += HandleVideoError;
        _videoPlayer.Prepare();
    }

    private void OnDestroy()
    {
        if (_videoPlayer == null)
        {
            return;
        }

        _videoPlayer.prepareCompleted -= HandlePrepared;
        _videoPlayer.frameReady -= HandleFirstFrameReady;
        _videoPlayer.errorReceived -= HandleVideoError;

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }

    private void HandlePrepared(VideoPlayer player)
    {
        AspectRatioFitter fitter = _videoSurface.GetComponent<AspectRatioFitter>();
        if (player.width > 0 && player.height > 0)
        {
            fitter.aspectRatio = (float)player.width / player.height;
        }

        player.Play();
    }

    private void HandleFirstFrameReady(VideoPlayer player, long frameIndex)
    {
        player.sendFrameReadyEvents = false;
        player.frameReady -= HandleFirstFrameReady;
        _videoSurface.enabled = true;

        if (_fallbackImage != null)
        {
            _fallbackImage.enabled = false;
        }
    }

    private void HandleVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning($"Main Menu background video could not be played: {message}", this);

        if (_fallbackImage != null)
        {
            _fallbackImage.enabled = true;
        }
    }

    private void CreateVideoSurface()
    {
        RectTransform root = transform as RectTransform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = Vector2.zero;

        int width = _videoClip.width > 0 ? (int)_videoClip.width : 1920;
        int height = _videoClip.height > 0 ? (int)_videoClip.height : 1080;
        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "MainMenuBackgroundVideo"
        };
        _renderTexture.Create();

        GameObject surfaceObject = new GameObject("VideoSurface", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        RectTransform surface = surfaceObject.GetComponent<RectTransform>();
        surface.SetParent(transform, false);
        surface.anchorMin = new Vector2(0.5f, 0.5f);
        surface.anchorMax = new Vector2(0.5f, 0.5f);
        surface.anchoredPosition = Vector2.zero;
        surface.sizeDelta = Vector2.one;

        _videoSurface = surfaceObject.GetComponent<RawImage>();
        _videoSurface.texture = _renderTexture;
        _videoSurface.raycastTarget = false;
        _videoSurface.enabled = false;

        AspectRatioFitter fitter = surfaceObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (float)width / height;
    }
}
