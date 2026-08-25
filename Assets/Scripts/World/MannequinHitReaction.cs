using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class MannequinHitReaction : MonoBehaviour
{
    private static readonly int HitHash = Animator.StringToHash("Hit");

    [Header("Hit Feedback")]
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField, Min(0.01f)] private float _flashDuration = 0.12f;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Coroutine _flashRoutine;
    private Color _originalColor;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalColor = _spriteRenderer.color;
    }

    public void PlayHitFeedback()
    {
        // Training mannequin has no health, death, movement or knockback logic.
        _animator.ResetTrigger(HitHash);
        _animator.SetTrigger(HitHash);

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRed());
    }

    private IEnumerator FlashRed()
    {
        _spriteRenderer.color = _flashColor;
        yield return new WaitForSeconds(_flashDuration);
        _spriteRenderer.color = _originalColor;
        _flashRoutine = null;
    }

    private void OnDisable()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }

        if (_spriteRenderer != null)
            _spriteRenderer.color = _originalColor;
    }

    private void OnValidate() => _flashDuration = Mathf.Max(0.01f, _flashDuration);
}
