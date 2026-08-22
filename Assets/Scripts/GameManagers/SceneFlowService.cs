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
        if (!IsTransitioning && GameStateManager.Instance.CurrentState == GameState.Loading
            && GameSessionManager.Instance.HasActiveSession)
        {
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
        GameStateManager.Instance.ReplaceState(GameState.Loading);

        if (enterMainMenu)
        {
            GameSessionManager.Instance.ClearSession();

            GameplaySceneLifetime lifetime = FindAnyObjectByType<GameplaySceneLifetime>(FindObjectsInactive.Include);
            if (lifetime != null)
                lifetime.ReleaseForSceneExit();
        }

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

        operation.completed += _ => CompleteSceneLoad(enterMainMenu);
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
