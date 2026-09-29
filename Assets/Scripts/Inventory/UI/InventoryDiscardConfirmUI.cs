using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Modal opened when an inventory item is dragged out past the Inventory window's bounds
/// (InventorySlotUI.OnEndDrag) -- asks "discard this?" and, if the stack has more than one unit,
/// a follow-up quantity input before actually calling InventoryManager.DiscardFromSlot. Sits as a
/// raycast-blocking overlay on top of the already-open Inventory window instead of pushing a new
/// GameState -- Inventory (GameplayMenuPage.Inventory) already blocks gameplay input/world time, so
/// there is nothing additional to pause.</summary>
public sealed class InventoryDiscardConfirmUI : MonoBehaviour
{
    public static InventoryDiscardConfirmUI Instance { get; private set; }

    [SerializeField] private GameObject _root;
    [SerializeField] private GameObject _confirmPage;
    [SerializeField] private GameObject _quantityPage;

    [SerializeField] private Image _confirmIcon;
    [SerializeField] private TMP_Text _confirmMessageText;
    [SerializeField] private Button _confirmYesButton;
    [SerializeField] private Button _confirmNoButton;

    [SerializeField] private Image _quantityIcon;
    [SerializeField] private TMP_Text _quantityItemNameText;
    [SerializeField] private TMP_InputField _quantityInput;
    [SerializeField, HideInInspector] private TMP_Text _quantityValueText;
    [SerializeField, HideInInspector] private Button _quantityMinusButton;
    [SerializeField, HideInInspector] private Button _quantityPlusButton;
    [SerializeField] private Button _quantityConfirmButton;
    [SerializeField] private Button _quantityCancelButton;

    private InventorySlot _slot;
    private int _maxQuantity;

    public bool IsOpen => _root != null && _root.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureQuantityInput();
        _root.SetActive(false);
    }

    private void OnEnable()
    {
        _confirmYesButton.onClick.AddListener(HandleConfirmYes);
        _confirmNoButton.onClick.AddListener(Close);
        _quantityConfirmButton.onClick.AddListener(HandleQuantityConfirm);
        _quantityCancelButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        _confirmYesButton.onClick.RemoveListener(HandleConfirmYes);
        _confirmNoButton.onClick.RemoveListener(Close);
        _quantityConfirmButton.onClick.RemoveListener(HandleQuantityConfirm);
        _quantityCancelButton.onClick.RemoveListener(Close);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Opens the "discard this?" confirm step for the given inventory slot. The slot
    /// reference is held live (not a snapshot), so InventoryManager.DiscardFromSlot always acts on
    /// whatever that slot currently holds when the player finally confirms.</summary>
    public bool Open(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty || IsOpen)
            return false;

        _slot = slot;
        _confirmIcon.sprite = slot.item.icon;
        _confirmIcon.enabled = slot.item.icon != null;
        _confirmMessageText.text = slot.quantity > 1
            ? $"Discard {slot.item.itemName} x{slot.quantity}?"
            : $"Discard {slot.item.itemName}?";

        _root.SetActive(true);
        _confirmPage.SetActive(true);
        _quantityPage.SetActive(false);
        return true;
    }

    private void HandleConfirmYes()
    {
        if (_slot == null || _slot.IsEmpty)
        {
            Close();
            return;
        }

        if (_slot.item.isStackable && _slot.quantity > 1)
        {
            _maxQuantity = _slot.quantity;
            _quantityIcon.sprite = _slot.item.icon;
            _quantityIcon.enabled = _slot.item.icon != null;
            _quantityItemNameText.text = _slot.item.itemName;
            _quantityInput.text = "1";

            _confirmPage.SetActive(false);
            _quantityPage.SetActive(true);
            _quantityInput.Select();
            _quantityInput.ActivateInputField();
            return;
        }

        DoDiscard(_slot.quantity);
    }

    private void HandleQuantityConfirm()
    {
        if (!int.TryParse(_quantityInput.text, out int amount) || amount < 1 || amount > _maxQuantity)
        {
            _quantityInput.text = Mathf.Clamp(amount, 1, _maxQuantity).ToString();
            _quantityInput.Select();
            _quantityInput.ActivateInputField();
            return;
        }

        DoDiscard(amount);
    }

    private void DoDiscard(int amount)
    {
        if (_slot != null)
            InventoryManager.Instance?.DiscardFromSlot(_slot, amount);
        Close();
    }

    private void Close()
    {
        _root.SetActive(false);
        _slot = null;
    }

    private void EnsureQuantityInput()
    {
        if (_quantityInput != null)
            return;

        // Backward-compatible migration for prefab instances authored with the old +/- stepper.
        // Reuses its center text as the editable text so existing scene instances remain valid.
        if (_quantityValueText == null)
            return;

        GameObject inputObject = _quantityValueText.transform.parent.gameObject;
        _quantityInput = inputObject.GetComponent<TMP_InputField>()
            ?? inputObject.AddComponent<TMP_InputField>();
        _quantityInput.textViewport = _quantityValueText.rectTransform;
        _quantityInput.textComponent = _quantityValueText;
        _quantityValueText.raycastTarget = true;
        _quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        _quantityInput.lineType = TMP_InputField.LineType.SingleLine;
        _quantityInput.characterLimit = 6;

        if (_quantityMinusButton != null)
            _quantityMinusButton.gameObject.SetActive(false);
        if (_quantityPlusButton != null)
            _quantityPlusButton.gameObject.SetActive(false);
    }
}
