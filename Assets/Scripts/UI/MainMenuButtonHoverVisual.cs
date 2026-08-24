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

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        _normalSprite = _image.sprite;
    }

    private void OnDisable()
    {
        RestoreNormalSprite();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button.IsInteractable() && _hoverSprite != null)
        {
            _image.sprite = _hoverSprite;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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
