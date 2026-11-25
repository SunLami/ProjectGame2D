using System;
using System.Collections.Generic;

/// <summary>
/// Pure planning rule behind QuestFarmSupply: given a quest's remaining Gather (harvest) and
/// Replant objectives, how many crops of each kind are already in the ground and how many matching
/// seeds the player holds, decide the smallest top-up that keeps the quest completable. No Unity,
/// scene or save dependency, so it is unit-tested directly.
///
/// The quest runtime is strictly ordered (QuestRuntimeState), so a crop harvested while a
/// different objective is current does not count. Without a top-up that mistake could leave no
/// ripe crop and no seed for a later objective (Grape/Lemon seeds have no shop price yet). The plan
/// is capped to what the remaining objectives need, so it can never become a free farming source.
/// </summary>
public static class QuestFarmSupplyPlanner
{
    public readonly struct CropNeed
    {
        public CropNeed(string cropId, int harvestRemaining, int replantRemaining)
        {
            CropId = cropId;
            HarvestRemaining = harvestRemaining;
            ReplantRemaining = replantRemaining;
        }

        public string CropId { get; }
        public int HarvestRemaining { get; }
        public int ReplantRemaining { get; }
    }

    public readonly struct CropSupply
    {
        public CropSupply(string cropId, int maturePlots, int seeds)
        {
            CropId = cropId;
            MaturePlots = maturePlots;
            Seeds = seeds;
        }

        public string CropId { get; }
        public int MaturePlots { get; }
        public int Seeds { get; }
    }

    /// <summary>Remaining per-crop need from the current objective onward. Gather objectives are
    /// harvest (item count), Replant objectives are plantings. Earlier objectives are done.</summary>
    public static List<CropNeed> RemainingNeeds(
        IReadOnlyList<QuestObjectiveDefinition> objectives, int currentIndex, IReadOnlyList<int> counters)
    {
        var harvest = new Dictionary<string, int>(StringComparer.Ordinal);
        var replant = new Dictionary<string, int>(StringComparer.Ordinal);
        var order = new List<string>();
        if (objectives == null)
            return new List<CropNeed>();

        for (int i = Math.Max(0, currentIndex); i < objectives.Count; i++)
        {
            QuestObjectiveDefinition objective = objectives[i];
            if (objective == null || string.IsNullOrEmpty(objective.TargetId))
                continue;
            if (objective.Type != QuestObjectiveType.Gather && objective.Type != QuestObjectiveType.Replant)
                continue;

            int done = counters != null && i < counters.Count ? counters[i] : 0;
            int remaining = Math.Max(0, objective.TargetCount - done);
            Dictionary<string, int> bucket = objective.Type == QuestObjectiveType.Gather ? harvest : replant;
            if (!harvest.ContainsKey(objective.TargetId) && !replant.ContainsKey(objective.TargetId))
                order.Add(objective.TargetId);
            bucket[objective.TargetId] = (bucket.TryGetValue(objective.TargetId, out int sum) ? sum : 0) + remaining;
        }

        var needs = new List<CropNeed>(order.Count);
        foreach (string cropId in order)
        {
            needs.Add(new CropNeed(cropId,
                harvest.TryGetValue(cropId, out int h) ? h : 0,
                replant.TryGetValue(cropId, out int r) ? r : 0));
        }
        return needs;
    }

    /// <summary>Top-up for one crop. plantedPlots counts every plot growing that crop (any stage);
    /// heldSeeds counts storage + inventory. allowMature lets the plan place ripe crops into empty
    /// plots (quest start / restoring a save whose farm cannot cover the need); otherwise only
    /// seeds are added, so a mistake mid-quest is recovered by replanting, not by crops appearing.</summary>
    public static CropSupply PlanCrop(CropNeed need, int yieldPerHarvest, int plantedPlots, int heldSeeds,
        int emptyPlotsAvailable, bool allowMature, bool questJustStarted = false)
    {
        int yield = Math.Max(1, yieldPerHarvest);
        int plotsForHarvest = (need.HarvestRemaining + yield - 1) / yield;
        int missingPlots = Math.Max(0, plotsForHarvest - Math.Max(0, plantedPlots));
        int seedsBeyondReplant = Math.Max(0, heldSeeds - need.ReplantRemaining);

        int mature = 0;
        if (allowMature)
        {
            // At quest start the authored "plots full of ripe crops" always appear, whatever seeds
            // the player already carries; on a later restore, held seeds count as coverage first.
            int coveredBySeeds = questJustStarted ? 0 : seedsBeyondReplant;
            mature = Math.Min(Math.Max(0, missingPlots - coveredBySeeds), Math.Max(0, emptyPlotsAvailable));
            missingPlots -= mature;
        }

        int seedsNeeded = need.ReplantRemaining + missingPlots;
        return new CropSupply(need.CropId, mature, Math.Max(0, seedsNeeded - Math.Max(0, heldSeeds)));
    }
}
