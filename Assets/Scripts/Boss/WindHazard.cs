using System;
using UnityEngine;

/// <summary>
/// One moving danger of the Wind owl's skills (D-111): a feather, a cyclone or a crescent blade. The skill gives it a mover
/// (position over time) and a hit callback; the hazard tests the distance to the player every frame and removes itself after `life`
/// seconds. The look is separate from the hit logic (a child "Visual"): it pops in, fades out, can spin, sway, leave a trail and cast a
/// ground shadow, so the effects read as living wind instead of rigid sprites sliding along a line. Dash invulnerability is honoured by
/// the callback (Player.TakeDamage ignores hits while dashing).
/// </summary>
public sealed class WindHazard : MonoBehaviour
{
    /// <summary>Presentation of a hazard (all optional).</summary>
    public struct Look
    {
        public float spinDegreesPerSecond;
        public float wobbleAmplitude;      // sideways sway of the sprite (world units), not of the hit position
        public float wobbleHertz;
        public float breathe;              // 0..1: the sprite pulses in size
        public float popSeconds;           // scale-in time with a small overshoot
        public float fadeSeconds;          // fade-out time at the end of its life
        public bool trail;
        public Color trailColor;
        public float trailWidth;
        public float trailSeconds;
        public bool shadow;
        public float shadowScale;
        public bool orbitLeaves;           // leaves circling the hazard (cyclones)
    }

    private Func<float, Vector2, Vector2> _mover;
    private Action<Player, Vector2> _onHit;
    private float _radius;
    private float _life;
    private float _age;
    private bool _oneHit;
    private bool _hasHit;
    private float _rehitCooldown;
    private float _nextHit;
    private Player _player;
    private bool _faceMotion;
    private Vector2 _lastPosition;
    private Look _look;
    private Transform _visual;
    private SpriteRenderer _renderer;
    private SpriteRenderer _shadowRenderer;
    private float _baseScale;
    private float _wobbleSeed;
    private float _baseAlpha = 1f;

    /// <summary>Spawns a hazard. `mover(age, currentPosition)` returns the next position.</summary>
    public static WindHazard Spawn(Sprite[] frames, Vector2 position, float scale, float fps, int order, float radius, float life,
        Func<float, Vector2, Vector2> mover, Action<Player, Vector2> onHit, bool oneHit = true, bool faceMotion = false,
        Color? tint = null, float rehitSeconds = 0.8f, Look? look = null)
    {
        var go = new GameObject("WindHazard");
        go.transform.position = position;
        var hazard = go.AddComponent<WindHazard>();
        hazard._look = look ?? default;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        if (tint.HasValue)
            renderer.color = tint.Value;
        if (frames != null && frames.Length > 0)
            visual.AddComponent<SkillFrameAnimator>().Play(frames, fps, true, UnityEngine.Random.value * frames.Length / fps);

        hazard._visual = visual.transform;
        hazard._renderer = renderer;
        hazard._baseAlpha = renderer.color.a;
        hazard._baseScale = scale;
        hazard._wobbleSeed = UnityEngine.Random.value * 10f;
        visual.transform.localScale = Vector3.one * (hazard._look.popSeconds > 0f ? 0.01f : scale);

        hazard._mover = mover;
        hazard._onHit = onHit;
        hazard._radius = radius;
        hazard._life = life;
        hazard._oneHit = oneHit;
        hazard._faceMotion = faceMotion;
        hazard._rehitCooldown = rehitSeconds;
        hazard._lastPosition = position;
        hazard.BuildExtras(order, scale);
        return hazard;
    }

    public float Age => _age;

