using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuBackgroundMotion : MonoBehaviour
{
    [SerializeField] private RectTransform _background;
    [SerializeField, Min(0f)] private float _overscan = 0.06f;
    [SerializeField, Min(0f)] private float _pointerTravel = 18f;
    [SerializeField, Min(0f)] private float _idleTravel = 4f;
    [SerializeField, Min(0.01f)] private float _smoothTime = 0.18f;

    private RectTransform _viewport;
    private Image _image;
    private Vector2 _velocity;

    private void Awake()
    {
        if (_background == null)
        {
            _background = transform as RectTransform;
        }

        _viewport = _background != null ? _background.parent as RectTransform : null;
        _image = _background != null ? _background.GetComponent<Image>() : null;
        ResizeToCover();
    }

    private void OnEnable()
    {
        ResizeToCover();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
        {
            ResizeToCover();
        }
    }

    private void Update()
    {
        if (_background == null)
        {
            return;
        }

        float time = Time.unscaledTime;
        Vector2 target = new Vector2(Mathf.Sin(time * 0.23f), Mathf.Cos(time * 0.19f)) * _idleTravel;

        Pointer pointer = Pointer.current;
        if (pointer != null && Screen.width > 0 && Screen.height > 0)
        {
            Vector2 screenPosition = pointer.position.ReadValue();
            Vector2 normalized = new Vector2(
                Mathf.Clamp(screenPosition.x / Screen.width, 0f, 1f) * 2f - 1f,
                Mathf.Clamp(screenPosition.y / Screen.height, 0f, 1f) * 2f - 1f);
            target += new Vector2(-normalized.x, -normalized.y * 0.6f) * _pointerTravel;
        }

        Vector2 position = Vector2.SmoothDamp(
            _background.anchoredPosition,
            target,
            ref _velocity,
            _smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
        _background.anchoredPosition = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
    }

    private void ResizeToCover()
    {
        if (_background == null || _viewport == null || _image == null || _image.sprite == null)
        {
            return;
        }

        Vector2 viewportSize = _viewport.rect.size;
        Vector2 sourceSize = _image.sprite.rect.size;
        if (viewportSize.x <= 0f || viewportSize.y <= 0f || sourceSize.x <= 0f || sourceSize.y <= 0f)
        {
            return;
        }

        float coverScale = Mathf.Max(viewportSize.x / sourceSize.x, viewportSize.y / sourceSize.y);
        _background.sizeDelta = sourceSize * coverScale * (1f + _overscan);
    }
}
