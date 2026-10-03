using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Ground danger markers for boss attacks (D-086). Drawn entirely in code (no art generation):
/// a red zone with an outline whose fill grows until the hit lands, so the player can read where and
/// when the attack arrives. Uses scaled time so hit-stop/slow-mo slows it with the rest of the game.</summary>
public static class BossTelegraph
{
    public static readonly Color Red = new Color(0.92f, 0.26f, 0.16f, 1f);
    public static readonly Color Safe = new Color(0.4f, 1f, 0.55f, 1f);

    private const int SortingOrder = -95;

    private static Sprite _circle;
    private static Sprite _ring;
    private static Sprite _square;

    public static Sprite CircleSprite => _circle != null ? _circle : _circle = BuildCircle(128, 0);
    public static Sprite RingSprite => _ring != null ? _ring : _ring = BuildCircle(128, 4);
    public static Sprite SquareSprite => _square != null ? _square : _square = BuildSquare();

    private static Sprite BuildCircle(int size, int ringThickness)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                bool inside = distance <= radius - 0.5f && (ringThickness <= 0 || distance >= radius - ringThickness);
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite BuildSquare()
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        // Pivot at the left-centre so scaling X grows the bar away from its origin.
        return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4);
    }

    /// <summary>Round zone of `radius` around `center` that fills over `duration`.</summary>
    public static BossTelegraphVisual Circle(Vector2 center, float radius, float duration)
    {
        var root = new GameObject("BossTelegraphCircle");
        root.transform.position = center;
        float diameter = radius * 2f;

        SpriteRenderer back = AddSprite(root.transform, "Back", CircleSprite, new Color(Red.r, Red.g, Red.b, 0.16f), SortingOrder);
        back.transform.localScale = Vector3.one * diameter;
        SpriteRenderer outline = AddSprite(root.transform, "Outline", RingSprite, new Color(Red.r, Red.g, Red.b, 0.9f), SortingOrder + 2);
        outline.transform.localScale = Vector3.one * diameter;
        SpriteRenderer fill = AddSprite(root.transform, "Fill", CircleSprite, new Color(Red.r, Red.g, Red.b, 0.4f), SortingOrder + 1);
        fill.transform.localScale = Vector3.zero;

        var visual = root.AddComponent<BossTelegraphVisual>();
        visual.Begin(fill.transform, Vector3.one * diameter, true, duration);
        return visual;
    }

    /// <summary>Straight lane of `length` x `width` from `origin` along `direction` whose fill sweeps out
    /// from the origin over `duration`.</summary>
    public static BossTelegraphVisual Line(Vector2 origin, Vector2 direction, float length, float width, float duration)
    {
        var root = new GameObject("BossTelegraphLine");
        root.transform.position = origin;
        root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        SpriteRenderer back = AddSprite(root.transform, "Back", SquareSprite, new Color(Red.r, Red.g, Red.b, 0.16f), SortingOrder);
        back.transform.localScale = new Vector3(length / 1f, width / 1f, 1f);
        SpriteRenderer fill = AddSprite(root.transform, "Fill", SquareSprite, new Color(Red.r, Red.g, Red.b, 0.42f), SortingOrder + 1);
        fill.transform.localScale = new Vector3(0f, width, 1f);

        // thin edges so the lane width reads clearly
        for (int side = -1; side <= 1; side += 2)
        {
            SpriteRenderer edge = AddSprite(root.transform, "Edge", SquareSprite, new Color(Red.r, Red.g, Red.b, 0.85f), SortingOrder + 2);
            edge.transform.localPosition = new Vector3(0f, side * (width * 0.5f - 0.04f), 0f);
            edge.transform.localScale = new Vector3(length, 0.08f, 1f);
        }

        var visual = root.AddComponent<BossTelegraphVisual>();
        visual.Begin(fill.transform, new Vector3(length, width, 1f), false, duration);
        return visual;
    }

    /// <summary>Safe-gap marker for the Core Resonance rings: a green ray from `origin` that shows where
    /// the ring will leave a gap.</summary>
    public static GameObject GapMarker(Vector2 origin, float angleDegrees, float halfGapDegrees, float length, float lifetime)
    {
        var root = new GameObject("BossGapMarker");
        root.transform.position = origin;
        var line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(Safe.r, Safe.g, Safe.b, 0.85f);
        line.endColor = new Color(Safe.r, Safe.g, Safe.b, 0.15f);
        line.sortingOrder = SortingOrder + 3;
        line.startWidth = 0.18f;
        line.endWidth = 0.1f;

        float left = (angleDegrees - halfGapDegrees) * Mathf.Deg2Rad;
        float right = (angleDegrees + halfGapDegrees) * Mathf.Deg2Rad;
        float mid = angleDegrees * Mathf.Deg2Rad;
        line.positionCount = 5;
        line.SetPosition(0, origin + new Vector2(Mathf.Cos(left), Mathf.Sin(left)) * length);
        line.SetPosition(1, origin);
        line.SetPosition(2, origin + new Vector2(Mathf.Cos(mid), Mathf.Sin(mid)) * length);
        line.SetPosition(3, origin);
        line.SetPosition(4, origin + new Vector2(Mathf.Cos(right), Mathf.Sin(right)) * length);

        UnityEngine.Object.Destroy(root, lifetime);
        return root;
    }

    private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }
}

