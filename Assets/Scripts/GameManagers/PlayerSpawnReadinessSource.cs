using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IGameplayReadinessSource that restores the whole active session's GameSaveData into the
/// gameplay scene in deterministic order: base progression -> position -> inventory -> equipment
/// -> recalculate derived stats once -> finalize health against final MaxHealth. Plugs into the
/// GameplayReadinessGate added in Phase 1 (not modified here). For New Game sessions, starting
/// inventory is seeded exactly once and the initial save is (re)written including the seeded
/// snapshot (D-011). Sessions without SaveData (Development, or callers still using the legacy
/// TryStartNewGame/TryStartLoadedGame overloads) report ready immediately without touching
/// Player/Inventory/Equipment.
/// </summary>
public sealed class PlayerSpawnReadinessSource : MonoBehaviour, IGameplayReadinessSource
{
    [SerializeField] private string _sourceId = "PlayerSpawn";
    [SerializeField] private PlayerStat _playerStat;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private SpawnRegistry _spawnRegistry;
    [SerializeField] private InventorySeeder _inventorySeeder;

    public string SourceId => _sourceId;
    public bool IsReady { get; private set; }

    public event Action ReadyChanged;

    internal void ConfigureForTests(
        PlayerStat playerStat, Transform playerTransform, SpawnRegistry spawnRegistry,
        InventorySeeder inventorySeeder = null)
    {
        _playerStat = playerStat;
        _playerTransform = playerTransform;
        _spawnRegistry = spawnRegistry;
        _inventorySeeder = inventorySeeder;
    }

    private void Start()
    {
        GameSession session = GameSessionManager.Instance != null
            ? GameSessionManager.Instance.Current
            : default;

        if (session.SaveData?.player != null)
        {
            RestoreAll(session);

            if (session.Kind == GameSessionKind.NewGame)
                WriteInitialSave(session);
        }

        MarkReady();
    }

    private void RestoreAll(GameSession session)
    {
        PlayerSaveData playerData = session.SaveData.player;

        // 1. Base progression -- health is finalized last, after equipment is known.
        if (_playerStat != null)
            _playerStat.RestoreProgression(playerData.level, playerData.currentExperience);

        // 2. Position.
        RestorePosition(playerData.location);

        // 3. Inventory: seed once for New Game, else restore from save.
        IItemResolver resolver = new ResourcesItemResolver();
        if (session.Kind == GameSessionKind.NewGame)
        {
            _inventorySeeder?.SeedStartingInventory();
        }
        else if (InventoryManager.Instance != null && session.SaveData.inventory != null)
        {
            List<string> missingItemIds = new();
            InventoryManager.Instance.LoadFromSaveData(session.SaveData.inventory, resolver, missingItemIds);
            if (missingItemIds.Count > 0)
            {
                Debug.LogWarning(
                    $"PlayerSpawnReadinessSource: {missingItemIds.Count} inventory item(s) could not "
                    + $"be resolved and were dropped: {string.Join(", ", missingItemIds)}", this);
            }
        }

        // 4. Equipment -- restored directly, never through the public Equip() UI path.
        if (EquipmentManager.Instance != null && session.SaveData.equipment?.slots != null)
        {
            foreach (EquipmentSaveData.SlotData slotData in session.SaveData.equipment.slots)
            {
                if (resolver.TryResolve(slotData.itemId, out ItemSO item) && item is EquipmentItemSO equipment)
                {
                    EquipmentManager.Instance.RestoreEquipped(slotData.slot, equipment);
                }
                else
                {
                    Debug.LogWarning(
                        $"PlayerSpawnReadinessSource: could not resolve equipped item "
                        + $"'{slotData.itemId}' for slot {slotData.slot}.", this);
                }
            }
        }

        // 5. Recalculate derived stats exactly once, now that equipment is final.
        EquipmentManager.Instance?.RecalculateStats();

        // 6. Finalize health against the final MaxHealth (not the live-equip delta formula).
        _playerStat?.RestoreHealth(playerData.health);
    }

    private void RestorePosition(PlayerLocationSaveData location)
    {
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

        GameSaveData snapshot = session.SaveData;
        if (InventoryManager.Instance != null)
            snapshot.inventory = InventoryManager.Instance.ToSaveData();
        if (EquipmentManager.Instance != null)
            snapshot.equipment = EquipmentManager.Instance.ToSaveData();

        SaveOperationResult result = repository.WriteSave(session.SlotId, snapshot);
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
