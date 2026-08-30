using NUnit.Framework;
using UnityEngine;

public sealed class QuestSaveDataTests
{
    [Test]
    public void RoundTripsQuestProgressEntries()
    {
        QuestSaveData data = new();
        data.quests.Add(new QuestProgressSaveData
        {
            questId = "quest.tutorial.crafting.001",
            status = QuestStatus.Active,
            currentObjectiveIndex = 1,
            objectiveCounters = new[] { 1, 0, 2 }
        });

        string json = JsonUtility.ToJson(data);
        QuestSaveData loaded = JsonUtility.FromJson<QuestSaveData>(json);

        Assert.AreEqual(1, loaded.quests.Count);
        Assert.AreEqual("quest.tutorial.crafting.001", loaded.quests[0].questId);
        Assert.AreEqual(QuestStatus.Active, loaded.quests[0].status);
        Assert.AreEqual(1, loaded.quests[0].currentObjectiveIndex);
        CollectionAssert.AreEqual(new[] { 1, 0, 2 }, loaded.quests[0].objectiveCounters);
    }

    [Test]
    public void GameSaveData_RoundTripsQuests()
    {
        GameSaveData data = new() { saveId = "s1", quests = new QuestSaveData() };
        data.quests.quests.Add(new QuestProgressSaveData
        {
            questId = "quest.main.001",
            status = QuestStatus.Completed,
            currentObjectiveIndex = 3,
            objectiveCounters = new[] { 1 }
        });

        string json = JsonUtility.ToJson(data);
        GameSaveData loaded = JsonUtility.FromJson<GameSaveData>(json);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, loaded.saveVersion);
        Assert.AreEqual(1, loaded.quests.quests.Count);
        Assert.AreEqual(QuestStatus.Completed, loaded.quests.quests[0].status);
    }
}
