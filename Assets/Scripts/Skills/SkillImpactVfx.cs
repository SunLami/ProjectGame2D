using UnityEngine;

/// <summary>Generic one-shot impact VFX for the "Projectile định hướng" skill shape
/// (SkillVfxPipeline.md §7.1/§8.3): plays its frame sequence once then destroys itself. Spawned by
/// SkillProjectile at the hit point on impact.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillImpactVfx : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 16f;

    private float _frameTimer;
    private int _frameIndex;

    private void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        if (_frames is { Length: > 0 } && _renderer != null)
            _renderer.sprite = _frames[0];
    }

    private void Update()
    {
        if (_frames is not { Length: > 0 })
        {
            Destroy(gameObject);
            return;
        }

        _frameTimer += Time.deltaTime;
        float frameDuration = 1f / _frameRate;
        if (_frameTimer < frameDuration)
            return;

        _frameTimer -= frameDuration;
        _frameIndex++;
        if (_frameIndex >= _frames.Length)
        {
            Destroy(gameObject);
            return;
        }

        if (_renderer != null)
            _renderer.sprite = _frames[_frameIndex];
    }
}
