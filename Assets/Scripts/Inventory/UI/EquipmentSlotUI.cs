using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private EquipSlot _slot;
    [SerializeField] private Image _iconImage;

    private GameObject _dragIcon;

    public EquipSlot Slot => _slot;

    private void OnEnable()
    {
        EquipmentManager.Instance.OnEquipmentChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        EquipmentItemSO item = EquipmentManager.Instance.GetEquipped(_slot);
        _iconImage.sprite = item != null ? item.icon : null;
        _iconImage.enabled = item != null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount != 2) return;
        EquipmentManager.Instance.Unequip(_slot);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        InventorySlotUI sourceSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (sourceSlot == null) return;

        if (sourceSlot.Item is EquipmentItemSO equipmentItem && equipmentItem.slot == _slot)
        {
            EquipmentManager.Instance.Equip(equipmentItem, sourceSlot.Slot);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (EquipmentManager.Instance.GetEquipped(_slot) == null) return;

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
