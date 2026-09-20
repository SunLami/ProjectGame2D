using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FishingSpot", menuName = "Project Game 2D/Fishing/Fishing Spot")]
public sealed class FishingSpotDefinition : ScriptableObject
{
    [Serializable]
    public sealed class FishEntry
    {
        [SerializeField] private FishDefinitionSO _fish;
        [SerializeField, Min(0.01f)] private float _weight = 1f;

        public FishDefinitionSO Fish => _fish;
        public float Weight => _weight;
    }

    [Header("Bite")]
    [SerializeField, Min(0f)] private float _minimumWaitSeconds = 1.5f;
    [SerializeField, Min(0f)] private float _maximumWaitSeconds = 4f;
    [SerializeField, Min(0.1f)] private float _hookWindowSeconds = 1.25f;

    [Header("Mini-game")]
    [SerializeField, Min(1f)] private float _timeLimitSeconds = 18f;
    [SerializeField, Range(0.05f, 0.8f)] private float _catchZoneSize = 0.25f;
    [SerializeField, Min(0.01f)] private float _catchProgressPerSecond = 0.34f;
    [SerializeField, Min(0.01f)] private float _progressLossPerSecond = 0.2f;
    [SerializeField, Min(0.01f)] private float _fishSpeed = 0.8f;
    [SerializeField, Min(0.01f)] private float _fishTargetInterval = 0.75f;
    [SerializeField, Min(0.01f)] private float _liftAcceleration = 2.8f;
    [SerializeField, Min(0.01f)] private float _gravity = 2.1f;
    [SerializeField, Min(0.01f)] private float _maximumCatchZoneSpeed = 1.25f;

    [Header("Catch Table")]
    [SerializeField] private FishEntry[] _fish = Array.Empty<FishEntry>();

    public float MinimumWaitSeconds => _minimumWaitSeconds;
    public float MaximumWaitSeconds => _maximumWaitSeconds;
    public float HookWindowSeconds => _hookWindowSeconds;
    public float TimeLimitSeconds => _timeLimitSeconds;
    public float CatchZoneSize => _catchZoneSize;
    public float CatchProgressPerSecond => _catchProgressPerSecond;
    public float ProgressLossPerSecond => _progressLossPerSecond;
    public float FishSpeed => _fishSpeed;
    public float FishTargetInterval => _fishTargetInterval;
    public float LiftAcceleration => _liftAcceleration;
    public float Gravity => _gravity;
    public float MaximumCatchZoneSpeed => _maximumCatchZoneSpeed;
    public FishEntry[] FishEntries => _fish;

    public bool TryRollFish(out FishDefinitionSO result)
    {
        result = null;
        float totalWeight = 0f;
        foreach (FishEntry entry in _fish)
            if (entry?.Fish != null && entry.Weight > 0f)
                totalWeight += entry.Weight;

        if (totalWeight <= 0f)
            return false;

        float roll = UnityEngine.Random.value * totalWeight;
        foreach (FishEntry entry in _fish)
        {
            if (entry?.Fish == null || entry.Weight <= 0f)
                continue;
            roll -= entry.Weight;
            if (roll <= 0f)
            {
                result = entry.Fish;
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        _minimumWaitSeconds = Mathf.Max(0f, _minimumWaitSeconds);
        _maximumWaitSeconds = Mathf.Max(_minimumWaitSeconds, _maximumWaitSeconds);
        _hookWindowSeconds = Mathf.Max(0.1f, _hookWindowSeconds);
        _timeLimitSeconds = Mathf.Max(1f, _timeLimitSeconds);
        _catchProgressPerSecond = Mathf.Max(0.01f, _catchProgressPerSecond);
        _progressLossPerSecond = Mathf.Max(0.01f, _progressLossPerSecond);
    }
}
