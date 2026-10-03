using NUnit.Framework;
using UnityEngine;

public sealed class BossDefinitionTests
{
    private BossDefinition _definition;

    [SetUp]
    public void SetUp() => _definition = BossDefinition.CreateEarthGolemDefaults();

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_definition);

    [Test]
    public void EarthGolemDefaults_HaveThreeDescendingPhasesWithCombos()
    {
        Assert.AreEqual(3, _definition.phases.Length);
        Assert.AreEqual(1f, _definition.phases[0].startsBelowHealth, 0.0001f);
        for (int i = 1; i < _definition.phases.Length; i++)
            Assert.Less(_definition.phases[i].startsBelowHealth, _definition.phases[i - 1].startsBelowHealth);

        foreach (BossPhase phase in _definition.phases)
        {
            Assert.IsNotEmpty(phase.combos, phase.name);
            foreach (BossCombo combo in phase.combos)
                Assert.IsNotEmpty(combo.steps, combo.name);
        }
    }

    [TestCase(1f, 0)]
    [TestCase(0.66f, 0)]
    [TestCase(0.65f, 1)]
    [TestCase(0.5f, 1)]
    [TestCase(0.31f, 1)]
    [TestCase(0.30f, 2)]
    [TestCase(0f, 2)]
    public void PhaseIndexForHealthFraction_UsesThresholds(float fraction, int expected)
    {
        Assert.AreEqual(expected, _definition.PhaseIndexForHealthFraction(fraction));
    }

    [Test]
    public void PerPhase_ClampsIndexAndFallsBackWhenEmpty()
    {
        int[] values = { 6, 9, 12 };
        Assert.AreEqual(6, BossDefinition.PerPhase(values, 0, -1));
        Assert.AreEqual(12, BossDefinition.PerPhase(values, 2, -1));
        Assert.AreEqual(12, BossDefinition.PerPhase(values, 7, -1));
        Assert.AreEqual(6, BossDefinition.PerPhase(values, -3, -1));
        Assert.AreEqual(-1, BossDefinition.PerPhase(new int[0], 1, -1));
        Assert.AreEqual(-1, BossDefinition.PerPhase<int>(null, 1, -1));
    }

    [Test]
    public void Defaults_FollowThePlan_TelegraphsGetFasterAndRecoveryShorterWithPhase()
    {
        for (int i = 1; i < _definition.phases.Length; i++)
        {
            Assert.LessOrEqual(_definition.phases[i].telegraphScale, _definition.phases[i - 1].telegraphScale);
            Assert.LessOrEqual(_definition.phases[i].recoverySeconds, _definition.phases[i - 1].recoverySeconds);
        }

        Assert.AreEqual(0, _definition.phases[0].ultimateEveryCombos);
        Assert.AreEqual(3, _definition.phases[1].ultimateEveryCombos);
        Assert.AreEqual(2, _definition.phases[2].ultimateEveryCombos);
        Assert.AreEqual(14f, _definition.baseDamage, 0.0001f);
        Assert.AreEqual(2400f, _definition.maxHealth, 0.0001f);
    }

    [Test]
    public void LaneStep_LeavesWalkableGapAtEightUnits()
    {
        // centre-to-centre spacing at distance 8 minus the lane width must leave room for the player
        float spacing = 2f * 8f * Mathf.Sin(_definition.laneStepDegrees * Mathf.Deg2Rad * 0.5f);
        Assert.GreaterOrEqual(spacing - _definition.laneWidth, 1.6f);
    }

    [Test]
    public void WaterCrabDefaults_UseWhirlpoolAndOnlyWaterSkillsAfterTheEarthOnes()
    {
        BossDefinition water = BossDefinition.CreateWaterCrabDefaults();
        try
        {
            Assert.AreEqual(BossSkillId.Whirlpool, water.ultimateSkill);
            Assert.AreEqual(3, water.phases.Length);
            Assert.AreEqual(0, water.phases[0].ultimateEveryCombos); // no ultimate in phase 1, like the Earth boss
            foreach (BossPhase phase in water.phases)
            {
                foreach (BossCombo combo in phase.combos)
                {
                    foreach (BossComboStep step in combo.steps)
                        Assert.GreaterOrEqual((int)step.skill, (int)BossSkillId.ClawClamp, combo.name);
                }
            }

            // Earth defaults must be untouched by the Water additions.
            Assert.AreEqual(BossSkillId.CoreResonance, _definition.ultimateSkill);
        }
        finally
        {
            Object.DestroyImmediate(water);
        }
    }

    [Test]
    public void WaterTide_IsFasterInLaterPhasesAndFloodsDeeperInPhaseThree()
    {
        BossDefinition water = BossDefinition.CreateWaterCrabDefaults();
        try
        {
            Assert.Less(water.tideCycleSecondsByPhase[1], water.tideCycleSecondsByPhase[0]);
            Assert.Greater(water.tideDepthByPhase[2], water.tideDepthByPhase[1]);
            Assert.Greater(water.tideCrabSpeedMultiplier, 1f);
            Assert.Less(water.tideShallowPlayerSpeed, 1f);
        }
        finally
        {
            Object.DestroyImmediate(water);
        }
    }
}
