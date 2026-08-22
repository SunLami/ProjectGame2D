using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class GameStateManager : MonoBehaviour
{
    private static readonly IReadOnlyDictionary<GameState, GameStatePolicy> Policies =
        new Dictionary<GameState, GameStatePolicy>
        {
            [GameState.Booting] = new(true, false, false, false),
            [GameState.MainMenu] = new(true, false, true, true),
            [GameState.Loading] = new(true, false, false, true),
            [GameState.Playing] = new(false, true, true, true),
            [GameState.Paused] = new(true, false, true, true),
            [GameState.GameplayMenu] = new(true, false, true, true),
            [GameState.Dialogue] = new(false, false, true, true),
            [GameState.Cutscene] = new(false, false, false, false),
            [GameState.Saving] = new(true, false, false, true),
            [GameState.PlayerDead] = new(true, false, true, true)
        };

    private readonly Stack<GameStateSnapshot> _history = new();

    public static GameStateManager Instance { get; private set; }
    public static bool AllowsGameplayInput => Instance != null && Instance.CurrentPolicy.AllowsGameplayInput;

    public GameStateSnapshot Current { get; private set; } = new(GameState.Booting);
    public GameState CurrentState => Current.State;
    public GameplayMenuPage CurrentMenuPage => Current.MenuPage;
    public GameStatePolicy CurrentPolicy => Policies[Current.State];
    public bool CanReturn => _history.Count > 0;

    public event Action<GameStateChange> StateChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject managerObject = new(nameof(GameStateManager));
        managerObject.AddComponent<GameStateManager>();
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

    public void Pause()
    {
        if (CurrentState == GameState.Playing)
            PushState(GameState.Paused);
    }

    public void Resume()
    {
        ResetToPlaying();
    }

    public void OpenMenu(GameplayMenuPage page)
    {
        if (page == GameplayMenuPage.None || !CurrentPolicy.AllowsUIInput)
            return;

        if (CurrentState == GameState.GameplayMenu)
            ReplaceState(GameState.GameplayMenu, page);
        else
            PushState(GameState.GameplayMenu, page);
    }

    public bool ReturnToPreviousState()
    {
        if (_history.Count == 0)
            return false;

        ApplySnapshot(_history.Pop());
        return true;
    }

    public void PushState(GameState state, GameplayMenuPage menuPage = GameplayMenuPage.None)
    {
        if (!IsValidSnapshot(state, menuPage))
            return;

        GameStateSnapshot next = new(state, menuPage);
        if (next == Current)
            return;

        _history.Push(Current);
        ApplySnapshot(next);
    }

    public void ReplaceState(GameState state, GameplayMenuPage menuPage = GameplayMenuPage.None)
    {
        if (!IsValidSnapshot(state, menuPage))
            return;

        GameStateSnapshot next = new(state, menuPage);
        if (next != Current)
            ApplySnapshot(next);
    }

    public void ResetToPlaying()
    {
        _history.Clear();
        ReplaceState(GameState.Playing);
    }

    public void ResetToMainMenu()
    {
        _history.Clear();
        ReplaceState(GameState.MainMenu);
    }

    private void ApplySnapshot(GameStateSnapshot next)
    {
        GameStateSnapshot previous = Current;
        Current = next;

        GameStatePolicy policy = CurrentPolicy;
        Time.timeScale = policy.PausesWorld ? 0f : 1f;
        Cursor.visible = policy.ShowsCursor;
        Cursor.lockState = CursorLockMode.None;

        StateChanged?.Invoke(new GameStateChange(previous, Current));
    }

    private static bool IsValidSnapshot(GameState state, GameplayMenuPage menuPage)
    {
        if (state != GameState.GameplayMenu || menuPage != GameplayMenuPage.None)
            return true;

        Debug.LogError("GameState.GameplayMenu requires a concrete GameplayMenuPage.");
        return false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
