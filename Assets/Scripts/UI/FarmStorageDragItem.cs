using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum FarmStorageGridSide
{
    Inventory,
    Storage
}

public sealed class FarmStorageDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ItemSO _item;
    private int _quantity;
    private FarmStorageGridSide _origin;
    private Image _icon;
    private GameObject _dragIcon;

    public ItemSO Item => _item;
    public int Quantity => _quantity;
    public FarmStorageGridSide Origin => _origin;

    public void Configure(ItemSO item, int quantity, FarmStorageGridSide origin, Image icon)
    {
        _item = item;
        _quantity = quantity;
        _origin = origin;
        _icon = icon;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_item == null || _quantity <= 0 || _icon == null || _icon.sprite == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        _dragIcon = new GameObject("FarmStorageDragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        _dragIcon.transform.SetParent(canvas.rootCanvas.transform, false);
        _dragIcon.transform.SetAsLastSibling();

        Image dragImage = _dragIcon.GetComponent<Image>();
        dragImage.sprite = _icon.sprite;
        dragImage.preserveAspect = true;
        dragImage.raycastTarget = false;

        RectTransform dragRect = _dragIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = ((RectTransform)_icon.transform).rect.size;
        _dragIcon.GetComponent<CanvasGroup>().blocksRaycasts = false;
        _dragIcon.transform.position = eventData.position;
        _icon.color = new Color(1f, 1f, 1f, 0.4f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon != null)
            _dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIcon != null)
            Destroy(_dragIcon);
        _dragIcon = null;
        if (_icon != null)
            _icon.color = Color.white;
    }
}
