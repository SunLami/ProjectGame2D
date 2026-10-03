using System.Collections;
using UnityEngine;

/// <summary>Screen-level effects for ultimate skills (D-078): camera shake, screen dim and flash, hit-stop
/// and slow-motion. A lazily created singleton, so skills just call the static helpers. Everything
/// is drawn with plain sprites/transforms (no UI dependency): the dim/flash overlay is a camera-child
/// sprite stretched over the view, and shake is applied as a positional offset after the camera's
/// own follow logic has run (offset removed again at the start of the next frame).
///
/// Time effects use real (unscaled) time and always restore the previous time scale, even if the
/// object is destroyed mid-effect.</summary>
[DefaultExecutionOrder(10000)]
public class SkillScreenFX : MonoBehaviour
{
    private static SkillScreenFX _instance;

    private Camera _camera;
    private SpriteRenderer _overlay;
    private Vector3 _appliedShakeOffset;

    private float _shakeMagnitude;
    private float _shakeEndTime;
    private float _shakeDuration;

    private Color _dimColor = Color.black;
    private float _dimTarget;
    private float _dimCurrent;
    private float _dimSpeed = 1f;

    private float _flashAlpha;
    private Color _flashColor = Color.white;
    private float _flashFadeSpeed = 1f;

    private float _baseTimeScale = 1f;
    private bool _timeModified;

    public static SkillScreenFX Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SkillScreenFX");
                _instance = go.AddComponent<SkillScreenFX>();
            }

            return _instance;
        }
    }

    /// <summary>Camera shake that fades out linearly over `duration` seconds (real time).</summary>
    public static void Shake(float magnitude, float duration)
    {
        Instance.StartShake(magnitude, duration);
    }

    /// <summary>Fades a dark tint over the whole view to `alpha` (0 = clear). Call again with 0 to fade out.</summary>
    public static void Dim(float alpha, float fadeSeconds)
    {
        Instance.SetDim(alpha, fadeSeconds, Color.black);
    }

    /// <summary>Same as Dim but tinted (e.g. a deep blue for water skills).</summary>
    public static void Dim(float alpha, float fadeSeconds, Color tint)
    {
        Instance.SetDim(alpha, fadeSeconds, tint);
    }

    /// <summary>Full-screen colour flash that starts at `alpha` and fades out over `seconds`.</summary>
    public static void Flash(Color color, float alpha, float seconds)
    {
        Instance.StartFlash(color, alpha, seconds);
    }

    /// <summary>Freezes time for `freezeSeconds`, then plays slow motion at `slowScale` for `slowSeconds`
    /// (both real time), then restores normal speed.</summary>
    public static void HitStopAndSlowMo(float freezeSeconds, float slowScale, float slowSeconds)
    {
        Instance.StartCoroutine(Instance.TimeRoutine(freezeSeconds, slowScale, slowSeconds));
    }

    private void StartShake(float magnitude, float duration)
    {
        // A stronger shake replaces a weaker one in progress; a weaker one never cuts a stronger one short.
        float remaining = Mathf.Max(0f, _shakeEndTime - Time.unscaledTime);
        float currentStrength = _shakeDuration > 0f ? _shakeMagnitude * (remaining / _shakeDuration) : 0f;
        if (magnitude < currentStrength)
            return;

        _shakeMagnitude = magnitude;
        _shakeDuration = Mathf.Max(0.01f, duration);
        _shakeEndTime = Time.unscaledTime + _shakeDuration;
    }

    private void SetDim(float alpha, float fadeSeconds, Color tint)
    {
        _dimColor = tint;
        _dimTarget = Mathf.Clamp01(alpha);
        _dimSpeed = Mathf.Abs(_dimTarget - _dimCurrent) / Mathf.Max(0.01f, fadeSeconds);
    }

    private void StartFlash(Color color, float alpha, float seconds)
    {
        _flashColor = color;
        _flashAlpha = Mathf.Clamp01(alpha);
        _flashFadeSpeed = _flashAlpha / Mathf.Max(0.01f, seconds);
    }

    private IEnumerator TimeRoutine(float freezeSeconds, float slowScale, float slowSeconds)
    {
        if (!_timeModified)
            _baseTimeScale = Time.timeScale;
        _timeModified = true;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(freezeSeconds);

        Time.timeScale = Mathf.Max(0.01f, slowScale) * _baseTimeScale;
        yield return new WaitForSecondsRealtime(slowSeconds);

        Time.timeScale = _baseTimeScale;
        _timeModified = false;
    }

    private void OnDestroy()
    {
        if (_timeModified)
            Time.timeScale = _baseTimeScale;

        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        // Remove last frame's shake offset before the camera's own logic runs this frame.
        if (_camera != null && _appliedShakeOffset != Vector3.zero)
        {
            _camera.transform.position -= _appliedShakeOffset;
            _appliedShakeOffset = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        float dt = Time.unscaledDeltaTime;

        // Shake: random offset whose strength falls off linearly to zero.
        float remaining = _shakeEndTime - Time.unscaledTime;
        if (remaining > 0f)
        {
            float strength = _shakeMagnitude * (remaining / _shakeDuration);
            Vector2 random = Random.insideUnitCircle * strength;
            _appliedShakeOffset = new Vector3(random.x, random.y, 0f);
            _camera.transform.position += _appliedShakeOffset;
        }

        _dimCurrent = Mathf.MoveTowards(_dimCurrent, _dimTarget, _dimSpeed * dt);
        _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, _flashFadeSpeed * dt);
        UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        float dimAlpha = _dimCurrent;
        float flashAlpha = _flashAlpha;
        if (dimAlpha <= 0.001f && flashAlpha <= 0.001f)
        {
            if (_overlay != null)
                _overlay.enabled = false;
            return;
        }

        if (_overlay == null)
            CreateOverlay();

        _overlay.enabled = true;
        // Flash on top of dim: composite into one colour so a single sprite suffices.
        float totalAlpha = 1f - (1f - dimAlpha) * (1f - flashAlpha);
        Color color = totalAlpha > 0f
            ? (_dimColor * dimAlpha * (1f - flashAlpha) + _flashColor * flashAlpha) / totalAlpha
            : _dimColor;
        color.a = totalAlpha;
        _overlay.color = color;

        // Stretch over the whole view.
        if (_camera.orthographic)
        {
            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            _overlay.transform.localScale = new Vector3(width + 1f, height + 1f, 1f);
        }

        _overlay.transform.localPosition = new Vector3(0f, 0f, 1f);
    }

    private void CreateOverlay()
    {
        var go = new GameObject("SkillScreenOverlay");
        go.transform.SetParent(_camera.transform, false);
        _overlay = go.AddComponent<SpriteRenderer>();
        _overlay.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        _overlay.sortingLayerName = "Player";
        _overlay.sortingOrder = 30000;
    }
}
