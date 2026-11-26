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
        new V6ToV7_IntroducesInventoryItemInstances(),
        new V7ToV8_IntroducesTrackedQuest(),
        new V8ToV9_IntroducesQuickBar(),
        new V9ToV10_IntroducesFarming(),
        new V10ToV11_IntroducesFarmStorage(),
        new V11ToV12_FourQuickSlots(),
        new V12ToV13_SkillProgression(),
        new V13ToV14_RetireOutOfSheetItems(),
        new V14ToV15_FundPendingActOneDrinks(),
        new V15ToV16_StageActOneQuests(),
        new V16ToV17_ActOneForgeProgression(),
        new V17ToV18_RequireAgnesSkillsPanel(),
    };

    /// <summary>Stable item IDs retired 2026-11-25 (owner-approved: the 13 ItemSO not in the Items
    /// List sheet) and the sheet item each one becomes. Several old IDs may share one target; the
    /// remap is per slot, so quantities and slot positions are kept and nothing merges or splits
    /// (every target's maxStackSize is 99, >= every retired item's 1/20/99). Kept public and
    /// read-only for tests and tooling; never extend it to rename IDs that still exist.</summary>
    public static readonly IReadOnlyDictionary<string, string> RetiredItemReplacements =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["item.drink.water"] = "item.drink.lemon_juice",
            ["item.drink.ale"] = "item.drink.apple_juice",
            ["item.material.medicinal_leaf"] = "item.material.health_herb",
            ["item.material.plank"] = "item.material.wood_log",
            ["item.consumable.health_potion"] = "item.consumable.small_health_potion",
            ["item.unique.ancient_relic"] = "item.material.tanzanite",
            ["item.quest.tutorial_badge"] = "item.material.gold_bar",
            ["item.food.bread"] = "item.food.harvest_stew",
            ["item.food.cheese"] = "item.food.harvest_stew",
            ["item.seed.eggplant"] = "item.seed.carrot",
            ["item.crop.eggplant"] = "item.crop.carrot",
            ["item.consumable.potion_buff_defense"] = "item.consumable.small_health_potion",
            ["item.consumable.potion_buff_attack"] = "item.consumable.small_mana_potion",
        };

    /// <summary>Retired crop IDs (planted plots) and their replacement crop.</summary>
    public static readonly IReadOnlyDictionary<string, string> RetiredCropReplacements =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["crop.eggplant"] = "crop.carrot",
        };

    private static string RemapItem(string itemId) =>
        itemId != null && RetiredItemReplacements.TryGetValue(itemId, out string replacement) ? replacement : itemId;

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
    /// <summary>Rewrites every persisted item/crop ID that was retired with the 13 out-of-sheet
    /// ItemSO (DecisionRegister 2026-11-25). Touches only ID strings in place: inventory slots,
    /// equipment slots, quick bar assignments, farm storage slots and planted crops (plot ID and
    /// plantedAtUtcTicks kept). Quantities, slot order, gold, quest state/counters and world
    /// records (persistentId + flag only, no item IDs) are left untouched, so e.g. the DemoScene
    /// Ancient Relic pickup stays collected under its unchanged persistentId.</summary>
    private sealed class V13ToV14_RetireOutOfSheetItems : ISaveMigrationStep
    {
        public int FromVersion => 13;
        public int ToVersion => 14;

        public void Apply(GameSaveData data)
        {
            if (data.inventory?.slots != null)
                foreach (InventorySaveData.SlotData slot in data.inventory.slots)
                    if (slot != null) slot.itemId = RemapItem(slot.itemId);

            if (data.equipment?.slots != null)
                foreach (EquipmentSaveData.SlotData slot in data.equipment.slots)
                    if (slot != null) slot.itemId = RemapItem(slot.itemId);

            if (data.quickBar?.assignedItemIds != null)
                for (int i = 0; i < data.quickBar.assignedItemIds.Count; i++)
                    data.quickBar.assignedItemIds[i] = RemapItem(data.quickBar.assignedItemIds[i]);

            if (data.farmStorage?.slots != null)
                foreach (FarmStorageSaveData.SlotData slot in data.farmStorage.slots)
                    if (slot != null) slot.itemId = RemapItem(slot.itemId);

            if (data.farming?.plots != null)
                foreach (FarmPlotSaveData plot in data.farming.plots)
                    if (plot?.cropId != null && RetiredCropReplacements.TryGetValue(plot.cropId, out string crop))
                        plot.cropId = crop;
        }
    }

    // The formerly free tutorial purchase now costs gold. Only top up the outstanding
    // purchase budget for saves that have already passed Wigheard's new gold reward.
    private sealed class V14ToV15_FundPendingActOneDrinks : ISaveMigrationStep
    {
        public int FromVersion => 14;
        public int ToVersion => 15;

        public void Apply(GameSaveData data)
        {
            var quests = data.quests?.quests;
            if (quests == null || !quests.Exists(quest => quest?.questId == "quest.act1.new_journey"
                && quest.status == QuestStatus.Completed))
                return;

            var drinks = quests.Find(quest => quest?.questId == "quest.act1.drink_before_road");
            if (drinks?.status is QuestStatus.ReadyToTurnIn or QuestStatus.Completed)
                return;

            // Counters [1] and [2] are the existing sequential Carrot and Grape purchases.
            // Freeze the initial maximum prices here so later balancing cannot alter migration.
            int[] counters = drinks?.objectiveCounters;
            int carrot = Remaining(counters, 1);
            int grape = Remaining(counters, 2);
            int budget = carrot * 12 + grape * 16;
            data.inventory ??= new InventorySaveData();
            data.inventory.gold = Math.Max(data.inventory.gold, budget);
        }

        private static int Remaining(int[] counters, int index) =>
            5 - (counters != null && index < counters.Length
                ? Math.Max(0, Math.Min(5, counters[index]))
                : 0);
    }

    private sealed class V17ToV18_RequireAgnesSkillsPanel : ISaveMigrationStep
    {
        public int FromVersion => 17;
        public int ToVersion => 18;

        public void Apply(GameSaveData data)
        {
            QuestProgressSaveData quest = data.quests?.quests?.Find(entry =>
                entry?.questId == "quest.act1.far_from_your_kind");
            if (quest == null)
                return;

            if (quest.status == QuestStatus.Completed)
            {
                quest.currentObjectiveIndex = 2;
                quest.objectiveCounters = new[] { 1, 1 };
            }
            else if (quest.status == QuestStatus.ReadyToTurnIn
                || (quest.status == QuestStatus.Active && quest.currentObjectiveIndex > 0))
            {
                quest.status = QuestStatus.Active;
                quest.currentObjectiveIndex = 1;
                quest.objectiveCounters = new[] { 1, 0 };
            }
        }
    }

    private sealed class V16ToV17_ActOneForgeProgression : ISaveMigrationStep
    {
        private const string ForgeVisit = "quest.act1.ring_of_forge";
        private const string ForgeSword = "quest.act1.forge_brasstan_edge";
        private const string GoldBar = "item.material.gold_bar";

        public int FromVersion => 16;
        public int ToVersion => 17;

        public void Apply(GameSaveData data)
        {
            var quests = data.quests?.quests;
            if (quests == null)
                return;

            int experience = 0;
            var counted = new HashSet<string>(StringComparer.Ordinal);
            foreach (QuestProgressSaveData quest in quests)
                if (quest?.status == QuestStatus.Completed && counted.Add(quest.questId))
                    experience += ExperienceFor(quest.questId);

            if (experience > 0)
                ApplyExperience(data, experience);

            if (!counted.Contains(ForgeVisit))
                return;

            var slots = data.inventory?.slots;
            bool alreadyForged = slots?.Exists(slot => slot?.itemId == "sword_lvl2" && slot.quantity > 0) == true
                || data.equipment?.slots?.Exists(slot => slot?.itemId == "sword_lvl2") == true;
            bool startedAgnes = quests.Exists(quest => quest?.questId == "quest.act1.far_from_your_kind");
            if (alreadyForged || startedAgnes)
            {
                quests.Add(new QuestProgressSaveData
                {
                    questId = ForgeSword,
                    status = QuestStatus.Completed,
                    currentObjectiveIndex = 1,
                    objectiveCounters = new[] { 1 }
                });
                return;
            }

            // The old turn-in granted five bars. Credit the five-bar difference, using a stack or
            // an empty slot first. A fully occupied inventory keeps the bars in an extra saved slot.
            data.inventory ??= new InventorySaveData();
            slots = data.inventory.slots ??= new List<InventorySaveData.SlotData>();
            InventorySaveData.SlotData stack = slots.Find(slot => slot?.itemId == GoldBar && slot.quantity <= 94);
            if (stack != null)
            {
                stack.quantity += 5;
                return;
            }

            int emptyIndex = slots.FindIndex(slot => slot == null || string.IsNullOrEmpty(slot.itemId));
            var grant = new InventorySaveData.SlotData { itemId = GoldBar, quantity = 5 };
            if (emptyIndex >= 0)
                slots[emptyIndex] = grant;
            else
                slots.Add(grant);
        }

        private static int ExperienceFor(string questId) => questId switch
        {
            "quest.act1.new_journey" => 100,
            "quest.act1.drink_before_road" => 150,
            "quest.act1.stocking_up_oswin" => 80,
            "quest.act1.stocking_up_eadric" => 80,
            "quest.act1.back_to_fields" => 180,
            "quest.act1.harvest_the_fields" => 260,
            "quest.act1.back_to_eadric" => 140,
            "quest.act1.back_to_oswin" => 140,
            "quest.act1.cast_a_line" => 320,
            _ => 0
        };

        private static void ApplyExperience(GameSaveData data, int experience)
        {
            PlayerSaveData player = data.player ??= new PlayerSaveData();
            int oldLevel = Math.Max(1, player.level);
            player.level = oldLevel;
            long total = (long)Math.Max(0, player.currentExperience) + experience;
            int required = PlayerStat.CalculateExperienceRequirement(player.level);
            while (total >= required)
            {
                total -= required;
                player.level++;
                required = PlayerStat.CalculateExperienceRequirement(player.level);
            }

            player.currentExperience = (int)total;
            SkillProgressionSaveData skills = player.skills ??= SkillProgressionSaveData.ForLevel(oldLevel);
            int awardedThrough = Math.Max(oldLevel, skills.awardedThroughLevel);
            if (player.level > awardedThrough)
            {
                skills.points += player.level - awardedThrough;
                skills.awardedThroughLevel = player.level;
            }
        }
    }

    private sealed class V15ToV16_StageActOneQuests : ISaveMigrationStep
    {
        private const string Journey = "quest.act1.new_journey";
        private const string Planting = "quest.act1.back_to_fields";
        private const string Harvest = "quest.act1.harvest_the_fields";

        public int FromVersion => 15;
        public int ToVersion => 16;

        public void Apply(GameSaveData data)
        {
            var quests = data.quests?.quests;
            if (quests == null)
                return;

            QuestProgressSaveData journey = quests.Find(entry => entry?.questId == Journey);
            if (journey != null && journey.status != QuestStatus.Completed)
            {
                // The old one-step conversation did not give the sword until turn-in.
                // Make the revised offer available so the player can claim the blade on Accept.
                quests.Remove(journey);
                if (data.quests.trackedQuestId == Journey)
                    data.quests.trackedQuestId = null;
            }
            if (data.tutorial != null && !data.tutorial.completed
                && (data.tutorial.currentStepId is "tutorial.controls.open_inventory"
                    or "tutorial.controls.equip_item" or "tutorial.controls.attack_dummy"
                    || (data.tutorial.currentStepId == "tutorial.controls.wait_trainer"
                        && journey?.status == QuestStatus.Completed)))
            {
                data.tutorial.completed = true;
                data.tutorial.currentStepId = null;
            }

            QuestProgressSaveData farm = quests.Find(entry => entry?.questId == Planting);
            if (farm == null)
                return;

            int[] planted = CopyCounters(farm.objectiveCounters, 3, 5);
            int[] harvested = CopyCounters(farm.objectiveCounters, 0, 15);
            bool alreadyPaid = farm.status == QuestStatus.Completed;
            int plantIndex = FirstIncomplete(planted, 5);
            int harvestIndex = FirstIncomplete(harvested, 15);
            bool readyToTurnIn = farm.status == QuestStatus.ReadyToTurnIn
                || (plantIndex == 3 && harvestIndex == 3);
            bool plantingFinished = alreadyPaid || readyToTurnIn;

            farm.status = plantingFinished ? QuestStatus.Completed : QuestStatus.Active;
            farm.currentObjectiveIndex = plantingFinished ? 3 : plantIndex;
            farm.objectiveCounters = plantingFinished ? new[] { 5, 5, 5 } : planted;
            quests.Add(new QuestProgressSaveData
            {
                questId = Harvest,
                // Defer unfinished legacy harvest counters until the planting stage completes.
                status = alreadyPaid ? QuestStatus.Completed
                    : readyToTurnIn ? QuestStatus.ReadyToTurnIn : QuestStatus.Locked,
                currentObjectiveIndex = plantingFinished ? 3 : harvestIndex,
                objectiveCounters = plantingFinished ? new[] { 15, 15, 15 } : harvested
            });
            if (readyToTurnIn && !alreadyPaid && data.quests.trackedQuestId == Planting)
                data.quests.trackedQuestId = Harvest;
        }

        private static int[] CopyCounters(int[] oldCounters, int offset, int target)
        {
            var counters = new int[3];
            for (int i = 0; i < counters.Length; i++)
                if (oldCounters != null && offset + i < oldCounters.Length)
                    counters[i] = Math.Max(0, Math.Min(oldCounters[offset + i], target));
            return counters;
        }

        private static int FirstIncomplete(int[] counters, int target)
        {
            int index = Array.FindIndex(counters, value => value < target);
            return index < 0 ? counters.Length : index;
        }
    }

    private sealed class V12ToV13_SkillProgression : ISaveMigrationStep
    {
        public int FromVersion => 12;
        public int ToVersion => 13;
        public void Apply(GameSaveData data)
        {
            data.player ??= new PlayerSaveData();
            // V12 never owned this field. Unity may synthesize a default nested object even
            // when the JSON field is absent, so null-coalescing would miss legacy backfill.
            data.player.skills = SkillProgressionSaveData.ForLevel(data.player.level);
        }
    }

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

    private sealed class V6ToV7_IntroducesInventoryItemInstances : ISaveMigrationStep
    {
        public int FromVersion => 6;
        public int ToVersion => 7;

        public void Apply(GameSaveData data)
        {
            data.inventory ??= new InventorySaveData();
            data.inventory.slots ??= new List<InventorySaveData.SlotData>();
        }
    }

    private sealed class V7ToV8_IntroducesTrackedQuest : ISaveMigrationStep
    {
        public int FromVersion => 7;
        public int ToVersion => 8;

        public void Apply(GameSaveData data)
        {
            data.quests ??= new QuestSaveData();
            if (!string.IsNullOrEmpty(data.quests.trackedQuestId) || data.quests.quests == null)
                return;

            foreach (QuestProgressSaveData quest in data.quests.quests)
            {
                if (quest.status == QuestStatus.Active || quest.status == QuestStatus.ReadyToTurnIn)
                {
                    data.quests.trackedQuestId = quest.questId;
                    break;
                }
            }
        }
    }

    private sealed class V8ToV9_IntroducesQuickBar : ISaveMigrationStep
    {
        public int FromVersion => 8;
        public int ToVersion => 9;

        public void Apply(GameSaveData data) => data.quickBar ??= new QuickBarSaveData();
    }

    private sealed class V9ToV10_IntroducesFarming : ISaveMigrationStep
    {
        public int FromVersion => 9;
        public int ToVersion => 10;

        public void Apply(GameSaveData data) => data.farming ??= new FarmingSaveData();
    }

    private sealed class V11ToV12_FourQuickSlots : ISaveMigrationStep
    {
        public int FromVersion => 11;
        public int ToVersion => 12;

        public void Apply(GameSaveData data)
        {
            data.quickBar ??= new QuickBarSaveData();
            data.quickBar.assignedItemIds ??= QuickBarSaveData.CreateEmptyAssignments();
            var assignments = data.quickBar.assignedItemIds;
            if (assignments.Count > QuickBarSaveData.SlotCount)
                assignments.RemoveRange(QuickBarSaveData.SlotCount, assignments.Count - QuickBarSaveData.SlotCount);
            while (assignments.Count < QuickBarSaveData.SlotCount) assignments.Add(null);
            if (data.quickBar.selectedIndex < 0 || data.quickBar.selectedIndex >= QuickBarSaveData.SlotCount)
                data.quickBar.selectedIndex = 0;
        }
    }

    private sealed class V10ToV11_IntroducesFarmStorage : ISaveMigrationStep
    {
        public int FromVersion => 10;
        public int ToVersion => 11;

        public void Apply(GameSaveData data) => data.farmStorage ??= new FarmStorageSaveData();
    }
}
