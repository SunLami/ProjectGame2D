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
    public GameSession(GameSessionKind kind, int slotId, string gameplaySceneName, GameSaveData saveData = null)
    {
        Kind = kind;
        SlotId = slotId;
        GameplaySceneName = gameplaySceneName;
        SaveData = saveData;
    }

    public GameSessionKind Kind { get; }
    public int SlotId { get; }
    public string GameplaySceneName { get; }

    /// <summary>Save payload to restore once the gameplay scene loads. Null for Development
    /// sessions and for callers still using the legacy TryStartNewGame/TryStartLoadedGame
    /// overloads without a GameSaveData argument.</summary>
    public GameSaveData SaveData { get; }
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

    /// <summary>File-backed by default; tests substitute an in-memory repository via
    /// SetSaveRepositoryForTests so they never touch a real player save.</summary>
    public ISaveSlotRepository SaveRepository { get; private set; }

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
        SaveRepository = new FileSaveSlotRepository();
    }

    public bool TryStartNewGame(int slotId, string gameplaySceneName) =>
        TryStartSlotSession(GameSessionKind.NewGame, slotId, gameplaySceneName, null);

    public bool TryStartNewGame(int slotId, string gameplaySceneName, GameSaveData saveData) =>
        TryStartSlotSession(GameSessionKind.NewGame, slotId, gameplaySceneName, saveData);

    public bool TryStartLoadedGame(int slotId, string gameplaySceneName) =>
        TryStartSlotSession(GameSessionKind.LoadedGame, slotId, gameplaySceneName, null);

    public bool TryStartLoadedGame(int slotId, string gameplaySceneName, GameSaveData saveData) =>
        TryStartSlotSession(GameSessionKind.LoadedGame, slotId, gameplaySceneName, saveData);

    public bool TryStartDevelopment(string gameplaySceneName)
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
            return false;

        SetSession(new GameSession(GameSessionKind.Development, 0, gameplaySceneName));
        return true;
    }

    public void ClearSession() => SetSession(default);

    internal void SetSaveRepositoryForTests(ISaveSlotRepository repository) => SaveRepository = repository;

    private bool TryStartSlotSession(GameSessionKind kind, int slotId, string gameplaySceneName, GameSaveData saveData)
    {
        if (slotId < MinimumSlotId || slotId > MaximumSlotId
            || string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            return false;
        }

        SetSession(new GameSession(kind, slotId, gameplaySceneName, saveData));
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
