using System.Reflection;
using NUnit.Framework;

public sealed class QuestObjectiveMatchersTests
{
    private static QuestObjectiveDefinition MakeObjective(
        QuestObjectiveType type, string targetId, string targetAreaId = null,
        ObtainObjectiveMode obtainMode = ObtainObjectiveMode.CountAcquired)
    {
        var objective = new QuestObjectiveDefinition();
        SetPrivate(objective, "_type", type);
        SetPrivate(objective, "_targetId", targetId);
        SetPrivate(objective, "_targetAreaId", targetAreaId);
        SetPrivate(objective, "_targetCount", 1);
        SetPrivate(objective, "_obtainMode", obtainMode);
        return objective;
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    [Test]
    public void MatchesTalk_RequiresTypeAndExactNpcId()
    {
        QuestObjectiveDefinition talk = MakeObjective(QuestObjectiveType.Talk, "npc.town.blacksmith");

        Assert.IsTrue(QuestObjectiveMatchers.MatchesTalk(talk, "npc.town.blacksmith"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesTalk(talk, "npc.town.other"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesObtain(talk, "npc.town.blacksmith"), "Wrong type must never match.");
    }

    [Test]
    public void MatchesObtain_RequiresExactItemId()
    {
        QuestObjectiveDefinition obtain = MakeObjective(QuestObjectiveType.Obtain, "item.material.wood");

        Assert.IsTrue(QuestObjectiveMatchers.MatchesObtain(obtain, "item.material.wood"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesObtain(obtain, "item.material.iron"));
    }

    [Test]
    public void MatchesCraftAndPurchase_AreIndependentOfEachOther()
    {
        QuestObjectiveDefinition craft = MakeObjective(QuestObjectiveType.Craft, "item.weapon.sword.iron");
        QuestObjectiveDefinition purchase = MakeObjective(QuestObjectiveType.Purchase, "item.weapon.sword.iron");

        Assert.IsTrue(QuestObjectiveMatchers.MatchesCraft(craft, "item.weapon.sword.iron"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesPurchase(craft, "item.weapon.sword.iron"));

        Assert.IsTrue(QuestObjectiveMatchers.MatchesPurchase(purchase, "item.weapon.sword.iron"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesCraft(purchase, "item.weapon.sword.iron"));
    }

    [Test]
    public void MatchesGather_EmptyAreaIdMeansAnyArea()
    {
        QuestObjectiveDefinition anyArea = MakeObjective(QuestObjectiveType.Gather, "resource.wood.log");
        QuestObjectiveDefinition specificArea = MakeObjective(QuestObjectiveType.Gather, "resource.wood.log", "area.forest");

        Assert.IsTrue(QuestObjectiveMatchers.MatchesGather(anyArea, "resource.wood.log", "area.forest"));
        Assert.IsTrue(QuestObjectiveMatchers.MatchesGather(anyArea, "resource.wood.log", "area.tutorial"));

        Assert.IsTrue(QuestObjectiveMatchers.MatchesGather(specificArea, "resource.wood.log", "area.forest"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesGather(specificArea, "resource.wood.log", "area.tutorial"));
    }

    [Test]
    public void MatchesKill_RequiresEnemyIdAndAreaWhenAreaIsSpecified()
    {
        QuestObjectiveDefinition kill = MakeObjective(QuestObjectiveType.Kill, "enemy.slime.green", "area.tutorial");

        Assert.IsTrue(QuestObjectiveMatchers.MatchesKill(kill, "enemy.slime.green", "area.tutorial"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesKill(kill, "enemy.slime.green", "area.town"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesKill(kill, "enemy.slime.blue", "area.tutorial"));
    }

    [Test]
    public void NullObjective_NeverMatches()
    {
        Assert.IsFalse(QuestObjectiveMatchers.MatchesTalk(null, "npc.any"));
        Assert.IsFalse(QuestObjectiveMatchers.MatchesKill(null, "enemy.any", "area.any"));
    }
}
