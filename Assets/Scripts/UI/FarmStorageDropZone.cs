using UnityEngine;
using UnityEngine.EventSystems;

public sealed class FarmStorageDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private FarmStorageGridSide _destination;

    public void Configure(FarmStorageGridSide destination) => _destination = destination;

    public void OnDrop(PointerEventData eventData)
    {
        FarmStorageDragItem source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FarmStorageDragItem>()
            : null;
        if (source == null || source.Item == null || source.Origin == _destination)
            return;

        GetComponentInParent<FarmStorageUI>()?.HandleDrop(
            source.Item,
            source.Quantity,
            source.Origin,
            _destination);
    }
}
