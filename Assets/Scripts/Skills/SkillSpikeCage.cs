using UnityEngine;

/// <summary>Ring of small rock spikes around a rooted enemy's feet (D-077). Top-down needs a front and a
/// back: spikes on the far (upper) half of the ring are drawn just behind the enemy and spikes on the
/// near (lower) half just in front of it, so the enemy reads as standing inside the cage. The ring
/// follows the target, rises with the spike frames, holds for the root duration, then sinks again
/// (the rise frames played backwards). One cage per target: `Show` again only extends it.
/// The cage itself does not move or damage anything; the root/stun is applied by the skill that
/// spawns it (SkillSpikeCone).</summary>
public class SkillSpikeCage : MonoBehaviour
{
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private int _spikeCount = 10;
    [SerializeField] private float _spikeScale = 0.14f;
    [SerializeField] private float _riseFrameRate = 14f;
    [Tooltip("Ring half-width = target collider half-width + this padding.")]
    [SerializeField] private float _radiusPadding = 0.2f;
    [SerializeField] private float _minRadius = 0.35f;
    [Tooltip("Vertical squash of the ring so it reads as lying on the ground in top-down view.")]
    [SerializeField, Range(0.2f, 1f)] private float _depthRatio = 0.45f;
    [Tooltip("How far above the collider bottom the ring centre sits (the enemy's feet).")]
    [SerializeField] private float _feetOffset = 0.1f;

    private SpriteRenderer[] _spikes;
    private float[] _angles;
    private float[] _delays;
    private Transform _target;
    private Collider2D _targetCollider;
    private SpriteRenderer _targetRenderer;
    private IDamageable _targetDamageable;
    private float _elapsed;
    private float _until;
    private float _radiusX;

    public Transform Target => _target;

    public void Show(Transform target, float duration)
    {
        bool first = _target == null;
        _target = target;
        _until = Mathf.Max(_until, _elapsed + duration);

        if (!first)
            return;

        SoundFXManager.PlaySfxAt(SfxIds.SkillEarthS3Cage, target.position);
        _targetCollider = target.GetComponentInChildren<Collider2D>();
        _targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        foreach (MonoBehaviour behaviour in target.GetComponentsInParent<MonoBehaviour>())
        {
            if (behaviour is IDamageable damageable)
            {
                _targetDamageable = damageable;
                break;
            }
        }

        float halfWidth = _targetCollider != null ? _targetCollider.bounds.extents.x : 0.3f;
        _radiusX = Mathf.Max(_minRadius, halfWidth + _radiusPadding);
        BuildSpikes();
        UpdateSpikes();
    }

    private void BuildSpikes()
    {
        int count = Mathf.Max(4, _spikeCount);
        _spikes = new SpriteRenderer[count];
        _angles = new float[count];
        _delays = new float[count];

        for (int i = 0; i < count; i++)
        {
            _angles[i] = i / (float)count * Mathf.PI * 2f;
            _delays[i] = i * 0.03f;

            var go = new GameObject("CageSpike_" + i);
            go.transform.SetParent(transform, false);
            float scale = _spikeScale * Random.Range(0.9f, 1.1f);
            go.transform.localScale = new Vector3(Random.value < 0.5f ? -scale : scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            if (_targetRenderer != null)
                sr.sortingLayerID = _targetRenderer.sortingLayerID;
            else
                sr.sortingLayerName = "Default";
            _spikes[i] = sr;
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_target == null || !_target.gameObject.activeInHierarchy
            || (_targetDamageable != null && _targetDamageable.IsDead)
            || _elapsed >= _until + SinkDuration)
        {
            Destroy(gameObject);
            return;
        }

        UpdateSpikes();
    }

    private float RiseDuration => (_frames != null ? _frames.Length : 0) / Mathf.Max(0.01f, _riseFrameRate);
    private float SinkDuration => RiseDuration;

    private void UpdateSpikes()
    {
        if (_spikes == null || _frames is not { Length: > 0 })
            return;

        Vector2 feet = _target.position;
        if (_targetCollider != null)
            feet = new Vector2(_targetCollider.bounds.center.x, _targetCollider.bounds.min.y + _feetOffset);

        int targetOrder = _targetRenderer != null ? _targetRenderer.sortingOrder : 0;
        float rise = RiseDuration;
        float remaining = _until - _elapsed;

        for (int i = 0; i < _spikes.Length; i++)
        {
            float angle = _angles[i];
            // sin > 0 = far side (above the feet on screen): draw behind the enemy; otherwise in front.
            float sin = Mathf.Sin(angle);
            _spikes[i].transform.position = new Vector3(
                feet.x + Mathf.Cos(angle) * _radiusX,
                feet.y + sin * _radiusX * _depthRatio,
                0f);
            _spikes[i].sortingOrder = targetOrder + (sin > 0f ? -1 : 1);

            float t = _elapsed - _delays[i];
            int frame;
            if (t < 0f)
            {
                _spikes[i].sprite = null;
                continue;
            }
            else if (remaining < 0f)
                frame = Mathf.FloorToInt((SinkDuration + remaining) * _riseFrameRate); // sinking after the root ends
            else if (t < rise)
                frame = Mathf.FloorToInt(t * _riseFrameRate);
            else
                frame = _frames.Length - 1;

            if (frame < 0)
            {
                _spikes[i].sprite = null;
                continue;
            }

            _spikes[i].sprite = _frames[Mathf.Clamp(frame, 0, _frames.Length - 1)];
        }
    }
}
