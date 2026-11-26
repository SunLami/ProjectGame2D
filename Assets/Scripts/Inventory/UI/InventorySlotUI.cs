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
    private Image _quantityBackground;
    private TMP_Text _levelBadge;
    private PlayerStat _observedPlayer;
    private bool _requiresHigherLevel;
    private bool _showItem;
    private bool _dropWasHandled;

    public ItemSO Item => _slot?.item;
    public InventorySlot Slot => _slot;

    private void OnEnable() => RefreshLevelRequirement();

    private void OnDisable()
    {
        ObservePlayer(null);
        InventoryItemTooltipUI.Instance?.Hide();
    }

    private InventorySlot _gridDropTarget;

    public void SetSlot(InventorySlot slot, bool hideAssignedItem = false)
    {
        _gridDropTarget = slot;
        _slot = hideAssignedItem ? null : slot;

        if (_slot == null || _slot.IsEmpty)
        {
            Clear();
            return;
        }

        _showItem = true;
        _iconImage.sprite = slot.item.icon;
        _iconImage.enabled = slot.item.icon != null;
        SetQuantity(slot.quantity);
        RefreshLevelRequirement();
    }

    public void Clear()
    {
        _showItem = false;
        _iconImage.sprite = null;
        _iconImage.enabled = false;
        SetQuantity(0);
        RefreshLevelRequirement();
    }

    private void ObservePlayer(PlayerStat player)
    {
        if (ReferenceEquals(_observedPlayer, player)) return;
        if (_observedPlayer != null) _observedPlayer.OnStatsChanged -= RefreshLevelRequirement;
        _observedPlayer = player;
        if (_observedPlayer != null) _observedPlayer.OnStatsChanged += RefreshLevelRequirement;
    }

    private void RefreshLevelRequirement()
    {
        var equipment = _showItem ? Item as EquipmentItemSO : null;
        ObservePlayer(isActiveAndEnabled && equipment != null ? PlayerStat.Instance : null);
        _requiresHigherLevel = equipment != null && equipment.RequiresHigherLevel(PlayerStat.Instance);
        UpdateIconTint();

        if (_requiresHigherLevel)
        {
            if (_levelBadge == null) _levelBadge = CreateLevelBadge();
            _levelBadge.text = $"LV {equipment.requiredLevel}";
        }
        if (_levelBadge != null) _levelBadge.transform.parent.gameObject.SetActive(_requiresHigherLevel);
    }

    private TMP_Text CreateLevelBadge()
    {
        var badge = new GameObject("LevelRequirementBadge", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)badge.transform;
        rect.SetParent(_iconImage.transform.parent, false);
        rect.anchorMin = new Vector2(0.17f, 0.65f);
        rect.anchorMax = new Vector2(0.83f, 0.85f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var background = badge.GetComponent<Image>();
        background.color = new Color32(100, 34, 32, 245);
        background.raycastTarget = false;

        var label = new GameObject("Level", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.font = _quantityText.font;
        label.fontSize = 32f;
        label.fontSizeMax = 32f;
        label.fontSizeMin = 2f;
        label.enableAutoSizing = true;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = new Color32(255, 224, 216, 255);
        label.raycastTarget = false;
        return label;
    }

    private void UpdateIconTint()
    {
        if (_iconImage == null) return;
        Color tint = _requiresHigherLevel ? new Color(0.72f, 0.61f, 0.61f, 1f) : Color.white;
        if (_dragIcon != null) tint.a = 0.4f;
        _iconImage.color = tint;
    }

    private void SetQuantity(int quantity)
    {
        bool showCount = quantity > 1;
        _quantityText.text = showCount ? quantity.ToString() : string.Empty;

        if (showCount && _quantityBackground == null)
        {
            var badge = new GameObject("QuantityBackground", typeof(RectTransform), typeof(Image));
            var text = _quantityText.rectTransform;
            var rect = (RectTransform)badge.transform;
            rect.SetParent(text.parent, false);
            rect.anchorMin = rect.anchorMax = text.anchorMax;
            rect.pivot = text.pivot;
            rect.anchoredPosition = text.anchoredPosition;
            rect.sizeDelta = text.sizeDelta;
            rect.SetSiblingIndex(text.GetSiblingIndex());

            _quantityBackground = badge.GetComponent<Image>();
            _quantityBackground.color = new Color32(27, 19, 13, 255);
            _quantityBackground.raycastTarget = false;
        }

        if (_quantityBackground != null) _quantityBackground.enabled = showCount;
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

        if (eventData.pointerDrag.TryGetComponent(out QuickBarSlotUI quickBarSlot))
        {
            if (GetComponentInParent<InventoryUI>() != null)
                quickBarSlot.ReturnToInventory(_gridDropTarget);
            return;
        }

        if (_slot == null) return;
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
        RectTransform iconRect = _iconImage.rectTransform;
        Vector3 worldSize = iconRect.TransformVector(iconRect.rect.size);
        dragRect.sizeDelta = canvas.rootCanvas.transform.InverseTransformVector(worldSize);

        _dragIcon.GetComponent<CanvasGroup>().blocksRaycasts = false;
        _dragIcon.transform.position = eventData.position;

        UpdateIconTint();
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

        UpdateIconTint();

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
