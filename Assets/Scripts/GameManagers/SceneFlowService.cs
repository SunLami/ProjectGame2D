using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public sealed class SceneFlowService : MonoBehaviour
{
    public const string MainMenuSceneName = "MainMenu";
    public const string BootstrapSceneName = "Bootstrap";

    private const float RevealFadeDuration = 2.5f;

    public static SceneFlowService Instance { get; private set; }

    public bool IsTransitioning { get; private set; }

    public event Action<string> TransitionFailed;
    public event Action<float> TransitionProgressChanged;

    // Scene-independent full-screen black cover, alive for the whole game (DontDestroyOnLoad
    // sibling of this service) so a transition always has something opaque to hold behind it --
    // no per-scene UI (e.g. MainMenu's own Loading panel) has to be kept alive as a side effect
    // just to avoid flashing the next scene's raw camera view before it's ready to present itself.
    private CanvasGroup _transitionOverlay;
    private Coroutine _fadeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        new GameObject(nameof(SceneFlowService)).AddComponent<SceneFlowService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildTransitionOverlay();
    }

    private void BuildTransitionOverlay()
    {
        var canvasObject = new GameObject("SceneTransitionOverlay", typeof(Canvas), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        _transitionOverlay = canvasObject.GetComponent<CanvasGroup>();
        _transitionOverlay.alpha = 0f;
        _transitionOverlay.blocksRaycasts = false;
        _transitionOverlay.interactable = false;

        var imageObject = new GameObject("Fade", typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>Loads a content scene while the Bootstrap scene (_Managers/_UI) stays loaded, so
    /// session state carries over live instead of going through a save/restore round trip. Use
    /// for ordinary map-to-map travel in the same play session.</summary>
    public bool TryLoadGameplay(string sceneName) => TryLoadGameplay(sceneName, resetBootstrap: false);

    /// <summary>resetBootstrap tears down and reloads the Bootstrap scene (fresh manager
    /// singletons) before loading sceneName -- required for New Game/Continue so one session's
    /// Inventory/Equipment/Quest/etc. state never leaks into the next.</summary>
    public bool TryLoadGameplay(string sceneName, bool resetBootstrap)
    {
        if (!CanStartTransition(sceneName) || !GameSessionManager.Instance.HasActiveSession)
            return false;

        BeginSceneLoad(sceneName, enterMainMenu: false, resetBootstrap);
        return true;
    }

    public bool TryReturnToMainMenu() => TryReturnToMainMenu(MainMenuSceneName);

    public bool TryReturnToMainMenu(string sceneName)
    {
        if (!CanStartTransition(sceneName))
            return false;

        BeginSceneLoad(sceneName, enterMainMenu: true, resetBootstrap: true);
        return true;
    }

    public void CompleteGameplayRestore()
    {
        if (GameStateManager.Instance.CurrentState == GameState.Loading
            && GameSessionManager.Instance.HasActiveSession)
        {
            // A gameplay scene's Start methods run before TrackSceneLoad resumes after scene
            // activation. Readiness can therefore complete while this flag is still true.
            IsTransitioning = false;
            GameStateManager.Instance.ResetToPlaying();
        }
        else
        {
            Debug.LogWarning(
                "CompleteGameplayRestore ignored: not in an active Loading transition.", this);
        }
    }

    public void FailGameplayRestore(string reason)
    {
        if (IsTransitioning || GameStateManager.Instance.CurrentState != GameState.Loading)
            return;

        string message = $"Gameplay restore failed: {reason}";
        Debug.LogError(message, this);
        TransitionFailed?.Invoke(message);

        BeginSceneLoad(MainMenuSceneName, enterMainMenu: true, resetBootstrap: true);
    }

    private bool CanStartTransition(string sceneName) =>
        !IsTransitioning && !string.IsNullOrWhiteSpace(sceneName);

    private void BeginSceneLoad(string sceneName, bool enterMainMenu, bool resetBootstrap)
    {
        IsTransitioning = true;
        TransitionProgressChanged?.Invoke(0f);
        GameStateManager.Instance.ReplaceState(GameState.Loading);

        if (enterMainMenu)
            GameSessionManager.Instance.ClearSession();

        StartCoroutine(RunSceneLoad(sceneName, enterMainMenu, resetBootstrap));
    }

    private IEnumerator RunSceneLoad(string sceneName, bool enterMainMenu, bool resetBootstrap)
    {
        // Snapshot what's loaded now so it can be unloaded only AFTER the replacements are in.
        // Unity refuses to unload the only scene left loaded, and unloading the old content
        // scene first would also leave a frame with no active Camera ("No cameras rendering").
        List<Scene> outgoingContentScenes = new();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name != BootstrapSceneName)
                outgoingContentScenes.Add(scene);
        }

        // Destroy the outgoing Player synchronously, right now -- Player is still a per-map
        // DontDestroyOnLoad singleton (only the Inventory/Equipment/Quest/etc. managers moved out
        // to Bootstrap). The new content scene loads additively below and its own Player.Awake()
        // may run before the outgoing scene actually unloads (that unload is deferred to the end
        // so the old Camera stays alive until the new one is ready -- see below). A regular
        // Destroy() only takes effect at end of frame, which is too late: the new Player would
        // still see a live Instance and self-destroy instead of taking over. DestroyImmediate
        // guarantees Instance reads Unity-null before the new scene loads.
        Player outgoingPlayer = FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        if (outgoingPlayer != null)
            DestroyImmediate(outgoingPlayer.gameObject);

        if (resetBootstrap)
        {
            // The manager singletons detached into Unity's DontDestroyOnLoad pseudo-scene the
            // moment they Awake()'d, so they no longer belong to "Bootstrap" at all -- pull them
            // back in first or unloading this scene would not touch them.
            BootstrapManagerLifetime managerLifetime = FindAnyObjectByType<BootstrapManagerLifetime>();
            if (managerLifetime != null)
                managerLifetime.ReleaseForReset();

            // Actually destroy the old managers now (while an outgoing content scene, e.g.
            // MainMenu, is still loaded -- Unity refuses to unload the only scene left) so their
            // static Instance fields go Unity-null before the fresh Bootstrap's copies Awake()
            // and check for a pre-existing Instance.
            Scene outgoingBootstrap = SceneManager.GetSceneByName(BootstrapSceneName);
            if (outgoingBootstrap.IsValid() && outgoingBootstrap.isLoaded)
            {
                AsyncOperation unloadOldBootstrap = SceneManager.UnloadSceneAsync(outgoingBootstrap);
                if (unloadOldBootstrap != null)
                    yield return unloadOldBootstrap;
            }

            AsyncOperation freshBootstrapLoad = SceneManager.LoadSceneAsync(BootstrapSceneName, LoadSceneMode.Additive);
            if (freshBootstrapLoad != null)
                yield return freshBootstrapLoad;
        }

        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
        catch (Exception exception)
        {
            FailTransition(sceneName, exception.Message);
            yield break;
        }

        if (operation == null)
        {
            FailTransition(sceneName, "Unity did not create a scene load operation.");
            yield break;
        }

        yield return TrackSceneLoad(operation, sceneName, enterMainMenu);

        // The incoming scene is activated at this point, but GameState is typically still
        // Loading -- its own IGameplayReadinessSource(s) (inventory/equipment/world restore,
        // or the intro cutscene waiting to start) can take several more frames to finish. Snap
        // the overlay fully opaque now (no visible change -- the outgoing scene's own content,
        // e.g. MainMenu's Loading panel, is still what's on screen) so there is guaranteed
        // continuous cover from here through the unload below, with no gap for the incoming
        // scene's raw camera view to flash through.
        SetOverlayAlphaImmediate(1f);

        // Bounded wait so a stuck readiness gate (already timed out + logged elsewhere) can't
        // hang this coroutine forever.
        yield return WaitForLoadingStateToClear();

        // Now that the replacement scene is fully in and ready, it's safe to unload the outgoing
        // content scene(s) (e.g. MainMenu) that were kept alive only so Bootstrap was never the
        // last scene loaded.
        foreach (Scene scene in outgoingContentScenes)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            if (unload != null)
                yield return unload;
        }

        // Everything the destination needs is in and ready -- reveal it with a smooth fade
        // instead of an abrupt cut, regardless of how long the load/restore actually took.
        yield return FadeOverlay(1f, 0f, RevealFadeDuration);
    }

    private void SetOverlayAlphaImmediate(float alpha)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (_transitionOverlay != null)
            _transitionOverlay.alpha = alpha;
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (_transitionOverlay == null)
            yield break;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(RunFadeOverlay(from, to, duration));
        yield return _fadeRoutine;
    }

    private IEnumerator RunFadeOverlay(float from, float to, float duration)
    {
        _transitionOverlay.alpha = from;

        if (duration <= 0f)
        {
            _transitionOverlay.alpha = to;
            _fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _transitionOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        _transitionOverlay.alpha = to;
        _fadeRoutine = null;
    }

    private static IEnumerator WaitForLoadingStateToClear()
    {
        float deadline = Time.unscaledTime + 10f;
        while (GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Loading
            && Time.unscaledTime < deadline)
        {
            yield return null;
        }
    }

    private IEnumerator TrackSceneLoad(AsyncOperation operation, string sceneName, bool enterMainMenu)
    {
        operation.allowSceneActivation = false;
        float displayedProgress = 0f;

        while (operation.progress < 0.9f)
        {
            // Unity reports scene loading in the 0..0.9 range until activation.
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(
                displayedProgress, targetProgress, Time.unscaledDeltaTime * 1.5f);
            TransitionProgressChanged?.Invoke(displayedProgress);
            yield return null;
        }

        // A small presentation pass guarantees that the final part of the bar is rendered before
        // the incoming scene's own Start methods begin running.
        while (displayedProgress < 1f)
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, Time.unscaledDeltaTime * 1.5f);
            TransitionProgressChanged?.Invoke(displayedProgress);
            yield return null;
        }

        TransitionProgressChanged?.Invoke(1f);
        yield return new WaitForSecondsRealtime(0.25f);

        operation.allowSceneActivation = true;
        while (!operation.isDone)
            yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
            SceneManager.SetActiveScene(loadedScene);

        CompleteSceneLoad(enterMainMenu);
    }

    private void CompleteSceneLoad(bool enterMainMenu)
    {
        IsTransitioning = false;

        if (enterMainMenu)
            GameStateManager.Instance.ResetToMainMenu();
    }

    private void FailTransition(string sceneName, string reason)
    {
        IsTransitioning = false;
        GameSessionManager.Instance.ClearSession();
        GameStateManager.Instance.ResetToMainMenu();

        string message = $"Failed to load scene '{sceneName}': {reason}";
        Debug.LogError(message, this);
        TransitionFailed?.Invoke(message);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
