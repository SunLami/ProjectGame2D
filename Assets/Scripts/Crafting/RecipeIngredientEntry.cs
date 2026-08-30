using System;
using UnityEngine;

[Serializable]
public sealed class RecipeIngredientEntry
{
    [SerializeField] private string _itemId;
    [SerializeField, Min(1)] private int _quantity = 1;

    public string ItemId => _itemId;
    public int Quantity => _quantity;
}
