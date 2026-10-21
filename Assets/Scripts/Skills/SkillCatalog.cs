using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillCastKind
{
    None = 0, WaterBolt = 1, WaterVortex = 2, WaterBeam = 3, WaterTsunami = 4,
    EarthBolt = 5, EarthArena = 6, EarthSpikes = 7, EarthMeteors = 8,
    WindBlade = 9, WindWhirlwind = 10, WindGust = 11, WindRoc = 12
}

[CreateAssetMenu(menuName = "Game/Skills/Skill Catalog")]
public sealed class SkillCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Definition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Min(1)] public int learningCost;
        public Sprite icon;
        public SkillCastKind castKind;
        [Tooltip("Seconds this skill's loadout slot is locked out after casting (D-118). 0 = no cooldown.")]
        [Min(0)] public float cooldownSeconds = 1f;
    }
    [SerializeField] private Definition[] _definitions = Array.Empty<Definition>();
    public bool TryResolve(string id, out Definition definition)
    {
        definition = Array.Find(_definitions, d => d != null && d.id == id);
        return !string.IsNullOrEmpty(id) && definition != null;
    }
    public void Validate()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in _definitions)
            if (d == null || string.IsNullOrWhiteSpace(d.id) || !ids.Add(d.id) || d.learningCost < 1 || d.icon == null
                || d.castKind == SkillCastKind.None || !Enum.IsDefined(typeof(SkillCastKind), d.castKind))
                throw new InvalidOperationException(name + ": invalid/duplicate skill definition or missing icon.");
    }
}
