using UnityEngine;

/// <summary>
/// The wind of the Wind arena (D-111): a direction that turns every 15-20 seconds, which the clouds and leaves of
/// <see cref="SkyWeather"/> follow. It is purely cosmetic: the player is NOT pushed by it (user, 2026-10-05: stop the drifting).
/// Boss skills that shove the player (Gale Wall, Talon Dive, the screech, the storm) do it themselves in BossSkillsWind.cs.
/// </summary>
public sealed class ArenaWind : MonoBehaviour
{
    public static ArenaWind Instance { get; private set; }

    [SerializeField] private float _turnDegreesPerSecond = 40f;

    private float _targetAngle;
    private float _angle;
    private float _nextChange;
    private Vector2 _changeSeconds = new Vector2(15f, 20f);
    private bool _active;

    /// <summary>The way the wind blows right now (unit vector).</summary>
    public Vector2 Direction => new Vector2(Mathf.Cos(_angle * Mathf.Deg2Rad), Mathf.Sin(_angle * Mathf.Deg2Rad));
    public bool Active => _active;

    private void Awake()
    {
        Instance = this;
        _angle = _targetAngle = Random.Range(-25f, 25f); // mostly left to right, like the drifting clouds
        _nextChange = Time.time + Random.Range(_changeSeconds.x, _changeSeconds.y);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Called when the boss fight begins (the turning interval comes from the definition).</summary>
    public void BeginFight(BossDefinition definition)
    {
        if (definition != null)
            _changeSeconds = definition.windDirectionChangeSeconds;
        _active = true;
    }

    /// <summary>Kept for the boss controller; the wind no longer depends on the phase.</summary>
    public void SetPhase(int phase)
    {
    }

    public void EndFight() => _active = false;

    /// <summary>Kept so skills can ask for a stronger blow; it never moves the player any more.</summary>
    public void Gust(float strength, float seconds)
    {
    }

    private void Update()
    {
        if (Time.time >= _nextChange)
        {
            _targetAngle = Random.Range(0f, 360f);
            _nextChange = Time.time + Random.Range(_changeSeconds.x, _changeSeconds.y);
        }

        _angle = Mathf.MoveTowardsAngle(_angle, _targetAngle, _turnDegreesPerSecond * Time.deltaTime);
    }
}
