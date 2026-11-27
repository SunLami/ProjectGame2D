using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Play Mode balance probe (debug tool, not shipped logic, companion of <see cref="BossCombatAudit"/>): drives the
/// player through a whole boss fight. The bot reacts to red/amber telegraphs after a human-like reaction delay
/// (runs out of circles and lanes, dashes when the exit is far), walks up to the boss and swings whenever stamina
/// allows, and casts a tier-1 skill when mana allows. Player health is replaced by a large pool so the fight always
/// finishes; the log reports health lost per skill, telegraph lengths, recovery windows, phase times and time-to-kill.
/// Output: Logs/BossFightBot_&lt;boss&gt;.txt. Add the component in Play Mode, set the fields, call <see cref="Begin"/>.
/// </summary>
public sealed class BossFightBot : MonoBehaviour
{
    [Header("Simulated player")]
    public float attackDamage = 12f;          // level 1 + Training Sword
    public float defense = 2f;
    public float damageReduction;
    public float referenceMaxHealth = 100f;   // used only to express damage as "player health bars"
    public float tier1SkillDamage = 18f;      // average of the three tier-1 skills (15 / 22 / 14+10)
    public float reactionSeconds = 0.35f;
    public bool dodge = true;
    public bool fight = true;
    public float timeScale = 2f;
    public float timeoutSeconds = 900f;

    public bool Done { get; private set; }
    public string Summary { get; private set; } = "";

    private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
    private const float Pool = 1000000f;

    private Player _player;
    private PlayerStat _stat;
    private Rigidbody2D _body;
    private BossArenaController _arena;
    private BossController _boss;
    private FieldInfo _health, _moveInput, _running;
    private Rect _bounds;
    private Vector2[] _polygon;

    private float _start = -1f, _fightStart = -1f, _nextSwing, _nextSkill, _lastHealth;
    private string _currentSkill = "(none)";
    private readonly Dictionary<string, float> _damageBySkill = new Dictionary<string, float>();
    private readonly Dictionary<string, int> _hitsBySkill = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _castsBySkill = new Dictionary<string, int>();
    private readonly Dictionary<string, List<float>> _telegraphs = new Dictionary<string, List<float>>();
    private readonly HashSet<BossTelegraphVisual> _seenTelegraphs = new HashSet<BossTelegraphVisual>();
    private readonly StringBuilder _timeline = new StringBuilder();
    private float _bossDamageDealt, _recoveryTotal, _swingDamage, _skillDamage;
    private int _swings, _skills, _dashes, _recoveries;

    public void Begin()
    {
        _player = FindAnyObjectByType<Player>();
        _stat = FindAnyObjectByType<PlayerStat>();
        _body = _player.GetComponent<Rigidbody2D>();
        _arena = FindAnyObjectByType<BossArenaController>();
        _health = typeof(PlayerStat).GetField("_health", F);
        _moveInput = typeof(Player).GetField("_moveInput", F);
        _running = typeof(Player).GetField("_isRunning", F);
        typeof(PlayerStat).GetField("_maxHealth", F).SetValue(_stat, Pool);
        typeof(PlayerStat).GetField("_attackDamage", F).SetValue(_stat, attackDamage - (_stat.AttackDamage - _stat.BaseAtkDmg));
        typeof(PlayerStat).GetField("_defense", F).SetValue(_stat, defense - (_stat.Defense - (float)typeof(PlayerStat).GetField("_defense", F).GetValue(_stat)));
        typeof(PlayerStat).GetField("_damageReduction", F).SetValue(_stat, damageReduction);
        _health.SetValue(_stat, Pool);
        _lastHealth = Pool;
        _bounds = (Rect)typeof(BossArenaController).GetField("_walkableBounds", F).GetValue(_arena);
        _polygon = (Vector2[])typeof(BossArenaController).GetField("_walkablePolygon", F).GetValue(_arena);
        if (_polygon != null && _polygon.Length < 3)
            _polygon = null;
        _start = Time.time;
        _arena.TrySummon();
    }

    private void Update()
    {
        if (_start < 0f || Done)
            return;

        if (Mathf.Approximately(Time.timeScale, 1f))
            Time.timeScale = timeScale;

        if (_boss == null && _arena.Boss != null)
            Hook(_arena.Boss);

        TrackDamage();
        TrackTelegraphs();
        if (_boss != null && _boss.State != BossController.BossState.Dormant && _boss.State != BossController.BossState.Spawning)
            Drive();

        if (Time.time - _start > timeoutSeconds)
            Finish("TIMEOUT");
    }

