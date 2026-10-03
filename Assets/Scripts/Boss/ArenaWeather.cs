using UnityEngine;

/// <summary>
/// Desert-style environment motion for the Earth arena (D-087): now and then a gust of sand sweeps
/// across the screen in front of the player, a tumbleweed rolls through, and fine sand grains drift
/// with the wind. Everything is cosmetic (no colliders) and can be switched off for low quality.
/// Gusts/tumbleweeds are spawned relative to the main camera so they always cross the visible area.
/// </summary>
public sealed class ArenaWeather : MonoBehaviour
{
    [Header("Sand gust (foreground)")]
    [SerializeField] private Sprite[] _gustFrames;
    [SerializeField] private Vector2 _gustIntervalSeconds = new Vector2(20f, 50f);
    [SerializeField, Min(0f)] private float _firstGustDelay = 6f;
    [SerializeField] private Vector2 _gustSpeed = new Vector2(8f, 11f);
    [SerializeField] private float _gustScale = 1.5f;
    [SerializeField, Range(0f, 1f)] private float _gustAlpha = 0.55f;

    [Header("Tumbleweed")]
    [SerializeField] private Sprite[] _tumbleFrames;
    [SerializeField] private Vector2 _tumbleIntervalSeconds = new Vector2(40f, 90f);
    [SerializeField, Min(0f)] private float _firstTumbleDelay = 15f;
    [SerializeField] private float _tumbleSpeed = 5f;
    [SerializeField] private float _tumbleScale = 0.8f;

    [Header("Sand grains")]
    [SerializeField] private bool _grainsEnabled = true;

    private float _nextGust;
    private float _nextTumble;
    private ParticleSystem _grains;
    private Camera _camera;

    private void Start()
    {
        _nextGust = Time.time + _firstGustDelay;
        _nextTumble = Time.time + _firstTumbleDelay;
    }

    private void Update()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        if (_grainsEnabled && _grains == null)
            _grains = BuildGrains(_camera);
        else if (!_grainsEnabled && _grains != null)
            Destroy(_grains.gameObject);

        if (Time.time >= _nextGust)
        {
            SpawnGust();
            _nextGust = Time.time + Random.Range(_gustIntervalSeconds.x, _gustIntervalSeconds.y);
        }

        if (Time.time >= _nextTumble)
        {
            SpawnTumbleweed();
            _nextTumble = Time.time + Random.Range(_tumbleIntervalSeconds.x, _tumbleIntervalSeconds.y);
        }
    }

    /// <summary>Spawns a gust right now (debug / cutscenes / boss phases).</summary>
    public void SpawnGust()
    {
        if (_camera == null || _gustFrames == null || _gustFrames.Length == 0)
            return;

        float direction = Random.value < 0.5f ? -1f : 1f;
        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;
        Vector3 center = _camera.transform.position;
        float y = center.y + Random.Range(-halfHeight * 0.7f, halfHeight * 0.7f);
        var go = new GameObject("SandGust");
        go.transform.position = new Vector3(center.x - direction * (halfWidth + 18f * _gustScale * 0.5f), y, 0f);
        go.transform.localScale = new Vector3(_gustScale * direction, _gustScale, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 100;
        renderer.color = new Color(1f, 1f, 1f, _gustAlpha);
        go.AddComponent<SkillFrameAnimator>().Play(_gustFrames, 8f, true, Random.value);
        go.AddComponent<DriftingEffect>().Begin(_camera, direction * Random.Range(_gustSpeed.x, _gustSpeed.y), 0f,
            halfWidth + 24f * _gustScale);
    }

    public void SpawnTumbleweed()
    {
        if (_camera == null || _tumbleFrames == null || _tumbleFrames.Length == 0)
            return;

        float direction = Random.value < 0.5f ? -1f : 1f;
        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;
        Vector3 center = _camera.transform.position;
        float y = center.y + Random.Range(-halfHeight * 0.6f, halfHeight * 0.2f);
        var go = new GameObject("Tumbleweed");
        go.transform.position = new Vector3(center.x - direction * (halfWidth + 4f), y, 0f);
        go.transform.localScale = Vector3.one * _tumbleScale;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 0;
        go.AddComponent<SkillFrameAnimator>().Play(_tumbleFrames, 8f, true);
        go.AddComponent<DriftingEffect>().Begin(_camera, direction * _tumbleSpeed, -direction * 220f, halfWidth + 8f);
    }

    private ParticleSystem BuildGrains(Camera camera)
    {
        var go = new GameObject("SandGrains");
        go.transform.SetParent(camera.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 5f);

        var particles = go.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new Color(0.9f, 0.78f, 0.52f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;

        var emission = particles.emission;
        emission.rateOverTime = 16f;

        float halfHeight = camera.orthographicSize;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(halfHeight * camera.aspect * 2.4f, halfHeight * 2.2f, 1f);

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f); // all three axes must use the same curve mode

        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 99;
        return particles;
    }
}

/// <summary>Moves a cosmetic object across the view at a constant velocity (optionally spinning) and
/// destroys it once it is `maxOffset` beyond the camera on the far side.</summary>
public sealed class DriftingEffect : MonoBehaviour
{
    private Camera _camera;
    private float _velocityX;
    private float _spinDegreesPerSecond;
    private float _maxOffset;

    public void Begin(Camera camera, float velocityX, float spinDegreesPerSecond, float maxOffset)
    {
        _camera = camera;
        _velocityX = velocityX;
        _spinDegreesPerSecond = spinDegreesPerSecond;
        _maxOffset = maxOffset;
    }

    private void Update()
    {
        transform.position += new Vector3(_velocityX * Time.deltaTime, 0f, 0f);
        if (_spinDegreesPerSecond != 0f)
            transform.Rotate(0f, 0f, _spinDegreesPerSecond * Time.deltaTime);

        if (_camera == null)
        {
            Destroy(gameObject);
            return;
        }

        float offset = (transform.position.x - _camera.transform.position.x) * Mathf.Sign(_velocityX);
        if (offset > _maxOffset)
            Destroy(gameObject);
    }
}
