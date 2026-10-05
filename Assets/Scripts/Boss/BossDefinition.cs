using System;
using UnityEngine;

public enum BossSkillId
{
    Slam,
    SlamDouble,
    StoneRain,
    SpikeLanes,
    RocketFist,
    CoreResonance,
    // Water crab (D-101)
    ClawClamp,
    BubbleTrap,
    SandAmbush,
    TidalWave,
    Whirlpool,
    // Wind owl (D-111)
    FeatherVolley,
    TalonDive,
    Cyclones,
    GaleWall,
    CrescentBlades,
    SkyStorm,
}

[Serializable]
public sealed class BossComboStep
{
    public BossSkillId skill;
    [Tooltip("Start the next step at the same time instead of waiting for this one to finish.")]
    public bool parallelWithNext;

    public BossComboStep() { }

    public BossComboStep(BossSkillId skill, bool parallelWithNext = false)
    {
        this.skill = skill;
        this.parallelWithNext = parallelWithNext;
    }
}

[Serializable]
public sealed class BossCombo
{
    public string name = "Combo";
    public BossComboStep[] steps = Array.Empty<BossComboStep>();
}

[Serializable]
public sealed class BossPhase
{
    public string name = "Phase";
    [Tooltip("The phase becomes active once boss health fraction is at or below this value (the first phase uses 1).")]
    [Range(0f, 1f)] public float startsBelowHealth = 1f;
    [Tooltip("Multiplier on every telegraph time (<1 = faster, harder).")]
    [Min(0.1f)] public float telegraphScale = 1f;
    [Tooltip("Telegraph times are never shortened below this.")]
    [Min(0.1f)] public float telegraphMinSeconds = 0.6f;
    [Min(0f)] public float recoverySeconds = 3f;
    [Min(0f)] public float gapBetweenSkills = 0.6f;
    [Tooltip("Run the Core Resonance ultimate after this many combos (0 = never).")]
    [Min(0)] public int ultimateEveryCombos;
    public BossCombo[] combos = Array.Empty<BossCombo>();
}

/// <summary>Data for one boss encounter (D-086): health, damage scale, phases/combos and the tuning of
/// every skill. Damage values are multipliers of <see cref="baseDamage"/>; the numbers are starting
/// values to be rebalanced here (one place) once the boss is playable.</summary>
[CreateAssetMenu(menuName = "Project Game/Boss/Boss Definition", fileName = "BossDefinition")]
public sealed class BossDefinition : ScriptableObject
{
    [Header("Identity")]
    public string bossId = "boss.earth_golem";
    public string displayName = "Earth Golem"; // UI font has no extended Vietnamese glyphs (Typography.md)

    [Header("Core stats")]
    [Min(1f)] public float maxHealth = 2400f;
    [Tooltip("Base damage B; every skill multiplies it.")]
    [Min(0f)] public float baseDamage = 14f;
    [Min(0f)] public float moveSpeed = 2.4f;
    [Tooltip("Boss damage taken multiplier while recovering after a combo.")]
    [Min(1f)] public float recoveryDamageTaken = 1.25f;
    [Tooltip("Same, but after the ultimate.")]
    [Min(1f)] public float ultimateRecoveryDamageTaken = 1.5f;
    [Tooltip("Same, while the Rocket Fist is away from the body.")]
    [Min(1f)] public float fistAwayDamageTaken = 1.15f;
    [Min(0f)] public float spawnFadeSeconds = 2.5f;
    [Min(0f)] public float phaseTransitionSeconds = 2f;
    [Tooltip("Global difficulty scale for repeat farming runs (1 = normal): multiplies damage.")]
    [Min(0.1f)] public float difficultyDamageScale = 1f;

    [Tooltip("VFX set (Resources/VFX/Skills/<name>) for dust while appearing and dying. Plain names are Earth, 'Water/Name' addresses another element.")]
    public string impactVfx = "RockProjectile_Impact";
    [Tooltip("VFX set pulsed on the floor when the boss roars into its next phase.")]
    public string phaseVfx = "EarthRune_Pulse";
    [Tooltip("Skill run after `ultimateEveryCombos` combos (Core Resonance for Earth, Whirlpool for the Water crab).")]
    public BossSkillId ultimateSkill = BossSkillId.CoreResonance;

