using UnityEngine;

/// <summary>
/// Atomic craft transaction engine, separate from UI (Roadmap Phase 7: "Tach CraftingService khoi
/// UI"). Session-scoped persistent singleton like ShopManager/QuestManager, torn down by
/// GameplaySceneLifetime. On a successful craft it raises QuestDomainEvents.ItemCrafted exactly
/// once, closing the Phase 6 Quest integration gap for Craft objectives. Never partially consumes
/// ingredients: fully validates possession and output capacity before mutating anything.
/// </summary>
public sealed class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [SerializeField] private RecipeCatalog _catalog;

    private IItemResolver _itemResolver;

    public IRecipeResolver Catalog => _catalog;

    private IItemResolver ItemResolver => _itemResolver ??= new ResourcesItemResolver();

    internal void ConfigureForTests(RecipeCatalog catalog, IItemResolver itemResolver = null)
    {
        _catalog = catalog;
        _itemResolver = itemResolver;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Crafts recipeId at the given stationTag (null/empty = no station). Validates every
    /// ingredient is possessed in full and the output fits before consuming/granting anything.</summary>
    public bool TryCraft(string recipeId, string stationTag, out CraftingTransactionResult result)
    {
        if (!GameStateManager.AllowsGameplayInput)
        {
            result = CraftingTransactionResult.GameplayNotAllowed;
            return false;
        }

        if (_catalog == null || !_catalog.TryResolve(recipeId, out RecipeDefinition recipe))
        {
            result = CraftingTransactionResult.RecipeNotFound;
            return false;
        }

        if (!string.IsNullOrEmpty(recipe.RequiredStationTag)
            && !string.Equals(recipe.RequiredStationTag, stationTag, System.StringComparison.Ordinal))
        {
            result = CraftingTransactionResult.WrongStation;
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            result = CraftingTransactionResult.InsufficientIngredients;
            return false;
        }

        var resolvedIngredients = new ItemSO[recipe.Ingredients.Count];
        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            RecipeIngredientEntry ingredient = recipe.Ingredients[i];
            if (!ItemResolver.TryResolve(ingredient.ItemId, out ItemSO item)
                || !InventoryManager.Instance.HasItem(item, ingredient.Quantity))
            {
                result = CraftingTransactionResult.InsufficientIngredients;
                return false;
            }
            resolvedIngredients[i] = item;
        }

        if (!ItemResolver.TryResolve(recipe.OutputItemId, out ItemSO outputItem)
            || !InventoryManager.Instance.HasCapacityFor(outputItem, recipe.OutputQuantity))
        {
            result = CraftingTransactionResult.InsufficientOutputCapacity;
            return false;
        }

        for (int i = 0; i < recipe.Ingredients.Count; i++)
            InventoryManager.Instance.RemoveItem(resolvedIngredients[i], recipe.Ingredients[i].Quantity);
        InventoryManager.Instance.AddItem(outputItem, recipe.OutputQuantity);

        result = CraftingTransactionResult.Success;
        QuestDomainEvents.RaiseItemCrafted(recipe.OutputItemId, recipe.OutputQuantity, stationTag);
        return true;
    }
}
