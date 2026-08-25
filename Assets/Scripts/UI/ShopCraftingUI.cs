using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Presentation-only Shop/Crafting modal. All NPC-bound transactions go through capability services.</summary>
public sealed class ShopCraftingUI : MonoBehaviour
{
    public static ShopCraftingUI Instance { get; private set; }

    [Header("Roots")]
    [SerializeField] private GameObject _backdrop;
    [SerializeField] private GameObject _shopWindow;
    [SerializeField] private GameObject _craftingWindow;

    [Header("Shop")]
    [SerializeField] private TMP_Text _shopTitle;
    [SerializeField] private TMP_Text _shopGold;
    [SerializeField] private Transform _shopListContent;
    [SerializeField] private GameObject _shopRowTemplate;
    [SerializeField] private TMP_Text _shopDetails;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private TMP_Text _shopFeedback;
    [SerializeField] private Button _quantityMinusButton;
    [SerializeField] private Button _quantityPlusButton;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Button _sellButton;
    [SerializeField] private Button _shopCloseButton;

    [Header("Crafting")]
    [SerializeField] private TMP_Text _craftingTitle;
    [SerializeField] private Transform _recipeListContent;
    [SerializeField] private GameObject _recipeRowTemplate;
    [SerializeField] private TMP_Text _recipeDetails;
    [SerializeField] private TMP_Text _craftingFeedback;
    [SerializeField] private Button _craftButton;
    [SerializeField] private Button _craftingCloseButton;

    private readonly List<GameObject> _shopRows = new();
    private readonly List<GameObject> _recipeRows = new();
    private ResourcesItemResolver _items;
    private ShopNpcInteractionService _shopService;
    private CraftingNpcInteractionService _craftingService;
    private ShopDefinition _shop;
    private ShopStockEntry _selectedStock;
    private RecipeDefinition _selectedRecipe;
    private string _npcId;
    private string _stationTag;
    private int _quantity = 1;
    private PlayerInput _playerInput;

