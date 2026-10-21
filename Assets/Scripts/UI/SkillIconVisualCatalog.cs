using System;
using UnityEngine;

// Presentation data only: source art identity, transparent motif and measured alpha hull.
public sealed class SkillIconVisualCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public Sprite source;
        public Sprite foreground;
        public Vector2[] hull;
    }

    [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

    public Entry Resolve(Sprite source)
    {
        if (source == null) return null;
        return Array.Find(_entries, entry => entry.source == source);
    }

    public void Validate()
    {
        var sources = new System.Collections.Generic.HashSet<Sprite>();
        foreach (var entry in _entries)
        {
            if (entry == null || entry.source == null || entry.foreground == null ||
                !sources.Add(entry.source) || entry.hull == null || entry.hull.Length < 3)
                throw new InvalidOperationException(name + ": missing/duplicate icon presentation.");
            if (entry.foreground.rect.width <= 0f || entry.foreground.rect.height <= 0f)
                throw new InvalidOperationException(name + ": empty foreground sprite rect.");
            foreach (Vector2 point in entry.hull)
                if (float.IsNaN(point.x) || float.IsInfinity(point.x) || float.IsNaN(point.y) ||
                    float.IsInfinity(point.y) || Mathf.Abs(point.x) > .501f || Mathf.Abs(point.y) > .501f)
                    throw new InvalidOperationException(name + ": invalid foreground hull.");
        }
    }
}
