using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Material,
    Consumable,
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Scriptable Objects/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType type;
    public bool isStackable = true;
    public int maxStackSize = 99;

    [Header("NPC Price Range")]
    [SerializeField, Min(0)] private int _minBuyPrice;
    [SerializeField, Min(0)] private int _maxBuyPrice;
    [SerializeField, Min(0)] private int _minSellPrice;
    [SerializeField, Min(0)] private int _maxSellPrice;

    public int MinBuyPrice => Mathf.Max(0, _minBuyPrice);
    public int MaxBuyPrice => Mathf.Max(MinBuyPrice, _maxBuyPrice);
    public int MinSellPrice => Mathf.Max(0, _minSellPrice);
    public int MaxSellPrice => Mathf.Max(MinSellPrice, _maxSellPrice);

    private void OnValidate()
    {
        _minBuyPrice = Mathf.Max(0, _minBuyPrice);
        _maxBuyPrice = Mathf.Max(_minBuyPrice, _maxBuyPrice);
        _minSellPrice = Mathf.Max(0, _minSellPrice);
        _maxSellPrice = Mathf.Max(_minSellPrice, _maxSellPrice);
    }
}