    [Header("Phases")]
    public BossPhase[] phases = Array.Empty<BossPhase>();

    [Header("Skill 1 - Crushing Fist (Slam)")]
    public float slamRadius = 3.6f;
    public float slamWindup = 1f;
    public float slamLockSeconds = 0.15f;
    public float slamDamage = 1f;
    public float slamKnockback = 6f;
    public float slamDoubleDelay = 0.5f;
    public float slamDoubleWindup = 0.7f;

    [Header("Skill 2 - Stone Rain")]
    public int[] rainCountByPhase = { 6, 9, 12 };
    public float rainRadius = 2f;
    public float rainLead = 0.9f;
    public float rainSpacing = 0.22f;
    public float rainDamage = 0.7f;
    public float rainKnockback = 4f;
    public float rainPredictSeconds = 0.6f;
    public float rainSpreadRadius = 9f;
    public float rainMinSpacing = 2.4f;

    [Header("Skill 3 - Spike Lanes")]
    public int[] laneCountByPhase = { 3, 5, 7 };
    [Tooltip("Angle between neighbouring lanes. 22 degrees keeps a walkable gap (>= ~1.6 units) 8 units from the boss.")]
    public float laneStepDegrees = 22f;
    public float laneWidth = 1.4f;
    public float laneLength = 22f;
    public float laneTelegraph = 0.9f;
    public float laneDamage = 0.9f;
    public float laneKnockback = 3f;
    [Tooltip("Spikes erupt along each lane at this speed (units/second).")]
    public float laneSpreadSpeed = 40f;

    [Header("Skill 4 - Rocket Fist")]
    public float fistLineWidth = 2f;
    public float fistLineLength = 26f;
    public float fistWindup = 0.8f;
    public float fistSpeed = 24f;
    public float fistDamage = 1.3f;
    public float fistReturnDamage = 0.6f;
    public float fistKnockback = 7f;
    public float fistHoverSeconds = 1.6f;
    public float fistReturnSpeed = 18f;

    [Header("Skill 5 - Core Resonance (ultimate)")]
    public float resonanceChargeSeconds = 1.8f;
    public int[] resonanceRingsByPhase = { 3, 3, 4 };
    public float resonanceRingInterval = 0.9f;
    public float resonanceRingSpeed = 9f;
    public float resonanceRingThickness = 1.1f;
    public float[] resonanceGapDegreesByPhase = { 40f, 40f, 34f };
    public float resonanceGapMarkerLead = 0.6f;
    public float resonanceDamage = 1.1f;
    public float resonanceKnockback = 5f;
    public float resonanceMaxRadius = 40f;

    [Header("Skill 5 - Core Resonance: 360° rock burst (D-095, replaces the expanding rings)")]
    [Tooltip("Volleys fired in a row per phase. Each volley shoots `bullets` rocks evenly around the boss; every second volley is rotated by half a step so the safe lanes alternate.")]
    public int[] resonanceBurstVolleysByPhase = { 4, 5, 6 };
    public int[] resonanceBurstBulletsByPhase = { 20, 24, 28 };
    public float resonanceBurstSpeed = 8f;
    [Tooltip("Hit radius of one rock (the player's own radius is added).")]
    public float resonanceBurstHitRadius = 0.45f;
    [Tooltip("Damage multiplier of one rock (x B). Lower than the old ring because several volleys can connect.")]
    public float resonanceBurstDamage = 0.8f;
    [Tooltip("Seconds the red spokes of a volley are shown before the rocks fly.")]
    public float resonanceBurstTelegraphSeconds = 0.5f;
    public float ultimateRecoverySeconds = 4.5f;

    // ------------------------------------------------------------------ Water crab (WaterBossCombatPlan.md, D-101)
    [Header("Water - tide")]
    [Tooltip("Seconds for one full tide cycle (low -> high -> low) per phase.")]
    public float[] tideCycleSecondsByPhase = { 40f, 20f, 20f };
    [Tooltip("How far (world units) the water line climbs from the low-tide shoreline at high tide, per phase.")]
    public float[] tideDepthByPhase = { 8f, 8f, 17f };
    [Tooltip("Player speed multiplier in shallow water.")]
    [Range(0.1f, 1f)] public float tideShallowPlayerSpeed = 0.6f;
    [Tooltip("Crab speed multiplier in shallow water.")]
    [Min(1f)] public float tideCrabSpeedMultiplier = 2f;

