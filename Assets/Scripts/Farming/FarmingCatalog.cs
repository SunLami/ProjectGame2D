using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FarmingCatalog", menuName = "Game/Farming/Farming Catalog")]
public sealed class FarmingCatalog : ScriptableObject
{
    [SerializeField] private CropDefinition[] _crops;
    private Dictionary<string, CropDefinition> _lookup;

    public CropDefinition[] Crops => _crops;

    public bool TryResolve(string cropId, out CropDefinition crop)
    {
        crop = null;
        EnsureLookup();
        return !string.IsNullOrWhiteSpace(cropId) && _lookup.TryGetValue(cropId, out crop);
    }

    private void EnsureLookup()
    {
        if (_lookup != null) return;
        _lookup = new Dictionary<string, CropDefinition>(StringComparer.Ordinal);
        if (_crops == null) return;
        foreach (CropDefinition crop in _crops)
            if (crop != null && !string.IsNullOrWhiteSpace(crop.CropId) && !_lookup.ContainsKey(crop.CropId))
                _lookup.Add(crop.CropId, crop);
    }

    private void OnValidate() => _lookup = null;
    internal void ConfigureForTests(params CropDefinition[] crops) { _crops = crops; _lookup = null; }
}
