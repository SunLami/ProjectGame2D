using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Points the player toward whatever an active quest needs next. Two target kinds, each with
/// their own "you're basically there" state:
///
/// NPC target (QuestManager.GetActionableNpcIds, resolved via QuestNpcRegistry) -- someone to
/// talk to or turn a quest in with:
///   - off-screen: edge-of-screen indicator
///   - on-screen: a marker floats above the NPC's own head instead (no more directional guessing
///     needed once you can already see them)
///
/// Area target (QuestManager.GetActionableAreaIds, resolved via AreaZoneRegistry) -- a zone the
/// current objective wants the player standing in (e.g. "defeat 3 dummies in the training area"):
///   - far: edge-of-screen indicator toward the zone
///   - near/on-screen but not yet inside: ground arrow under the player pointing at it
///   - player is inside (AreaZoneRegistry.IsPlayerInside): direction indicators hide because
///     there is nowhere further to walk
///
/// NPC targets take priority over area targets (checked first each frame) since talking to
/// someone is usually the more immediate action. Hides everything when neither resolves.
///
/// Uses ProceduralArrowSprite as placeholder art until real arrow sprites exist -- only the
/// Image components' sprite needs swapping later, this script only rotates/positions them.
/// </summary>
public sealed class QuestDirectionIndicator : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _camera;

    [Header("Area target -- approaching")]
    [Tooltip("World-space RectTransform that orbits the player and rotates to face an area target while outside it.")]
    [SerializeField] private RectTransform _groundArrow;
    [SerializeField] private Image _groundArrowImage;
    [Min(0f)]
    [SerializeField] private float _groundArrowOrbitRadius = 0.75f;

    [Header("Off-screen (either target kind)")]
    [Tooltip("Screen-space overlay RectTransform clamped to the edge of the screen toward the target.")]
    [SerializeField] private RectTransform _edgeIndicator;
    [SerializeField] private Image _edgeIndicatorImage;

    [Header("NPC target -- on-screen")]
    [Tooltip("Screen-space overlay RectTransform that floats above an on-screen NPC target's head.")]
    [SerializeField] private RectTransform _npcHeadMarker;
    [SerializeField] private Image _npcHeadMarkerImage;
    [SerializeField] private float _npcHeadWorldOffset = 1.4f;

    [Tooltip("Vertical bob animation for the head marker -- a \"pointing at this\" arrow bobs up/down, unlike the directional arrows.")]
    [SerializeField] private float _bobAmplitudePixels = 8f;
    [SerializeField] private float _bobSpeed = 2.5f;

    [Tooltip("Area target within this world distance uses the ground arrow instead of the edge indicator, even if technically off-screen.")]
    [SerializeField] private float _nearRadius = 12f;

    [Tooltip("Screen-space padding (pixels) the edge indicator stays inside of.")]
    [SerializeField] private float _edgeMargin = 60f;

    private QuestManager _questManager;
    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_groundArrowImage == null && _groundArrow != null)
            _groundArrowImage = _groundArrow.GetComponent<Image>();
        if (_edgeIndicatorImage == null && _edgeIndicator != null)
            _edgeIndicatorImage = _edgeIndicator.GetComponent<Image>();
        if (_npcHeadMarkerImage == null && _npcHeadMarker != null)
            _npcHeadMarkerImage = _npcHeadMarker.GetComponent<Image>();

        Sprite arrow = ProceduralArrowSprite.GetUpArrow();
        if (_groundArrowImage != null && _groundArrowImage.sprite == null)
            _groundArrowImage.sprite = arrow;
        if (_edgeIndicatorImage != null && _edgeIndicatorImage.sprite == null)
            _edgeIndicatorImage.sprite = arrow;
        if (_npcHeadMarkerImage != null && _npcHeadMarkerImage.sprite == null)
            _npcHeadMarkerImage.sprite = arrow;
    }

    private void OnEnable()
    {
        _questManager = QuestManager.Instance;
        if (_player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                _player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (_questManager == null)
            _questManager = QuestManager.Instance;
        if (_player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                _player = playerObject.transform;
        }

        if (_questManager == null || _player == null || _camera == null)
        {
            HideAll();
            return;
        }

        if (TryResolveNpcTarget(out Transform npcTarget))
        {
            UpdateForNpcTarget(npcTarget);
            return;
        }

        if (TryResolveAreaTarget(out string areaId, out Transform areaTarget))
        {
            UpdateForAreaTarget(areaId, areaTarget);
            return;
        }

        HideAll();
    }

    // Not every actionable NPC/area is necessarily present in the player's current scene (e.g. a
    // Town Elder quest being offered while the player is off in a Training Area) -- QuestManager
    // only knows quest state, so it can't filter for that; walk its priority-ordered candidates
    // here until one actually resolves to a live Transform via the scene registry.
    private bool TryResolveNpcTarget(out Transform target)
    {
        foreach (string npcId in _questManager.GetActionableNpcIds())
        {
            if (QuestNpcRegistry.TryGet(npcId, out target))
                return true;
        }
        target = null;
        return false;
    }

    private bool TryResolveAreaTarget(out string areaId, out Transform target)
    {
        foreach (string candidate in _questManager.GetActionableAreaIds())
        {
            if (AreaZoneRegistry.TryGet(candidate, out target))
            {
                areaId = candidate;
                return true;
            }
        }
        areaId = null;
        target = null;
        return false;
    }

    private void UpdateForNpcTarget(Transform target)
    {
        SetActive(_groundArrow, false);

        Vector3 viewportPos = _camera.WorldToViewportPoint(target.position);
        bool onScreen = IsOnScreen(viewportPos);

        if (onScreen)
        {
            SetActive(_edgeIndicator, false);
            ShowNpcHeadMarker(target);
        }
        else
        {
            SetActive(_npcHeadMarker, false);
            ShowEdgeIndicator(target.position);
        }
    }

    private void UpdateForAreaTarget(string areaId, Transform target)
    {
        SetActive(_npcHeadMarker, false);

        if (AreaZoneRegistry.IsPlayerInside(areaId))
        {
            SetActive(_groundArrow, false);
            SetActive(_edgeIndicator, false);
            return;
        }

        Vector3 toTarget = target.position - _player.position;
        toTarget.z = 0f;
        float distance = toTarget.magnitude;

        Vector3 viewportPos = _camera.WorldToViewportPoint(target.position);
        bool onScreen = IsOnScreen(viewportPos);

        if (distance <= _nearRadius && onScreen)
            ShowGroundArrow(toTarget);
        else
            ShowEdgeIndicator(target.position);
    }

    private bool IsOnScreen(Vector3 viewportPos) =>
        viewportPos.z > 0f
        && viewportPos.x > 0.02f && viewportPos.x < 0.98f
        && viewportPos.y > 0.02f && viewportPos.y < 0.98f;

    private void ShowNpcHeadMarker(Transform target)
    {
        if (_npcHeadMarker == null)
            return;

        SetActive(_npcHeadMarker, true);
        Vector3 worldPos = target.position + new Vector3(0f, _npcHeadWorldOffset, 0f);
        Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);
        // Bobs up/down in screen pixels -- this marker means "the thing right here", so it nods at
        // its target instead of rotating like the directional arrows.
        screenPos.y += Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitudePixels;
        _npcHeadMarker.position = screenPos;
    }

    private void ShowGroundArrow(Vector3 direction)
    {
        if (_groundArrow == null)
            return;

        SetActive(_groundArrow, true);
        SetActive(_edgeIndicator, false);
        Vector2 normalizedDirection = new Vector2(direction.x, direction.y).normalized;
        _groundArrow.anchoredPosition = normalizedDirection * _groundArrowOrbitRadius;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        _groundArrow.localEulerAngles = new Vector3(0f, 0f, -angle);
    }

    private void ShowEdgeIndicator(Vector3 worldTarget)
    {
        SetActive(_groundArrow, false);
        if (_edgeIndicator == null)
            return;

        SetActive(_edgeIndicator, true);

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 screenPos = _camera.WorldToScreenPoint(worldTarget);

        // Behind the camera the raw projected point flips to the wrong side -- mirror it back
        // through the center so the arrow still points the correct way round the screen edge.
        if (screenPos.z < 0f)
            screenPos = screenCenter + (screenCenter - screenPos);

        Vector3 fromCenter = screenPos - screenCenter;
        if (fromCenter.sqrMagnitude < 0.001f)
            fromCenter = Vector3.up;

        float halfWidth = Mathf.Max(1f, Screen.width / 2f - _edgeMargin);
        float halfHeight = Mathf.Max(1f, Screen.height / 2f - _edgeMargin);

        float scaleX = halfWidth / Mathf.Max(0.001f, Mathf.Abs(fromCenter.x));
        float scaleY = halfHeight / Mathf.Max(0.001f, Mathf.Abs(fromCenter.y));
        float scale = Mathf.Min(scaleX, scaleY);

        _edgeIndicator.position = screenCenter + fromCenter * scale;

        float angle = Mathf.Atan2(fromCenter.x, fromCenter.y) * Mathf.Rad2Deg;
        _edgeIndicator.localEulerAngles = new Vector3(0f, 0f, -angle);
    }

    private void HideAll()
    {
        SetActive(_groundArrow, false);
        SetActive(_edgeIndicator, false);
        SetActive(_npcHeadMarker, false);
    }

    private static void SetActive(RectTransform rect, bool active)
    {
        if (rect != null && rect.gameObject.activeSelf != active)
            rect.gameObject.SetActive(active);
    }
}