    private void BuildExtras(int order, float scale)
    {
        if (_look.shadow)
        {
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            shadow.transform.localScale = new Vector3(_look.shadowScale * 1.3f, _look.shadowScale * 0.45f, 1f);
            _shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            _shadowRenderer.sprite = BossTelegraph.CircleSprite;
            _shadowRenderer.color = new Color(0.05f, 0.05f, 0.2f, 0.28f);
            _shadowRenderer.sortingLayerName = "Default";
            _shadowRenderer.sortingOrder = order - 5;
        }

        if (_look.trail)
        {
            var trail = _visual.gameObject.AddComponent<TrailRenderer>();
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.time = Mathf.Max(0.05f, _look.trailSeconds);
            trail.widthMultiplier = Mathf.Max(0.05f, _look.trailWidth);
            trail.minVertexDistance = 0.08f;
            trail.sortingLayerName = "Default";
            trail.sortingOrder = order - 1;
            var gradient = new Gradient();
            Color c = _look.trailColor;
            gradient.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        }

        if (_look.orbitLeaves)
            BuildOrbitLeaves(scale);
    }

    private void BuildOrbitLeaves(float scale)
    {
        var go = new GameObject("OrbitLeaves");
        go.transform.SetParent(transform, false);
        var particles = go.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.96f, 1f, 0.96f, 0.95f), new Color(0.5f, 0.82f, 0.4f, 0.95f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 40;
        var emission = particles.emission;
        emission.rateOverTime = 22f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.5f, scale * 1.1f);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(4.5f, 6.5f);
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = _renderer.sortingOrder + 1;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= _life)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 current = transform.position;
        Vector2 next = _mover != null ? _mover(_age, current) : current;
        transform.position = next;
        Vector2 delta = next - _lastPosition;
        if (_faceMotion && delta.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        _lastPosition = next;

        AnimateVisual(delta);

        if (_player == null)
            _player = FindAnyObjectByType<Player>();
        if (_player == null || _player.IsDead || (_oneHit && _hasHit) || Time.time < _nextHit)
            return;

        Vector2 playerCentre = (Vector2)_player.transform.position + Vector2.up * 0.3f;
        if (Vector2.Distance(playerCentre, next) > _radius + 0.35f)
            return;

        _hasHit = true;
        _nextHit = Time.time + _rehitCooldown;
        _onHit?.Invoke(_player, next);
        if (_oneHit)
            Destroy(gameObject);
    }

    private void AnimateVisual(Vector2 delta)
    {
        if (_visual == null)
            return;

        // pop in with a small overshoot (ease-out-back)
        float scale = _baseScale;
        if (_look.popSeconds > 0f && _age < _look.popSeconds)
        {
            float k = _age / _look.popSeconds - 1f;
            scale *= 1f + 2.70158f * k * k * k + 1.70158f * k * k;
        }

        if (_look.breathe > 0f)
            scale *= 1f + _look.breathe * Mathf.Sin(_age * 7f + _wobbleSeed);
        _visual.localScale = Vector3.one * Mathf.Max(0.01f, scale);

        if (_look.spinDegreesPerSecond != 0f)
            _visual.localRotation = Quaternion.Euler(0f, 0f, _age * _look.spinDegreesPerSecond + _wobbleSeed * 30f);

        // sideways sway of the picture, perpendicular to the way it travels
        if (_look.wobbleAmplitude > 0f)
        {
            Vector2 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
            Vector2 side = new Vector2(-direction.y, direction.x);
            float sway = Mathf.Sin(_age * Mathf.PI * 2f * Mathf.Max(0.1f, _look.wobbleHertz) + _wobbleSeed) * _look.wobbleAmplitude;
            _visual.localPosition = (Vector3)(side * sway);
            if (_look.spinDegreesPerSecond == 0f && _faceMotion)
                _visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_age * Mathf.PI * 2f * _look.wobbleHertz + _wobbleSeed) * 14f);
        }

        // fade out at the end of life
        if (_renderer != null && _look.fadeSeconds > 0f)
        {
            float remaining = _life - _age;
            Color color = _renderer.color;
            color.a = _baseAlpha * Mathf.Clamp01(remaining / _look.fadeSeconds);
            _renderer.color = color;
        }
    }
}
