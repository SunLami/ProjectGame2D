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

    /// <summary>True once real gameplay mutated session state since the last successful save (or
    /// since the session started). Set by SessionDirtyTracker subscribing to domain events;
    /// GameSessionManager itself does not know about Inventory/Quest/World -- see
    /// ServiceOwnershipLifecycle.md "GameSessionManager | ... dirty/play time".</summary>
    public bool IsDirty { get; private set; }

    /// <summary>True while PlayerSpawnReadinessSource is applying a save snapshot to the scene.
    /// Domain restore APIs (RestoreState/RestoreProgression/etc.) legitimately fire the same
    /// change events real gameplay does; dirty-tracking must ignore them while this is true so a
    /// freshly loaded/New Game session never starts dirty (RuntimeArchitecture.md "Event rules").</summary>
    public bool IsRestoring { get; private set; }

    /// <summary>File-backed by default; tests substitute an in-memory repository via
    /// SetSaveRepositoryForTests so they never touch a real player save.</summary>
    public ISaveSlotRepository SaveRepository { get; private set; }

    public event Action<GameSession> SessionChanged;

    /// <summary>Fires only on an actual Dirty/Clean transition, not on every mutation.</summary>
    public event Action<bool> DirtyStateChanged;

    private double _sessionStartRealtimeSeconds;
    private long _sessionBaseTotalPlayTimeSeconds;

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

    /// <summary>Marks the active session as having unsaved gameplay changes. Idempotent -- calling
    /// it while already dirty is a no-op and does not re-fire DirtyStateChanged.</summary>
    public void MarkDirty()
    {
        if (IsDirty || IsRestoring)
            return;

        IsDirty = true;
        DirtyStateChanged?.Invoke(true);
    }

    /// <summary>Call after a successful save write. Idempotent.</summary>
    public void ClearDirty()
    {
        if (!IsDirty)
            return;

        IsDirty = false;
        DirtyStateChanged?.Invoke(false);
    }

    /// <summary>Wraps PlayerSpawnReadinessSource's restore pass so dirty-tracking (and any other
    /// restore-sensitive listener) can tell "this change came from applying a save" apart from
    /// real gameplay. Always pair with EndRestore in a try/finally at the call site.</summary>
    public void BeginRestore() => IsRestoring = true;
    public void EndRestore() => IsRestoring = false;

    /// <summary>Total play time for the active session: whatever the loaded save already carried
    /// (0 for New Game) plus real elapsed seconds since this session began. Recomputed on demand,
    /// not ticked every frame.</summary>
    public long GetTotalPlayTimeSeconds()
    {
        double elapsed = Time.realtimeSinceStartupAsDouble - _sessionStartRealtimeSeconds;
        return _sessionBaseTotalPlayTimeSeconds + (long)Math.Max(0d, elapsed);
    }

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
        IsDirty = false;
        IsRestoring = false;
        _sessionBaseTotalPlayTimeSeconds = session.SaveData?.totalPlayTimeSeconds ?? 0;
        _sessionStartRealtimeSeconds = Time.realtimeSinceStartupAsDouble;
        SessionChanged?.Invoke(Current);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
