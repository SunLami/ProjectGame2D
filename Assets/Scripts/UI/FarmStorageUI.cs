using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Presentation-only Farm Storage modal: click an entry in the Inventory list to deposit
/// its whole stack, click an entry in the Storage list to withdraw its whole stack. Mirrors
/// ShopCraftingUI's minimal open/close contract (no GameState push, just PlayerInput handoff) so it
/// composes the same way behind a NPC dialogue outcome. All domain validation stays in
/// FarmStorageManager -- this class only reads its state and calls TryDeposit/TryWithdraw.</summary>
public sealed class FarmStorageUI : MonoBehaviour
{
    public static FarmStorageUI Instance { get; private set; }

    [Header("Roots")]
    [SerializeField] private GameObject _backdrop;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _feedbackText;
    [SerializeField] private Button _closeButton;

    [Header("Inventory (source) list")]
    [SerializeField] private Transform _inventoryListContent;
    [SerializeField] private GameObject _rowTemplate;

    [Header("Storage (destination) list")]
    [SerializeField] private Transform _storageListContent;

    private readonly List<GameObject> _inventoryRows = new();
    private readonly List<GameObject> _storageRows = new();
    private PlayerInput _playerInput;
    private Color _feedbackDefaultColor;

    public bool IsOpen => _backdrop != null && _backdrop.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (_feedbackText != null)
            _feedbackDefaultColor = _feedbackText.color;
        _rowTemplate.SetActive(false);
        SetVisible(false);
    }

    private void OnEnable()
    {
        _closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        _closeButton.onClick.RemoveListener(Close);
        UnbindEvents();
        RestorePlayerInput();
    }

    private void Update()
    {
        bool cancelPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        cancelPressed |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        if (IsOpen && cancelPressed)
            Close();
    }

    public void Open(PlayerInput playerInput)
    {
        if (GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing
            || FarmStorageManager.Instance == null
            || InventoryManager.Instance == null)
        {
            return;
        }

        RestorePlayerInput();
        _playerInput = playerInput;
        _playerInput?.DeactivateInput();

        FarmStorageManager.Instance.EnsureSlotCount(InventoryManager.Instance.Slots.Count);
        BindEvents();
        SetVisible(true);
        if (_title != null)
            _title.text = "Farm Storage";
        if (_feedbackText != null)
            _feedbackText.text = string.Empty;
        Rebuild();
    }

    public void Close()
    {
        SetVisible(false);
        UnbindEvents();
        RestorePlayerInput();
    }

    private void BindEvents()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += Rebuild;
        if (FarmStorageManager.Instance != null)
            FarmStorageManager.Instance.Changed += Rebuild;
    }

    private void UnbindEvents()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Rebuild;
        if (FarmStorageManager.Instance != null)
            FarmStorageManager.Instance.Changed -= Rebuild;
    }

    private void RestorePlayerInput()
    {
        if (_playerInput != null)
            _playerInput.ActivateInput();
        _playerInput = null;
    }

    private void SetVisible(bool visible)
    {
        if (_backdrop != null)
            _backdrop.SetActive(visible);
    }

    private void Rebuild()
    {
        if (!IsOpen)
            return;

        ClearRows(_inventoryRows, _inventoryListContent);
        ClearRows(_storageRows, _storageListContent);

        if (InventoryManager.Instance != null && FarmStorageManager.Instance != null)
        {
            if (FarmStorageManager.Instance.Slots.Count < InventoryManager.Instance.Slots.Count)
            {
                FarmStorageManager.Instance.EnsureSlotCount(InventoryManager.Instance.Slots.Count);
                return;
            }

            foreach (InventorySlot slot in InventoryManager.Instance.Slots)
            {
                if (slot.IsEmpty)
                    continue;
                bool canDeposit = FarmStorageManager.Instance.IsFarmingItem(slot.item);
                CreateRow(_inventoryListContent, _inventoryRows, slot.item, slot.quantity,
                    canDeposit ? () => Deposit(slot.item, slot.quantity) : null,
                    FarmStorageGridSide.Inventory);
            }

            foreach (FarmStorageSlot slot in FarmStorageManager.Instance.Slots)
            {
                if (slot.IsEmpty)
                    continue;
                CreateRow(_storageListContent, _storageRows, slot.item, slot.quantity,
                    () => Withdraw(slot.item, slot.quantity), FarmStorageGridSide.Storage);
            }

            ResizeGridContent(_inventoryListContent, InventoryManager.Instance.Slots.Count);
            ResizeGridContent(_storageListContent, FarmStorageManager.Instance.Slots.Count);
        }
    }

    public void HandleDrop(ItemSO item, int amount, FarmStorageGridSide origin, FarmStorageGridSide destination)
    {
        if (item == null || amount <= 0 || origin == destination)
            return;

        if (destination == FarmStorageGridSide.Storage)
        {
            if (FarmStorageManager.Instance == null || !FarmStorageManager.Instance.IsFarmingItem(item))
            {
                SetFeedback("Only farming items can be stored here.", true);
                return;
            }
            Deposit(item, amount);
            return;
        }

        Withdraw(item, amount);
    }

    private static void ResizeGridContent(Transform itemContent, int slotCount)
    {
        if (itemContent == null)
            return;

        const int columns = 6;
        const float cellSize = 48f;
        const float spacing = 10f;
        const float verticalPadding = 40f;
        int rows = Mathf.Max(5, Mathf.CeilToInt(slotCount / (float)columns));
        float height = verticalPadding + rows * cellSize + (rows - 1) * spacing;

        RectTransform itemRect = itemContent as RectTransform;
        RectTransform scrollContent = itemContent.parent as RectTransform;
        if (itemRect != null)
            itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, height);
        if (scrollContent != null)
            scrollContent.sizeDelta = new Vector2(scrollContent.sizeDelta.x, height);

        Transform emptySlots = scrollContent != null ? scrollContent.Find("EmptySlots") : null;
        if (emptySlots == null || emptySlots.childCount == 0)
            return;

        GameObject template = emptySlots.GetChild(0).gameObject;
        while (emptySlots.childCount < slotCount)
        {
            GameObject slot = Instantiate(template, emptySlots);
            slot.name = $"EmptySlot_{emptySlots.childCount:00}";
        }
    }

    private void Deposit(ItemSO item, int amount)
    {
        if (InventoryManager.Instance == null || FarmStorageManager.Instance == null)
            return;

        if (!FarmStorageManager.Instance.TryDeposit(item, amount))
        {
            SetFeedback("Storage is full.", true);
            return;
        }

        InventoryManager.Instance.RemoveItem(item, amount);
        SetFeedback($"Stored {amount}x {item.itemName}.");
    }

    private void Withdraw(ItemSO item, int amount)
    {
        if (InventoryManager.Instance == null || FarmStorageManager.Instance == null)
            return;

        if (!InventoryManager.Instance.HasCapacityFor(item, amount))
        {
            SetFeedback("Inventory full.", true);
            return;
        }

        if (!FarmStorageManager.Instance.TryWithdraw(item, amount))
            return;

        InventoryManager.Instance.AddItem(item, amount);
        SetFeedback($"Withdrew {amount}x {item.itemName}.");
    }

    private void SetFeedback(string message, bool isError = false)
    {
        if (_feedbackText != null)
        {
            _feedbackText.text = message;
            _feedbackText.color = isError ? new Color(1f, 0.25f, 0.2f, 1f) : _feedbackDefaultColor;
        }
    }

    private void CreateRow(Transform parent, List<GameObject> tracked, ItemSO item, int quantity,
        System.Action onClick, FarmStorageGridSide origin)
    {
        if (parent == null || _rowTemplate == null)
            return;

        GameObject row = Instantiate(_rowTemplate, parent);
        row.SetActive(true);
        tracked.Add(row);

        Image icon = row.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
        }

        TMP_Text label = row.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label != null)
            label.text = quantity > 1 ? quantity.ToString() : string.Empty;

        Button button = row.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = onClick != null;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }

        FarmStorageDragItem dragItem = row.GetComponent<FarmStorageDragItem>();
        if (dragItem == null)
            dragItem = row.AddComponent<FarmStorageDragItem>();
        dragItem.Configure(item, quantity, origin, icon);
    }

    private static void ClearRows(List<GameObject> rows, Transform parent)
    {
        foreach (GameObject row in rows)
            if (row != null) Destroy(row);
        rows.Clear();
    }
}