    private void Hook(BossController boss)
    {
        _boss = boss;
        _fightStart = Time.time;
        boss.SkillStarted += id =>
        {
            _currentSkill = id.ToString();
            Count(_castsBySkill, _currentSkill, 1);
            _timeline.AppendLine(string.Format("{0,6:0.0}s  skill {1,-15} bossHP {2,5:0}  phase {3}", T, id, boss.Health, boss.PhaseIndex + 1));
        };
        boss.RecoveryStarted += s =>
        {
            _recoveries++;
            _recoveryTotal += s;
            _currentSkill = "(recovery)";
            _timeline.AppendLine(string.Format("{0,6:0.0}s  recovery {1:0.0}s", T, s));
        };
        boss.PhaseChanged += p => _timeline.AppendLine(string.Format("{0,6:0.0}s  PHASE {1}  bossHP {2:0}", T, p + 1, boss.Health));
        boss.Died += () => Finish("BOSS DEFEATED");
    }

    private float T => Time.time - (_fightStart >= 0f ? _fightStart : _start);

    private void TrackDamage()
    {
        float h = (float)_health.GetValue(_stat);
        if (h < _lastHealth - 0.01f)
        {
            Count(_damageBySkill, _currentSkill, _lastHealth - h);
            Count(_hitsBySkill, _currentSkill, 1);
        }

        _lastHealth = h;
    }

    private void TrackTelegraphs()
    {
        foreach (BossTelegraphVisual v in FindObjectsByType<BossTelegraphVisual>(FindObjectsInactive.Exclude))
        {
            if (_seenTelegraphs.Add(v))
            {
                if (!_telegraphs.TryGetValue(_currentSkill, out List<float> list))
                    _telegraphs[_currentSkill] = list = new List<float>();
                list.Add(v.Duration);
            }
        }
    }

    // ------------------------------------------------------------------ behaviour

    private void Drive()
    {
        Vector2 me = _body.position;
        Vector2 move = Vector2.zero;
        bool danger = false;
        if (dodge && InDanger(me, 0.4f))
        {
            danger = true;
            if (FindSafe(me, out Vector2 safe))
            {
                move = (safe - me).normalized;
                if ((safe - me).magnitude > 1.6f && _stat.Stamina >= 25f)
                {
                    _moveInput.SetValue(_player, move);
                    if (_player.TryStartDash())
                        _dashes++;
                }
            }
        }
        else if (fight && _boss.State == BossController.BossState.Fighting)
        {
            float reach = Reach(me);
            if (reach > 1.2f)
            {
                Vector2 approach = ((Vector2)_boss.transform.position - me).normalized;
                if (!dodge || !InDanger(me + approach * 1.2f, 0.4f))
                    move = approach;
            }
        }

        _moveInput.SetValue(_player, move);
        _running.SetValue(_player, move != Vector2.zero);

        if (!fight || _boss.State != BossController.BossState.Fighting || _player.IsDashing)
            return;

        float r = Reach(me);
        if (!danger && r <= 1.6f && Time.time >= _nextSwing && _stat.Stamina >= 15f && _stat.TryConsumeAttackStamina())
        {
            _nextSwing = Time.time + 0.8f; // attack clip length
            float before = _boss.Health;
            _boss.TakeDamage(_stat.RollOutgoingDamage(out _), Vector2.zero, 0f);
            _swingDamage += before - _boss.Health;
            _swings++;
        }

        if (r <= 8f && Time.time >= _nextSkill && _stat.TryConsumeMana(8f))
        {
            _nextSkill = Time.time + 2f; // tier-1 cooldown
            float before = _boss.Health;
            _boss.TakeDamage(tier1SkillDamage, Vector2.zero, 0f);
            _skillDamage += before - _boss.Health;
            _skills++;
        }
    }

    private float Reach(Vector2 me)
    {
        float best = Vector2.Distance(me, _boss.transform.position);
        foreach (Collider2D c in _boss.GetComponents<Collider2D>())
            best = Mathf.Min(best, Vector2.Distance(me, c.ClosestPoint(me)));
        return best;
    }

