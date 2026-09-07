using System.Collections.Generic;
using NUnit.Framework;

public sealed class QuestTrackerOrderingTests
{
    [Test]
    public void Sort_PlacesMainBeforeSideBeforeDaily()
    {
        var quests = new List<TrackedQuestView>
        {
            View("daily", "Daily", QuestCategory.Daily),
            View("side", "Side", QuestCategory.Side),
            View("main", "Main", QuestCategory.Main)
        };

        quests.Sort(QuestTrackerOrdering.Compare);

        Assert.That(quests[0].QuestId, Is.EqualTo("main"));
        Assert.That(quests[1].QuestId, Is.EqualTo("side"));
        Assert.That(quests[2].QuestId, Is.EqualTo("daily"));
    }

    [Test]
    public void Sort_OrdersTitlesWithinSameCategory()
    {
        var quests = new List<TrackedQuestView>
        {
            View("b", "B Quest", QuestCategory.Side),
            View("a", "A Quest", QuestCategory.Side)
        };

        quests.Sort(QuestTrackerOrdering.Compare);

        Assert.That(quests[0].QuestId, Is.EqualTo("a"));
        Assert.That(quests[1].QuestId, Is.EqualTo("b"));
    }

    private static TrackedQuestView View(string id, string title, QuestCategory category) =>
        new(id, title, "Objective", category);
}
