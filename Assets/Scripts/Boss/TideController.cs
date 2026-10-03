using UnityEngine;

/// <summary>
/// Tide of the Water boss arena (WaterBossCombatPlan.md section 2, D-101). While the fight runs the water line
/// climbs from the low-tide shoreline and falls back on a cycle that depends on the boss phase; everything north of
/// the line is shallow water: the player is slowed there and the crab is faster. Presentation is a translucent water
/// sheet + a foam strip drawn in code (the animated sea tiles of the map stay underneath). Does nothing until
/// <see cref="BeginFight"/> (the lines stay at low tide), and nothing for arenas without this component.
/// </summary>
public sealed class TideController : MonoBehaviour
{
    public static TideController Instance { get; private set; }

    [Tooltip("World y of the shoreline at low tide (where the animated sea meets the wet sand).")]
    [SerializeField] private float _lowTideY = 8f;
    [SerializeField] private float _minX = -31.8f;
    [SerializeField] private float _maxX = 31.8f;
    [Tooltip("Top of the water sheet (somewhere above the visible arena).")]
    [SerializeField] private float _topY = 30f;
    [SerializeField] private Color _waterColor = new Color(0.2f, 0.62f, 0.9f, 0.34f);
    [SerializeField] private Color _foamColor = new Color(0.92f, 0.98f, 1f, 0.8f);

    private BossDefinition _definition;
    private int _phase;
    private bool _active;
    private float _clock;
    private Transform _sheet;
    private Transform _foam;
    private Player _player;

    public bool Active => _active;
    public float LowTideY => _lowTideY;

    /// <summary>0 = low tide, 1 = high tide.</summary>
    public float Level { get; private set; }

    public float WaterLineY { get; private set; }

    private void Awake()
    {
        Instance = this;
        WaterLineY = _lowTideY;
        BuildVisuals();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Configure(float lowTideY, float minX, float maxX)
    {
        _lowTideY = lowTideY;
        _minX = minX;
        _maxX = maxX;
        WaterLineY = lowTideY;
    }

    public void BeginFight(BossDefinition definition)
    {
        _definition = definition;
        _phase = 0;
        _clock = 0f;
        _active = true;
    }

    public void SetPhase(int phase)
    {
        _phase = Mathf.Max(0, phase);
        _clock = 0f; // a new phase restarts the cycle at low tide
    }

    public void EndFight()
    {
        _active = false;
    }

    public bool IsShallow(Vector2 point) => _active && point.y > WaterLineY;

    private void Update()
    {
        if (_active && _definition != null)
        {
            float cycle = Mathf.Max(2f, BossDefinition.PerPhase(_definition.tideCycleSecondsByPhase, _phase, 40f));
            float depth = BossDefinition.PerPhase(_definition.tideDepthByPhase, _phase, 8f);
            _clock += Time.deltaTime;
            float wave = 0.5f - 0.5f * Mathf.Cos(_clock / cycle * Mathf.PI * 2f);
            // Flood phase: the water never drains completely.
            Level = _phase >= 2 ? Mathf.Lerp(0.35f, 1f, wave) : wave;
            WaterLineY = _lowTideY - depth * Level;
        }
        else
        {
            Level = Mathf.MoveTowards(Level, 0f, Time.deltaTime * 0.5f);
            WaterLineY = _lowTideY;
        }

        UpdateVisuals();
        SlowPlayerInWater();
    }

    private void SlowPlayerInWater()
    {
        if (!_active || _definition == null)
            return;

        if (_player == null)
            _player = FindAnyObjectByType<Player>();
        if (_player != null && !_player.IsDead && IsShallow(_player.transform.position))
            _player.ApplySlow(_definition.tideShallowPlayerSpeed, 0.2f);
    }

    private void BuildVisuals()
    {
        // below the animated sea tiles (-180) so the surf and shoreline of the map stay visible; only the sand below is flooded
        _sheet = MakeBar("TideWater", _waterColor, -185);
        _foam = MakeBar("TideFoam", _foamColor, -184);
    }

    private static Transform MakeBar(string name, Color color, int order)
    {
        var go = new GameObject(name);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = BossTelegraph.SquareSprite; // 1x1 unit, pivot left-centre
        renderer.color = color;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        return go.transform;
    }

    private void UpdateVisuals()
    {
        float width = _maxX - _minX;
        if (_sheet != null)
        {
            // from the water line up to the top (only where the tide has actually risen above the low-tide shoreline)
            float height = Mathf.Max(0f, _topY - WaterLineY);
            _sheet.position = new Vector3(_minX, WaterLineY + height * 0.5f, 0f);
            _sheet.localScale = new Vector3(width, height, 1f);
            _sheet.gameObject.SetActive(Level > 0.02f);
        }

        if (_foam != null)
        {
            float wobble = Mathf.Sin(Time.time * 2.2f) * 0.12f;
            _foam.position = new Vector3(_minX, WaterLineY + wobble, 0f);
            _foam.localScale = new Vector3(width, 0.35f, 1f);
            _foam.gameObject.SetActive(Level > 0.02f);
        }
    }
}
