using UnityEngine;

/// <summary>
/// Sky motion for the Wind arena (D-110): clouds drift across the screen in front of the player and leaves/feathers float with the breeze.
/// Everything is cosmetic (no colliders), spawned relative to the main camera so it always crosses the visible area.
/// </summary>
public sealed class SkyWeather : MonoBehaviour
{
    [Header("Drifting clouds (foreground)")]
    [SerializeField] private Sprite[] _cloudSprites;
    [SerializeField] private Vector2 _cloudIntervalSeconds = new Vector2(2.5f, 6f);
    [SerializeField] private Vector2 _cloudSpeed = new Vector2(1.8f, 4.2f);
    [SerializeField] private Vector2 _cloudScale = new Vector2(1.6f, 3.4f);
    [SerializeField] private Vector2 _cloudAlpha = new Vector2(0.38f, 0.62f);
    [SerializeField, Min(0)] private int _initialClouds = 4;

    [Header("Leaves and feathers")]
    [SerializeField] private bool _leavesEnabled = true;

    private float _nextCloud;
    private float _nextLeafSteer;
    private bool _seeded;
    private ParticleSystem _leaves;
    private Camera _camera;

    private void Start()
    {
        _nextCloud = Time.time + Random.Range(_cloudIntervalSeconds.x, _cloudIntervalSeconds.y);
    }

    private void Update()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        if (!_seeded)
        {
            _seeded = true;
            for (int i = 0; i < _initialClouds; i++)
                SpawnCloud(true);
        }

        if (_leavesEnabled && _leaves == null)
            _leaves = BuildLeaves(_camera);
        else if (!_leavesEnabled && _leaves != null)
            Destroy(_leaves.gameObject);

        if (_leaves != null && ArenaWind.Instance != null && Time.time >= _nextLeafSteer)
        {
            _nextLeafSteer = Time.time + 0.5f;
            Vector2 wind = ArenaWind.Instance.Direction;
            var velocity = _leaves.velocityOverLifetime;
            velocity.x = new ParticleSystem.MinMaxCurve(wind.x * 3f, wind.x * 8f);
            velocity.y = new ParticleSystem.MinMaxCurve(wind.y * 3f - 0.4f, wind.y * 8f + 0.4f);
        }

        if (Time.time >= _nextCloud)
        {
            SpawnCloud(false);
            _nextCloud = Time.time + Random.Range(_cloudIntervalSeconds.x, _cloudIntervalSeconds.y);
        }
    }

    /// <summary>Spawns a cloud now; `inView` places it somewhere on screen (used once at the start so the sky is never empty).</summary>
    public void SpawnCloud(bool inView)
    {
        if (_camera == null || _cloudSprites == null || _cloudSprites.Length == 0)
            return;

        // the clouds drift with the arena wind (ArenaWind); without one the breeze mostly blows left to right
        float direction = ArenaWind.Instance != null
            ? (Mathf.Abs(ArenaWind.Instance.Direction.x) < 0.1f ? 1f : Mathf.Sign(ArenaWind.Instance.Direction.x))
            : (Random.value < 0.85f ? 1f : -1f);
        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;
        Vector3 center = _camera.transform.position;
        Sprite sprite = _cloudSprites[Random.Range(0, _cloudSprites.Length)];
        float scale = Random.Range(_cloudScale.x, _cloudScale.y);
        float width = sprite.bounds.size.x * scale;
        float x = inView
            ? center.x + Random.Range(-halfWidth, halfWidth)
            : center.x - direction * (halfWidth + width * 0.5f);
        float y = center.y + Random.Range(-halfHeight * 0.9f, halfHeight * 0.9f);

        var go = new GameObject("DriftCloud");
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(scale * (Random.value < 0.5f ? 1f : -1f), scale, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 95;
        // while the boss fight is on the clouds are much fainter so they never hide a telegraph or the boss
        float fightFade = ArenaWind.Instance != null && ArenaWind.Instance.Active ? 0.35f : 1f;
        renderer.color = new Color(1f, 1f, 1f, Random.Range(_cloudAlpha.x, _cloudAlpha.y) * fightFade);
        go.AddComponent<DriftingEffect>().Begin(_camera, direction * Random.Range(_cloudSpeed.x, _cloudSpeed.y), 0f, halfWidth + width * 0.5f + 2f);
    }

    private ParticleSystem BuildLeaves(Camera camera)
    {
        var go = new GameObject("DriftLeaves");
        go.transform.SetParent(camera.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 5f);

        var particles = go.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.98f, 1f, 0.98f, 0.9f), new Color(0.55f, 0.82f, 0.45f, 0.9f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 90;

        var emission = particles.emission;
        emission.rateOverTime = 9f;

        float halfHeight = camera.orthographicSize;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(halfHeight * camera.aspect * 2.4f, halfHeight * 2.2f, 1f);

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(4f, 8f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var spin = particles.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

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
