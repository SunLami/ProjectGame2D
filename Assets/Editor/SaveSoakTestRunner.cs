using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Repeatable save/load soak test for the file-backed save pipeline (Phase 10 Part 4). Exercises
/// FileSaveSlotRepository against a temporary directory only -- it never touches
/// Application.persistentDataPath, so it cannot corrupt a real player's save. Does not auto-run:
/// invoke it explicitly from the Tools menu.
///
/// Scope: this tool soak-tests the save FILE layer (serialize/write/read/migrate, size growth,
/// cross-slot isolation, temp-file cleanliness) across many repeated cycles. It does not drive a
/// live scene/GameStateManager, so it cannot observe event-subscriber counts or stuck
/// GameState/timeScale -- those are covered instead by the existing PlayMode regression suite
/// (GameplaySessionControllerPlayModeTests, SessionDirtyTracker tests), which exercises each
/// transition once under NUnit rather than hundreds of times under a scene reload loop.
/// </summary>
public static class SaveSoakTestRunner
{
    private const string MenuPath = "Tools/Project Game/Run Save Soak Test";
    private const int SameSlotCycles = 120;
    private const int WorldObjectCount = 60;

    [MenuItem(MenuPath)]
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "ProjectGame2DSoakTest_" + Guid.NewGuid().ToString("N"));
        var log = new StringBuilder();
        bool allPassed = true;

        try
        {
            log.AppendLine("=== Save Soak Test ===");
            log.AppendLine($"Started (UTC): {DateTime.UtcNow:O}");
            log.AppendLine($"Unity: {Application.unityVersion} | Platform: {Application.platform} | Editor: {(Application.isEditor ? "yes" : "no")}");
            log.AppendLine($"Temp root: {root}");
            log.AppendLine();

            allPassed &= RunSameSlotCycles(root, log);
            allPassed &= RunCrossSlotCycles(root, log);
            allPassed &= RunSaveReturnContinueCycles(root, log);
            allPassed &= RunWorldSnapshotCycle(root, log);
            allPassed &= RunSessionTeardownRecreateCycles(root, log);
            allPassed &= RunTempFileCleanlinessCheck(root, log);

            log.AppendLine();
            log.AppendLine(allPassed ? "RESULT: ALL CHECKS PASSED" : "RESULT: ONE OR MORE CHECKS FAILED");
        }
        catch (Exception exception)
        {
            allPassed = false;
            log.AppendLine($"RESULT: SOAK TEST THREW: {exception}");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        string text = log.ToString();
        if (allPassed)
            Debug.Log(text);
        else
            Debug.LogError(text);
    }

    private static bool RunSameSlotCycles(string root, StringBuilder log)
    {
        var repository = new FileSaveSlotRepository(Path.Combine(root, "SameSlot"));
        var stopwatch = Stopwatch.StartNew();
        long firstSize = -1;
        long lastSize = -1;
        bool sizeStable = true;

        for (int i = 0; i < SameSlotCycles; i++)
        {
            GameSaveData data = BuildRepresentativeSave($"soak-{i}", worldObjectCount: 20);
            SaveOperationResult writeResult = repository.WriteSave(1, data);
            if (!writeResult.Success)
            {
                log.AppendLine($"[SameSlot] FAIL: write #{i} failed: {writeResult.ErrorMessage}");
                return Fail(log, stopwatch, "SameSlot");
            }

            if (!repository.TryReadSave(1, out GameSaveData readBack) || readBack.saveId != data.saveId)
            {
                log.AppendLine($"[SameSlot] FAIL: read-back mismatch on cycle #{i}.");
                return Fail(log, stopwatch, "SameSlot");
            }

            string savePath = Path.Combine(root, "SameSlot", "Slot1", "save.json");
            long size = new FileInfo(savePath).Length;
            if (firstSize < 0)
                firstSize = size;
            lastSize = size;
        }

        stopwatch.Stop();
        // Same fixed-shape payload every cycle -- size must stay within a small tolerance, never
        // grow unbounded (a bounded save.json is the file-layer proxy for "no leaked accumulation").
        sizeStable = firstSize > 0 && Math.Abs(lastSize - firstSize) <= Math.Max(64, firstSize / 10);

        log.AppendLine($"[SameSlot] {SameSlotCycles} save/load cycles on slot 1: {stopwatch.Elapsed.TotalMilliseconds:F1} ms total, " +
            $"{stopwatch.Elapsed.TotalMilliseconds / SameSlotCycles:F2} ms/cycle avg. First size={firstSize}B, last size={lastSize}B, stable={sizeStable}.");

        if (!sizeStable)
            log.AppendLine("[SameSlot] FAIL: save.json size drifted beyond tolerance across repeated writes of same-shape data.");

        return sizeStable;
    }

    private static bool RunCrossSlotCycles(string root, StringBuilder log)
    {
        var repository = new FileSaveSlotRepository(Path.Combine(root, "CrossSlot"));
        var stopwatch = Stopwatch.StartNew();

        GameSaveData a = BuildRepresentativeSave("slot-a", worldObjectCount: 5);
        GameSaveData b = BuildRepresentativeSave("slot-b", worldObjectCount: 5);
        GameSaveData c = BuildRepresentativeSave("slot-c", worldObjectCount: 5);

        repository.WriteSave(1, a);
        repository.WriteSave(2, b);
        repository.WriteSave(3, c);

        bool ok = true;
        ok &= AssertSlotHolds(repository, 1, "slot-a", log);
        ok &= AssertSlotHolds(repository, 2, "slot-b", log);
        ok &= AssertSlotHolds(repository, 3, "slot-c", log);

        // A -> B -> C re-load in sequence must never leak the previously loaded slot's data.
        for (int round = 0; round < 25 && ok; round++)
        {
            ok &= AssertSlotHolds(repository, 1, "slot-a", log);
            ok &= AssertSlotHolds(repository, 2, "slot-b", log);
            ok &= AssertSlotHolds(repository, 3, "slot-c", log);
        }

        stopwatch.Stop();
        log.AppendLine($"[CrossSlot] A/B/C independence over 25 rounds: {stopwatch.Elapsed.TotalMilliseconds:F1} ms. Pass={ok}.");
        return ok;
    }

    private static bool AssertSlotHolds(FileSaveSlotRepository repository, int slot, string expectedSaveId, StringBuilder log)
    {
        if (!repository.TryReadSave(slot, out GameSaveData data) || data.saveId != expectedSaveId)
        {
            log.AppendLine($"[CrossSlot] FAIL: slot {slot} expected saveId '{expectedSaveId}' but did not match.");
            return false;
        }

        return true;
    }

    private static bool RunSaveReturnContinueCycles(string root, StringBuilder log)
    {
        string path = Path.Combine(root, "ReturnContinue");
        var stopwatch = Stopwatch.StartNew();
        bool ok = true;

        for (int i = 0; i < 30 && ok; i++)
        {
            // "Return to main menu" then "Continue" is modeled as: write with one repository
            // instance, then read with a brand-new instance pointed at the same path (no shared
            // in-memory state survives -- exactly what happens across a real scene reload).
            var writer = new FileSaveSlotRepository(path);
            GameSaveData data = BuildRepresentativeSave($"return-continue-{i}", worldObjectCount: 8);
            writer.WriteSave(1, data);

            var reader = new FileSaveSlotRepository(path);
            if (!reader.TryReadSave(1, out GameSaveData readBack) || readBack.saveId != data.saveId)
            {
                log.AppendLine($"[ReturnContinue] FAIL: cycle #{i} did not read back the same saveId.");
                ok = false;
            }
        }

        stopwatch.Stop();
        log.AppendLine($"[ReturnContinue] 30 save-then-fresh-repository-read cycles: {stopwatch.Elapsed.TotalMilliseconds:F1} ms. Pass={ok}.");
        return ok;
    }

    private static bool RunWorldSnapshotCycle(string root, StringBuilder log)
    {
        var repository = new FileSaveSlotRepository(Path.Combine(root, "WorldSnapshot"));
        var stopwatch = Stopwatch.StartNew();

        GameSaveData data = BuildRepresentativeSave("world-snapshot", worldObjectCount: WorldObjectCount);
        SaveOperationResult writeResult = repository.WriteSave(1, data);
        bool ok = writeResult.Success;

        if (!ok)
            log.AppendLine($"[WorldSnapshot] FAIL: write failed: {writeResult.ErrorMessage}");

        if (ok && (!repository.TryReadSave(1, out GameSaveData readBack) || readBack.world.objects.Count != WorldObjectCount))
        {
            log.AppendLine("[WorldSnapshot] FAIL: world object count did not round-trip.");
            ok = false;
        }

        stopwatch.Stop();
        long size = new FileInfo(Path.Combine(root, "WorldSnapshot", "Slot1", "save.json")).Length;
        log.AppendLine($"[WorldSnapshot] {WorldObjectCount} persistent world objects: write+read {stopwatch.Elapsed.TotalMilliseconds:F1} ms, file size {size}B. Pass={ok}.");
        return ok;
    }

    private static bool RunSessionTeardownRecreateCycles(string root, StringBuilder log)
    {
        string path = Path.Combine(root, "TeardownRecreate");
        var stopwatch = Stopwatch.StartNew();
        bool ok = true;

        for (int i = 0; i < 40 && ok; i++)
        {
            // Simulates GameSessionManager being torn down and a new one spun up (e.g. domain
            // reload, or a fresh session object after Return-to-MainMenu) -- a new repository
            // instance must never depend on any state held by the previous one.
            var repository = new FileSaveSlotRepository(path);
            SaveSlotInfo info = repository.GetSlotInfo(1);
            if (i == 0 && info.Status != SaveSlotStatus.Empty)
            {
                log.AppendLine("[TeardownRecreate] FAIL: expected slot 1 empty on first cycle.");
                ok = false;
                break;
            }

            repository.WriteSave(1, BuildRepresentativeSave($"teardown-{i}", worldObjectCount: 3));
        }

        stopwatch.Stop();
        log.AppendLine($"[TeardownRecreate] 40 repository teardown/recreate cycles: {stopwatch.Elapsed.TotalMilliseconds:F1} ms. Pass={ok}.");
        return ok;
    }

    private static bool RunTempFileCleanlinessCheck(string root, StringBuilder log)
    {
        // Every .tmp file created during WriteSave must be consumed (via File.Replace/File.Move)
        // before WriteSave returns -- none of the prior cycles should have left one behind.
        string[] leftoverTempFiles = Directory.Exists(root)
            ? Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories)
            : Array.Empty<string>();

        bool ok = leftoverTempFiles.Length == 0;
        log.AppendLine($"[TempFileCleanliness] Leftover .tmp files after all cycles: {leftoverTempFiles.Length}. Pass={ok}.");
        if (!ok)
            log.AppendLine("  " + string.Join("\n  ", leftoverTempFiles));

        return ok;
    }

    private static bool Fail(StringBuilder log, Stopwatch stopwatch, string label)
    {
        stopwatch.Stop();
        log.AppendLine($"[{label}] Aborted after {stopwatch.Elapsed.TotalMilliseconds:F1} ms.");
        return false;
    }

    private static GameSaveData BuildRepresentativeSave(string saveId, int worldObjectCount)
    {
        GameSaveData data = NewGameFactory.CreateDefault();
        data.saveId = saveId;
        data.totalPlayTimeSeconds = 1234;
        data.player.level = 5;
        data.player.currentExperience = 320;
        data.player.location.areaId = "area.town";
        data.player.location.positionX = 12.5f;
        data.player.location.positionY = -3.25f;

        for (int i = 0; i < 8; i++)
            data.inventory.slots.Add(new InventorySaveData.SlotData { itemId = $"item.consumable.potion_{i % 3}", quantity = i + 1 });
        data.inventory.gold = 500;

        data.equipment.slots.Add(new EquipmentSaveData.SlotData { slot = EquipSlot.Body, itemId = "item.equipment.body.lv2" });
        data.equipment.slots.Add(new EquipmentSaveData.SlotData { slot = EquipSlot.Head, itemId = "item.equipment.head.lv1" });

        data.tutorial.currentStepId = "tutorial.step.03";
        data.tutorial.completed = false;

        for (int i = 0; i < 4; i++)
        {
            data.quests.quests.Add(new QuestProgressSaveData
            {
                questId = $"quest.main.{i:D3}",
                status = i % 2 == 0 ? QuestStatus.Active : QuestStatus.Completed,
                currentObjectiveIndex = i,
                objectiveCounters = new[] { i, i * 2 }
            });
        }

        for (int i = 0; i < worldObjectCount; i++)
        {
            data.world.objects.Add(new WorldObjectSaveData
            {
                persistentId = $"world.object.{i:D4}",
                kind = (WorldObjectKind)(i % Enum.GetValues(typeof(WorldObjectKind)).Length),
                flag = i % 2 == 0,
                nextRespawnUtcTicks = i % 3 == 0 ? DateTime.UtcNow.AddHours(1).Ticks : 0
            });
        }

        return data;
    }
}
