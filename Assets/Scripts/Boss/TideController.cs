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
    [SerializeField] private Color _waterColor = new Color(0.2f, 0.62f, 0.9f, 0.34f);
    [SerializeField] private Color _foamColor = new Color(0.92f, 0.98f, 1f, 0.8f);

    private BossDefinition _definition;
    private int _phase;
    private bool _active;
    private float _clock;
    private SpriteRenderer _sheet;
    private Texture2D _sheetTexture;
    private Sprite _sheetSprite;
    private SpriteRenderer[] _edgeTiles;
    private Transform _foam;
    private Transform _edge;       // animated shoreline tiles (TideEdge_Flow) riding the water line
    private float _edgeLift;       // tile centre above the water line so its foam crest sits exactly on it
    private Player _player;
    private SfxLoopHandle _tideLoop;   // lapping waves, louder the higher the water
    private int _sfxCycle = -1;        // tide cycle index that already played its "tide coming in" cue

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
        StopTideSfx();
        if (_sheetSprite != null) Destroy(_sheetSprite);
        if (_sheetTexture != null) Destroy(_sheetTexture);
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
        _sfxCycle = -1;
        _active = true;
    }

    public void SetPhase(int phase)
    {
        _phase = Mathf.Max(0, phase);
        _clock = 0f; // a new phase restarts the cycle at low tide
        _sfxCycle = -1;
    }

    public void EndFight()
    {
        _active = false;
        StopTideSfx();
    }

    public bool IsShallow(Vector2 point) => _active && point.y > WaterLineY;

    private void Update()
    {
        float depth = _definition != null
            ? BossDefinition.PerPhase(_definition.tideDepthByPhase, _phase, 8f) : 0f;
        if (_active && _definition != null)
        {
            float cycle = Mathf.Max(2f, BossDefinition.PerPhase(_definition.tideCycleSecondsByPhase, _phase, 40f));
            _clock += Time.deltaTime;
            // Each cycle starts at low tide and rises first: one "tide coming in" cue per cycle.
            int cycleIndex = Mathf.FloorToInt(_clock / cycle);
            if (cycleIndex != _sfxCycle)
            {
                _sfxCycle = cycleIndex;
                SoundFXManager.PlaySfx(SfxIds.BosswTideRise);
            }
            float wave = 0.5f - 0.5f * Mathf.Cos(_clock / cycle * Mathf.PI * 2f);
            // Flood phase: the water never drains completely.
            Level = _phase >= 2 ? Mathf.Lerp(0.35f, 1f, wave) : wave;
        }
        else
        {
            Level = Mathf.MoveTowards(Level, 0f, Time.deltaTime * 0.5f);
        }

        WaterLineY = _lowTideY - depth * Level;
        UpdateVisuals();
        UpdateTideSfx();
        SlowPlayerInWater();
    }

    private void UpdateTideSfx()
    {
        if (!_active)
        {
            StopTideSfx();
            return;
        }
        if (_tideLoop == null || !_tideLoop.IsPlaying)
            _tideLoop = SoundFXManager.StartLoop(SfxIds.BosswTideLoop, 0f, 1f, 1f);
        _tideLoop?.SetLevel(Mathf.Lerp(0.15f, 1f, Level));
    }

    private void StopTideSfx()
    {
        _tideLoop?.Stop(1f);
        _tideLoop = null;
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
        // Overlay the base and the animated sea strips together so the tint cannot show through
        // the small spaces between strips. Fade into the sea instead of drawing a hard top edge.
        _sheet = MakeBar("TideWater", _waterColor, -179);
        const int textureSize = 128;
        _sheetTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        for (int x = 0; x < textureSize; x++)
        {
            float vertical = 1f - Mathf.SmoothStep(0f, 1f, (float)y / (textureSize - 1));
            float horizontal = Mathf.Clamp01(Mathf.Min(x, textureSize - 1 - x) / 2f);
            pixels[y * textureSize + x] = new Color(1f, 1f, 1f, vertical * horizontal);
        }
        _sheetTexture.SetPixels32(pixels);
        _sheetTexture.Apply(false, true);
        _sheetSprite = Sprite.Create(_sheetTexture, new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0f, 0.5f), textureSize, 0, SpriteMeshType.FullRect);
        _sheet.sprite = _sheetSprite;

        Sprite[] frames = BossVfx.Frames("Water/TideEdge_Flow");
        if (frames == null || frames.Length == 0)
        {
            _foam = MakeBar("TideFoam", _foamColor, -178).transform;
            UpdateVisuals();
            return;
        }

        var root = new GameObject("TideEdge");
        root.transform.SetParent(transform, false);
        int count = Mathf.Max(1, Mathf.CeilToInt((_maxX - _minX) / 3.6f));
        float tileSize = (_maxX - _minX) / count;
        float scale = tileSize / frames[0].bounds.size.x;
        float verticalScale = scale * 0.55f;
        _edgeTiles = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var tile = MakeBar("Tile" + i, Color.white, -178);
            tile.transform.SetParent(root.transform, false);
            tile.transform.localPosition = new Vector3((i + 0.5f) * tileSize, 0f, 0f);
            tile.transform.localScale = new Vector3(scale, verticalScale, 1f);
            _edgeTiles[i] = tile;
            // Adjacent tiles use the same animation frame so the crest remains continuous.
            tile.gameObject.AddComponent<SkillFrameAnimator>().Play(frames, 9f, true);
        }

        // The bottom of the foam sits 50 pixels below the centre of the 176-pixel source frame.
        _edgeLift = 50f / frames[0].pixelsPerUnit * verticalScale;
        _edge = root.transform;
        UpdateVisuals();
    }

    private SpriteRenderer MakeBar(string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = BossTelegraph.SquareSprite;
        renderer.color = color;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        return renderer;
    }

    private void UpdateVisuals()
    {
        float width = _maxX - _minX;
        float opacity = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Level / 0.18f));
        float height = Mathf.Max(0f, _lowTideY + 5f - WaterLineY);
        _sheet.transform.position = new Vector3(_minX, WaterLineY + height * 0.5f, 0f);
        _sheet.transform.localScale = new Vector3(width, height, 1f);
        _sheet.color = new Color(_waterColor.r, _waterColor.g, _waterColor.b, _waterColor.a * opacity);
        _sheet.gameObject.SetActive(opacity > 0f);

        if (_edge != null)
        {
            _edge.position = new Vector3(_minX, WaterLineY + _edgeLift, 0f);
            _edge.gameObject.SetActive(opacity > 0f);
            foreach (SpriteRenderer tile in _edgeTiles)
                tile.color = new Color(1f, 1f, 1f, _foamColor.a * opacity);
        }
        else if (_foam != null)
        {
            _foam.position = new Vector3(_minX, WaterLineY + 0.125f, 0f);
            _foam.localScale = new Vector3(width, 0.25f, 1f);
            _foam.GetComponent<SpriteRenderer>().color = new Color(_foamColor.r, _foamColor.g, _foamColor.b, _foamColor.a * opacity);
            _foam.gameObject.SetActive(opacity > 0f);
        }
    }
}
