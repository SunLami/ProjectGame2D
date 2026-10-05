using UnityEngine;

/// <summary>Scrolls the cloud sea under the Wind arena's platform (D-110): the children are copies of one horizontally seamless tile,
/// this moves them left/right at `speed` units per second and wraps by one tile width, so the clouds below the platform flow forever.</summary>
public sealed class SkyScroll : MonoBehaviour
{
    [SerializeField] private float _speed = 0.7f;
    [SerializeField, Min(0.1f)] private float _tileWidth = 51.2f;

    private Vector3 _origin;

    private void Start()
    {
        _origin = transform.position;
    }

    private void Update()
    {
        float offset = Mathf.Repeat(Time.time * _speed, _tileWidth);
        transform.position = _origin + new Vector3(offset, 0f, 0f);
    }
}
