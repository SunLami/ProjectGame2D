using System;
using UnityEngine;

/// <summary>Scene service resolving stable spawn IDs to world positions. Bound per-scene via the
/// Inspector; never looked up by GameObject name.</summary>
public sealed class SpawnRegistry : MonoBehaviour
{
    [Serializable]
    private struct Entry
    {
        public string spawnId;
        public Transform point;
    }

    [SerializeField] private Entry[] _entries;

    internal void ConfigureForTests(string spawnId, Transform point)
    {
        _entries = new[] { new Entry { spawnId = spawnId, point = point } };
    }

    public bool TryGetSpawn(string spawnId, out Vector3 position)
    {
        if (!string.IsNullOrWhiteSpace(spawnId) && _entries != null)
        {
            foreach (Entry entry in _entries)
            {
                if (entry.point != null && entry.spawnId == spawnId)
                {
                    position = entry.point.position;
                    return true;
                }
            }
        }

        position = default;
        return false;
    }
}
