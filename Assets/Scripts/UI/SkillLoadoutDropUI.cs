using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SkillLoadoutDropUI : MonoBehaviour, IDropHandler
{
    private SkillPanelUI _owner;
    private int _slot;
    public void Bind(SkillPanelUI owner, int slot) { _owner=owner; _slot=slot; }
    public void OnDrop(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left) _owner?.DropSkill(_slot);
    }
}
