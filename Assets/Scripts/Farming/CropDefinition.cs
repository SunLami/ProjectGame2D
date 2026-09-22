using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CropDefinition", menuName = "Game/Farming/Crop Definition")]
public sealed class CropDefinition : ScriptableObject
{
    [SerializeField] private string _cropId;
    [SerializeField] private ItemSO _harvestItem;
    [SerializeField, Min(1)] private int _minimumHarvestQuantity = 1;
    [SerializeField, Min(1)] private int _maximumHarvestQuantity = 1;
    [SerializeField] private CropGrowthStage[] _stages;

    public string CropId => _cropId;
    public ItemSO HarvestItem => _harvestItem;
    public int MinimumHarvestQuantity => Mathf.Max(1, _minimumHarvestQuantity);
    public int MaximumHarvestQuantity => Mathf.Max(MinimumHarvestQuantity, _maximumHarvestQuantity);
    public CropGrowthStage[] Stages => _stages;
    public int MatureStageIndex => Mathf.Max(0, (_stages?.Length ?? 1) - 1);

    public int GetStageIndex(long plantedAtUtcTicks, long nowUtcTicks)
    {
        if (_stages == null || _stages.Length <= 1) return 0;
        double elapsed = TimeSpan.FromTicks(Math.Max(0L, nowUtcTicks - plantedAtUtcTicks)).TotalSeconds;
        for (int i = 0; i < _stages.Length - 1; i++)
        {
            elapsed -= Math.Max(0.1f, _stages[i]?.durationSeconds ?? 0.1f);
            if (elapsed < 0d) return i;
        }
        return _stages.Length - 1;
    }

    public Sprite GetStageSprite(int index)
    {
        if (_stages == null || _stages.Length == 0) return null;
        return _stages[Mathf.Clamp(index, 0, _stages.Length - 1)]?.sprite;
    }

    private void OnValidate()
    {
        _minimumHarvestQuantity = Mathf.Max(1, _minimumHarvestQuantity);
        _maximumHarvestQuantity = Mathf.Max(_minimumHarvestQuantity, _maximumHarvestQuantity);
        if (_stages == null) return;
        foreach (CropGrowthStage stage in _stages)
            if (stage != null) stage.durationSeconds = Mathf.Max(0.1f, stage.durationSeconds);
    }

    internal void ConfigureForTests(string cropId, ItemSO harvestItem, int minimum, int maximum, CropGrowthStage[] stages)
    {
        _cropId = cropId;
        _harvestItem = harvestItem;
        _minimumHarvestQuantity = minimum;
        _maximumHarvestQuantity = maximum;
        _stages = stages;
        OnValidate();
    }
}
