using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Boss-fight camera framing (D-088). While a boss is alive the camera follows a focus point that sits
/// between the player and the boss (weighted towards the player) and zooms out just enough to keep both
/// in view, never below the base size or above the maximum; the Cinemachine confiner still keeps the
/// view inside the painted arena. With no boss it follows the player at the base size.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossCameraDirector : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private CinemachineConfiner2D _confiner;
    [SerializeField] private BossArenaController _arena;
    [SerializeField, Min(1f)] private float _baseOrthographicSize = 12f;
    [SerializeField, Min(1f)] private float _maxOrthographicSize = 15.5f;
    [Tooltip("0 = follow the player only, 0.5 = exactly between player and boss.")]
    [SerializeField, Range(0f, 0.5f)] private float _bossWeight = 0.35f;
    [Tooltip("Free space kept around the farthest of the two targets.")]
    [SerializeField, Min(0f)] private float _margin = 3.5f;
    [SerializeField, Min(0.1f)] private float _zoomSpeed = 2.5f;

    private Transform _focus;
    private Transform _playerTransform;
    private Transform _originalFollow;
    private bool _usingFocus;
    private float _currentSize;
    private bool _ritualActive;
    private Vector3 _ritualCenter;
    private float _ritualSize;

    /// <summary>Summon ritual (D-091): frames `center` at `orthographicSize` (may exceed the fight maximum)
    /// so the player sees the whole arena while the guardians fire. Ended by <see cref="EndRitual"/>.</summary>
    public void BeginRitual(Vector3 center, float orthographicSize)
    {
        _ritualActive = true;
        _ritualCenter = center;
        _ritualSize = Mathf.Max(1f, orthographicSize);
    }

    public void EndRitual() => _ritualActive = false;

    private void Start()
    {
        if (_camera == null)
            _camera = FindAnyObjectByType<CinemachineCamera>();
        if (_confiner == null && _camera != null)
            _confiner = _camera.GetComponent<CinemachineConfiner2D>();

        var focusObject = new GameObject("BossCameraFocus");
        _focus = focusObject.transform;
        _originalFollow = _camera != null ? _camera.Follow : null;
        _currentSize = _baseOrthographicSize;
        ApplySize(_currentSize);
    }

    private void OnDestroy()
    {
        if (_focus != null)
            Destroy(_focus.gameObject);
    }

    private void LateUpdate()
    {
        if (_camera == null || _focus == null)
            return;

        if (_playerTransform == null)
        {
            Player player = FindAnyObjectByType<Player>();
            if (player != null)
                _playerTransform = player.transform;
        }

        BossController boss = _arena != null ? _arena.Boss : null;
        bool bossActive = boss != null && !boss.IsDead && _playerTransform != null;

        float targetSize = _baseOrthographicSize;
        if (_ritualActive)
        {
            _focus.position = new Vector3(_ritualCenter.x, _ritualCenter.y, 0f);
            targetSize = _ritualSize;
            if (!_usingFocus)
            {
                _camera.Follow = _focus;
                _usingFocus = true;
            }
        }
        else if (bossActive)
        {
            Vector2 player = _playerTransform.position;
            Vector2 bossPosition = boss.transform.position;
            Vector2 focus = Vector2.Lerp(player, bossPosition, _bossWeight);
            _focus.position = new Vector3(focus.x, focus.y, 0f);

            Vector2 distance = bossPosition - player;
            float aspect = Camera.main != null ? Camera.main.aspect : 16f / 9f;
            // The player is the farther target from the focus point: (1 - weight) of the distance away.
            float far = 1f - _bossWeight;
            float neededHeight = Mathf.Abs(distance.y) * far + _margin;
            float neededWidth = (Mathf.Abs(distance.x) * far + _margin * aspect) / aspect;
            targetSize = Mathf.Clamp(Mathf.Max(_baseOrthographicSize, neededHeight, neededWidth), _baseOrthographicSize, _maxOrthographicSize);

            if (!_usingFocus)
            {
                _camera.Follow = _focus;
                _usingFocus = true;
            }
        }
        else if (_usingFocus)
        {
            _camera.Follow = _originalFollow;
            _usingFocus = false;
        }

        float next = Mathf.MoveTowards(_currentSize, targetSize, _zoomSpeed * Time.deltaTime);
        if (!Mathf.Approximately(next, _currentSize))
        {
            _currentSize = next;
            ApplySize(_currentSize);
        }
    }

    private void ApplySize(float size)
    {
        if (_camera == null)
            return;

        LensSettings lens = _camera.Lens;
        lens.OrthographicSize = size;
        _camera.Lens = lens;
        if (_confiner != null)
            _confiner.InvalidateLensCache();
    }
}
