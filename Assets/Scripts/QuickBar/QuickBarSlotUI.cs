using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class QuickBarSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField, Range(0, QuickBarSaveData.SlotCount - 1)] private int _slotIndex;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _quantity;
    [SerializeField] private Image _selection;
    private bool _subscribed;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Update()
    {
        if (!_subscribed) Subscribe();
    }

    private void OnDisable() => Unsubscribe();

    public void Configure(int slotIndex, Image icon, TMP_Text quantity, Image selection)
    {
        _slotIndex = Mathf.Clamp(slotIndex, 0, QuickBarSaveData.SlotCount - 1);
        _icon = icon;
        _quantity = quantity;
        _selection = selection;
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            QuickBarManager.Instance?.Select(_slotIndex);
        else if (eventData.button == PointerEventData.InputButton.Right)
            QuickBarManager.Instance?.Clear(_slotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<InventorySlotUI>()
            : null;
        if (source?.Item != null && QuickBarManager.Instance != null)
        {
            QuickBarManager.Instance.Assign(_slotIndex, source.Item);
            QuickBarManager.Instance.Select(_slotIndex);
        }
    }

    private void Subscribe()
    {
        if (_subscribed || QuickBarManager.Instance == null || InventoryManager.Instance == null)
            return;
        QuickBarManager.Instance.Changed += Refresh;
        InventoryManager.Instance.OnInventoryChanged += Refresh;
        _subscribed = true;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        if (QuickBarManager.Instance != null) QuickBarManager.Instance.Changed -= Refresh;
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged -= Refresh;
        _subscribed = false;
    }

    private void Refresh()
    {
        QuickBarManager quickBar = QuickBarManager.Instance;
        ItemSO item = null;
        bool resolved = quickBar != null && quickBar.TryGetAssignedItem(_slotIndex, out item);
        int count = resolved && InventoryManager.Instance != null
            ? InventoryManager.Instance.GetTotalQuantity(item.itemId)
            : 0;

        if (_icon != null)
        {
            _icon.sprite = resolved ? item.icon : null;
            _icon.enabled = resolved && item.icon != null;
            _icon.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }
        if (_quantity != null)
            _quantity.text = resolved ? count.ToString() : string.Empty;
        if (_selection != null)
            _selection.enabled = quickBar != null && quickBar.SelectedIndex == _slotIndex;
    }
}
