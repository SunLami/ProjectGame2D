using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button), typeof(Image))]
public sealed class MainMenuButtonHoverVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Sprite _hoverSprite;

    private Button _button;
    private Image _image;
    private Sprite _normalSprite;
    private bool _pointerInside;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        _normalSprite = _image.sprite;
    }

    private void OnDisable()
    {
        _pointerInside = false;
        RestoreNormalSprite();
    }

    private void LateUpdate()
    {
        if (_pointerInside && _button.IsInteractable() && _hoverSprite != null)
        {
            _image.sprite = _hoverSprite;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerInside = true;
        if (_button.IsInteractable() && _hoverSprite != null)
        {
            _image.sprite = _hoverSprite;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerInside = false;
        RestoreNormalSprite();
    }

    private void RestoreNormalSprite()
    {
        if (_image != null && _normalSprite != null)
        {
            _image.sprite = _normalSprite;
        }
    }
}
