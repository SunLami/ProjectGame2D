using System.IO;
using System;
using NUnit.Framework;

public sealed class NewGameFactoryTests
{
    [Test]
    public void CreateDefault_HasTutorialAreaAndSpawn()
    {
        GameSaveData data = NewGameFactory.CreateDefault();

        Assert.IsFalse(string.IsNullOrWhiteSpace(data.saveId));
        Assert.IsNotNull(data.player);
        Assert.AreEqual(1, data.player.level);
        Assert.AreEqual(0, data.player.currentExperience);
        Assert.Less(data.player.health, 0f, "New Game health should be the 'use max health' sentinel.");
        Assert.AreEqual(NewGameFactory.TutorialAreaId, data.player.location.areaId);
        Assert.AreEqual(NewGameFactory.TutorialStartSpawnId, data.player.location.fallbackSpawnId);
        Assert.IsFalse(data.player.location.HasSavedPosition);
    }

    [Test]
    public void TwoDefaults_HaveDifferentSaveIds()
    {
        GameSaveData a = NewGameFactory.CreateDefault();
        GameSaveData b = NewGameFactory.CreateDefault();

        Assert.AreNotEqual(a.saveId, b.saveId);
    }

    [Test]
    public void NewGameSaveData_RoundTripsThroughFileRepository()
    {
        string rootPath = Path.Combine(Path.GetTempPath(), "ProjectGame2DSaveTests_" + Guid.NewGuid().ToString("N"));
        var repository = new FileSaveSlotRepository(rootPath);

        try
        {
            GameSaveData original = NewGameFactory.CreateDefault();
            SaveOperationResult write = repository.WriteSave(1, original);
            Assert.IsTrue(write.Success, write.ErrorMessage);

            Assert.IsTrue(repository.TryReadSave(1, out GameSaveData loaded));
            Assert.AreEqual(original.saveId, loaded.saveId);
            Assert.AreEqual(original.player.level, loaded.player.level);
            Assert.AreEqual(original.player.location.areaId, loaded.player.location.areaId);
            Assert.AreEqual(original.player.location.fallbackSpawnId, loaded.player.location.fallbackSpawnId);
            Assert.IsFalse(loaded.player.location.HasSavedPosition,
                "NaN sentinel for 'no saved position yet' must round-trip through JsonUtility.");
        }
        finally
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }
    }
}
