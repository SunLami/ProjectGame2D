using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// File-backed ISaveSlotRepository. Layout per slot: save.json, save.backup.json, metadata.json
/// under rootPath/SlotN. Writes are atomic (temp file, round-trip validated, then File.Replace
/// which performs the backup rotation and swap in one call) so a failure mid-write never
/// corrupts the last valid save.
/// </summary>
public sealed class FileSaveSlotRepository : ISaveSlotRepository
{
    private const string SaveFileName = "save.json";
    private const string BackupFileName = "save.backup.json";
    private const string MetadataFileName = "metadata.json";

    private readonly string _rootPath;

    public FileSaveSlotRepository(string rootPath = null)
    {
        _rootPath = string.IsNullOrWhiteSpace(rootPath)
            ? Path.Combine(Application.persistentDataPath, "Saves")
            : rootPath;
    }

    public SaveSlotInfo GetSlotInfo(int slotId)
    {
        ValidateSlotId(slotId);

        string savePath = SavePath(slotId);
        string backupPath = BackupPath(slotId);

        if (!File.Exists(savePath) && !File.Exists(backupPath))
            return new SaveSlotInfo(slotId, SaveSlotStatus.Empty, null);

        if (TryLoadValid(savePath, out GameSaveData data, out SaveSlotStatus primaryStatus))
            return new SaveSlotInfo(slotId, SaveSlotStatus.Valid, ResolveMetadata(slotId, data, savePath));

        if (TryLoadValid(backupPath, out data, out SaveSlotStatus backupStatus))
            return new SaveSlotInfo(slotId, SaveSlotStatus.Valid, ResolveMetadata(slotId, data, backupPath));

        // Neither file yielded a usable save. Prefer whichever status is non-Empty (a file that
        // exists but failed to load), since "Empty" only means that particular file is absent.
        SaveSlotStatus finalStatus = primaryStatus != SaveSlotStatus.Empty ? primaryStatus : backupStatus;
        return new SaveSlotInfo(slotId, finalStatus, null);
    }

    public SaveSlotInfo[] GetAllSlotInfo()
    {
        int count = GameSessionManager.MaximumSlotId - GameSessionManager.MinimumSlotId + 1;
        SaveSlotInfo[] result = new SaveSlotInfo[count];
        for (int i = 0; i < count; i++)
            result[i] = GetSlotInfo(GameSessionManager.MinimumSlotId + i);

        return result;
    }

    public bool TryReadSave(int slotId, out GameSaveData data)
    {
        ValidateSlotId(slotId);

        if (TryLoadValid(SavePath(slotId), out data, out _))
            return true;

        return TryLoadValid(BackupPath(slotId), out data, out _);
    }

    public SaveOperationResult WriteSave(int slotId, GameSaveData data)
    {
        ValidateSlotId(slotId);

        if (data == null)
            return SaveOperationResult.Failure("GameSaveData is null.");
        if (string.IsNullOrWhiteSpace(data.saveId))
            return SaveOperationResult.Failure("GameSaveData.saveId is empty.");

        string slotDir = SlotDirectory(slotId);
        string savePath = SavePath(slotId);
        string backupPath = BackupPath(slotId);
        string tempPath = savePath + ".tmp";

        try
        {
            Directory.CreateDirectory(slotDir);

            string json = JsonUtility.ToJson(data);
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            // Round-trip validate before it ever becomes the live save.
            string readBack = File.ReadAllText(tempPath, Encoding.UTF8);
            GameSaveData parsed = JsonUtility.FromJson<GameSaveData>(readBack);
            if (parsed == null || parsed.saveId != data.saveId)
            {
                File.Delete(tempPath);
                return SaveOperationResult.Failure("Written save failed round-trip validation.");
            }

            if (File.Exists(savePath))
                File.Replace(tempPath, savePath, backupPath);
            else
                File.Move(tempPath, savePath);

            string checksum = ComputeChecksum(json);
            SaveSlotMetadata metadata = BuildMetadata(slotId, data, checksum);
            metadata.lastSavedUtcTicks = DateTime.UtcNow.Ticks;
            WriteMetadata(slotId, metadata);

            return SaveOperationResult.Ok();
        }
        catch (Exception exception)
        {
            if (File.Exists(tempPath))
                TryDelete(tempPath);

            return SaveOperationResult.Failure($"Save write failed: {exception.Message}");
        }
    }

