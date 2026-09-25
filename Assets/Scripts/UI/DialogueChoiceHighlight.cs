using UnityEngine;
using UnityEngine.EventSystems;

public sealed class DialogueChoiceHighlight : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject _frame;
    private bool _hovered;
    private bool _selected;

    private void Awake()
    {
        if (_frame == null)
            _frame = transform.Find("HoverFrame")?.gameObject;
        Refresh();
    }

    private void OnEnable() => ResetState();

    private void OnDisable() => ResetState();

    public void ResetState()
    {
        _hovered = false;
        _selected = false;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        Refresh();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _selected = true;
        Refresh();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false;
        Refresh();
    }

    private void Refresh()
    {
        if (_frame != null)
            _frame.SetActive(_hovered || _selected);
    }
}
