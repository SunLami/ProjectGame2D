using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _quantityText;

    private InventorySlot _slot;
    private GameObject _dragIcon;

    public ItemSO Item => _slot?.item;
    public InventorySlot Slot => _slot;

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
            EquipmentManager.Instance.Equip(equipmentItem, _slot);
        }
    }

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
            EquipmentManager.Instance.Unequip(sourceEquipSlot.Slot);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_slot == null || _slot.IsEmpty) return;

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
    }
}
