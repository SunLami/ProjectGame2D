using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Measures the save pipeline's individual stages separately (Phase 10 Part 5) -- JSON serialize,
/// atomic write, file read, and migration are reported as distinct numbers rather than one combined
/// figure, using representative (non-empty) save data. Runs against a temporary directory only.
/// Does not auto-run: invoke explicitly from the Tools menu.
///
/// Scope note: this measures the save FILE pipeline (the part owned by FileSaveSlotRepository /
/// SaveMigration). It does not measure "restore" in the sense of applying a GameSaveData onto live
/// MonoBehaviours (PlayerSpawnReadinessSource's step 1-9 orchestration), since that requires a
/// running scene and is exercised functionally (not timed) by the PlayMode test suite instead.
/// </summary>
public static class SaveProfilingRunner
{
    private const string MenuPath = "Tools/Project Game/Run Save Profiling";
    private const int Iterations = 50;

    [MenuItem(MenuPath)]
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "ProjectGame2DProfiling_" + Guid.NewGuid().ToString("N"));
        var log = new StringBuilder();

        try
        {
            log.AppendLine("=== Save Pipeline Profiling ===");
            log.AppendLine($"Started (UTC): {DateTime.UtcNow:O}");
            log.AppendLine($"Unity: {Application.unityVersion} | Platform: {Application.platform} | Editor: yes (results are Editor-process timings, not a device build)");
            log.AppendLine($"Iterations per stage: {Iterations}");
            log.AppendLine();

            GameSaveData representative = BuildRepresentativeSave();
            string json = JsonUtility.ToJson(representative);
            log.AppendLine($"Representative save.json size: {Encoding.UTF8.GetByteCount(json)} bytes " +
                $"(player+8 inventory slots+2 equipment+4 quests+60 world objects).");
            log.AppendLine();

            ProfileStage(log, "Serialize (JsonUtility.ToJson)", Iterations,
                () => JsonUtility.ToJson(representative));

            ProfileStage(log, "Deserialize (JsonUtility.FromJson)", Iterations,
                () => JsonUtility.FromJson<GameSaveData>(json));

            var repository = new FileSaveSlotRepository(root);
            ProfileStage(log, "Atomic write (WriteSave: serialize+temp-write+round-trip-validate+File.Replace)", Iterations,
                () => repository.WriteSave(1, representative));

            ProfileStage(log, "File read (TryReadSave: File.ReadAllText+FromJson+status checks)", Iterations,
                () => repository.TryReadSave(1, out _));

            GameSaveData v1Fixture = new() { saveVersion = 1, saveId = "profiling-v1", totalPlayTimeSeconds = 1 };
            string v1Json = JsonUtility.ToJson(v1Fixture);
            ProfileStage(log, "Migration (V1 -> Current, deserialize+additive defaults)", Iterations,
                () => SaveMigration.Migrate(JsonUtility.FromJson<GameSaveData>(v1Json)));

            log.AppendLine();
            log.AppendLine("No fabricated performance budgets are asserted here -- these are baseline measurements " +
                "for this machine/Editor only, to be compared against future runs, not a pass/fail gate.");
        }
        catch (Exception exception)
        {
            log.AppendLine($"PROFILING THREW: {exception}");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        Debug.Log(log.ToString());
    }

    private static void ProfileStage(StringBuilder log, string label, int iterations, Action action)
    {
        // Warm up once (JIT/first-file-handle) so the timed loop reflects steady-state cost.
        action();

        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            action();
        stopwatch.Stop();
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        double totalMs = stopwatch.Elapsed.TotalMilliseconds;
        double avgMs = totalMs / iterations;
        long allocDelta = allocAfter - allocBefore;

        log.AppendLine($"[{label}]");
        log.AppendLine($"  {iterations} iterations: {totalMs:F2} ms total, {avgMs:F3} ms/iteration avg.");
        if (allocDelta > 0)
        {
            log.AppendLine($"  Managed allocation across run: {allocDelta / 1024.0:F1} KB total, " +
                $"{allocDelta / (double)iterations:F0} bytes/iteration avg (GC.GetAllocatedBytesForCurrentThread delta).");
        }
        else
        {
            log.AppendLine("  Managed allocation: not reliably measurable in this Editor/Mono runtime " +
                "(GC.GetAllocatedBytesForCurrentThread returned 0 delta despite real allocations occurring) -- " +
                "reported honestly as unavailable rather than fabricated.");
        }
        log.AppendLine();
    }

    private static GameSaveData BuildRepresentativeSave()
    {
        GameSaveData data = NewGameFactory.CreateDefault();
        data.saveId = "profiling-representative";
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

        for (int i = 0; i < 60; i++)
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