    [Header("Water skill 1 - Claw Clamp")]
    public float clampRadius = 5f;
    public float clampArcDegrees = 100f;
    public float clampWindup = 0.9f;
    public float clampSecondDelay = 0.5f;
    public float clampSecondRotateDegrees = 25f;
    public float clampDamage = 1f;
    public float clampKnockback = 5f;
    [Tooltip("Seconds the player is held in place when two cuts in a row connect (D-103).")]
    public float clampRootSeconds = 0.6f;
    [Tooltip("D-106: the crab chases the player and cuts this many times per Claw Clamp (per phase).")]
    public int[] clampCutsByPhase = { 3, 3, 4 };
    [Tooltip("Speed of the crab while it runs the player down between cuts.")]
    public float clampChaseSpeed = 7f;
    [Tooltip("Damage multiplier of one chasing cut (there are several per skill).")]
    public float clampCutDamage = 0.8f;

    [Header("Water skill 2 - Bubble rings (D-106)")]
    [Tooltip("Bubbles in wave 1, 2 and 3 for phase 1 / 2 / 3 (a full ring of 360 degrees each).")]
    public int[] bubbleRingWave1ByPhase = { 8, 10, 12 };
    public int[] bubbleRingWave2ByPhase = { 14, 18, 22 };
    public int[] bubbleRingWave3ByPhase = { 20, 26, 32 };
    [Tooltip("Seconds between two waves.")]
    public float bubbleWaveInterval = 1.1f;
    [Tooltip("Size and damage of a ring bubble relative to a normal bubble.")]
    public float bubbleRingScale = 0.65f;

    [Header("Water skill 2 - Bubble Trap")]
    public int[] bubbleCountByPhase = { 3, 4, 5 };
    public float bubbleSpeed = 3f;
    public float bubbleHitRadius = 1f;
    public float bubbleLifeSeconds = 9f;
    public float bubbleSpawnInterval = 0.45f;
    public float bubblePredictSeconds = 0.8f;
    public float bubbleDamage = 0.9f;
    public float bubbleKnockback = 5f;

    [Header("Water skill 3 - Sand Ambush")]
    public float ambushSinkSeconds = 0.7f;
    public float ambushChaseSeconds = 2f;
    public float ambushChaseSpeed = 6f;
    public float ambushRadius = 3.2f;
    public float ambushWarnSeconds = 0.8f;
    public float ambushDamage = 1.2f;
    public float ambushKnockback = 8f;
    public float ambushStandSeconds = 0.8f;

    [Header("Water skill 4 - Tidal Wave")]
    public float waveWarnSeconds = 1.5f;
    public int[] waveGapCountByPhase = { 2, 2, 1 };
    public float[] waveGapWidthByPhase = { 4.5f, 4.5f, 4f };
    public float waveSpeed = 14f;
    public float waveThickness = 1.2f;
    public float waveDamage = 1.2f;
    public float wavePushDistance = 3f;

    [Header("Water skill 5 - Whirlpool (ultimate)")]
    public float whirlRadius = 9f;
    public float whirlWarnSeconds = 2f;
    public float whirlPullSeconds = 3f;
    public float whirlPullSpeed = 3.5f;
    public float whirlCoreRadius = 2.5f;
    public float whirlTickDamage = 0.5f;
    public float whirlTickInterval = 0.5f;
    public int whirlJetCount = 4;
    public float whirlJetSeconds = 2f;
    public float whirlJetLength = 12f;
    public float whirlJetWidth = 1.6f;
    [Tooltip("D-107: extra jets in phase 2 / 3 on top of whirlJetCount.")]
    public int[] whirlExtraJetsByPhase = { 0, 1, 2 };
    [Tooltip("Seconds the jet lanes are shown (and the pull keeps going) before the jets lash out.")]
    public float whirlJetTelegraphSeconds = 0.9f;
    [Tooltip("Share of the pull speed that keeps dragging the player while the jets spin.")]
    [Range(0f, 1f)] public float whirlPullDuringJets = 0.45f;
    public float whirlJetSpinDegreesPerSecond = 120f;
    public float whirlJetDamage = 0.8f;


