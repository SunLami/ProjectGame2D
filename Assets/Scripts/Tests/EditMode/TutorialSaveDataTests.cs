using NUnit.Framework;
using UnityEngine;

public sealed class TutorialSaveDataTests
{
    [Test]
    public void RoundTripsCurrentStepAndCompleted()
    {
        TutorialSaveData data = new() { currentStepId = "step.move", completed = false };

        string json = JsonUtility.ToJson(data);
        TutorialSaveData loaded = JsonUtility.FromJson<TutorialSaveData>(json);

        Assert.AreEqual("step.move", loaded.currentStepId);
        Assert.IsFalse(loaded.completed);
    }

    [Test]
    public void GameSaveData_RoundTripsTutorial()
    {
        GameSaveData data = new() { saveId = "s1", tutorial = new TutorialSaveData { currentStepId = "step.attack", completed = true } };

        string json = JsonUtility.ToJson(data);
        GameSaveData loaded = JsonUtility.FromJson<GameSaveData>(json);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, loaded.saveVersion);
        Assert.AreEqual("step.attack", loaded.tutorial.currentStepId);
        Assert.IsTrue(loaded.tutorial.completed);
    }
}
