using System;

[Serializable]
public sealed class FishInstanceData
{
    public string instanceId;
    public int weightGrams;

    public float WeightKilograms => weightGrams / 1000f;

    public int GetTotalValue(FishDefinitionSO definition) =>
        definition == null ? 0 : definition.GetValueForWeight(weightGrams);

    public FishInstanceData Clone() => new()
    {
        instanceId = instanceId,
        weightGrams = weightGrams
    };

    public static FishInstanceData Create(FishDefinitionSO definition, int weightGrams, string instanceId = null)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        return new FishInstanceData
        {
            instanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId,
            weightGrams = definition.ClampWeight(weightGrams)
        };
    }
}
