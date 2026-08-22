using System;
using System.IO;
using NUnit.Framework;

public sealed class FileSaveSlotRepositoryTests
{
    private string _rootPath;
    private FileSaveSlotRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "ProjectGame2DSaveTests_" + Guid.NewGuid().ToString("N"));
        _repository = new FileSaveSlotRepository(_rootPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, recursive: true);
    }

    private static GameSaveData MakeSave(string saveId, long playTime = 0) => new()
    {
        saveId = saveId,
        totalPlayTimeSeconds = playTime
    };

    [Test]
    public void EmptySlot_ReturnsEmptyStatus()
    {
        SaveSlotInfo info = _repository.GetSlotInfo(1);

        Assert.AreEqual(SaveSlotStatus.Empty, info.Status);
        Assert.IsNull(info.Metadata);
    }

    [Test]
    public void WriteThenRead_RoundTripsData()
    {
        SaveOperationResult result = _repository.WriteSave(1, MakeSave("save-abc", 42));
        Assert.IsTrue(result.Success, result.ErrorMessage);

        Assert.IsTrue(_repository.TryReadSave(1, out GameSaveData data));
        Assert.AreEqual("save-abc", data.saveId);
        Assert.AreEqual(42, data.totalPlayTimeSeconds);

        SaveSlotInfo info = _repository.GetSlotInfo(1);
        Assert.AreEqual(SaveSlotStatus.Valid, info.Status);
        Assert.AreEqual("save-abc", info.Metadata.saveId);
        Assert.IsFalse(string.IsNullOrEmpty(info.Metadata.contentChecksum));
    }

    [Test]
    public void WriteThenOverwrite_ReadsLatest()
    {
        _repository.WriteSave(1, MakeSave("save-v1", 10));
        SaveOperationResult second = _repository.WriteSave(1, MakeSave("save-v2", 20));

        Assert.IsTrue(second.Success, second.ErrorMessage);
        Assert.IsTrue(_repository.TryReadSave(1, out GameSaveData data));
        Assert.AreEqual("save-v2", data.saveId);
        Assert.AreEqual(20, data.totalPlayTimeSeconds);
    }

    [Test]
    public void DeleteSlot_ReturnsToEmpty()
    {
        _repository.WriteSave(1, MakeSave("save-abc"));
        SaveOperationResult delete = _repository.DeleteSlot(1);

        Assert.IsTrue(delete.Success, delete.ErrorMessage);
        Assert.AreEqual(SaveSlotStatus.Empty, _repository.GetSlotInfo(1).Status);
    }

    [Test]
    public void SlotsAreIndependent()
    {
        _repository.WriteSave(1, MakeSave("save-1"));
        _repository.WriteSave(2, MakeSave("save-2"));

        _repository.DeleteSlot(1);

        Assert.AreEqual(SaveSlotStatus.Empty, _repository.GetSlotInfo(1).Status);
        Assert.AreEqual(SaveSlotStatus.Valid, _repository.GetSlotInfo(2).Status);
        Assert.AreEqual(SaveSlotStatus.Empty, _repository.GetSlotInfo(3).Status);
    }

    [Test]
    public void GetAllSlotInfo_ReturnsThreeSlots()
    {
        SaveSlotInfo[] all = _repository.GetAllSlotInfo();
        Assert.AreEqual(3, all.Length);
    }

    [Test]
    public void CorruptedCurrentWithValidBackup_RecoversFromBackup()
    {
        _repository.WriteSave(1, MakeSave("save-v1"));
        _repository.WriteSave(1, MakeSave("save-v2")); // rotates v1 into save.backup.json

        string savePath = Path.Combine(_rootPath, "Slot1", "save.json");
        File.WriteAllText(savePath, "{ not valid json ][");

        SaveSlotInfo info = _repository.GetSlotInfo(1);
        Assert.AreEqual(SaveSlotStatus.Valid, info.Status);
        Assert.AreEqual("save-v1", info.Metadata.saveId);

        Assert.IsTrue(_repository.TryReadSave(1, out GameSaveData data));
        Assert.AreEqual("save-v1", data.saveId);
    }

    [Test]
    public void BothCurrentAndBackupCorrupted_ReturnsCorrupted()
    {
        _repository.WriteSave(1, MakeSave("save-v1"));
        _repository.WriteSave(1, MakeSave("save-v2"));

        File.WriteAllText(Path.Combine(_rootPath, "Slot1", "save.json"), "not json");
        File.WriteAllText(Path.Combine(_rootPath, "Slot1", "save.backup.json"), "also not json");

        SaveSlotInfo info = _repository.GetSlotInfo(1);
        Assert.AreEqual(SaveSlotStatus.Corrupted, info.Status);
        Assert.IsNull(info.Metadata);

        Assert.IsFalse(_repository.TryReadSave(1, out _));
    }

    [Test]
    public void CorruptedCurrentWithNoBackup_ReturnsCorrupted()
    {
        _repository.WriteSave(1, MakeSave("save-v1"));
        File.WriteAllText(Path.Combine(_rootPath, "Slot1", "save.json"), "not json");

        Assert.AreEqual(SaveSlotStatus.Corrupted, _repository.GetSlotInfo(1).Status);
    }

    [Test]
    public void NewerSaveVersion_ReturnsIncompatibleAndDoesNotLoad()
    {
        string slotDir = Path.Combine(_rootPath, "Slot1");
        Directory.CreateDirectory(slotDir);
        File.WriteAllText(Path.Combine(slotDir, "save.json"),
            "{\"saveVersion\":999,\"saveId\":\"future-save\",\"totalPlayTimeSeconds\":0}");

        SaveSlotInfo info = _repository.GetSlotInfo(1);
        Assert.AreEqual(SaveSlotStatus.IncompatibleVersion, info.Status);
        Assert.IsFalse(_repository.TryReadSave(1, out _));
    }

    [Test]
    public void WriteFailure_DoesNotDestroyExistingValidSave()
    {
        _repository.WriteSave(1, MakeSave("save-good"));

        SaveOperationResult badWrite = _repository.WriteSave(1, MakeSave(null));
        Assert.IsFalse(badWrite.Success);

        Assert.IsTrue(_repository.TryReadSave(1, out GameSaveData data));
        Assert.AreEqual("save-good", data.saveId);
    }

    [Test]
    public void InvalidSlotId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _repository.GetSlotInfo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _repository.GetSlotInfo(4));
    }
}
