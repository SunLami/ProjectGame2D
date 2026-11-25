using System;
using UnityEngine;

/// <summary>One purchasable line in a ShopDefinition: stable itemId (resolved via IItemResolver at
/// transaction time, never a direct ScriptableObject reference in the save/runtime boundary) plus
/// buy price. No stock quantity in this phase -- shops sell unlimited copies; limited/restocking
/// stock is deferred (DataDrivenDevelopment.md "Restock definition tuong lai").</summary>
[Serializable]
public sealed class ShopStockEntry
{
    [SerializeField] private string _itemId;
    [SerializeField, Min(0)] private int _price;

    [Tooltip("Optional questId. When set, this line is a quest-scoped allowance: it can only be "
        + "bought while that quest is Active with a Purchase objective for this item as its current "
        + "step, and never more than that objective still needs (QuestManager."
        + "GetRemainingPurchaseAllowance). Lets a complimentary 0 G quest line exist without becoming "
        + "an unlimited free item source.")]
    [SerializeField] private string _questAllowanceId;

    public string ItemId => _itemId;
    public int Price => _price;
    public string QuestAllowanceId => _questAllowanceId;
    public bool IsQuestAllowance => !string.IsNullOrEmpty(_questAllowanceId);
}
