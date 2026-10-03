using UnityEngine;

/// <summary>Minimal sprite-frame player for effects that are spawned from code (D-078): loops or plays
/// once over a set of sprites on the same GameObject's SpriteRenderer. Uses scaled time so hit-stop
/// and slow motion also slow the effect animations.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillFrameAnimator : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Sprite[] _frames;
    private float _frameRate = 12f;
    private bool _loop = true;
    private float _timer;
    private int _index;

    /// <summary>Starts playing `frames` at `frameRate`. When `loop` is false it holds the last frame.</summary>
    public void Play(Sprite[] frames, float frameRate, bool loop = true, float startOffset = 0f)
    {
        _renderer = GetComponent<SpriteRenderer>();
        _frames = frames;
        _frameRate = Mathf.Max(0.01f, frameRate);
        _loop = loop;
        _timer = startOffset;
        _index = 0;
        Apply();
    }

    /// <summary>Normalised playback progress for non-looping animations (0..1).</summary>
    public float Progress => _frames is { Length: > 0 }
        ? Mathf.Clamp01(_timer * _frameRate / _frames.Length)
        : 1f;

    private void Update()
    {
        if (_frames is not { Length: > 1 })
            return;

        _timer += Time.deltaTime;
        int frame = Mathf.FloorToInt(_timer * _frameRate);
        _index = _loop ? frame % _frames.Length : Mathf.Min(frame, _frames.Length - 1);
        Apply();
    }

    private void Apply()
    {
        if (_renderer != null && _frames is { Length: > 0 })
            _renderer.sprite = _frames[Mathf.Clamp(_index, 0, _frames.Length - 1)];
    }
}
