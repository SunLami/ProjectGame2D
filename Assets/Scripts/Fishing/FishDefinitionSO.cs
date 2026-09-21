using UnityEngine;

[CreateAssetMenu(fileName = "FishDefinition", menuName = "Project Game 2D/Fishing/Fish Definition")]
public sealed class FishDefinitionSO : ItemSO
{
    [Header("Fish Weight")]
    [SerializeField, Min(1)] private int _minimumWeightGrams = 250;
    [SerializeField, Min(1)] private int _maximumWeightGrams = 1000;

    [Header("Economy")]
    [SerializeField, Min(0)] private int _pricePerKilogram = 10;

    public string FishId => itemId;
    public int MinimumWeightGrams => _minimumWeightGrams;
    public int MaximumWeightGrams => _maximumWeightGrams;
    public int PricePerKilogram => _pricePerKilogram;

    public int RollWeightGrams() => Random.Range(_minimumWeightGrams, _maximumWeightGrams + 1);
    public int ClampWeight(int weightGrams) => Mathf.Clamp(weightGrams, _minimumWeightGrams, _maximumWeightGrams);
    public int GetValueForWeight(int weightGrams) =>
        Mathf.Max(0, Mathf.RoundToInt(_pricePerKilogram * Mathf.Max(0, weightGrams) / 1000f));

    private void OnValidate()
    {
        _minimumWeightGrams = Mathf.Max(1, _minimumWeightGrams);
        _maximumWeightGrams = Mathf.Max(_minimumWeightGrams, _maximumWeightGrams);
        _pricePerKilogram = Mathf.Max(0, _pricePerKilogram);
        isStackable = false;
        maxStackSize = 1;
        type = ItemType.Material;
    }
}
