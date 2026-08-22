using System;
using UnityEngine;

public enum GameSessionKind
{
    None,
    NewGame,
    LoadedGame,
    Development
}

public readonly struct GameSession
{
    public GameSession(GameSessionKind kind, int slotId, string gameplaySceneName)
    {
        Kind = kind;
        SlotId = slotId;
        GameplaySceneName = gameplaySceneName;
    }

    public GameSessionKind Kind { get; }
    public int SlotId { get; }
    public string GameplaySceneName { get; }
    public bool IsActive => Kind != GameSessionKind.None;
}

[DefaultExecutionOrder(-1000)]
public sealed class GameSessionManager : MonoBehaviour
{
    public const int MinimumSlotId = 1;
    public const int MaximumSlotId = 3;

    public static GameSessionManager Instance { get; private set; }

    public GameSession Current { get; private set; }
    public bool HasActiveSession => Current.IsActive;

    public event Action<GameSession> SessionChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        new GameObject(nameof(GameSessionManager)).AddComponent<GameSessionManager>();
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

    public bool TryStartNewGame(int slotId, string gameplaySceneName) =>
        TryStartSlotSession(GameSessionKind.NewGame, slotId, gameplaySceneName);

    public bool TryStartLoadedGame(int slotId, string gameplaySceneName) =>
        TryStartSlotSession(GameSessionKind.LoadedGame, slotId, gameplaySceneName);

    public bool TryStartDevelopment(string gameplaySceneName)
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
            return false;

        SetSession(new GameSession(GameSessionKind.Development, 0, gameplaySceneName));
        return true;
    }

    public void ClearSession() => SetSession(default);

    private bool TryStartSlotSession(GameSessionKind kind, int slotId, string gameplaySceneName)
    {
        if (slotId < MinimumSlotId || slotId > MaximumSlotId
            || string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            return false;
        }

        SetSession(new GameSession(kind, slotId, gameplaySceneName));
        return true;
    }

    private void SetSession(GameSession session)
    {
        Current = session;
        SessionChanged?.Invoke(Current);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
