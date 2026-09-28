using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class QuickBarSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField, Range(0, QuickBarSaveData.SlotCount - 1)] private int _slotIndex;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _quantity;
    [SerializeField] private Image _selection;
    private bool _subscribed;
    private bool _isDragging;
    private RectTransform _dragGhost;
    private RectTransform _dragCanvasRect;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Update()
    {
        if (!_subscribed) Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        CleanupDragGhost();
        _isDragging = false;
    }

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
        QuickBarSlotUI quickBarSource = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<QuickBarSlotUI>()
            : null;
        if (quickBarSource != null && quickBarSource != this &&
            quickBarSource.TryGetAssignedItem(out ItemSO quickBarItem) && QuickBarManager.Instance != null)
        {
            QuickBarManager.Instance.Assign(_slotIndex, quickBarItem);
            QuickBarManager.Instance.Clear(quickBarSource._slotIndex);
            QuickBarManager.Instance.Select(_slotIndex);
            return;
        }

        InventorySlotUI source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<InventorySlotUI>()
            : null;
        if (source?.Item != null && QuickBarManager.Instance != null)
        {
            QuickBarManager.Instance.Assign(_slotIndex, source.Item);
            QuickBarManager.Instance.Select(_slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TryGetAssignedItem(out ItemSO item)) return;

        _isDragging = true;
        Canvas canvas = GetComponentInParent<Canvas>();
        _dragCanvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (_dragCanvasRect == null || item.icon == null) return;

        GameObject ghost = new("QuickBarDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _dragGhost = ghost.GetComponent<RectTransform>();
        _dragGhost.SetParent(_dragCanvasRect, false);
        _dragGhost.SetAsLastSibling();
        _dragGhost.sizeDelta = _icon != null
            ? _icon.rectTransform.rect.size
            : new Vector2(31f, 31f);

        Image ghostImage = ghost.GetComponent<Image>();
        ghostImage.sprite = item.icon;
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;
        ghostImage.color = new Color(1f, 1f, 1f, 0.85f);
        UpdateDragGhost(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging) UpdateDragGhost(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool shouldClear = _isDragging && !IsPointerOverQuickBarSlot(eventData);
        CleanupDragGhost();
        _isDragging = false;

        if (shouldClear)
            QuickBarManager.Instance?.Clear(_slotIndex);
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

    private bool TryGetAssignedItem(out ItemSO item)
    {
        item = null;
        return QuickBarManager.Instance != null &&
               QuickBarManager.Instance.TryGetAssignedItem(_slotIndex, out item);
    }

    private void UpdateDragGhost(PointerEventData eventData)
    {
        if (_dragGhost == null || _dragCanvasRect == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragCanvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            _dragGhost.localPosition = localPoint;
    }

    private static bool IsPointerOverQuickBarSlot(PointerEventData eventData)
    {
        if (EventSystem.current == null) return false;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (RaycastResult result in results)
            if (result.gameObject.GetComponentInParent<QuickBarSlotUI>() != null)
                return true;
        return false;
    }

    private void CleanupDragGhost()
    {
        if (_dragGhost != null) Destroy(_dragGhost.gameObject);
        _dragGhost = null;
        _dragCanvasRect = null;
    }

    private void OnDestroy() => CleanupDragGhost();

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
