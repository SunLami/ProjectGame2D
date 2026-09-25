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
    [SerializeField] private Button _buyTabButton;
    [SerializeField] private Button _sellTabButton;
    [SerializeField] private GameObject _buyPanel;
    [SerializeField] private GameObject _sellPanel;
    [SerializeField] private Image _sellItemIcon;
    [SerializeField] private TMP_Text _sellItemName;
    [SerializeField] private TMP_Text _sellQuoteText;

    [Header("Crafting")]
    [SerializeField] private TMP_Text _craftingTitle;
    [SerializeField] private Transform _recipeListContent;
    [SerializeField] private GameObject _recipeRowTemplate;
    [SerializeField] private GameObject _recipeCategoryRowTemplate;
    [SerializeField] private TMP_Text _recipeDetails;
    [SerializeField] private Image _recipeOutputIcon;
    [SerializeField] private Image[] _ingredientIcons;
    [SerializeField] private TMP_Text[] _ingredientCounts;
    [SerializeField] private TMP_Text _craftingFeedback;
    [SerializeField] private Button _craftButton;
    [SerializeField] private Button _craftingCloseButton;

    private readonly List<GameObject> _shopRows = new();
    private readonly List<GameObject> _recipeRows = new();
    private readonly Dictionary<string, List<RecipeDefinition>> _recipesByCategory = new();
    private ResourcesItemResolver _items;
    private ShopNpcInteractionService _shopService;
    private CraftingNpcInteractionService _craftingService;
    private ShopDefinition _shop;
    private string _selectedBuyItemId;
    private RecipeDefinition _selectedRecipe;
    private string _npcId;
    private string _stationTag;
    private int _quantity = 1;
    private int _buyUnitPrice;
    private int _sellUnitPrice;
    private InventorySlot _stagedSellSlot;
    private PlayerInput _playerInput;
    private string _expandedRecipeCategory;

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
        _buyTabButton?.onClick.AddListener(ShowBuyTab);
        _sellTabButton?.onClick.AddListener(ShowSellTab);
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
        _buyTabButton?.onClick.RemoveListener(ShowBuyTab);
        _sellTabButton?.onClick.RemoveListener(ShowSellTab);
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
        ShowBuyTab();
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
        _selectedBuyItemId = null;
        _stagedSellSlot = null;
        if (_shop.UsesExplicitBuyItems)
        {
            foreach (ItemSO item in _shop.BuyItems)
                if (item != null) CreateShopRow(item.itemId, FormatBuyRange(item));
        }
        else
        {
            foreach (ShopStockEntry stock in _shop.Stock)
                CreateShopRow(stock.ItemId, $"{stock.Price} G");
        }
        if (_shopRows.Count > 0)
            SelectBuyItem(_shopRows[0].name.Substring("ShopRow_".Length));
        SelectDefault(_shopRows, _shopCloseButton);
    }

    private void CreateShopRow(string itemId, string priceLabel)
    {
        GameObject row = Instantiate(_shopRowTemplate, _shopListContent);
        row.name = $"ShopRow_{itemId}";
        row.SetActive(true);
        row.transform.Find("Name").GetComponent<TMP_Text>().text = ItemName(itemId);
        row.transform.Find("Price").GetComponent<TMP_Text>().text = priceLabel;
        Image icon = row.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && _items.TryResolve(itemId, out ItemSO rowItem))
        {
            icon.sprite = rowItem.icon;
            icon.enabled = rowItem.icon != null;
        }
        string selectedItemId = itemId;
        row.GetComponent<Button>().onClick.AddListener(() => SelectBuyItem(selectedItemId));
        _shopRows.Add(row);
    }

    private string FormatBuyRange(ItemSO item)
    {
        if (item.MaxBuyPrice > 0)
            return item.MinBuyPrice == item.MaxBuyPrice ? $"{item.MinBuyPrice} G" : $"{item.MinBuyPrice}-{item.MaxBuyPrice} G";
        foreach (ShopStockEntry stock in _shop.Stock)
            if (stock != null && stock.ItemId == item.itemId)
                return $"{stock.Price} G";
        return "PRICE NOT SET";
    }

    private void RebuildRecipes()
    {
        ClearRows(_recipeRows);
        _recipesByCategory.Clear();
        _craftingTitle.text = "CRAFTING";
        _craftingFeedback.text = string.Empty;
        _selectedRecipe = null;
        IReadOnlyList<RecipeDefinition> recipes = _craftingService.GetOfferedRecipes(_npcId);
        foreach (RecipeDefinition recipe in recipes)
        {
            string category = RecipeCategory(recipe);
            if (!_recipesByCategory.TryGetValue(category, out List<RecipeDefinition> group))
            {
                group = new List<RecipeDefinition>();
                _recipesByCategory.Add(category, group);
            }
            group.Add(recipe);
        }

        string[] categoryOrder = { "HEAD", "BODY", "FOOT", "RING", "NECKLACE", "SHIELD", "SWORD" };
        foreach (string category in categoryOrder)
        {
            if (!_recipesByCategory.TryGetValue(category, out List<RecipeDefinition> group))
                group = new List<RecipeDefinition>();
            CreateCategoryRow(category, group);
            if (_expandedRecipeCategory != category) continue;
            foreach (RecipeDefinition recipe in group) CreateBlueprintRow(recipe);
        }
        RefreshRecipeDetails();
        SelectDefault(_recipeRows, _craftingCloseButton);
    }

    private void CreateCategoryRow(string category, List<RecipeDefinition> recipes)
    {
        GameObject template = _recipeCategoryRowTemplate != null ? _recipeCategoryRowTemplate : _recipeRowTemplate;
        GameObject row = Instantiate(template, _recipeListContent);
        row.name = $"RecipeCategory_{category}";
        row.SetActive(true);
        TMP_Text name = row.transform.Find("Name")?.GetComponent<TMP_Text>();
        if (name != null) name.text = $"{(_expandedRecipeCategory == category ? "−" : "+")}  {category}";
        TMP_Text station = row.transform.Find("Station")?.GetComponent<TMP_Text>();
        if (station != null) station.text = recipes.Count.ToString();
        Image icon = row.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            ItemSO categoryItem = null;
            bool hasIcon = recipes.Count > 0 && _items.TryResolve(recipes[0].OutputItemId, out categoryItem)
                && categoryItem.icon != null;
            icon.sprite = hasIcon ? categoryItem.icon : null;
            icon.enabled = hasIcon;
        }
        row.GetComponent<Button>().onClick.AddListener(() => ToggleRecipeCategory(category));
        _recipeRows.Add(row);
    }

    private void CreateBlueprintRow(RecipeDefinition recipe)
    {
            GameObject row = Instantiate(_recipeRowTemplate, _recipeListContent);
            row.name = $"RecipeRow_{recipe.RecipeId}";
            row.SetActive(true);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = recipe.DisplayName;
            row.transform.Find("Station").GetComponent<TMP_Text>().text = string.IsNullOrEmpty(recipe.RequiredStationTag)
                ? "ANYWHERE"
                : "FORGE";
            Image icon = row.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && _items.TryResolve(recipe.OutputItemId, out ItemSO blueprintItem))
                icon.sprite = blueprintItem.icon;
            RecipeDefinition selected = recipe;
            row.GetComponent<Button>().onClick.AddListener(() => SelectRecipe(selected));
            _recipeRows.Add(row);
    }

    private void ToggleRecipeCategory(string category)
    {
        _expandedRecipeCategory = _expandedRecipeCategory == category ? null : category;
        _selectedRecipe = null;
        RebuildRecipes();
    }

    private string RecipeCategory(RecipeDefinition recipe)
    {
        if (!_items.TryResolve(recipe.OutputItemId, out ItemSO item) || item is not EquipmentItemSO equipment)
            return string.Empty;
        return equipment.slot == EquipSlot.Weapon ? "SWORD" : equipment.slot.ToString().ToUpperInvariant();
    }

    private void SelectBuyItem(string itemId)
    {
        _selectedBuyItemId = itemId;
        _quantity = 1;
        CreateBuyQuote();
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
        RefreshActiveQuote();
        RefreshShopDetails();
    }

    private void IncreaseQuantity()
    {
        _quantity = Mathf.Min(99, _quantity + 1);
        RefreshActiveQuote();
        RefreshShopDetails();
    }

    private void Buy()
    {
        if (string.IsNullOrEmpty(_selectedBuyItemId))
            return;
        bool success = _shopService.TryPurchaseQuoted(_npcId, _shop.ShopId, _selectedBuyItemId, _quantity, _buyUnitPrice, out _, out ShopTransactionResult result);
        _shopFeedback.text = success ? $"Purchased {ItemName(_selectedBuyItemId)} x{_quantity}." : FormatShopFailure(result);
        if (success)
            foreach (CommerceInventoryPanelUI panel in _shopWindow.GetComponentsInChildren<CommerceInventoryPanelUI>(true))
                panel.Refresh();
        RefreshShopDetails();
    }

    private void Sell()
    {
        if (_stagedSellSlot == null || _stagedSellSlot.IsEmpty)
            return;
        string itemId = _stagedSellSlot.item.itemId;
        bool success = _shopService.TrySellQuoted(_npcId, _shop.ShopId, itemId, _quantity, _sellUnitPrice, out int total, out ShopTransactionResult result);
        _shopFeedback.text = success ? $"Sold {ItemName(itemId)} x{_quantity} for {total} Gold." : FormatShopFailure(result);
        if (success) ClearSellStage(); else RefreshSellStage();
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
        _shopGold.text = (InventoryManager.Instance?.Gold ?? 0).ToString("N0");
        _quantityText.text = _quantity.ToString();
        if (string.IsNullOrEmpty(_selectedBuyItemId))
        {
            _shopDetails.text = "Select an item.";
            return;
        }
        int owned = OwnedCount(_selectedBuyItemId);
        var text = new StringBuilder();
        text.Append("<align=\"center\"><size=18><b>").Append(ItemName(_selectedBuyItemId))
            .Append("</b></size></align>\n")
            .Append("<align=\"left\"><size=12>").Append(ItemDescription(_selectedBuyItemId)).Append("</size>\n")
            .Append("<size=13><color=#C9A34A><b>OWNED</b></color>  ").Append(owned)
            .Append("     <color=#C9A34A><b>TOTAL</b></color>  <b>")
            .Append(_buyUnitPrice * _quantity).Append(" G</b></size></align>");
        _shopDetails.text = text.ToString();
    }

    public void StageSell(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty || _shop == null) return;
        _stagedSellSlot = slot;
        _quantity = 1;
        ShowSellTab();
        RefreshSellQuote();
    }

    private void ShowBuyTab()
    {
        if (_buyPanel != null) _buyPanel.SetActive(true);
        if (_sellPanel != null) _sellPanel.SetActive(false);
        if (_buyButton != null) _buyButton.gameObject.SetActive(true);
        if (_sellButton != null) _sellButton.gameObject.SetActive(false);
        SetTabVisual(_buyTabButton, true);
        SetTabVisual(_sellTabButton, false);
        if (_shopTitle != null) _shopTitle.text = _shop?.DisplayName ?? "SHOP";
        CreateBuyQuote();
        RefreshShopDetails();
    }

    private void ShowSellTab()
    {
        if (_buyPanel != null) _buyPanel.SetActive(false);
        if (_sellPanel != null) _sellPanel.SetActive(true);
        if (_buyButton != null) _buyButton.gameObject.SetActive(false);
        if (_sellButton != null) _sellButton.gameObject.SetActive(true);
        SetTabVisual(_buyTabButton, false);
        SetTabVisual(_sellTabButton, true);
        if (_shopTitle != null) _shopTitle.text = _shop?.DisplayName ?? "SHOP";
        RefreshSellStage();
    }

    private static void SetTabVisual(Button tab, bool selected)
    {
        if (tab == null) return;
        Image background = tab.GetComponent<Image>();
        if (background != null)
            background.color = selected ? new Color(0.48f, 0.29f, 0.08f, 1f) : new Color(0.15f, 0.09f, 0.045f, 0.94f);
        Transform selectedAccent = tab.transform.Find("SelectedAccent");
        if (selectedAccent != null) selectedAccent.gameObject.SetActive(selected);
        TMP_Text label = tab.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.color = selected ? new Color(1f, 0.88f, 0.35f, 1f) : new Color(0.82f, 0.74f, 0.58f, 1f);
    }

    private void CreateBuyQuote()
    {
        if (string.IsNullOrEmpty(_selectedBuyItemId) || _shopService == null) return;
        _shopService.TryCreateBuyQuote(_npcId, _shop.ShopId, _selectedBuyItemId, _quantity, out _buyUnitPrice, out _);
    }

    private void RefreshSellQuote()
    {
        if (_stagedSellSlot == null || _stagedSellSlot.IsEmpty) { RefreshSellStage(); return; }
        if (!_shopService.TryCreateSellQuote(_npcId, _shop.ShopId, _stagedSellSlot.item.itemId, _quantity, out _sellUnitPrice, out ShopTransactionResult result))
        {
            _sellUnitPrice = 0;
            _shopFeedback.text = FormatShopFailure(result);
        }
        RefreshSellStage();
    }

    private void RefreshActiveQuote()
    {
        if (_sellPanel != null && _sellPanel.activeSelf) RefreshSellQuote();
        else CreateBuyQuote();
    }

    private void RefreshSellStage()
    {
        bool hasItem = _stagedSellSlot != null && !_stagedSellSlot.IsEmpty;
        if (_sellItemIcon != null)
        {
            _sellItemIcon.enabled = hasItem;
            _sellItemIcon.sprite = hasItem ? _stagedSellSlot.item.icon : null;
        }
        if (_sellItemName != null) _sellItemName.text = hasItem ? ItemName(_stagedSellSlot.item.itemId) : "DROP ITEM HERE TO SELL";
        if (_sellQuoteText != null) _sellQuoteText.text = hasItem && _sellUnitPrice > 0 ? $"{_sellUnitPrice * _quantity} GOLD" : string.Empty;
    }

    private void ClearSellStage()
    {
        _stagedSellSlot = null;
        _sellUnitPrice = 0;
        _quantity = 1;
        RefreshSellStage();
    }

    private void RefreshRecipeDetails()
    {
        if (_selectedRecipe == null)
        {
            _recipeDetails.text = "Select a category, then choose a blueprint.";
            RefreshRecipeVisuals(null);
            return;
        }
        var text = new StringBuilder();
        text.Append("<align=\"center\"><size=25><b>").Append(_selectedRecipe.DisplayName).Append("</b></size></align>\n");
        text.Append("<align=\"left\"><size=17><color=#C9A34A><b>OUTPUT</b></color>  ")
            .Append(ItemName(_selectedRecipe.OutputItemId)).Append("  ×")
            .Append(_selectedRecipe.OutputQuantity).Append("</size>\n")
            .Append("<size=17><color=#C9A34A><b>STATION</b></color>  ")
            .Append(FormatStationName(_selectedRecipe.RequiredStationTag)).Append("</size>\n")
            .Append("<color=#8A4B14><size=17><b>INGREDIENTS</b></size></color></align>");
        _recipeDetails.text = text.ToString();
        RefreshRecipeVisuals(_selectedRecipe);
    }

    private void RefreshRecipeVisuals(RecipeDefinition recipe)
    {
        if (_recipeOutputIcon != null)
        {
            ItemSO output = null;
            bool hasOutput = recipe != null && _items.TryResolve(recipe.OutputItemId, out output);
            _recipeOutputIcon.enabled = hasOutput;
            _recipeOutputIcon.sprite = hasOutput ? output.icon : null;
        }

        int visualCount = Mathf.Min(_ingredientIcons?.Length ?? 0, _ingredientCounts?.Length ?? 0);
        for (int i = 0; i < visualCount; i++)
        {
            ItemSO ingredientItem = null;
            bool hasIngredient = recipe != null && i < recipe.Ingredients.Count
                && _items.TryResolve(recipe.Ingredients[i].ItemId, out ingredientItem);
            _ingredientIcons[i].enabled = hasIngredient;
            _ingredientIcons[i].sprite = hasIngredient ? ingredientItem.icon : null;
            _ingredientCounts[i].text = hasIngredient
                ? $"{OwnedCount(recipe.Ingredients[i].ItemId)}/{recipe.Ingredients[i].Quantity}"
                : string.Empty;
        }
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
