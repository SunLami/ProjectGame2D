using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuCampfireAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform _background;
    [SerializeField] private RectTransform _fire;
    [SerializeField] private Image _fireImage;
    [SerializeField] private RectTransform _glow;
    [SerializeField] private Image _glowImage;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private Vector2 _sourceFrameSize = new Vector2(128f, 128f);
    [SerializeField, Min(0.01f)] private float _frameDuration = 0.14f;

    private int _frameIndex = -1;

    private void OnEnable()
    {
        UpdateResponsiveSize();
        SetFrame(0);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
        {
            UpdateResponsiveSize();
        }
    }

    private void Update()
    {
        if (_frames == null || _frames.Length == 0)
        {
            return;
        }

        int nextFrame = Mathf.FloorToInt(Time.unscaledTime / _frameDuration) % _frames.Length;
        SetFrame(nextFrame);

        float pulse = Mathf.Sin(Time.unscaledTime * 8.5f);
        if (_fire != null)
        {
            _fire.localScale = Vector3.one * (1f + pulse * 0.025f);
        }

        if (_glow != null)
        {
            _glow.localScale = Vector3.one * (1.18f + pulse * 0.06f);
        }

        if (_glowImage != null)
        {
            Color color = _glowImage.color;
            color.a = 0.16f + (pulse + 1f) * 0.035f;
            _glowImage.color = color;
        }
    }

    private void SetFrame(int index)
    {
        if (index == _frameIndex || _frames == null || index < 0 || index >= _frames.Length)
        {
            return;
        }

        _frameIndex = index;
        if (_fireImage != null)
        {
            _fireImage.sprite = _frames[index];
        }

        if (_glowImage != null)
        {
            _glowImage.sprite = _frames[index];
        }
    }

    private void UpdateResponsiveSize()
    {
        if (_background == null)
        {
            return;
        }

        Image backgroundImage = _background.GetComponent<Image>();
        if (backgroundImage == null || backgroundImage.sprite == null)
        {
            return;
        }

        float scale = _background.rect.width / backgroundImage.sprite.rect.width;
        Vector2 displaySize = _sourceFrameSize * scale;
        if (_fire != null)
        {
            _fire.sizeDelta = displaySize;
        }

        if (_glow != null)
        {
            _glow.sizeDelta = displaySize;
        }
    }
}
