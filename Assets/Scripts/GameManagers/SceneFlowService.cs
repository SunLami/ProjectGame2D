using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class SceneFlowService : MonoBehaviour
{
    public const string MainMenuSceneName = "MainMenu";

    public static SceneFlowService Instance { get; private set; }

    public bool IsTransitioning { get; private set; }

    public event Action<string> TransitionFailed;
    public event Action<float> TransitionProgressChanged;

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
    }

    public bool TryLoadGameplay(string sceneName)
    {
        if (!CanStartTransition(sceneName) || !GameSessionManager.Instance.HasActiveSession)
            return false;

        BeginSceneLoad(sceneName, enterMainMenu: false);
        return true;
    }

    public bool TryReturnToMainMenu() => TryReturnToMainMenu(MainMenuSceneName);

    public bool TryReturnToMainMenu(string sceneName)
    {
        if (!CanStartTransition(sceneName))
            return false;

        BeginSceneLoad(sceneName, enterMainMenu: true);
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

        BeginSceneLoad(MainMenuSceneName, enterMainMenu: true);
    }

    private bool CanStartTransition(string sceneName) =>
        !IsTransitioning && !string.IsNullOrWhiteSpace(sceneName);

    private void BeginSceneLoad(string sceneName, bool enterMainMenu)
    {
        IsTransitioning = true;
        TransitionProgressChanged?.Invoke(0f);
        GameStateManager.Instance.ReplaceState(GameState.Loading);

        if (enterMainMenu)
            GameSessionManager.Instance.ClearSession();

        // Always release any already-loaded gameplay scene's persistent singletons before loading
        // the next scene -- required both for Return to Main Menu and for reloading gameplay
        // in-place (Phase 9 Load Game slot switch: TryLoadGameplay called again while a gameplay
        // scene, and its DontDestroyOnLoad managers, are already active). Without this, the new
        // scene's own InventoryManager/etc. would self-destroy in Awake() because Instance is
        // still the *previous* session's manager, leaking slot A's state into slot B. No-op when
        // no gameplay scene was previously loaded (e.g. first load from MainMenu).
        GameplaySceneLifetime lifetime = FindAnyObjectByType<GameplaySceneLifetime>(FindObjectsInactive.Include);
        if (lifetime != null)
            lifetime.ReleaseForSceneExit();

        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            FailTransition(sceneName, exception.Message);
            return;
        }

        if (operation == null)
        {
            FailTransition(sceneName, "Unity did not create a scene load operation.");
            return;
        }

        StartCoroutine(TrackSceneLoad(operation, enterMainMenu));
    }

    private System.Collections.IEnumerator TrackSceneLoad(AsyncOperation operation, bool enterMainMenu)
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
        // LoadSceneMode.Single destroys the outgoing MainMenu Canvas.
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
