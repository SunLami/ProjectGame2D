using UnityEngine;
using UnityEngine.UI;

/// <summary>Arrow on the screen edge that points at the boss while it is outside the camera view (D-088).
/// Built in code; destroys itself with the boss.</summary>
public sealed class BossOffscreenIndicator : MonoBehaviour
{
    private BossController _boss;
    private RectTransform _arrow;
    private Image _image;
    private Camera _camera;

    private static Sprite _triangle;

    public static BossOffscreenIndicator Create(BossController boss)
    {
        var canvasObject = new GameObject("BossOffscreenIndicator");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 39;
        var indicator = canvasObject.AddComponent<BossOffscreenIndicator>();
        indicator.Build(boss);
        return indicator;
    }

    private static Sprite Triangle()
    {
        if (_triangle != null)
            return _triangle;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // right-pointing triangle: apex at the right edge
                float half = (size - x) * 0.5f;
                bool inside = Mathf.Abs(y - size * 0.5f) <= half * 0.9f;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        _triangle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _triangle;
    }

    private void Build(BossController boss)
    {
        _boss = boss;
        var go = new GameObject("Arrow", typeof(RectTransform));
        _arrow = go.GetComponent<RectTransform>();
        _arrow.SetParent(transform, false);
        _arrow.sizeDelta = new Vector2(56f, 56f);
        _image = go.AddComponent<Image>();
        _image.sprite = Triangle();
        _image.color = new Color(0.95f, 0.35f, 0.2f, 0.9f);
        _image.raycastTarget = false;
        _image.enabled = false;
    }

    private void LateUpdate()
    {
        if (_boss == null || _boss.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        Vector3 viewport = _camera.WorldToViewportPoint(_boss.transform.position);
        bool visible = viewport.z > 0f && viewport.x > 0.04f && viewport.x < 0.96f && viewport.y > 0.04f && viewport.y < 0.96f;
        _image.enabled = !visible;
        if (visible)
            return;

        Vector2 fromCenter = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
        if (fromCenter.sqrMagnitude < 0.0001f)
            fromCenter = Vector2.up;

        // push the arrow to the screen edge along the direction to the boss
        float scale = 0.46f / Mathf.Max(Mathf.Abs(fromCenter.x), Mathf.Abs(fromCenter.y));
        Vector2 edge = fromCenter * scale + new Vector2(0.5f, 0.5f);
        _arrow.anchorMin = _arrow.anchorMax = edge;
        _arrow.anchoredPosition = Vector2.zero;
        _arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg);
    }
}