    [Header("Wind - cosmetic wind direction (ArenaWind, D-111; the player is not pushed by it)")]
    [Tooltip("The wind turns to a new direction every random(min, max) seconds.")]
    public Vector2 windDirectionChangeSeconds = new Vector2(15f, 20f);
    [Tooltip("Damage multiplier when the player is pushed hard into the platform edge or an obstacle.")]
    public float windImpactDamage = 0.4f;

    [Header("Wind skill 1 - Feather Volley")]
    public int[] featherCountByPhase = { 9, 15, 21 };
    public float featherFanDegrees = 60f;
    public float featherWindup = 0.7f;
    public float featherSpeed = 11f;
    public float featherHitRadius = 0.45f;
    public float featherLifeSeconds = 3.2f;
    public float featherDamage = 0.5f;
    [Tooltip("Degrees per second a feather turns toward the player during its first second.")]
    public float featherHomingDegrees = 25f;
    [Tooltip("Phase 2+ adds a second fan crossing the first; phase 3 turns the volley into a spiral.")]
    public bool[] featherSecondFanByPhase = { false, true, true };
    public bool[] featherSpiralByPhase = { false, false, true };

    [Header("Wind skill 2 - Talon Dive")]
    public float diveTakeoffSeconds = 0.9f;
    public float diveShadowSeconds = 1.4f;
    [Tooltip("The target stops following the player this long before the strike.")]
    public float diveLockSeconds = 0.4f;
    public float diveRadius = 3.5f;
    public float diveDamage = 1f;
    public float diveKnockback = 4f;
    public int[] diveTargetsByPhase = { 1, 1, 3 };
    [Tooltip("Seconds the owl stays perched (vulnerable) after the last strike.")]
    public float[] divePerchByPhase = { 2f, 2f, 1.3f };

    [Header("Wind skill 3 - Cyclones")]
    public int[] cycloneCountByPhase = { 2, 3, 3 };
    public float cycloneWarnSeconds = 0.8f;
    public float cycloneLifeSeconds = 6f;
    public float cycloneSpeed = 3f;
    public float cycloneHitRadius = 1.1f;
    public float cycloneDamage = 0.8f;
    public float cycloneStunSeconds = 0.5f;

    [Header("Wind skill 4 - Gale Wall")]
    public float wallWarnSeconds = 1.5f;
    public float wallSpeed = 14f;
    public float wallThickness = 1.6f;
    public float wallDamage = 0.8f;
    [Tooltip("Units per second the wall shoves the player along while it overlaps them.")]
    public float wallPushSpeed = 9f;
    [Tooltip("Length of the safe shadow behind an obstacle (the wind totems).")]
    public float wallShelterLength = 6f;
    public float wallShelterHalfWidth = 1.9f;

    [Header("Wind skill 5 - Crescent Blades")]
    public int[] crescentCountByPhase = { 2, 2, 3 };
    public float crescentWarnSeconds = 0.9f;
    public float crescentSpeed = 13f;
    public float crescentHitRadius = 0.9f;
    public float crescentDamage = 1f;

    [Header("Wind skill 6 - Sky Storm (ultimate)")]
    [Tooltip("Seconds each collapsing floor tier flickers before it breaks.")]
    public float stormTierWarnSeconds = 1.2f;
    public float stormBrokenSeconds = 3.2f;
    public float stormTickDamage = 0.5f;
    public float stormTickInterval = 0.5f;
    public float stormPushSpeed = 5f;
    public float stormDimAlpha = 0.45f;
    [Tooltip("Radii of the safe 'eye' after each floor collapse (the whole platform outside it breaks, outermost first). Replaces the rect tiers below.")]
    public float[] stormEyeRadii = { 17f, 13f, 9.5f, 6.5f };
    [Tooltip("Obsolete since the eye version (kept so existing assets still load): floor tiers that break one after another, outermost first (world rects). Default: the two horizontal arms of the Wind arena.")]
    public Rect[] stormTierRects =
    {
        new Rect(-31.65f, -11.6f, 5.65f, 17.6f), new Rect(22.95f, -11.6f, 6f, 17.6f),
        new Rect(-26f, -11.6f, 6f, 17.6f), new Rect(17f, -11.6f, 5.95f, 17.6f),
        new Rect(-20f, -11.6f, 6.55f, 17.6f), new Rect(13.45f, -11.6f, 3.55f, 17.6f),
    };