    public bool IsOpen => _backdrop != null && _backdrop.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _items = new ResourcesItemResolver();
        SetVisible(false, false);
    }

    private void OnEnable()
    {
        _quantityMinusButton.onClick.AddListener(DecreaseQuantity);
        _quantityPlusButton.onClick.AddListener(IncreaseQuantity);
        _buyButton.onClick.AddListener(Buy);
        _sellButton.onClick.AddListener(Sell);
        _shopCloseButton.onClick.AddListener(Close);
        _craftButton.onClick.AddListener(Craft);
        _craftingCloseButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        _quantityMinusButton.onClick.RemoveListener(DecreaseQuantity);
        _quantityPlusButton.onClick.RemoveListener(IncreaseQuantity);
        _buyButton.onClick.RemoveListener(Buy);
        _sellButton.onClick.RemoveListener(Sell);
        _shopCloseButton.onClick.RemoveListener(Close);
        _craftButton.onClick.RemoveListener(Craft);
        _craftingCloseButton.onClick.RemoveListener(Close);
        UnbindInventory();
        RestorePlayerInput();
    }

    private void Update()
    {
        bool cancelPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        cancelPressed |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        if (IsOpen && cancelPressed)
            Close();
    }

    public void OpenShop(string npcId, PlayerInput playerInput)
    {
        if (!CanOpen() || ShopManager.Instance == null)
            return;

        _shopService = new ShopNpcInteractionService(ShopManager.Instance);
        if (!_shopService.TryGetShop(npcId, out _shop))
            return;

        _npcId = npcId;
        CapturePlayerInput(playerInput);
        BindInventory();
        SetVisible(true, false);
        RebuildShop();
    }

    public void OpenCrafting(string npcId, string stationTag, PlayerInput playerInput)
    {
        if (!CanOpen() || CraftingManager.Instance == null)
            return;

        _craftingService = new CraftingNpcInteractionService(CraftingManager.Instance);
        _npcId = npcId;
        _stationTag = stationTag;
        CapturePlayerInput(playerInput);
        BindInventory();
        SetVisible(false, true);
        RebuildRecipes();
    }

    public void Close()
    {
        SetVisible(false, false);
        UnbindInventory();
        RestorePlayerInput();
    }

    private bool CanOpen() => GameStateManager.Instance != null
        && GameStateManager.Instance.CurrentState == GameState.Playing;

    private void CapturePlayerInput(PlayerInput playerInput)
    {
        RestorePlayerInput();
        _playerInput = playerInput;
        _playerInput?.DeactivateInput();
    }

    private void RestorePlayerInput()
    {
        if (_playerInput != null)
            _playerInput.ActivateInput();
        _playerInput = null;
    }

    private void BindInventory()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshVisibleDetails;
    }

    private void UnbindInventory()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshVisibleDetails;
    }

    private void SetVisible(bool shop, bool crafting)
    {
        _backdrop.SetActive(shop || crafting);
        _shopWindow.SetActive(shop);
        _craftingWindow.SetActive(crafting);
    }

    private void RebuildShop()
    {
        ClearRows(_shopRows);
        _shopTitle.text = _shop.DisplayName;
        _shopFeedback.text = string.Empty;
        _selectedStock = null;
        foreach (ShopStockEntry stock in _shop.Stock)
        {
            GameObject row = Instantiate(_shopRowTemplate, _shopListContent);
            row.name = $"ShopRow_{stock.ItemId}";
            row.SetActive(true);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = ItemName(stock.ItemId);
            row.transform.Find("Price").GetComponent<TMP_Text>().text = $"{stock.Price} G";
            ShopStockEntry selected = stock;
            row.GetComponent<Button>().onClick.AddListener(() => SelectStock(selected));
            _shopRows.Add(row);
        }
        if (_shop.Stock.Count > 0)
            SelectStock(_shop.Stock[0]);
        SelectDefault(_shopRows, _shopCloseButton);
    }

    private void RebuildRecipes()
    {
        ClearRows(_recipeRows);
        _craftingTitle.text = "CRAFTING";
        _craftingFeedback.text = string.Empty;
        _selectedRecipe = null;
        IReadOnlyList<RecipeDefinition> recipes = _craftingService.GetOfferedRecipes(_npcId);
        foreach (RecipeDefinition recipe in recipes)
        {
            GameObject row = Instantiate(_recipeRowTemplate, _recipeListContent);
            row.name = $"RecipeRow_{recipe.RecipeId}";
            row.SetActive(true);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = recipe.DisplayName;
            row.transform.Find("Station").GetComponent<TMP_Text>().text = string.IsNullOrEmpty(recipe.RequiredStationTag)
                ? "ANYWHERE"
                : "FORGE";
            RecipeDefinition selected = recipe;
            row.GetComponent<Button>().onClick.AddListener(() => SelectRecipe(selected));
            _recipeRows.Add(row);
        }
        if (recipes.Count > 0)
            SelectRecipe(recipes[0]);
        SelectDefault(_recipeRows, _craftingCloseButton);
    }

    private void SelectStock(ShopStockEntry stock)
    {
        _selectedStock = stock;
        _quantity = 1;
        RefreshShopDetails();
    }

    private void SelectRecipe(RecipeDefinition recipe)
    {
        _selectedRecipe = recipe;
        RefreshRecipeDetails();
    }

    private void DecreaseQuantity()
    {
        _quantity = Mathf.Max(1, _quantity - 1);
        RefreshShopDetails();
    }

    private void IncreaseQuantity()
    {
        _quantity = Mathf.Min(99, _quantity + 1);
        RefreshShopDetails();
    }

    private void Buy()
    {
        if (_selectedStock == null)
            return;
        bool success = _shopService.TryPurchase(_npcId, _shop.ShopId, _selectedStock.ItemId, _quantity, out ShopTransactionResult result);
        _shopFeedback.text = success ? $"Purchased {ItemName(_selectedStock.ItemId)} x{_quantity}." : FormatShopFailure(result);
        RefreshShopDetails();
    }

    private void Sell()
    {
        if (_selectedStock == null)
            return;
        bool success = _shopService.TrySell(_npcId, _shop.ShopId, _selectedStock.ItemId, _quantity, out ShopTransactionResult result);
        _shopFeedback.text = success ? $"Sold {ItemName(_selectedStock.ItemId)} x{_quantity}." : FormatShopFailure(result);
        RefreshShopDetails();
    }

    private void Craft()
    {
        if (_selectedRecipe == null)
            return;
        bool success = _craftingService.TryCraft(_npcId, _selectedRecipe.RecipeId, _stationTag, out CraftingTransactionResult result);
        _craftingFeedback.text = success ? $"Crafted {_selectedRecipe.DisplayName}." : FormatCraftingFailure(result);
        RefreshRecipeDetails();
    }

    private void RefreshVisibleDetails()
    {
        if (_shopWindow.activeSelf)
            RefreshShopDetails();
        if (_craftingWindow.activeSelf)
            RefreshRecipeDetails();
    }

    private void RefreshShopDetails()
    {
        _shopGold.text = $"GOLD  {InventoryManager.Instance?.Gold ?? 0}";
        _quantityText.text = _quantity.ToString();
        if (_selectedStock == null)
        {
            _shopDetails.text = "Select an item.";
            return;
        }
        int owned = OwnedCount(_selectedStock.ItemId);
        int sellEach = Mathf.RoundToInt(_selectedStock.Price * _shop.SellPriceMultiplier);
        var text = new StringBuilder();
        text.Append("<align=\"center\"><size=32><b>").Append(ItemName(_selectedStock.ItemId))
            .Append("</b></size></align>\n\n")
            .Append("<align=\"left\"><size=20>").Append(ItemDescription(_selectedStock.ItemId)).Append("</size>\n\n")
            .Append("<color=#8A4B14><size=21><b>ITEM DETAILS</b></size></color>\n")
            .Append("<size=20>Owned   <b>").Append(owned).Append("</b></size>\n")
            .Append("<size=20>Buy total   <color=#9A6615><b>").Append(_selectedStock.Price * _quantity).Append(" G</b></color></size>\n")
            .Append("<size=20>Sell total  <color=#9A6615><b>").Append(sellEach * _quantity).Append(" G</b></color></size></align>");
        _shopDetails.text = text.ToString();
    }

    private void RefreshRecipeDetails()
    {
        if (_selectedRecipe == null)
        {
            _recipeDetails.text = "No recipes available.";
            return;
        }
        var text = new StringBuilder();
        text.Append("<align=\"center\"><size=32><b>").Append(_selectedRecipe.DisplayName).Append("</b></size></align>\n\n");
        text.Append("<align=\"left\"><color=#8A4B14><size=21><b>INGREDIENTS</b></size></color>\n");
        foreach (RecipeIngredientEntry ingredient in _selectedRecipe.Ingredients)
        {
            int owned = OwnedCount(ingredient.ItemId);
            string counterColor = owned >= ingredient.Quantity ? "#2D704A" : "#A13A2A";
            text.Append("<size=20>• ").Append(ItemName(ingredient.ItemId)).Append("   ")
                .Append("<color=").Append(counterColor).Append("><b>")
                .Append(owned).Append(" / ").Append(ingredient.Quantity)
                .Append("</b></color></size>\n");
        }
        text.Append("\n<color=#8A4B14><size=21><b>OUTPUT</b></size></color>\n")
            .Append("<size=20>• ").Append(ItemName(_selectedRecipe.OutputItemId)).Append("  ×")
            .Append(_selectedRecipe.OutputQuantity).Append("</size>");
        text.Append("\n\n<color=#8A4B14><size=21><b>STATION</b></size></color>\n")
            .Append("<size=20>").Append(FormatStationName(_selectedRecipe.RequiredStationTag)).Append("</size></align>");
        _recipeDetails.text = text.ToString();
    }

    private static string FormatStationName(string stationTag)
    {
        if (string.IsNullOrEmpty(stationTag))
            return "Anywhere";

        string value = stationTag.StartsWith("station.") ? stationTag.Substring("station.".Length) : stationTag;
        string[] words = value.Split('.', '_');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0) continue;
            words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
        }
        return string.Join(" ", words);
    }

    private int OwnedCount(string itemId)
    {
        int count = 0;
        if (InventoryManager.Instance?.Slots == null)
            return count;
        foreach (InventorySlot slot in InventoryManager.Instance.Slots)
            if (!slot.IsEmpty && slot.item.itemId == itemId)
                count += slot.quantity;
        return count;
    }

    private string ItemName(string itemId) => _items.TryResolve(itemId, out ItemSO item) ? item.itemName : itemId;
    private string ItemDescription(string itemId) => _items.TryResolve(itemId, out ItemSO item) ? item.description : string.Empty;

    private static string FormatShopFailure(ShopTransactionResult result) => result switch
    {
        ShopTransactionResult.ShopNotFound => "This NPC does not operate that shop.",
        ShopTransactionResult.ItemNotInStock => "This shop does not trade that item.",
        ShopTransactionResult.InsufficientGold => "Not enough gold.",
        ShopTransactionResult.InsufficientInventoryCapacity => "Not enough inventory space.",
        ShopTransactionResult.InsufficientItemQuantity => "You do not own enough of this item.",
        ShopTransactionResult.GameplayNotAllowed => "Trading is unavailable right now.",
        _ => "Unable to complete transaction."
    };

    private static string FormatCraftingFailure(CraftingTransactionResult result) => result switch
    {
        CraftingTransactionResult.RecipeNotFound => "This NPC does not offer that recipe.",
        CraftingTransactionResult.WrongStation => "This recipe requires the correct crafting station.",
        CraftingTransactionResult.InsufficientIngredients => "Not enough ingredients.",
        CraftingTransactionResult.InsufficientOutputCapacity => "Not enough inventory space for the result.",
        CraftingTransactionResult.GameplayNotAllowed => "Crafting is unavailable right now.",
        _ => "Unable to craft item."
    };

    private static void ClearRows(List<GameObject> rows)
    {
        foreach (GameObject row in rows)
            Destroy(row);
        rows.Clear();
    }

    private static void SelectDefault(List<GameObject> rows, Button fallback)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(rows.Count > 0 ? rows[0] : fallback.gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
