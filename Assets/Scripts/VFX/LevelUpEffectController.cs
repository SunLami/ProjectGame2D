using System.Collections;
using TMPro;
using UnityEngine;

public sealed class LevelUpEffectController : MonoBehaviour
{
    [SerializeField] private bool _subscribeToPlayerStat = true;
    [SerializeField, Min(1f)] private float _framesPerSecond = 16f;
    [SerializeField] private SpriteRenderer _backRenderer;
    [SerializeField] private SpriteRenderer _frontRenderer;
    [SerializeField] private SpriteMask _frontMask;
    [SerializeField] private Sprite[] _backFrames;
    [SerializeField] private Sprite[] _frontFrames;
    [SerializeField] private Sprite[] _maskFrames;
    [SerializeField, Min(1f)] private float _bellFramesPerSecond = 18f;
    [SerializeField] private SpriteRenderer _bellRenderer;
    [SerializeField] private Sprite[] _bellFrames;
    [SerializeField] private TMP_Text _levelUpText;
    [SerializeField, Min(0.1f)] private float _textRiseDuration = 0.9f;
    [SerializeField, Min(0f)] private float _textRiseDistance = 1.45f;
    private PlayerStat _subscribedPlayerStat;
    private Coroutine _playRoutine;
    private Vector3 _textStartPosition;
    private Vector3 _textStartScale;

    private void Awake()
    {
        if (_levelUpText != null)
        {
            _textStartPosition = _levelUpText.transform.localPosition;
            _textStartScale = _levelUpText.transform.localScale;
        }
        SetVisible(false);
    }

    private void Start()
    {
        if (!_subscribeToPlayerStat || PlayerStat.Instance == null) return;
        _subscribedPlayerStat = PlayerStat.Instance;
        _subscribedPlayerStat.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (_subscribedPlayerStat != null)
        {
            _subscribedPlayerStat.OnLevelUp -= HandleLevelUp;
            _subscribedPlayerStat = null;
        }
        if (_playRoutine != null) StopCoroutine(_playRoutine);
        _playRoutine = null;
        SetVisible(false);
    }

    private void HandleLevelUp(int newLevel) => Play();

    public void Play()
    {
        if (!HasValidFrames()) return;
        if (_playRoutine != null) StopCoroutine(_playRoutine);
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        SetVisible(true);
        float textStartTime = 11f / _bellFramesPerSecond;
        float totalDuration = textStartTime + _textRiseDuration;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            int groundFrame = Mathf.FloorToInt(elapsed * _framesPerSecond);
            if (groundFrame < _backFrames.Length)
            {
                _backRenderer.sprite = _backFrames[groundFrame];
                _frontRenderer.sprite = _frontFrames[groundFrame];
                _frontMask.sprite = _maskFrames[groundFrame];
            }
            else
            {
                _backRenderer.enabled = false;
                _frontRenderer.enabled = false;
                _frontMask.enabled = false;
            }

            int bellFrame = Mathf.FloorToInt(elapsed * _bellFramesPerSecond);
            if (bellFrame < _bellFrames.Length)
                _bellRenderer.sprite = _bellFrames[bellFrame];
            else
                _bellRenderer.enabled = false;

            if (elapsed >= textStartTime)
            {
                float progress = Mathf.Clamp01((elapsed - textStartTime) / _textRiseDuration);
                float riseProgress = 1f - (1f - progress) * (1f - progress);
                _levelUpText.enabled = true;
                _levelUpText.transform.localPosition = _textStartPosition + Vector3.up * (_textRiseDistance * riseProgress);
                _levelUpText.transform.localScale = _textStartScale * Mathf.Lerp(0.75f, 1.45f, progress);
                Color color = _levelUpText.color;
                color.a = progress < 0.85f ? 1f : 1f - Mathf.InverseLerp(0.85f, 1f, progress);
                _levelUpText.color = color;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        SetVisible(false);
        _playRoutine = null;
    }

    private bool HasValidFrames() =>
        _backRenderer != null && _frontRenderer != null && _frontMask != null &&
        _backFrames != null && _backFrames.Length > 0 &&
        _frontFrames != null && _frontFrames.Length == _backFrames.Length &&
        _maskFrames != null && _maskFrames.Length == _backFrames.Length &&
        _bellRenderer != null && _bellFrames != null && _bellFrames.Length >= 12 &&
        _levelUpText != null;

    private void SetVisible(bool visible)
    {
        if (_backRenderer != null) _backRenderer.enabled = visible;
        if (_frontRenderer != null) _frontRenderer.enabled = visible;
        if (_frontMask != null) _frontMask.enabled = visible;
        if (_bellRenderer != null) _bellRenderer.enabled = visible;
        if (_levelUpText != null)
        {
            _levelUpText.enabled = false;
            _levelUpText.transform.localPosition = _textStartPosition;
            _levelUpText.transform.localScale = _textStartScale;
            Color color = _levelUpText.color;
            color.a = 1f;
            _levelUpText.color = color;
        }
    }
}
