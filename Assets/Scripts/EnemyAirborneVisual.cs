using UnityEngine;

/// <summary>Visual side of `IAirborne` (D-080). Enemy sprites live on the same GameObject as their collider
/// and Rigidbody2D, so the transform cannot be lifted without moving the hitbox. Instead, while the
/// target is airborne this component hides the real SpriteRenderer(s), draws raised clones that copy the
/// animated sprite every frame, and leaves a flat shadow at the feet. The real renderer keeps being
/// updated by the Animator while hidden, so the clone always shows the correct frame.
///
/// Lift profile over the duration: rise (first 25%), hang (middle 50%), fall (last 25%).</summary>
public class EnemyAirborneVisual : MonoBehaviour
{
    private SpriteRenderer[] _originals;
    private SpriteRenderer[] _clones;
    private SpriteRenderer _shadow;
    private IDamageable _damageable;
    private float _duration;
    private float _height;
    private float _elapsed;
    private bool _active;
    private static Sprite _shadowSprite;

    /// <summary>Starts (or extends) the airborne visuals.</summary>
    public void Begin(float duration, float height)
    {
        if (_active)
        {
            // Extend: keep the clones, push the end time and take the larger height.
            _duration = Mathf.Max(_duration, _elapsed + duration);
            _height = Mathf.Max(_height, height);
            return;
        }

        _duration = Mathf.Max(0.1f, duration);
        _height = height;
        _elapsed = 0f;
        _damageable = GetComponent<IDamageable>();
        if (_damageable == null)
        {
            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour is IDamageable damageable)
                {
                    _damageable = damageable;
                    break;
                }
            }
        }

        _originals = GetComponentsInChildren<SpriteRenderer>();
        var clones = new SpriteRenderer[_originals.Length];
        for (int i = 0; i < _originals.Length; i++)
        {
            SpriteRenderer source = _originals[i];
            var go = new GameObject("AirborneClone");
            go.transform.SetParent(source.transform, false);
            SpriteRenderer clone = go.AddComponent<SpriteRenderer>();
            clone.sharedMaterial = source.sharedMaterial;
            clone.sortingLayerID = source.sortingLayerID;
            clone.sortingOrder = source.sortingOrder + 1;
            clones[i] = clone;
            source.enabled = false;
        }

        _clones = clones;
        CreateShadow();
        _active = true;
        Sync(0f);
    }

    private void Update()
    {
        if (!_active)
            return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= _duration || (_damageable != null && _damageable.IsDead))
        {
            End();
            return;
        }

        Sync(LiftAt(_elapsed / _duration));
    }

    private void LateUpdate()
    {
        // Copy again after the Animator has run this frame so the clone never lags a frame behind.
        if (_active)
            Sync(LiftAt(Mathf.Clamp01(_elapsed / _duration)));
    }

    private static float LiftAt(float t)
    {
        if (t < 0.25f)
            return Mathf.SmoothStep(0f, 1f, t / 0.25f);
        if (t < 0.75f)
            return 1f;
        return Mathf.SmoothStep(1f, 0f, (t - 0.75f) / 0.25f);
    }

    private void Sync(float lift)
    {
        for (int i = 0; i < _clones.Length; i++)
        {
            SpriteRenderer source = _originals[i];
            SpriteRenderer clone = _clones[i];
            if (source == null || clone == null)
                continue;

            clone.sprite = source.sprite;
            clone.flipX = source.flipX;
            clone.flipY = source.flipY;
            clone.color = source.color;
            // The clone is a child of the source, so a local offset divided by lossy scale lifts it in world units.
            float scale = Mathf.Max(0.0001f, source.transform.lossyScale.y);
            clone.transform.localPosition = new Vector3(0f, _height * lift / scale, 0f);
        }

        if (_shadow != null)
        {
            // The shadow shrinks and fades as the target rises.
            float size = Mathf.Lerp(0.9f, 0.55f, lift);
            Bounds bounds = SourceBounds();
            _shadow.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.05f, 0f);
            _shadow.transform.localScale = new Vector3(size * Mathf.Max(0.6f, bounds.size.x), size * Mathf.Max(0.6f, bounds.size.x) * 0.4f, 1f);
            _shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.4f, 0.25f, lift));
        }
    }

    private Bounds SourceBounds()
    {
        Collider2D collider = GetComponentInChildren<Collider2D>();
        if (collider != null)
            return collider.bounds;

        return new Bounds(transform.position, Vector3.one * 0.6f);
    }

    private void CreateShadow()
    {
        var go = new GameObject("AirborneShadow");
        _shadow = go.AddComponent<SpriteRenderer>();
        _shadow.sprite = GetShadowSprite();
        SpriteRenderer reference = _originals.Length > 0 ? _originals[0] : null;
        _shadow.sortingLayerID = reference != null ? reference.sortingLayerID : 0;
        _shadow.sortingOrder = (reference != null ? reference.sortingOrder : 0) - 1;
    }

    private void End()
    {
        _active = false;

        for (int i = 0; i < _clones.Length; i++)
        {
            if (_clones[i] != null)
                Destroy(_clones[i].gameObject);
            if (_originals[i] != null)
                _originals[i].enabled = true;
        }

        if (_shadow != null)
            Destroy(_shadow.gameObject);
    }

    private void OnDisable()
    {
        if (_active)
            End();
    }

    private static Sprite GetShadowSprite()
    {
        if (_shadowSprite != null)
            return _shadowSprite;

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, d < 0.6f ? 1f : d < 1f ? 0.5f : 0f));
            }
        }

        texture.Apply();
        _shadowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _shadowSprite;
    }
}