    private bool InDanger(Vector2 p, float margin)
    {
        foreach (BossTelegraphVisual v in FindObjectsByType<BossTelegraphVisual>(FindObjectsInactive.Exclude))
        {
            if (v.Elapsed < reactionSeconds)
                continue;
            Transform back = v.transform.Find("Back");
            if (back == null)
                continue;
            if (v.name.StartsWith("BossTelegraphCircle"))
            {
                if (Vector2.Distance(p, v.transform.position) < back.localScale.x * 0.5f + margin)
                    return true;
            }
            else if (v.name.StartsWith("BossTelegraphLine"))
            {
                Vector2 local = v.transform.InverseTransformPoint(p);
                if (local.x > -margin && local.x < back.localScale.x + margin && Mathf.Abs(local.y) < back.localScale.y * 0.5f + margin)
                    return true;
            }
        }

        return false;
    }

    private bool FindSafe(Vector2 me, out Vector2 best)
    {
        best = me;
        float bestScore = float.MaxValue;
        for (int ring = 1; ring <= 8; ring++)
        {
            float d = ring * 1.1f;
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                Vector2 p = me + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                if (!Walkable(p) || InDanger(p, 0.6f))
                    continue;
                float score = d + 0.15f * Vector2.Distance(p, _boss.transform.position) * (fight ? 1f : -1f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            if (bestScore < float.MaxValue)
                return true;
        }

        return false;
    }

    private bool Walkable(Vector2 p)
    {
        if (!_bounds.Contains(p))
            return false;
        return _polygon == null || ArenaPolygon.Contains(_polygon, p);
    }

    // ------------------------------------------------------------------ report

    private static void Count(Dictionary<string, float> d, string key, float add)
    {
        d.TryGetValue(key, out float v);
        d[key] = v + add;
    }

    private static void Count(Dictionary<string, int> d, string key, int add)
    {
        d.TryGetValue(key, out int v);
        d[key] = v + add;
    }

    private void Finish(string reason)
    {
        if (Done)
            return;
        Done = true;
        Time.timeScale = 1f;
        _moveInput.SetValue(_player, Vector2.zero);

        float ttk = T;
        float taken = Pool - (float)_health.GetValue(_stat);
        var sb = new StringBuilder();
        string bossName = _arena != null ? _arena.BossDisplayName : "boss";
        sb.AppendLine(string.Format("BossFightBot  {0}  result: {1}", bossName, reason));
        sb.AppendLine(string.Format("player atk {0} def {1} dr {7} refHP {2} skill {3} dodge {4} fight {5} reaction {6}s",
            attackDamage, defense, referenceMaxHealth, tier1SkillDamage, dodge, fight, reactionSeconds, damageReduction));
        sb.AppendLine(string.Format("time-to-kill {0:0.0}s | damage taken {1:0} = {2:0.00} x refHP | boss dmg: swings {3} ({4:0}), skills {5} ({6:0}) | dashes {7} | recoveries {8} totalling {9:0.0}s ({10:0}% of fight)",
            ttk, taken, taken / referenceMaxHealth, _swings, _swingDamage, _skills, _skillDamage, _dashes, _recoveries, _recoveryTotal, 100f * _recoveryTotal / Mathf.Max(1f, ttk)));
        sb.AppendLine(string.Format("player DPS on boss {0:0.0}", (_swingDamage + _skillDamage) / Mathf.Max(1f, ttk)));
        sb.AppendLine();
        sb.AppendLine("skill            casts  dmgTaken  hits  telegraphs(s)");
        foreach (KeyValuePair<string, int> kv in _castsBySkill)
        {
            _damageBySkill.TryGetValue(kv.Key, out float dmg);
            _hitsBySkill.TryGetValue(kv.Key, out int hits);
            string tele = "";
            if (_telegraphs.TryGetValue(kv.Key, out List<float> list))
            {
                list.Sort();
                tele = string.Format("n={0} min {1:0.00} med {2:0.00} max {3:0.00}", list.Count, list[0], list[list.Count / 2], list[list.Count - 1]);
            }

            sb.AppendLine(string.Format("{0,-16} {1,5}  {2,8:0.0}  {3,4}  {4}", kv.Key, kv.Value, dmg, hits, tele));
        }

        _damageBySkill.TryGetValue("(recovery)", out float recDmg);
        sb.AppendLine(string.Format("{0,-16} {1,5}  {2,8:0.0}", "(recovery)", "", recDmg));
        sb.AppendLine();
        sb.Append(_timeline);
        Summary = sb.ToString();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/BossFightBot_" + bossName.Replace(' ', '_') + ".txt", Summary);
        Debug.Log("BossFightBot finished: " + reason);
    }
}
