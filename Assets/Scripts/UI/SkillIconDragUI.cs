using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SkillIconDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private SkillPanelUI _owner;
    private string _id;
    private int _slot = -1;
    public void Bind(SkillPanelUI owner, string id, int slot) { _owner=owner; _id=id; _slot=slot; }
    public void OnBeginDrag(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left)
            _owner?.BeginSkillDrag(_slot < 0 ? _id : _owner.EquippedId(_slot), data.position);
    }
    public void OnDrag(PointerEventData data) => _owner?.MoveSkillDrag(data.position);
    public void OnEndDrag(PointerEventData data) => _owner?.CancelDrag();
    private void OnDisable() => _owner?.CancelDrag();
}
