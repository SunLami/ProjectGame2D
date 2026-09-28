using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CommerceSellDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private ShopCraftingUI _commerceUI;

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<InventorySlotUI>()
            : null;
        if (source?.Slot == null || source.Slot.IsEmpty) return;
        (_commerceUI != null ? _commerceUI : GetComponentInParent<ShopCraftingUI>())?.StageSell(source.Slot);
    }
}
