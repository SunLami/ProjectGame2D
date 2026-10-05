using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class BossDefinitionWindTests
{
    private BossDefinition _definition;

    [SetUp]
    public void SetUp() => _definition = BossDefinition.CreateWindOwlDefaults();

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_definition);

    [Test]
    public void Defaults_MatchThePlan()
    {
        Assert.AreEqual("boss.wind_owl", _definition.bossId);
        Assert.AreEqual(2400f, _definition.maxHealth);
        Assert.AreEqual(14f, _definition.baseDamage);
        Assert.AreEqual(BossSkillId.SkyStorm, _definition.ultimateSkill);
        Assert.AreEqual(3, _definition.phases.Length);
        Assert.AreEqual(0.65f, _definition.phases[1].startsBelowHealth, 0.001f);
        Assert.AreEqual(0.30f, _definition.phases[2].startsBelowHealth, 0.001f);
    }

    [Test]
    public void Combos_UseOnlyWindSkills()
    {
        BossSkillId[] wind =
        {
            BossSkillId.FeatherVolley, BossSkillId.TalonDive, BossSkillId.Cyclones, BossSkillId.GaleWall, BossSkillId.CrescentBlades,
            BossSkillId.SkyStorm,
        };
        foreach (BossPhase phase in _definition.phases)
        {
            Assert.IsNotEmpty(phase.combos);
            foreach (BossCombo combo in phase.combos)
            {
                Assert.IsNotEmpty(combo.steps);
                foreach (BossComboStep step in combo.steps)
                    Assert.Contains(step.skill, wind, combo.name);
            }
        }
    }

    [Test]
    public void UltimateComesAfterEveryThirdThenSecondCombo()
    {
        Assert.AreEqual(0, _definition.phases[0].ultimateEveryCombos);
        Assert.AreEqual(3, _definition.phases[1].ultimateEveryCombos);
        Assert.AreEqual(2, _definition.phases[2].ultimateEveryCombos);
    }

    [Test]
    public void PerPhaseNumbers_GrowWithThePhase()
    {
        Assert.Less(BossDefinition.PerPhase(_definition.featherCountByPhase, 0, 0), BossDefinition.PerPhase(_definition.featherCountByPhase, 2, 0));
        Assert.Less(BossDefinition.PerPhase(_definition.divePerchByPhase, 2, 0f), BossDefinition.PerPhase(_definition.divePerchByPhase, 0, 0f));
        Assert.AreEqual(3, BossDefinition.PerPhase(_definition.diveTargetsByPhase, 2, 0));
    }

    [Test]
    public void StormTiers_BreakOutsideInAndAreOrderedInPairs()
    {
        Rect[] tiers = _definition.stormTierRects;
        Assert.AreEqual(6, tiers.Length);
        Assert.IsTrue(tiers.All(r => r.width > 0f && r.height > 0f));
        // pairs (left, right) get closer to the middle: the outer pair is first
        for (int i = 0; i + 2 < tiers.Length; i += 2)
            Assert.Less(Mathf.Abs(tiers[i + 2].center.x), Mathf.Abs(tiers[i].center.x));
    }
}
