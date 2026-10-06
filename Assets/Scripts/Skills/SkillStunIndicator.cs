using UnityEngine;

/// <summary>Classic "dizzy stars" stun status shown above a stunned enemy (D-076): a few stars orbiting
/// an ellipse over the head. Not parented to the enemy (their scale/flip would distort it); it follows
/// the target's collider top every frame and removes itself when the stun ends or the target
/// dies/is destroyed. One indicator per target: `Show` again extends instead of stacking copies.
/// Stars on the far side of the ellipse are drawn smaller and dimmer so the loop reads as 3D.</summary>
public class SkillStunIndicator : MonoBehaviour
{
    [SerializeField] private Sprite _starSprite;
    [SerializeField] private int _starCount = 3;
    [SerializeField] private float _orbitRadiusX = 0.45f;
    [SerializeField] private float _orbitRadiusY = 0.13f;
    [Tooltip("Degrees per second.")]
    [SerializeField] private float _orbitSpeed = 260f;
    [SerializeField] private float _starScale = 0.14f;
    [SerializeField] private float _heightPadding = 0.12f;
    [SerializeField] private int _sortingOrder = 50;

    private SpriteRenderer[] _stars;
    private Transform _target;
    private Collider2D _targetCollider;
    private float _until;
    private float _angle;

    public Transform Target => _target;

    public void Show(Transform target, float duration)
    {
        _target = target;
        _targetCollider = target.GetComponentInChildren<Collider2D>();
        _until = Mathf.Max(_until, Time.time + duration);

        if (_stars == null)
            BuildStars();

        FollowTarget();
        UpdateStars();
    }

    private void BuildStars()
    {
        int count = Mathf.Max(1, _starCount);
        _stars = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Star_" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _starSprite;
            _stars[i] = sr;
        }
    }

    private void Update()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy || Time.time >= _until)
        {
            Destroy(gameObject);
            return;
        }

        _angle += _orbitSpeed * Time.deltaTime;
        FollowTarget();
        UpdateStars();
    }

    private void UpdateStars()
    {
        if (_stars == null)
            return;

        for (int i = 0; i < _stars.Length; i++)
        {
            float angle = (_angle + i * 360f / _stars.Length) * Mathf.Deg2Rad;
            float depth = Mathf.Sin(angle); // +1 = near side (below head center on screen), -1 = far side
            Transform t = _stars[i].transform;
            t.localPosition = new Vector3(Mathf.Cos(angle) * _orbitRadiusX, -depth * _orbitRadiusY, 0f);

            float scale = _starScale * (1f - 0.25f * -depth * 0.5f - 0.0f) * (depth >= 0f ? 1f : 0.8f);
            t.localScale = new Vector3(scale, scale, 1f);

            _stars[i].sortingOrder = _sortingOrder + (depth >= 0f ? 1 : -1);
            _stars[i].color = depth >= 0f ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
        }
    }

    private void FollowTarget()
    {
        Vector2 position = _target.position;
        if (_targetCollider != null)
            position = new Vector2(_targetCollider.bounds.center.x, _targetCollider.bounds.max.y);

        transform.position = new Vector3(position.x, position.y + _heightPadding, 0f);
    }
}
