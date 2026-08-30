using NUnit.Framework;

public sealed class InMemorySaveSlotRepositoryTests
{
    [Test]
    public void WriteReadDeleteRoundTrip()
    {
        ISaveSlotRepository repository = new InMemorySaveSlotRepository();

        Assert.AreEqual(SaveSlotStatus.Empty, repository.GetSlotInfo(2).Status);

        SaveOperationResult write = repository.WriteSave(2, new GameSaveData { saveId = "mock-save" });
        Assert.IsTrue(write.Success);
        Assert.AreEqual(SaveSlotStatus.Valid, repository.GetSlotInfo(2).Status);

        Assert.IsTrue(repository.TryReadSave(2, out GameSaveData data));
        Assert.AreEqual("mock-save", data.saveId);

        repository.DeleteSlot(2);
        Assert.AreEqual(SaveSlotStatus.Empty, repository.GetSlotInfo(2).Status);
    }

    [Test]
    public void RejectsEmptySaveId()
    {
        ISaveSlotRepository repository = new InMemorySaveSlotRepository();
        SaveOperationResult result = repository.WriteSave(1, new GameSaveData { saveId = "" });
        Assert.IsFalse(result.Success);
    }
}
