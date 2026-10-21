using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Material,
    Consumable,
}

[System.Flags]
public enum InventoryCategory
{
    All = 0,
    Equipment = 1,
    Resources = 2,
    Consumption = 4,
    Others = 8
}

public enum QuickBarUseMode { Inactive = 0, Active = 1 }

// Effect applied by ConsumableUseHandler when an Active quick-bar item is used. None means the
// item has no consumption effect wired up yet (ActivateSlot's request is simply ignored).
public enum ConsumableEffectType
{
    None = 0,
    Heal = 1,
    RestoreStamina = 2,
    RestoreMana = 3,
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Scriptable Objects/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType type;
    public InventoryCategory inventoryCategory;
    [Header("Quick Bar")]
    [Tooltip("Allow this item to be assigned to Quick Bar slots 1-4.")]
    public bool usedItems;
    [Tooltip("Active requests immediate use; Inactive only selects the item for farming/fishing.")]
    public QuickBarUseMode quickBarUseMode;
    [Tooltip("What ConsumableUseHandler applies when this item is used from an Active quick-bar slot. None = no effect/consumption (ActivateSlot's request is ignored).")]
    public ConsumableEffectType consumableEffect;
    [Tooltip("Magnitude for the effect above (health/stamina/mana restored). Unused when effect is None.")]
    public float effectAmount;
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

    protected virtual void OnValidate()
    {
        _minBuyPrice = Mathf.Max(0, _minBuyPrice);
        _maxBuyPrice = Mathf.Max(_minBuyPrice, _maxBuyPrice);
        _minSellPrice = Mathf.Max(0, _minSellPrice);
        _maxSellPrice = Mathf.Max(_minSellPrice, _maxSellPrice);
    }
}
