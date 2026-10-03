using UnityEngine;

/// <summary>Looping animated status icon that follows a target for a limited time (D-079 "wet" effect on
/// enemies hit by the Water ultimate). Like SkillStunIndicator it is not parented to the target (their
/// scale/flip would distort it); it tracks the target's collider and removes itself when the time is up
/// or the target dies/is destroyed. `Show` again extends the time instead of stacking copies.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkillStatusLoop : MonoBehaviour
{
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 10f;
    [Tooltip("Where on the target the icon sits: 0 = collider centre, 1 = collider top.")]
    [SerializeField, Range(0f, 1f)] private float _heightRatio = 0.35f;
    [SerializeField] private float _scale = 0.3f;
    [SerializeField] private int _sortingOrder = 40;

    private SpriteRenderer _renderer;
    private Transform _target;
    private Collider2D _targetCollider;
    private IDamageable _damageable;
    private float _until;
    private float _timer;

    public void Show(Transform target, float duration)
    {
        bool first = _target == null;
        _target = target;
        _until = Mathf.Max(_until, Time.time + duration);

        if (!first)
            return;

        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingLayerName = "Player";
        _renderer.sortingOrder = _sortingOrder;
        transform.localScale = new Vector3(_scale, _scale, 1f);
        if (_frames is { Length: > 0 })
            _renderer.sprite = _frames[0];

        _targetCollider = target.GetComponentInChildren<Collider2D>();
        foreach (MonoBehaviour behaviour in target.GetComponentsInParent<MonoBehaviour>())
        {
            if (behaviour is IDamageable damageable)
            {
                _damageable = damageable;
                break;
            }
        }

        Follow();
    }

    private void Update()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy || Time.time >= _until
            || (_damageable != null && _damageable.IsDead))
        {
            Destroy(gameObject);
            return;
        }

        Follow();

        if (_frames is { Length: > 1 })
        {
            _timer += Time.deltaTime;
            _renderer.sprite = _frames[Mathf.FloorToInt(_timer * _frameRate) % _frames.Length];
        }
    }

    private void Follow()
    {
        Vector2 position = _target.position;
        if (_targetCollider != null)
        {
            Bounds bounds = _targetCollider.bounds;
            position = new Vector2(bounds.center.x, Mathf.Lerp(bounds.center.y, bounds.max.y, _heightRatio));
        }

        transform.position = new Vector3(position.x, position.y, 0f);
    }
}
