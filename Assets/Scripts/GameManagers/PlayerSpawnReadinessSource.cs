using System;
using UnityEngine;

/// <summary>
/// Phase 3 IGameplayReadinessSource: restores PlayerStat progression and position from the
/// active session's GameSaveData, then (New Game only) writes the initial save so slot metadata
/// exists before the player can return to MainMenu. Sessions without SaveData (Development, or
/// callers still using the legacy TryStartNewGame/TryStartLoadedGame overloads) report ready
/// immediately without touching PlayerStat.
/// </summary>
public sealed class PlayerSpawnReadinessSource : MonoBehaviour, IGameplayReadinessSource
{
    [SerializeField] private string _sourceId = "PlayerSpawn";
    [SerializeField] private PlayerStat _playerStat;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private SpawnRegistry _spawnRegistry;

    public string SourceId => _sourceId;
    public bool IsReady { get; private set; }

    public event Action ReadyChanged;

    internal void ConfigureForTests(PlayerStat playerStat, Transform playerTransform, SpawnRegistry spawnRegistry)
    {
        _playerStat = playerStat;
        _playerTransform = playerTransform;
        _spawnRegistry = spawnRegistry;
    }

    private void Start()
    {
        GameSession session = GameSessionManager.Instance != null
            ? GameSessionManager.Instance.Current
            : default;

        if (session.SaveData?.player != null)
        {
            RestorePlayer(session.SaveData.player);

            if (session.Kind == GameSessionKind.NewGame)
                WriteInitialSave(session);
        }

        MarkReady();
    }

    private void RestorePlayer(PlayerSaveData playerData)
    {
        if (_playerStat != null)
            _playerStat.RestoreProgression(playerData.level, playerData.currentExperience, playerData.health);

        PlayerLocationSaveData location = playerData.location;
        if (_playerTransform == null || location == null)
            return;

        if (location.HasSavedPosition)
        {
            _playerTransform.position = new Vector3(
                location.positionX, location.positionY, _playerTransform.position.z);
            return;
        }

        if (_spawnRegistry != null && _spawnRegistry.TryGetSpawn(location.fallbackSpawnId, out Vector3 spawnPosition))
        {
            _playerTransform.position = new Vector3(
                spawnPosition.x, spawnPosition.y, _playerTransform.position.z);
        }
        else
        {
            Debug.LogWarning(
                $"PlayerSpawnReadinessSource: spawn id '{location.fallbackSpawnId}' not found; "
                + "keeping current position.", this);
        }
    }

    private void WriteInitialSave(GameSession session)
    {
        ISaveSlotRepository repository = GameSessionManager.Instance.SaveRepository;
        if (repository == null)
            return;

        SaveOperationResult result = repository.WriteSave(session.SlotId, session.SaveData);
        if (!result.Success)
            Debug.LogError($"PlayerSpawnReadinessSource: initial save write failed: {result.ErrorMessage}", this);
    }

    private void MarkReady()
    {
        if (IsReady)
            return;

        IsReady = true;
        ReadyChanged?.Invoke();
    }
}
