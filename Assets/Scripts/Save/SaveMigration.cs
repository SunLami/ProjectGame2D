using System;
using System.Collections.Generic;

/// <summary>
/// Runs GameSaveData through the additive-default migration chain from its own saveVersion up to
/// GameSaveData.CurrentSaveVersion, strictly N -> N+1 (SaveAndWorldPersistence.md: "Không viết
/// migration V1 -> Current riêng cho mỗi version"). Every step here fills in the exact same
/// defaults NewGameFactory.CreateDefault() would use for a brand-new character, so a save that
/// predates a domain restores that domain as "nothing happened yet" rather than crashing or
/// silently skipping the rest of restore (every restore call site already null-checks its own
/// sub-DTO -- see PlayerSpawnReadinessSource -- but a completely null `player` short-circuits the
/// *entire* restore pass, which this pipeline now prevents).
///
/// This never touches disk and never re-writes the source save -- FileSaveSlotRepository decides
/// when (if ever) the upgraded shape gets persisted, via the player's next real save.
/// </summary>
public static class SaveMigration
{
    /// <summary>Oldest saveVersion this pipeline knows how to upgrade. A save older than this (or
    /// with a non-positive/unrecognized version) is not migratable and must be treated as
    /// Corrupted/IncompatibleVersion by the caller, not silently guessed at.</summary>
    public const int MinimumSupportedVersion = 1;

    private static readonly List<ISaveMigrationStep> Steps = new()
    {
        new V1ToV2_IntroducesPlayer(),
        new V2ToV3_IntroducesInventoryAndEquipment(),
        new V3ToV4_IntroducesTutorial(),
        new V4ToV5_IntroducesQuests(),
        new V5ToV6_IntroducesWorld(),
    };

    /// <summary>True if this version can be upgraded to CurrentSaveVersion by Migrate(). Does not
    /// mean the save is otherwise valid (still needs to parse/have a saveId, checked separately).</summary>
    public static bool CanMigrate(int fromVersion) =>
        fromVersion >= MinimumSupportedVersion && fromVersion <= GameSaveData.CurrentSaveVersion;

    /// <summary>Mutates data in place up to CurrentSaveVersion and returns it. Idempotent: calling
    /// this again on already-current data (or re-running with the same fromVersion) changes
    /// nothing further, since every step only fills in fields that are still null.</summary>
    public static GameSaveData Migrate(GameSaveData data)
    {
        if (data == null)
            return null;

        int version = data.saveVersion;
        foreach (ISaveMigrationStep step in Steps)
        {
            if (version == step.FromVersion)
            {
                step.Apply(data);
                version = step.ToVersion;
            }
        }

        data.saveVersion = GameSaveData.CurrentSaveVersion;
        return data;
    }

    // ---- Steps (Phase 3-8 history, see SaveAndWorldPersistence.md) ----

    private sealed class V1ToV2_IntroducesPlayer : ISaveMigrationStep
    {
        public int FromVersion => 1;
        public int ToVersion => 2;

        public void Apply(GameSaveData data)
        {
            data.player ??= new PlayerSaveData
            {
                level = 1,
                currentExperience = 0,
                health = -1f, // sentinel: use current MaxHealth (NewGameFactory convention)
                location = new PlayerLocationSaveData
                {
                    sceneId = null,
                    areaId = NewGameFactory.TutorialAreaId,
                    positionX = float.NaN,
                    positionY = float.NaN,
                    fallbackSpawnId = NewGameFactory.TutorialStartSpawnId
                }
            };
        }
    }

    private sealed class V2ToV3_IntroducesInventoryAndEquipment : ISaveMigrationStep
    {
        public int FromVersion => 2;
        public int ToVersion => 3;

        public void Apply(GameSaveData data)
        {
            data.inventory ??= new InventorySaveData();
            data.equipment ??= new EquipmentSaveData();
        }
    }

    private sealed class V3ToV4_IntroducesTutorial : ISaveMigrationStep
    {
        public int FromVersion => 3;
        public int ToVersion => 4;

        public void Apply(GameSaveData data) => data.tutorial ??= new TutorialSaveData();
    }

    private sealed class V4ToV5_IntroducesQuests : ISaveMigrationStep
    {
        public int FromVersion => 4;
        public int ToVersion => 5;

        public void Apply(GameSaveData data) => data.quests ??= new QuestSaveData();
    }

    private sealed class V5ToV6_IntroducesWorld : ISaveMigrationStep
    {
        public int FromVersion => 5;
        public int ToVersion => 6;

        public void Apply(GameSaveData data) => data.world ??= new WorldSaveData();
    }
}