public sealed class BossTelegraphVisual : MonoBehaviour
{
    private Transform _fill;
    private Vector3 _fullScale;
    private bool _uniform;
    private float _duration;
    private float _elapsed;
    private bool _following;
    private Func<Vector2> _followSource;

    public float Elapsed => _elapsed;
    public float Duration => _duration;

    public void Begin(Transform fill, Vector3 fullScale, bool uniform, float duration)
    {
        _fill = fill;
        _fullScale = fullScale;
        _uniform = uniform;
        _duration = Mathf.Max(0.05f, duration);
    }

    /// <summary>Make the marker follow `source` (world position) until <see cref="Lock"/> is called.</summary>
    public void Follow(Func<Vector2> source)
    {
        _followSource = source;
        _following = true;
    }

    public void Lock() => _following = false;

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_following && _followSource != null)
            transform.position = _followSource();

        float k = Mathf.Clamp01(_elapsed / _duration);
        if (_uniform)
            _fill.localScale = _fullScale * k;
        else
            _fill.localScale = new Vector3(_fullScale.x * k, _fullScale.y, 1f);

        if (_elapsed >= _duration + 0.08f)
            Destroy(gameObject);
    }

    /// <summary>Removes the marker immediately (e.g. when the attack is cancelled).</summary>
    public void Dismiss() => Destroy(gameObject);
}

/// <summary>Loads and spawns the Earth skill sprite sheets as short-lived effects for boss attacks.</summary>
public static class BossVfx
{
    private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

    /// <summary>Frames of Resources/VFX/Skills/Earth/&lt;name&gt; ordered by their numeric suffix.</summary>
    public static Sprite[] Frames(string name)
    {
        if (Cache.TryGetValue(name, out Sprite[] cached) && cached != null && cached.Length > 0 && cached[0] != null)
            return cached;

        // "Water/WaterOrb_Wobble" style names address another element folder (D-101); plain names stay Earth.
        string path = name.Contains("/") ? "VFX/Skills/" + name : "VFX/Skills/Earth/" + name;
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        Array.Sort(sprites, (a, b) => Suffix(a.name).CompareTo(Suffix(b.name)));
        Cache[name] = sprites;
        return sprites;
    }

    private static int Suffix(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(spriteName.Substring(underscore + 1), out int value) ? value : 0;
    }

    public static GameObject Spawn(string frameSetName, Vector2 position, float scale, float frameRate, bool loop,
        float lifetime, int sortingOrder = 6, string sortingLayer = "Default")
    {
        Sprite[] frames = Frames(frameSetName);
        if (frames == null || frames.Length == 0)
            return null;

        var go = new GameObject("Boss_" + frameSetName);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        go.AddComponent<SkillFrameAnimator>().Play(frames, frameRate, loop);
        if (lifetime > 0f)
            UnityEngine.Object.Destroy(go, lifetime);
        return go;
    }
}
