using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Wind Skill 4 "Wind Roc" ultimate (D-080): a giant wind eagle sweeps along the aimed line (a rectangle
/// `_pathWidth` wide), snatches every enemy under it into the air and carries them to the end of the line,
/// then throws them forward and dives into the ground with a screech.
///
///   1. Gather  - animated wind ring under the caster, wind feathers orbit the caster, the screen dims
///                a little and the camera trembles; animated gust ripples run down the path and a shrinking wind
///                ring marks where the eagle will land (the danger zone).
///   2. Flight  - the eagle (drawn raised by `_altitude`, with a black shadow on the ground path) flies the
///                line; a flock of small eagles trails behind it; it leaves little whirlwinds on the ground and
///                fires feathers sideways that hurt enemies beside the path; leaves/dust stream across the
///                screen. Enemies under the eagle are carried (`IAirborne`, moved with the eagle) and take tick damage.
///   3. Finale  - captured enemies are thrown forward and take fall damage + stun when they land; the eagle dives
///                to the ground at the end point: screech (flash, shake, hit-stop), shockwave and a blast that hurts
///                and shoves everything around, then it flies off.
///   4. Aftermath - feathers rain down and a gale zone slows enemies for a few seconds.
/// Targets are de-duplicated on the IDamageable component, never on transform.root.</summary>
public class SkillWindRoc : MonoBehaviour
{
    [Header("Art (all animated)")]
    [SerializeField] private Sprite[] _rocFrames;
    [SerializeField] private Sprite[] _featherFrames;
    [SerializeField] private Sprite[] _ringFrames;
    [Tooltip("Crisp landing-zone marker loop shown at the end point; falls back to the wind ring frames when empty.")]
    [SerializeField] private Sprite[] _markerFrames;
    [SerializeField] private Sprite[] _gustFrames;
    [SerializeField] private Sprite[] _puffFrames;
    [Tooltip("Tumbling leaf loop used for the leaves streaming across the screen; falls back to the puff frames when empty.")]
    [SerializeField] private Sprite[] _leafFrames;
    [SerializeField] private Sprite[] _shockwaveFrames;
    [SerializeField] private GameObject _impactVfxPrefab;
    [SerializeField] private SkillStatusLoop _carriedIndicatorPrefab;
    [SerializeField] private SkillStunIndicator _stunIndicatorPrefab;
    [SerializeField] private float _effectFrameRate = 10f;

    [Header("Path")]
    [SerializeField] private float _pathWidth = 2.5f;
    [Tooltip("The eagle starts this far behind the caster (off-screen) and fades in over its first units, so it flies in instead of popping up.")]
    [SerializeField] private float _approachDistance = 10f;
    [SerializeField] private float _rocSpeed = 12f;
    [SerializeField] private float _rocScale = 0.75f;
    [SerializeField] private float _altitude = 1.6f;
    [Tooltip("The eagle descends over the last stretch of the path so the finale is a dive.")]
    [SerializeField] private float _diveDistance = 1.6f;

    [Header("Gather")]
    [SerializeField] private float _gatherDuration = 1f;
    [SerializeField] private Color _dimTint = new Color(0.05f, 0.2f, 0.18f, 1f);
    [SerializeField] private float _dimAlpha = 0.25f;

    [Header("Carry")]
    [SerializeField] private float _captureAlong = 1.2f;
    [SerializeField] private float _carryHeight = 1.2f;
    [SerializeField] private float _carryTickDamage = 8f;
    [SerializeField] private float _carryTickInterval = 0.25f;
    [SerializeField] private float _throwDistance = 2.5f;
    [SerializeField] private float _throwDuration = 0.5f;
    [SerializeField] private float _landingDamage = 40f;
    [SerializeField] private float _landingStun = 1f;

    [Header("Damage tuning (ultimate)")]
    [Tooltip("Global multiplier applied to EVERY damage number in this skill. Boss encounters will tune this one value later.")]
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private float _launchFeatherDamage = 6f;
    [SerializeField] private float _flockDamage = 6f;
    [SerializeField] private float _galeTickDamage = 4f;
    [SerializeField] private float _rainFeatherDamage = 3f;

    [Header("Gust arcs (the waves that rush down the path during the charge)")]
    [SerializeField] private float _rippleDamage = 8f;
    [SerializeField] private float _rippleKnockback = 2f;
    [Tooltip("Depth of the area each arc sweeps along the path (the crescent is thin).")]
    [SerializeField] private float _rippleDepth = 1.1f;

    [Header("Flank feathers")]
    [SerializeField] private float _featherInterval = 0.45f;
    [SerializeField] private float _featherSpeed = 7f;
    [SerializeField] private float _featherLifetime = 1.2f;
    [SerializeField] private float _featherDamage = 10f;
    [SerializeField] private float _featherRadius = 0.45f;

    [Header("Finale")]
    [SerializeField] private float _blastRadius = 2.2f;
    [SerializeField] private float _blastDamage = 35f;
    [SerializeField] private float _blastKnockback = 8f;
    [SerializeField] private float _galeDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float _galeSlow = 0.6f;
    [SerializeField] private float _featherRainDuration = 2f;

    [SerializeField] private LayerMask _targetLayers = ~0;

    private class Carried
    {
        public MonoBehaviour Behaviour;
        public IDamageable Damageable;
        public Rigidbody2D Body;
        public Vector2 Offset;
        public float NextTick;
        public float NextRefresh;
        public bool Released;
    }

    private Player _caster;
    private Vector2 _start;
    private Vector2 _direction;
    private Vector2 _perpendicular;
    private float _length;
    private float _progress;            // distance of the eagle's ground point along the path (starts negative)
    private bool _flying;
    private bool _finaleStarted;
    private Vector2 _endPoint;
    private Camera _camera;
    private SpriteRenderer _roc;
    private SpriteRenderer _shadow;
    private SpriteRenderer _markerRing;
    private readonly List<SpriteRenderer> _flock = new List<SpriteRenderer>();
    private readonly List<Carried> _carried = new List<Carried>();
    private readonly HashSet<MonoBehaviour> _everCarried = new HashSet<MonoBehaviour>();
    private readonly Dictionary<MonoBehaviour, SkillStatusLoop> _carriedIcons = new Dictionary<MonoBehaviour, SkillStatusLoop>();
    private readonly Dictionary<MonoBehaviour, SkillStunIndicator> _stunIcons = new Dictionary<MonoBehaviour, SkillStunIndicator>();
    private readonly List<GameObject> _persistent = new List<GameObject>();
    private static Sprite _whiteSprite;

    /// <summary>Starts the ultimate along `direction` from the caster's skill origin; `length` is the path length.</summary>
    public void Launch(Player caster, Vector2 direction, float length)
    {
        _caster = caster;
        _start = caster.SkillOrigin;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _perpendicular = new Vector2(-_direction.y, _direction.x);
        _length = Mathf.Max(3f, length);
        _endPoint = _start + _direction * _length;
        _progress = -_approachDistance;
        _camera = Camera.main;
        StartCoroutine(Run());
    }

    // ------------------------------------------------------------------ timeline

    private IEnumerator Run()
    {
        // 1. gather + warnings
        SoundFXManager.PlaySfx(SfxIds.SkillWindS4Charge);
        StartCoroutine(GatherRoutine());
        StartCoroutine(RippleWarningRoutine());
        CreateMarkerRing();
        SkillScreenFX.Dim(_dimAlpha, _gatherDuration, _dimTint);
        SkillScreenFX.Shake(0.04f, _gatherDuration);
        StartCoroutine(ScreenLeavesRoutine());
        // 2. flight: the eagle enters while the charge is still building and arrives over the caster just as
        // the charge ends (flyDelay = charge time minus the time it needs to cover the approach).
        float flyDelay = Mathf.Max(0f, _gatherDuration - _approachDistance / _rocSpeed + 0.1f);
        yield return new WaitForSeconds(flyDelay);
        SpawnRoc();
        _flying = true;
        StartCoroutine(FlankFeatherRoutine());
        StartCoroutine(FlockStrikeRoutine());

        while (_progress < _length)
            yield return null;

        // 3. finale
        _flying = false;
        yield return FinaleRoutine();

        yield return new WaitForSeconds(Mathf.Max(_galeDuration, _featherRainDuration) + 0.5f);
        Cleanup();
    }

    private void Cleanup()
    {
        foreach (GameObject go in _persistent)
        {
            if (go != null)
                Destroy(go);
        }

        Destroy(gameObject);
    }

    private float _flapTimer;

    private void Update()
    {
        if (!_flying)
            return;

        _progress += _rocSpeed * Time.deltaTime;
        _flapTimer -= Time.deltaTime;
        if (_flapTimer <= 0f)
        {
            _flapTimer = 0.55f;
            SoundFXManager.PlaySfx(SfxIds.SkillWindS4Flap);
        }

        UpdateRoc();
        CaptureAlongPath();
        UpdateCarried();
    }

    // ------------------------------------------------------------------ 1. gather

    private IEnumerator GatherRoutine()
    {
        if (_caster == null)
            yield break;

        SpriteRenderer ring = CreateAnimated("RocGatherRing", _ringFrames, _caster.transform.position, 0.6f, -95);
        _persistent.Add(ring.gameObject);

        const int featherCount = 6;
        var feathers = new SpriteRenderer[featherCount];
        SpriteRenderer casterRenderer = _caster.GetComponentInChildren<SpriteRenderer>();
        for (int i = 0; i < featherCount; i++)
        {
            feathers[i] = CreateAnimated("OrbitFeather_" + i, _featherFrames, _caster.SkillOrigin, 0.14f, 0);
            if (casterRenderer != null)
                feathers[i].sortingLayerID = casterRenderer.sortingLayerID;
            _persistent.Add(feathers[i].gameObject);
        }

        float t = 0f;
        while (t < _gatherDuration && _caster != null)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / _gatherDuration);
            SetAlpha(ring, Mathf.Clamp01(progress * 3f));
            ring.transform.position = _caster.transform.position;
            ring.transform.Rotate(0f, 0f, 90f * Time.deltaTime);

            Vector2 origin = _caster.SkillOrigin;
            int order = casterRenderer != null ? casterRenderer.sortingOrder : 0;
            for (int i = 0; i < featherCount; i++)
            {
                float angle = i / (float)featherCount * Mathf.PI * 2f + t * (2f + progress * 5f);
                float sin = Mathf.Sin(angle);
                // A flat ring around the HIPS (not the chest): the back half (sin > 0) is only waist-high and is
                // drawn behind the character; the front half passes low, at leg height, in front of the body.
                // Centring at chest height made the back half rise to face level.
                Vector2 hips = (Vector2)_caster.transform.position + new Vector2(0f, 0.25f);
                feathers[i].transform.position = new Vector3(hips.x + Mathf.Cos(angle) * 1.0f, hips.y + sin * 0.22f + Mathf.Sin(t * 5f + i) * 0.03f, 0f);
                feathers[i].sortingOrder = order + (sin > 0f ? -1 : 1);
                SetAlpha(feathers[i], Mathf.Clamp01(progress * 4f));
            }

            yield return null;
        }

        // Feathers streak forward along the path as the eagle arrives; the ring fades.
        var launchStruck = new HashSet<MonoBehaviour>();
        float launch = 0f;
        while (launch < 0.35f)
        {
            launch += Time.deltaTime;
            float k = launch / 0.35f;
            for (int i = 0; i < featherCount; i++)
            {
                if (feathers[i] == null)
                    continue;
                feathers[i].transform.position += (Vector3)(_direction * (14f * Time.deltaTime));
                SetAlpha(feathers[i], 1f - k);
                StrikeAt(feathers[i].transform.position, 0.5f, _launchFeatherDamage, _direction, launchStruck);
            }

            if (ring != null)
                SetAlpha(ring, 1f - k);
            yield return null;
        }
    }

    /// <summary>Animated gust arcs run down the path, as wide as the path, so the player can read where the eagle will fly.</summary>
    private IEnumerator RippleWarningRoutine()
    {
        float elapsed = 0f;
        float timer = 0f;
        while (elapsed < _gatherDuration + 0.3f)
        {
            if (timer <= 0f)
            {
                timer = 0.25f;
                StartCoroutine(RippleRoutine());
            }

            timer -= Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RippleRoutine()
    {
        if (_gustFrames is not { Length: > 0 })
            yield break;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        SpriteRenderer ripple = CreateAnimated("PathRipple", _gustFrames, _start, 0.2f, -60);
        ripple.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        float scale = (_pathWidth + 0.8f) / Mathf.Max(0.1f, _gustFrames[0].bounds.size.y);
        ripple.transform.localScale = new Vector3(scale, scale, 1f);

        var struck = new HashSet<MonoBehaviour>();
        float distance = 0.5f;
        while (distance < _length && ripple != null)
        {
            distance += 11f * Time.deltaTime;
            ripple.transform.position = _start + _direction * distance;
            SetAlpha(ripple, 0.8f * Mathf.Clamp01((_length - distance) / 2f));
            ApplyRippleHits(_start + _direction * distance, angle, struck);
            yield return null;
        }

        if (ripple != null)
            Destroy(ripple.gameObject);
    }

    /// <summary>The rushing gust arc hurts and shoves every enemy it sweeps across (once per arc per enemy).</summary>
    private void ApplyRippleHits(Vector2 center, float angleDegrees, HashSet<MonoBehaviour> struck)
    {
        Vector2 size = new Vector2(_rippleDepth, _pathWidth + 0.8f);
        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(center, size, angleDegrees, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!struck.Add(candidate))
                    break;

                target.TakeDamage(Dmg(_rippleDamage), _direction, _rippleKnockback);
                FlashAndBurst(candidate, candidate.transform.position, 0.45f);
                break;
            }
        }
    }

    /// <summary>Shrinking wind ring at the end point marks the landing zone for the whole skill.</summary>
    private void CreateMarkerRing()
    {
        Sprite[] frames = _markerFrames is { Length: > 0 } ? _markerFrames : _ringFrames;
        float width = frames is { Length: > 0 } ? frames[0].bounds.size.x : 3.67f;
        _markerRing = CreateAnimated("LandingMarker", frames, _endPoint, _blastRadius * 2f / width, -97);
        _markerRing.color = new Color(1f, 1f, 1f, 0f);
        _persistent.Add(_markerRing.gameObject);
        StartCoroutine(MarkerRoutine());
    }

    private IEnumerator MarkerRoutine()
    {
        float t = 0f;
        float baseScale = _markerRing.transform.localScale.x;
        while (_markerRing != null && !_finaleStarted)
        {
            t += Time.deltaTime;
            // Pulses inward toward the final size, rotating slowly.
            float pulse = 1f + 0.18f * Mathf.Sin(t * 5f);
            _markerRing.transform.localScale = Vector3.one * baseScale * pulse;
            // crisp marker: no spin, the animation already rotates its inner rings
            SetAlpha(_markerRing, Mathf.Min(t / 0.5f, 1f) * 0.95f);
            yield return null;
        }
    }

    /// <summary>Leaves and dust stream across the whole view in the wind's direction while the skill is active.</summary>
    private IEnumerator ScreenLeavesRoutine()
    {
        float time = 0f;
        float total = _gatherDuration + (_length + _approachDistance) / _rocSpeed + 1.5f;
        float spawn = 0f;
        while (time < total)
        {
            time += Time.deltaTime;
            spawn -= Time.deltaTime;
            while (spawn <= 0f)
            {
                spawn += 0.16f;
                StartCoroutine(LeafRoutine());
            }

            yield return null;
        }
    }

    private IEnumerator LeafRoutine()
    {
        Sprite[] leafFrames = _leafFrames is { Length: > 0 } ? _leafFrames : _puffFrames;
        if (leafFrames is not { Length: > 0 } || _camera == null)
            yield break;

        float height = _camera.orthographic ? _camera.orthographicSize * 2f : 10f;
        float width = height * _camera.aspect;
        Vector2 center = _camera.transform.position;
        // Start upwind of the view, anywhere across it, and blow downwind through it.
        Vector2 startPosition = center - _direction * (width * 0.5f)
            + _perpendicular * Random.Range(-height * 0.5f, height * 0.5f);
        SpriteRenderer leaf = CreateAnimated("ScreenLeaf", leafFrames, startPosition, 0.14f * Random.Range(0.8f, 1.2f), 95, _leafFrames is { Length: > 0 });
        leaf.sortingLayerName = "Player";
        SetAlpha(leaf, 0.85f);

        float speed = Random.Range(10f, 15f);
        float life = width / speed + 0.3f;
        float t = 0f;
        while (t < life && leaf != null)
        {
            t += Time.deltaTime;
            leaf.transform.position += (Vector3)(_direction * (speed * Time.deltaTime));
            yield return null;
        }

        if (leaf != null)
            Destroy(leaf.gameObject);
    }

    // ------------------------------------------------------------------ 2. flight

    private void SpawnRoc()
    {
        SoundFXManager.PlaySfx(SfxIds.SkillWindS4Screech);
        _roc = CreateAnimated("WindRoc", _rocFrames, GroundPoint(), _rocScale, 90, true, 12f);
        _roc.sortingLayerName = "Player";
        _persistent.Add(_roc.gameObject);

        _shadow = CreateAnimated("RocShadow", _rocFrames, GroundPoint(), _rocScale * 0.85f, -58, true, 12f);
        _shadow.color = new Color(0f, 0f, 0f, 0.35f);
        _persistent.Add(_shadow.gameObject);

        // Small eagles trail behind in a V.
        Vector2[] offsets =
        {
            new Vector2(-1.8f, 1.5f), new Vector2(-1.8f, -1.5f),
            new Vector2(-3.4f, 2.7f), new Vector2(-3.4f, -2.7f),
        };
        foreach (Vector2 offset in offsets)
        {
            SpriteRenderer bird = CreateAnimated("RocFlockBird", _rocFrames, GroundPoint(), _rocScale * 0.38f, 89, true, 12f);
            bird.sortingLayerName = "Player";
            bird.GetComponent<SkillFrameAnimator>()?.Play(_rocFrames, 12f, true, Random.value);
            _flock.Add(bird);
            _persistent.Add(bird.gameObject);
            birdOffsets.Add(offset);
            _birdGround.Add(GroundPoint());
            _birdStruck.Add(new HashSet<MonoBehaviour>());
        }

        UpdateRoc();
    }

    private readonly List<Vector2> birdOffsets = new List<Vector2>();
    private readonly List<Vector2> _birdGround = new List<Vector2>();
    private readonly List<HashSet<MonoBehaviour>> _birdStruck = new List<HashSet<MonoBehaviour>>();

    private Vector2 GroundPoint() => _start + _direction * _progress;

    private void UpdateRoc()
    {
        if (_roc == null)
            return;

        // Altitude: constant, then a dive over the last stretch of the path.
        float remaining = _length - _progress;
        float altitude = remaining < _diveDistance ? Mathf.Lerp(0.15f, _altitude, Mathf.Clamp01(remaining / _diveDistance)) : _altitude;

        Vector2 ground = GroundPoint();
        _roc.transform.position = ground + Vector2.up * altitude;
        _shadow.transform.position = ground;

        // Fades in over its first 3.5 units of flight (it comes in from beyond the screen).
        float fade = Mathf.Clamp01((_progress + _approachDistance) / 3.5f);
        _shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.5f, 0.3f, altitude / _altitude) * fade);
        SetAlpha(_roc, fade);

        // Heading: the sprite faces right/left only, so flip for leftward paths and tilt it toward the path's
        // vertical component (clamped) so the eagle visibly flies the way the path goes, not always sideways.
        bool left = _direction.x < 0f;
        float tilt = Mathf.Clamp(Mathf.Atan2(_direction.y, Mathf.Abs(_direction.x)) * Mathf.Rad2Deg, -45f, 45f);
        Quaternion heading = Quaternion.Euler(0f, 0f, left ? -tilt : tilt);
        _roc.flipX = left;
        _roc.transform.rotation = heading;
        _shadow.flipX = left;
        _shadow.transform.rotation = heading;

        for (int i = 0; i < _flock.Count; i++)
        {
            if (_flock[i] == null)
                continue;

            Vector2 offset = birdOffsets[i];
            // Offset is in path space: x behind (negative) along the path, y to the side.
            Vector2 world = ground + _direction * offset.x + _perpendicular * offset.y + Vector2.up * (altitude + 0.3f);
            // Tight follow so the V formation holds even at flight speed (a soft follow let the birds scatter).
            _flock[i].transform.position = Vector3.Lerp(_flock[i].transform.position, world, 28f * Time.deltaTime);
            _birdGround[i] = ground + _direction * offset.x + _perpendicular * offset.y;
            _flock[i].flipX = left;
            _flock[i].transform.rotation = heading;
            SetAlpha(_flock[i], fade);
        }
    }

    private IEnumerator FlockStrikeRoutine()
    {
        while (_flying)
        {
            if (_progress > 0f)
            {
                for (int i = 0; i < _birdGround.Count; i++)
                    StrikeAt(_birdGround[i], 0.6f, _flockDamage, _direction, _birdStruck[i]);
            }

            yield return null;
        }
    }

    private IEnumerator FlankFeatherRoutine()
    {
        float timer = 0f;
        while (_flying)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f && _progress > 0f)
            {
                timer = _featherInterval;
                StartCoroutine(FeatherShotRoutine(+1));
                StartCoroutine(FeatherShotRoutine(-1));
            }

            yield return null;
        }
    }

    private IEnumerator FeatherShotRoutine(int side)
    {
        if (_featherFrames is not { Length: > 0 })
            yield break;

        Vector2 direction = (_perpendicular * side + _direction * 0.35f).normalized;
        SpriteRenderer feather = CreateAnimated("FlankFeather", _featherFrames, GroundPoint(), 0.16f, 20);
        feather.sortingLayerName = "Player";
        feather.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 45f);

        var hit = new HashSet<MonoBehaviour>();
        float t = 0f;
        while (t < _featherLifetime && feather != null)
        {
            t += Time.deltaTime;
            feather.transform.position += (Vector3)(direction * (_featherSpeed * Time.deltaTime));
            SetAlpha(feather, Mathf.Clamp01((_featherLifetime - t) / 0.3f));

            foreach (Collider2D overlap in Physics2D.OverlapCircleAll(feather.transform.position, _featherRadius, _targetLayers))
            {
                if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                    continue;

                foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
                {
                    if (candidate is not IDamageable target || target.IsDead)
                        continue;

                    if (!hit.Add(candidate) || _everCarried.Contains(candidate))
                        break; // each feather hits an enemy once; carried enemies are already being hurt

                    target.TakeDamage(Dmg(_featherDamage), direction, 2f);
                    FlashAndBurst(candidate, candidate.transform.position, 0.5f);
                    break;
                }
            }

            yield return null;
        }

        if (feather != null)
            Destroy(feather.gameObject);
    }

    // ------------------------------------------------------------------ carrying

    private void CaptureAlongPath()
    {
        if (_progress < 0f)
            return;

        Vector2 ground = GroundPoint();
        float reach = _pathWidth * 0.5f + _captureAlong + 0.5f;
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(ground, reach, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (_everCarried.Contains(candidate))
                    break;

                Vector2 offsetFromGround = (Vector2)candidate.transform.position - ground;
                float along = Vector2.Dot(offsetFromGround, _direction);
                float lateral = Vector2.Dot(offsetFromGround, _perpendicular);
                if (Mathf.Abs(along) > _captureAlong || Mathf.Abs(lateral) > _pathWidth * 0.5f)
                    break;

                if (candidate is not IAirborne)
                    break;

                _everCarried.Add(candidate);
                _carried.Add(new Carried
                {
                    Behaviour = candidate,
                    Damageable = target,
                    Body = candidate.GetComponent<Rigidbody2D>(),
                    // Spread the carried enemies in a loose bunch under the eagle.
                    Offset = new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(-0.7f, 0.7f)),
                    NextTick = Time.time + _carryTickInterval,
                    NextRefresh = 0f,
                });
                ShowCarriedIcon(candidate);
                break;
            }
        }
    }

    private void UpdateCarried()
    {
        Vector2 ground = GroundPoint();
        for (int i = _carried.Count - 1; i >= 0; i--)
        {
            Carried c = _carried[i];
            if (c.Released)
                continue;

            if (c.Behaviour == null || c.Damageable.IsDead)
            {
                _carried.RemoveAt(i);
                continue;
            }

            // Keep them airborne (re-applied in short bursts so it extends, never drops them early).
            if (Time.time >= c.NextRefresh)
            {
                c.NextRefresh = Time.time + 0.3f;
                ((IAirborne)c.Behaviour).ApplyAirborne(0.6f, _carryHeight);
                if (_carriedIcons.TryGetValue(c.Behaviour, out SkillStatusLoop icon) && icon != null)
                    icon.Show(c.Behaviour.transform, 0.5f);
            }

            Vector2 target = ground + _direction * c.Offset.x + _perpendicular * c.Offset.y;
            MoveCarried(c, Vector2.Lerp(c.Behaviour.transform.position, target, 12f * Time.deltaTime));

            if (Time.time >= c.NextTick)
            {
                c.NextTick = Time.time + _carryTickInterval;
                c.Damageable.TakeDamage(Dmg(_carryTickDamage), Vector2.zero, 0f);
                var flash = c.Behaviour.GetComponent<EnemyHitFlash>();
                if (flash == null)
                    flash = c.Behaviour.gameObject.AddComponent<EnemyHitFlash>();
                flash.PlayFlash();
            }
        }
    }

    private static void MoveCarried(Carried carried, Vector2 position)
    {
        if (carried.Body != null)
        {
            carried.Body.position = position;
            carried.Body.linearVelocity = Vector2.zero;
        }
        else
        {
            carried.Behaviour.transform.position = position;
        }
    }

    private void ShowCarriedIcon(MonoBehaviour target)
    {
        if (_carriedIndicatorPrefab == null)
            return;

        // Short duration, refreshed every carry tick: the icon disappears right after the enemy is released.
        SkillStatusLoop icon = Instantiate(_carriedIndicatorPrefab);
        icon.Show(target.transform, 0.5f);
        _carriedIcons[target] = icon;
    }

    // ------------------------------------------------------------------ 3. finale

    private IEnumerator FinaleRoutine()
    {
        _finaleStarted = true;
        SoundFXManager.PlaySfx(SfxIds.SkillWindS4Dive);

        // Throw every captured enemy forward; each lands with damage and a stun.
        foreach (Carried c in _carried)
        {
            if (c.Behaviour != null && !c.Damageable.IsDead)
            {
                c.Released = true;
                StartCoroutine(ThrowRoutine(c));
            }
        }

        SkillScreenFX.Dim(0f, 1.2f);
        SkillScreenFX.Flash(new Color(0.85f, 1f, 0.92f), 0.8f, 0.55f);
        SkillScreenFX.Shake(0.35f, 0.6f);
        SkillScreenFX.HitStopAndSlowMo(0.05f, 0.35f, 0.3f);

        if (_impactVfxPrefab != null)
        {
            GameObject vfx = Instantiate(_impactVfxPrefab, _endPoint, Quaternion.identity);
            vfx.transform.localScale *= 2.2f;
        }

        StartCoroutine(ShockwaveRoutine(_endPoint));
        BlastAround(_endPoint);
        StartCoroutine(FeatherRainRoutine(_endPoint));
        StartCoroutine(GaleZoneRoutine(_endPoint));
        StartCoroutine(RocFlyOffRoutine());

        if (_markerRing != null)
            Destroy(_markerRing.gameObject);

        yield return null;
    }

    private IEnumerator ThrowRoutine(Carried c)
    {
        Vector2 from = c.Behaviour.transform.position;
        Vector2 to = from + _direction * _throwDistance;
        ((IAirborne)c.Behaviour).ApplyAirborne(_throwDuration + 0.1f, _carryHeight);

        float t = 0f;
        while (t < _throwDuration && c.Behaviour != null && !c.Damageable.IsDead)
        {
            t += Time.deltaTime;
            MoveCarried(c, Vector2.Lerp(from, to, t / _throwDuration));
            yield return null;
        }

        if (c.Behaviour == null || c.Damageable.IsDead)
            yield break;

        Vector2 landing = c.Behaviour.transform.position;
        c.Damageable.TakeDamage(Dmg(_landingDamage), Vector2.zero, 0f);
        if (c.Behaviour is IStunnable stunnable)
        {
            stunnable.ApplyStun(_landingStun);
            ShowStunIcon(c.Behaviour);
        }

        FlashAndBurst(c.Behaviour, landing, 0.9f);
        SkillScreenFX.Shake(0.08f, 0.15f);
    }

    private void BlastAround(Vector2 position)
    {
        var hit = new HashSet<MonoBehaviour>();
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(position, _blastRadius, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!hit.Add(candidate) || _everCarried.Contains(candidate))
                    break; // carried enemies already take the throw/landing damage

                Vector2 away = ((Vector2)candidate.transform.position - position).normalized;
                target.TakeDamage(Dmg(_blastDamage), away, _blastKnockback);
                FlashAndBurst(candidate, candidate.transform.position, 0.7f);
                break;
            }
        }
    }

    private IEnumerator ShockwaveRoutine(Vector2 center)
    {
        SoundFXManager.PlaySfxAt(SfxIds.SkillWindS4Shockwave, center);
        SoundFXManager.PlaySfx(SfxIds.SkillScreenSlam);
        if (_shockwaveFrames is not { Length: > 0 })
            yield break;

        // Wind-specific ring (WindShockwave_Expand). The frames only grow ~35% by themselves, so the scale also
        // expands from about half size to full, which reads as a proper blast wave.
        float fullScale = _blastRadius * 2.2f / _shockwaveFrames[0].bounds.size.x;
        SpriteRenderer ring = CreateAnimated("RocShockwave", _shockwaveFrames, center, fullScale * 0.5f, -60, false);
        float duration = _shockwaveFrames.Length / _effectFrameRate;
        float t = 0f;
        while (t < duration && ring != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - (1f - k) * (1f - k);
            ring.transform.localScale = Vector3.one * Mathf.Lerp(fullScale * 0.5f, fullScale, eased);
            if (k > 0.7f)
                SetAlpha(ring, (1f - k) / 0.3f);
            yield return null;
        }

        if (ring != null)
            Destroy(ring.gameObject);
    }

    private IEnumerator RocFlyOffRoutine()
    {
        if (_roc == null)
            yield break;

        float t = 0f;
        Vector2 ground = GroundPoint();
        while (t < 1.2f && _roc != null)
        {
            t += Time.deltaTime;
            float k = t / 1.2f;
            Vector2 p = ground + _direction * (3f * k) + Vector2.up * Mathf.Lerp(0.15f, 5f, k * k);
            _roc.transform.position = p;
            _shadow.transform.position = ground + _direction * (3f * k);
            SetAlpha(_roc, 1f - k);
            SetAlpha(_shadow, 0.3f * (1f - k));
            foreach (SpriteRenderer bird in _flock)
            {
                if (bird != null)
                {
                    bird.transform.position += (Vector3)(_direction * (8f * Time.deltaTime)) + Vector3.up * (3f * Time.deltaTime);
                    SetAlpha(bird, 1f - k);
                }
            }

            yield return null;
        }
    }

    // ------------------------------------------------------------------ 4. aftermath

    private IEnumerator FeatherRainRoutine(Vector2 center)
    {
        float t = 0f;
        float spawn = 0f;
        while (t < _featherRainDuration)
        {
            t += Time.deltaTime;
            spawn -= Time.deltaTime;
            while (spawn <= 0f)
            {
                spawn += 0.07f;
                StartCoroutine(FallingFeatherRoutine(center + Random.insideUnitCircle * _blastRadius));
            }

            yield return null;
        }
    }

    private IEnumerator FallingFeatherRoutine(Vector2 ground)
    {
        if (_featherFrames is not { Length: > 0 })
            yield break;

        SpriteRenderer feather = CreateAnimated("RainFeather", _featherFrames, ground + Vector2.up * 3.5f, 0.13f * Random.Range(0.8f, 1.3f), 70);
        feather.sortingLayerName = "Player";
        float fall = Random.Range(1.2f, 1.8f);
        float drift = Random.Range(-0.6f, 0.6f);
        float t = 0f;
        while (t < fall && feather != null)
        {
            t += Time.deltaTime;
            float k = t / fall;
            // Gentle sway so the feathers flutter down instead of dropping straight.
            feather.transform.position = new Vector3(ground.x + drift * k + Mathf.Sin(t * 6f) * 0.15f, ground.y + 3.5f * (1f - k), 0f);
            SetAlpha(feather, k > 0.8f ? (1f - k) / 0.2f : 1f);
            yield return null;
        }

        SoundFXManager.PlaySfxAt(SfxIds.SkillWindS4FeatherRain, ground);
        StrikeAt(ground, 0.5f, _rainFeatherDamage, Vector2.zero, new HashSet<MonoBehaviour>());

        if (feather != null)
            Destroy(feather.gameObject);
    }

    private IEnumerator GaleZoneRoutine(Vector2 center)
    {
        float t = 0f;
        float tick = 0f;
        while (t < _galeDuration)
        {
            t += Time.deltaTime;
            tick += Time.deltaTime;
            if (tick >= 0.4f)
            {
                tick = 0f;
                SlowAround(center);
            }

            yield return null;
        }
    }

    private void SlowAround(Vector2 center)
    {
        var processed = new HashSet<MonoBehaviour>();
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(center, _blastRadius, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!processed.Add(candidate))
                    break;

                if (candidate is ISlowable slowable)
                    slowable.ApplySlow(_galeSlow, 0.55f);
                target.TakeDamage(Dmg(_galeTickDamage), Vector2.zero, 0f);
                break;
            }
        }
    }

    // ------------------------------------------------------------------ helpers

    private void FlashAndBurst(MonoBehaviour candidate, Vector2 position, float burstScale)
    {
        var flash = candidate.GetComponent<EnemyHitFlash>();
        if (flash == null)
            flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
        flash.PlayFlash();

        if (_impactVfxPrefab != null)
        {
            GameObject vfx = Instantiate(_impactVfxPrefab, position, Quaternion.identity);
            vfx.transform.localScale *= burstScale;
        }
    }

    private void ShowStunIcon(MonoBehaviour target)
    {
        if (_stunIndicatorPrefab == null)
            return;

        if (_stunIcons.TryGetValue(target, out SkillStunIndicator existing) && existing != null)
        {
            existing.Show(target.transform, _landingStun);
            return;
        }

        SkillStunIndicator icon = Instantiate(_stunIndicatorPrefab);
        icon.Show(target.transform, _landingStun);
        _stunIcons[target] = icon;
    }

    private float Dmg(float value) => value * _damageMultiplier;

    /// <summary>Hurts every enemy within `radius` of `position`, once each per `struck` set (AoE helper for the
    /// contact effects: launched feathers, the flock, falling feathers).</summary>
    private void StrikeAt(Vector2 position, float radius, float damage, Vector2 knockDirection, HashSet<MonoBehaviour> struck)
    {
        foreach (Collider2D overlap in Physics2D.OverlapCircleAll(position, radius, _targetLayers))
        {
            if (overlap.isTrigger || overlap.GetComponentInParent<Player>() != null)
                continue;

            foreach (MonoBehaviour candidate in overlap.GetComponentsInParent<MonoBehaviour>())
            {
                if (candidate is not IDamageable target || target.IsDead)
                    continue;

                if (!struck.Add(candidate))
                    break;

                target.TakeDamage(Dmg(damage), knockDirection, knockDirection == Vector2.zero ? 0f : 2f);
                FlashAndBurst(candidate, candidate.transform.position, 0.4f);
                break;
            }
        }
    }

    private SpriteRenderer CreateAnimated(string name, Sprite[] frames, Vector2 position, float scale, int sortingOrder, bool loop = true, float frameRate = 0f)
    {
        var go = new GameObject(name);
        // Child of this skill object: the coroutines that animate and destroy these effects run on this
        // component, so when the skill ends (Destroy(gameObject)) any effect still alive (e.g. a leaf) must
        // go with it instead of being left behind on screen. The root sits at the origin with identity scale.
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames is { Length: > 0 } ? frames[0] : null;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOrder;
        if (frames is { Length: > 1 })
            go.AddComponent<SkillFrameAnimator>().Play(frames, frameRate > 0f ? frameRate : _effectFrameRate, loop, loop ? Random.value : 0f);
        return sr;
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }
}