    public SaveOperationResult DeleteSlot(int slotId)
    {
        ValidateSlotId(slotId);

        try
        {
            TryDelete(SavePath(slotId));
            TryDelete(BackupPath(slotId));
            TryDelete(MetadataPath(slotId));
            return SaveOperationResult.Ok();
        }
        catch (Exception exception)
        {
            return SaveOperationResult.Failure($"Delete failed: {exception.Message}");
        }
    }

    private bool TryLoadValid(string path, out GameSaveData data, out SaveSlotStatus status)
    {
        data = null;
        status = SaveSlotStatus.Empty;

        if (!File.Exists(path))
            return false;

        GameSaveData parsed;
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            parsed = JsonUtility.FromJson<GameSaveData>(json);
        }
        catch
        {
            status = SaveSlotStatus.Corrupted;
            return false;
        }

        if (parsed == null || string.IsNullOrWhiteSpace(parsed.saveId))
        {
            status = SaveSlotStatus.Corrupted;
            return false;
        }

        if (parsed.saveVersion > GameSaveData.CurrentSaveVersion)
        {
            status = SaveSlotStatus.IncompatibleVersion;
            return false;
        }

        if (parsed.saveVersion < GameSaveData.CurrentSaveVersion)
        {
            // No migration pipeline yet (Phase 10); treat older shapes as incompatible rather
            // than silently loading a partially-understood save.
            status = SaveSlotStatus.IncompatibleVersion;
            return false;
        }

        data = parsed;
        status = SaveSlotStatus.Valid;
        return true;
    }

    /// <summary>
    /// Prefers the metadata.json written alongside this save (has the real lastSavedUtcTicks) and
    /// only falls back to reconstructing from the GameSaveData itself when metadata.json is
    /// missing/stale/corrupted -- in that fallback path lastSavedUtcTicks is unrecoverable and
    /// stays 0 ("unknown"), everything else is rebuilt from the save that was actually loaded.
    /// </summary>
    private SaveSlotMetadata ResolveMetadata(int slotId, GameSaveData data, string sourcePath)
    {
        string checksum = ComputeChecksum(File.ReadAllText(sourcePath, Encoding.UTF8));

        SaveSlotMetadata persisted = TryReadPersistedMetadata(slotId);
        if (persisted != null && persisted.saveId == data.saveId && persisted.contentChecksum == checksum)
            return persisted;

        return BuildMetadata(slotId, data, checksum);
    }

    private SaveSlotMetadata TryReadPersistedMetadata(int slotId)
    {
        string path = MetadataPath(slotId);
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonUtility.FromJson<SaveSlotMetadata>(File.ReadAllText(path, Encoding.UTF8));
        }
        catch
        {
            return null;
        }
    }

    // characterName is intentionally never populated here -- there is no character-naming
    // domain yet (D-013 is still Open) and fabricating one would be display fiction, not data.
    // tutorialCompleted stays false until the tutorial domain (Phase 5) exists to report it.
    private static SaveSlotMetadata BuildMetadata(int slotId, GameSaveData data, string checksum) => new()
    {
        slotIndex = slotId,
        saveId = data.saveId,
        saveVersion = data.saveVersion,
        totalPlayTimeSeconds = data.totalPlayTimeSeconds,
        characterLevel = data.player?.level ?? 0,
        areaId = data.player?.location?.areaId,
        contentChecksum = checksum
    };

    private void WriteMetadata(int slotId, SaveSlotMetadata metadata)
    {
        string json = JsonUtility.ToJson(metadata);
        string path = MetadataPath(slotId);
        string tempPath = path + ".tmp";

        File.WriteAllText(tempPath, json, Encoding.UTF8);
        if (File.Exists(path))
            File.Delete(path);
        File.Move(tempPath, path);
    }

    private static string ComputeChecksum(string content)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void ValidateSlotId(int slotId)
    {
        if (slotId < GameSessionManager.MinimumSlotId || slotId > GameSessionManager.MaximumSlotId)
        {
            throw new ArgumentOutOfRangeException(nameof(slotId),
                $"Slot id must be between {GameSessionManager.MinimumSlotId} and {GameSessionManager.MaximumSlotId}.");
        }
    }

    private string SlotDirectory(int slotId) => Path.Combine(_rootPath, $"Slot{slotId}");
    private string SavePath(int slotId) => Path.Combine(SlotDirectory(slotId), SaveFileName);
    private string BackupPath(int slotId) => Path.Combine(SlotDirectory(slotId), BackupFileName);
    private string MetadataPath(int slotId) => Path.Combine(SlotDirectory(slotId), MetadataFileName);
}
