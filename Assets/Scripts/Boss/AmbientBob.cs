using UnityEngine;

/// <summary>Gently floats this object up/down (and a little sideways) around where it was placed: floating rock islands, hovering
/// crystals. Random phase per instance; uses scaled time.</summary>
public sealed class AmbientBob : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _amplitude = 0.35f;
    [SerializeField, Min(0.1f)] private float _periodSeconds = 5f;
    [SerializeField, Min(0f)] private float _sway = 0.12f;

    private Vector3 _origin;
    private float _phase;

    private void Start()
    {
        _origin = transform.position;
        _phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        float t = Time.time * Mathf.PI * 2f / _periodSeconds + _phase;
        transform.position = _origin + new Vector3(Mathf.Sin(t * 0.5f) * _sway, Mathf.Sin(t) * _amplitude, 0f);
    }
}
