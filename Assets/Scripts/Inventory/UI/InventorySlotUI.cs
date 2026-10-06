using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _quantityText;

    private InventorySlot _slot;
    private GameObject _dragIcon;
    private bool _dropWasHandled;

    public ItemSO Item => _slot?.item;
    public InventorySlot Slot => _slot;

    private void OnDisable() => InventoryItemTooltipUI.Instance?.Hide();

    public void SetSlot(InventorySlot slot)
    {
        _slot = slot;

        if (slot == null || slot.IsEmpty)
        {
            Clear();
            return;
        }

        _iconImage.sprite = slot.item.icon;
        _iconImage.enabled = true;
        _quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : string.Empty;
    }

    public void Clear()
    {
        _iconImage.sprite = null;
        _iconImage.enabled = false;
        _quantityText.text = string.Empty;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount != 2) return;
        if (_slot == null || _slot.IsEmpty) return;

        if (_slot.item is EquipmentItemSO equipmentItem && EquipmentManager.Instance != null)
        {
            if (!EquipmentManager.Instance.Equip(equipmentItem, _slot))
                InventoryActionFeedbackUI.Show(EquipFeedback.BuildEquipFailureMessage(equipmentItem));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_slot == null || _slot.IsEmpty) return;
        Vector2 pointerPosition = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
        InventoryItemTooltipUI.Instance?.Show(_slot.item, transform as RectTransform, pointerPosition);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_slot == null || _slot.IsEmpty || eventData == null) return;
        InventoryItemTooltipUI.Instance?.MoveToPointer(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData) => InventoryItemTooltipUI.Instance?.Hide();

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        InventorySlotUI sourceSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (sourceSlot != null)
        {
            if (sourceSlot == this) return;
            InventoryManager.Instance.SwapItems(sourceSlot.Slot, _slot);
            return;
        }

        EquipmentSlotUI sourceEquipSlot = eventData.pointerDrag.GetComponent<EquipmentSlotUI>();
        if (sourceEquipSlot != null)
        {
            EquipmentItemSO equipped = EquipmentManager.Instance.GetEquipped(sourceEquipSlot.Slot);
            if (equipped != null && !EquipmentManager.Instance.Unequip(sourceEquipSlot.Slot, _slot))
                InventoryActionFeedbackUI.Show(EquipFeedback.BuildUnequipFailureMessage(equipped));
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_slot == null || _slot.IsEmpty) return;

        _dropWasHandled = false;

        InventoryItemTooltipUI.Instance?.Hide();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        _dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        _dragIcon.transform.SetParent(canvas.rootCanvas.transform, false);
        _dragIcon.transform.SetAsLastSibling();

        Image dragImage = _dragIcon.GetComponent<Image>();
        dragImage.sprite = _iconImage.sprite;
        dragImage.raycastTarget = false;
        dragImage.preserveAspect = true;

        RectTransform dragRect = _dragIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = ((RectTransform)_iconImage.transform).rect.size;

        _dragIcon.GetComponent<CanvasGroup>().blocksRaycasts = false;
        _dragIcon.transform.position = eventData.position;

        _iconImage.color = new Color(1f, 1f, 1f, 0.4f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon != null)
        {
            _dragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIcon != null)
        {
            Destroy(_dragIcon);
            _dragIcon = null;
        }

        _iconImage.color = Color.white;

        // Drag-to-discard: released past the Inventory window's own bounds (out into the world),
        // not just onto empty space between slots inside the window -- that still just snaps back.
        if (!_dropWasHandled && !IsDroppedOnQuickBar(eventData)
            && _slot != null && !_slot.IsEmpty && IsDroppedOutsideInventoryWindow(eventData))
            InventoryDiscardConfirmUI.Instance?.Open(_slot);

        _dropWasHandled = false;
    }

    /// <summary>Called by valid drop targets outside the Inventory window so EndDrag does not
    /// reinterpret the same release as a discard gesture.</summary>
    public void MarkDropHandled() => _dropWasHandled = true;

    private static bool IsDroppedOnQuickBar(PointerEventData eventData)
    {
        if (eventData == null || EventSystem.current == null)
            return false;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponentInParent<QuickBarSlotUI>() != null)
                return true;
        }

        return false;
    }

    private bool IsDroppedOutsideInventoryWindow(PointerEventData eventData)
    {
        InventoryWindowUI window = GetComponentInParent<InventoryWindowUI>();
        RectTransform windowRect = window != null ? window.WindowRect : null;
        if (windowRect == null)
            return false;

        Camera eventCamera = eventData.pressEventCamera;
        return !RectTransformUtility.RectangleContainsScreenPoint(windowRect, eventData.position, eventCamera);
    }
}