    public int PhaseIndexForHealthFraction(float fraction)
    {
        int index = 0;
        for (int i = 0; i < phases.Length; i++)
        {
            if (fraction <= phases[i].startsBelowHealth + 0.0001f)
                index = i;
        }

        return index;
    }

    public static T PerPhase<T>(T[] values, int phaseIndex, T fallback)
    {
        if (values == null || values.Length == 0)
            return fallback;

        return values[Mathf.Clamp(phaseIndex, 0, values.Length - 1)];
    }

    /// <summary>The default Earth golem encounter of EarthBossCombatPlan.md section 5.</summary>
    public static BossDefinition CreateEarthGolemDefaults()
    {
        var definition = CreateInstance<BossDefinition>();
        definition.name = "EarthGolemBoss";
        definition.phases = new[]
        {
            new BossPhase
            {
                name = "Phase 1", startsBelowHealth = 1f, telegraphScale = 1f, recoverySeconds = 3f,
                gapBetweenSkills = 0.6f, ultimateEveryCombos = 0,
                combos = new[]
                {
                    Combo("A", new BossComboStep(BossSkillId.Slam), new BossComboStep(BossSkillId.SpikeLanes)),
                    Combo("B", new BossComboStep(BossSkillId.StoneRain), new BossComboStep(BossSkillId.Slam)),
                },
            },
            new BossPhase
            {
                name = "Phase 2", startsBelowHealth = 0.65f, telegraphScale = 0.9f, recoverySeconds = 2.5f,
                gapBetweenSkills = 0.5f, ultimateEveryCombos = 3,
                combos = new[]
                {
                    Combo("C", new BossComboStep(BossSkillId.RocketFist), new BossComboStep(BossSkillId.SpikeLanes), new BossComboStep(BossSkillId.Slam)),
                    Combo("D", new BossComboStep(BossSkillId.StoneRain), new BossComboStep(BossSkillId.RocketFist)),
                    Combo("E", new BossComboStep(BossSkillId.SlamDouble), new BossComboStep(BossSkillId.SpikeLanes)),
                },
            },
            new BossPhase
            {
                name = "Phase 3", startsBelowHealth = 0.30f, telegraphScale = 0.8f, recoverySeconds = 1.8f,
                gapBetweenSkills = 0.4f, ultimateEveryCombos = 2,
                combos = new[]
                {
                    Combo("F", new BossComboStep(BossSkillId.StoneRain, true), new BossComboStep(BossSkillId.SpikeLanes), new BossComboStep(BossSkillId.RocketFist)),
                    Combo("G", new BossComboStep(BossSkillId.CoreResonance), new BossComboStep(BossSkillId.Slam), new BossComboStep(BossSkillId.Slam)),
                },
            },
        };
        return definition;
    }

    /// <summary>The Water crab encounter of WaterBossCombatPlan.md section 5.</summary>
    public static BossDefinition CreateWaterCrabDefaults()
    {
        var definition = CreateInstance<BossDefinition>();
        definition.name = "WaterCrabBoss";
        definition.bossId = "boss.water_crab";
        definition.displayName = "Tidal Crab";
        definition.moveSpeed = 2.6f;
        definition.ultimateSkill = BossSkillId.Whirlpool;
        definition.impactVfx = "Water/WaterRainSplash_Hit";
        definition.phaseVfx = "Water/WaterGatherRing_Swirl";
        definition.phases = new[]
        {
            new BossPhase
            {
                name = "Phase 1", startsBelowHealth = 1f, telegraphScale = 1f, telegraphMinSeconds = 0.8f, recoverySeconds = 3f,
                gapBetweenSkills = 0.6f, ultimateEveryCombos = 0,
                combos = new[]
                {
                    Combo("A", new BossComboStep(BossSkillId.ClawClamp), new BossComboStep(BossSkillId.BubbleTrap)),
                    Combo("B", new BossComboStep(BossSkillId.SandAmbush), new BossComboStep(BossSkillId.TidalWave)),
                },
            },
            new BossPhase
            {
                name = "Phase 2", startsBelowHealth = 0.65f, telegraphScale = 0.9f, telegraphMinSeconds = 0.8f, recoverySeconds = 2.5f,
                gapBetweenSkills = 0.5f, ultimateEveryCombos = 3,
                combos = new[]
                {
                    Combo("C", new BossComboStep(BossSkillId.ClawClamp), new BossComboStep(BossSkillId.SandAmbush), new BossComboStep(BossSkillId.BubbleTrap)),
                    Combo("D", new BossComboStep(BossSkillId.TidalWave, true), new BossComboStep(BossSkillId.BubbleTrap)),
                    Combo("E", new BossComboStep(BossSkillId.SandAmbush), new BossComboStep(BossSkillId.SandAmbush), new BossComboStep(BossSkillId.ClawClamp)),
                },
            },
            new BossPhase
            {
                name = "Phase 3 - Flood", startsBelowHealth = 0.30f, telegraphScale = 0.8f, telegraphMinSeconds = 0.6f, recoverySeconds = 1.8f,
                gapBetweenSkills = 0.4f, ultimateEveryCombos = 2,
                combos = new[]
                {
                    Combo("F", new BossComboStep(BossSkillId.BubbleTrap, true), new BossComboStep(BossSkillId.SandAmbush), new BossComboStep(BossSkillId.ClawClamp)),
                    Combo("G", new BossComboStep(BossSkillId.Whirlpool), new BossComboStep(BossSkillId.ClawClamp), new BossComboStep(BossSkillId.ClawClamp)),
                },
            },
        };
        return definition;
    }


