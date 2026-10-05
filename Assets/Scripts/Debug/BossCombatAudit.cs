using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Play Mode audit of the three boss kits (debug tool, not shipped logic): for every skill of a boss it casts the skill in phase 1 and phase 3 with
/// the player standing still at a fixed spot, and logs how long the skill takes, how much damage it deals, how many hit events landed and how
/// long the boss stays "busy". Results go to Logs/BossCombatAudit.txt. Add this component to any object of an arena scene in Play Mode and call
/// <see cref="Run"/>; the player is made effectively invulnerable (damage is read from a health bookkeeping delta).
/// </summary>
public sealed class BossCombatAudit : MonoBehaviour
{
    private const string LogPath = "Logs/BossCombatAudit.txt";
    private readonly StringBuilder _log = new StringBuilder();
    public bool Done { get; private set; }

    public struct Kit
    {
        public string name, prefab, definition;
        public BossSkillId[] skills;
    }

    public static readonly Kit[] Kits =
    {
        new Kit { name = "Earth Golem", prefab = "Assets/Bosses/EarthGolem/EarthGolemBoss.prefab", definition = "Assets/Bosses/EarthGolem/EarthGolemBoss.asset",
            skills = new[] { BossSkillId.Slam, BossSkillId.SlamDouble, BossSkillId.StoneRain, BossSkillId.SpikeLanes, BossSkillId.RocketFist, BossSkillId.CoreResonance } },
        new Kit { name = "Water Crab", prefab = "Assets/Bosses/WaterCrab/WaterCrabBoss.prefab", definition = "Assets/Bosses/WaterCrab/WaterCrabBoss.asset",
            skills = new[] { BossSkillId.ClawClamp, BossSkillId.BubbleTrap, BossSkillId.SandAmbush, BossSkillId.TidalWave, BossSkillId.Whirlpool } },
        new Kit { name = "Wind Owl", prefab = "Assets/Bosses/WindOwl/WindOwlBoss.prefab", definition = "Assets/Bosses/WindOwl/WindOwlBoss.asset",
            skills = new[] { BossSkillId.FeatherVolley, BossSkillId.TalonDive, BossSkillId.Cyclones, BossSkillId.GaleWall, BossSkillId.CrescentBlades, BossSkillId.SkyStorm } },
    };

    public void Run(GameObject[] prefabs, BossDefinition[] definitions, Rect bounds, Vector2[] polygon)
    {
        StartCoroutine(RunAll(prefabs, definitions, bounds, polygon));
    }

    private IEnumerator RunAll(GameObject[] prefabs, BossDefinition[] definitions, Rect bounds, Vector2[] polygon)
    {
        var player = FindAnyObjectByType<Player>();
        var stat = FindAnyObjectByType<PlayerStat>();
        const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo health = typeof(PlayerStat).GetField("_health", F);
        FieldInfo maxHealth = typeof(PlayerStat).GetField("_maxHealth", F);
        maxHealth.SetValue(stat, 1000000f);
        var body = player.GetComponent<Rigidbody2D>();
        _log.AppendLine("BossCombatAudit  playerDefense=" + stat.Defense + "  (damage = health lost by a motionless level-1 player)");

        for (int k = 0; k < Kits.Length; k++)
        {
            Kit kit = Kits[k];
            _log.AppendLine();
            _log.AppendLine("=== " + kit.name + " ===");
            foreach (int phase in new[] { 0, 2 })
            {
                foreach (BossSkillId skill in kit.skills)
                {
                    GameObject go = Instantiate(prefabs[k], new Vector3(0f, 3f, 0f), Quaternion.identity);
                    BossController boss = go.GetComponent<BossController>();
                    boss.Initialize(Instantiate(definitions[k]));
                    boss.SetArenaBounds(bounds);
                    boss.SetArenaPolygon(polygon);
                    boss.BeginEncounter(false);
                    float guard = Time.time + 15f;
                    while (boss.State != BossController.BossState.Fighting && Time.time < guard)
                        yield return null;

                    typeof(BossController).GetField("_phaseIndex", F).SetValue(boss, phase);
                    health.SetValue(stat, 1000000f);
                    body.position = new Vector2(0f, -7f);
                    player.transform.position = new Vector3(0f, -7f, 0f);
                    yield return new WaitForSeconds(0.2f);

                    float t0 = Time.time;
                    float h0 = (float)health.GetValue(stat);
                    int hits = 0;
                    float last = h0;
                    Coroutine cast = boss.CastSkill(skill);
                    float safety = t0 + 40f;
                    var activeField = typeof(BossController).GetField("_activeSkills", F);
                    while ((int)activeField.GetValue(boss) > 0 && Time.time < safety)
                    {
                        float h = (float)health.GetValue(stat);
                        if (h < last - 0.01f)
                            hits++;
                        last = h;
                        yield return null;
                    }

                    float dur = Time.time - t0;
                    float dmg = h0 - (float)health.GetValue(stat);
                    string flag = dur >= 39.5f ? "  HANG?" : "";
                    _log.AppendLine(string.Format("P{0}  {1,-14} duration {2,5:0.0}s   damage {3,5:0.0}   hits {4}{5}", phase + 1, skill, dur, dmg, hits, flag));
                    Destroy(go);
                    foreach (var h in FindObjectsByType<WindHazard>(FindObjectsSortMode.None))
                        Destroy(h.gameObject);
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }

        File.WriteAllText(LogPath, _log.ToString());
        Done = true;
        Debug.Log("BossCombatAudit finished -> " + LogPath);
    }
}
