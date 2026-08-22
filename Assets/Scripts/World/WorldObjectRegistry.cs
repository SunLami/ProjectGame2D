using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene service (ServiceOwnershipLifecycle.md scene scope) that owns every persistent world
/// object placed in this scene. Bound explicitly via the Inspector, mirroring SpawnRegistry --
/// never discovered with Find/FindObjectsByType at runtime. Capture/restore go through this single
/// owner so PlayerSpawnReadinessSource has one call site per direction, matching how it already
/// owns inventory/equipment/tutorial/quest restore.
/// </summary>
public sealed class WorldObjectRegistry : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] _entries;

    private readonly List<IPersistentWorldObject> _objects = new();
    private readonly Dictionary<string, IPersistentWorldObject> _byId = new(StringComparer.Ordinal);

    internal void ConfigureForTests(IReadOnlyList<IPersistentWorldObject> objects)
    {
        _objects.Clear();
        _byId.Clear();
        if (objects == null)
            return;

        foreach (IPersistentWorldObject obj in objects)
            Register(obj);
    }

    private void Awake()
    {
        if (_entries == null)
            return;

        foreach (MonoBehaviour behaviour in _entries)
        {
            if (behaviour is IPersistentWorldObject obj)
                Register(obj);
        }
    }

    private void Register(IPersistentWorldObject obj)
    {
        if (obj == null || string.IsNullOrEmpty(obj.PersistentId))
            return;

        if (_byId.ContainsKey(obj.PersistentId))
        {
            Debug.LogError(
                $"WorldObjectRegistry: duplicate persistentId '{obj.PersistentId}' -- keeping the first entry.", this);
            return;
        }

        _byId.Add(obj.PersistentId, obj);
        _objects.Add(obj);
    }

    public IReadOnlyList<IPersistentWorldObject> Objects => _objects;

    /// <summary>Raw Inspector-authored entries, for editor validation only -- Objects (above) is
    /// empty until Awake runs, which the content validator (Edit Mode) never triggers.</summary>
    public IReadOnlyList<MonoBehaviour> Entries => _entries;

    public WorldSaveData ToSaveData()
    {
        var data = new WorldSaveData();
        foreach (IPersistentWorldObject obj in _objects)
        {
            WorldObjectState state = obj.CaptureState();
            data.objects.Add(new WorldObjectSaveData
            {
                persistentId = obj.PersistentId,
                kind = obj.Kind,
                flag = state.Flag,
                nextRespawnUtcTicks = state.NextRespawnUtcTicks
            });
        }
        return data;
    }

    /// <summary>Restore-only: applies saved state directly, no gameplay/progression events.
    /// Idempotent -- calling it twice with the same data reproduces the same state. A persistentId
    /// from the save with no matching object in this scene (removed/renamed content) is appended to
    /// missingIds instead of throwing.</summary>
    public void RestoreState(WorldSaveData data, List<string> missingIds = null)
    {
        if (data?.objects == null)
            return;

        foreach (WorldObjectSaveData record in data.objects)
        {
            if (!string.IsNullOrEmpty(record.persistentId) && _byId.TryGetValue(record.persistentId, out IPersistentWorldObject obj))
                obj.RestoreState(new WorldObjectState(record.flag, record.nextRespawnUtcTicks));
            else
                missingIds?.Add(record.persistentId);
        }
    }
}
