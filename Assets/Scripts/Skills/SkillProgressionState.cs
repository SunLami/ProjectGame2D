using System;
using System.Collections.Generic;

[Serializable]
public sealed class SkillProgressionSaveData
{
    public int points;
    public int awardedThroughLevel = 1;
    public List<string> learnedIds = new();
    public string[] loadout = new string[4];
    public static SkillProgressionSaveData ForLevel(int level) => new()
        { points = Math.Max(0, level - 1), awardedThroughLevel = Math.Max(1, level) };
}

/// <summary>Player-owned runtime state. Definitions and snapshots are never mutated.</summary>
public sealed class SkillProgressionState
{
    private int _awardedThroughLevel = 1;
    private readonly HashSet<string> _learned = new(StringComparer.Ordinal);
    private readonly string[] _loadout = new string[4];
    public int Points { get; private set; }
    public event Action Changed;
    public bool IsLearned(string id) => id != null && _learned.Contains(id);
    public string Equipped(int slot) => slot >= 0 && slot < 4 ? _loadout[slot] : null;
    public void AwardLevel(int level)
    {
        if (level <= _awardedThroughLevel) return;
        Points = (int)Math.Min(int.MaxValue, (long)Points + level - _awardedThroughLevel);
        _awardedThroughLevel = level;
        Changed?.Invoke();
    }
    public bool Learn(SkillCatalog catalog, string id)
    {
        if (catalog == null || !catalog.TryResolve(id, out var d) || d.learningCost <= 0
            || IsLearned(id) || Points < d.learningCost) return false;
        Points -= d.learningCost; _learned.Add(id); Changed?.Invoke(); return true;
    }
    public bool Equip(SkillCatalog catalog, int slot, string id)
    {
        if (slot < 0 || slot >= 4 || catalog == null || !catalog.TryResolve(id, out _) || !IsLearned(id)) return false;
        if (_loadout[slot] == id) return true;
        for (int i = 0; i < 4; i++) if (_loadout[i] == id) _loadout[i] = null;
        _loadout[slot] = id; Changed?.Invoke(); return true;
    }
    public void Restore(SkillProgressionSaveData data, int level)
    {
        data ??= SkillProgressionSaveData.ForLevel(level);
        Points = Math.Max(0, data.points);
        // Loading is not a level-up. Never award points from restore or UI binding.
        _awardedThroughLevel = Math.Max(Math.Max(1, level), data.awardedThroughLevel);
        _learned.Clear();
        if (data.learnedIds != null) foreach (var id in data.learnedIds)
            if (!string.IsNullOrWhiteSpace(id)) _learned.Add(id);
        var equipped = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < 4; i++)
        {
            var id = data.loadout != null && i < data.loadout.Length ? data.loadout[i] : null;
            _loadout[i] = IsLearned(id) && equipped.Add(id) ? id : null;
        }
        Changed?.Invoke();
    }
    public SkillProgressionSaveData Capture() => new()
    {
        points = Points, awardedThroughLevel = _awardedThroughLevel,
        learnedIds = new List<string>(_learned), loadout = (string[])_loadout.Clone()
    };
}
