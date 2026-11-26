using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows a discard confirmation over Inventory, with an editable quantity for stacks.</summary>
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
    private ItemSO _item;
    private FishInstanceData _fish;
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

    public bool Open(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty || slot.quantity < 1 || IsOpen)
            return false;

        bool chooseQuantity = slot.item.isStackable && slot.quantity > 1;
        if (chooseQuantity && _quantityInput == null)
            return false;

        _slot = slot;
        _item = slot.item;
        _fish = slot.fish;
        _maxQuantity = slot.quantity;

        Image icon = chooseQuantity ? _quantityIcon : _confirmIcon;
        icon.sprite = _item.icon;
        icon.enabled = icon.sprite != null;

        if (chooseQuantity)
        {
            _quantityItemNameText.text = $"Discard {_item.itemName} (1-{_maxQuantity})";
            _quantityInput.SetTextWithoutNotify("1");
        }
        else
        {
            _confirmMessageText.text = $"Discard {_item.itemName}?";
        }

        _confirmPage.SetActive(!chooseQuantity);
        _quantityPage.SetActive(chooseQuantity);
        _root.SetActive(true);
        if (chooseQuantity)
            FocusQuantityInput();
        return true;
    }

    private void HandleConfirmYes() => DoDiscard(_maxQuantity);

    private void HandleQuantityConfirm()
    {
        if (!int.TryParse(_quantityInput.text, out int amount) || amount < 1 || amount > _maxQuantity)
        {
            _quantityInput.text = Mathf.Clamp(amount, 1, _maxQuantity).ToString();
            FocusQuantityInput();
            return;
        }

        DoDiscard(amount);
    }

    private void FocusQuantityInput()
    {
        _quantityInput.Select();
        _quantityInput.ActivateInputField();
    }

    private void DoDiscard(int amount)
    {
        if (_slot != null && _slot.item == _item && ReferenceEquals(_slot.fish, _fish)
            && amount <= _slot.quantity)
            InventoryManager.Instance?.DiscardFromSlot(_slot, amount);
        Close();
    }

    private void Close()
    {
        _root.SetActive(false);
        _slot = null;
        _item = null;
        _fish = null;
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
        Image background = inputObject.GetComponent<Image>() ?? inputObject.AddComponent<Image>();
        background.color = new Color(0.07f, 0.055f, 0.04f, 0.92f);
        _quantityInput.targetGraphic = background;
        _quantityValueText.raycastTarget = false;
        _quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        _quantityInput.lineType = TMP_InputField.LineType.SingleLine;
        _quantityInput.characterLimit = 6;

        if (_quantityMinusButton != null)
            _quantityMinusButton.gameObject.SetActive(false);
        if (_quantityPlusButton != null)
            _quantityPlusButton.gameObject.SetActive(false);
    }
}
