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

    public string ItemId => _itemId;
    public int Price => _price;
}