    /// <summary>The Wind owl encounter of WindBossCombatPlan.md section 5.</summary>
    public static BossDefinition CreateWindOwlDefaults()
    {
        var definition = CreateInstance<BossDefinition>();
        definition.name = "WindOwlBoss";
        definition.bossId = "boss.wind_owl";
        definition.displayName = "Sky Owl";
        definition.moveSpeed = 3.4f;
        definition.ultimateSkill = BossSkillId.SkyStorm;
        definition.ultimateRecoverySeconds = 4f;
        definition.impactVfx = "Wind/WindImpact_Burst";
        definition.phaseVfx = "Wind/WindRing_Flow";
        definition.phases = new[]
        {
            new BossPhase
            {
                name = "Phase 1", startsBelowHealth = 1f, telegraphScale = 1f, telegraphMinSeconds = 0.8f, recoverySeconds = 3f,
                gapBetweenSkills = 0.6f, ultimateEveryCombos = 0,
                combos = new[]
                {
                    Combo("A", new BossComboStep(BossSkillId.FeatherVolley), new BossComboStep(BossSkillId.TalonDive)),
                    Combo("B", new BossComboStep(BossSkillId.Cyclones), new BossComboStep(BossSkillId.GaleWall)),
                },
            },
            new BossPhase
            {
                name = "Phase 2", startsBelowHealth = 0.65f, telegraphScale = 0.9f, telegraphMinSeconds = 0.8f, recoverySeconds = 2.5f,
                gapBetweenSkills = 0.5f, ultimateEveryCombos = 3,
                combos = new[]
                {
                    Combo("C", new BossComboStep(BossSkillId.CrescentBlades), new BossComboStep(BossSkillId.FeatherVolley)),
                    Combo("D", new BossComboStep(BossSkillId.GaleWall, true), new BossComboStep(BossSkillId.Cyclones)),
                    Combo("E", new BossComboStep(BossSkillId.TalonDive), new BossComboStep(BossSkillId.TalonDive), new BossComboStep(BossSkillId.CrescentBlades)),
                },
            },
            new BossPhase
            {
                name = "Phase 3 - Tempest", startsBelowHealth = 0.30f, telegraphScale = 0.8f, telegraphMinSeconds = 0.6f, recoverySeconds = 1.8f,
                gapBetweenSkills = 0.4f, ultimateEveryCombos = 2,
                combos = new[]
                {
                    Combo("F", new BossComboStep(BossSkillId.TalonDive)),
                    Combo("G", new BossComboStep(BossSkillId.CrescentBlades), new BossComboStep(BossSkillId.FeatherVolley)),
                    Combo("H", new BossComboStep(BossSkillId.GaleWall, true), new BossComboStep(BossSkillId.Cyclones)),
                },
            },
        };
        return definition;
    }

    private static BossCombo Combo(string name, params BossComboStep[] steps) => new BossCombo { name = name, steps = steps };
}
