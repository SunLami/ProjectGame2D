using UnityEngine;

/// <summary>Plays a looping sprite animation on this object's SpriteRenderer (decor motion: flames,
/// banners, vines, grass, rune glow). A random start phase keeps neighbouring copies from moving in
/// lock-step. Uses scaled time through <see cref="SkillFrameAnimator"/>.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class AmbientLoop : MonoBehaviour
{
    [SerializeField] private Sprite[] _frames;
    [SerializeField, Min(0.1f)] private float _frameRate = 8f;
    [SerializeField] private bool _randomPhase = true;

    private void Start()
    {
        if (_frames == null || _frames.Length == 0)
            return;

        var animator = GetComponent<SkillFrameAnimator>();
        if (animator == null)
            animator = gameObject.AddComponent<SkillFrameAnimator>();

        float offset = _randomPhase ? Random.value * _frames.Length / _frameRate : 0f;
        animator.Play(_frames, _frameRate, true, offset);
    }
}
