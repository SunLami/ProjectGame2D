using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene glue that keeps a harvest/replant quest (Act I quest.act1.back_to_fields) completable:
/// when the quest is accepted it fills the authored starter plots with ripe crops and leaves the
/// seeds needed for the Replant objectives in Farm Storage ("Frawand arrives at the farm and finds
/// several plots full of crops ... notices some seeds among the supplies"). It re-checks after
/// every harvest/plant/storage change and on loading a save, topping up only the missing seeds
/// (QuestFarmSupplyPlanner), so harvesting in the wrong order, a save made before this existed or
/// a save whose farm is empty can never soft-lock the quest.
///
/// Persists nothing of its own: every grant lands in existing farm/storage save data, and the plan
/// is derived from quest progress + world state each time, so it is idempotent across save/load.
/// Does nothing while the quest is not Active.
/// </summary>
public sealed class QuestFarmSupply : MonoBehaviour
{
    [Serializable]
    private struct StarterPlot
    {
        public string plotId;
        public CropDefinition crop;
    }

    [SerializeField] private string _questId = "quest.act1.back_to_fields";
    [SerializeField] private FarmingManager _farmingManager;
    [Tooltip("Preferred plots for the ripe crops placed when the quest starts. Occupied plots are "
        + "skipped; any other empty plot is used if these are not enough.")]
    [SerializeField] private StarterPlot[] _starterPlots;
    [Tooltip("Seed item for each crop the quest can need (matched by SeedItemSO.Crop).")]
    [SerializeField] private SeedItemSO[] _seeds;

    private QuestManager _questManager;
    private bool _dirty = true;
    private bool _allowMature = true;
    private bool _questJustStarted;

    private void OnEnable()
    {
        FarmingDomainEvents.CropHarvested += HandleHarvested;
        FarmingDomainEvents.CropPlanted += HandlePlanted;
        _dirty = true;
        _allowMature = true;
    }

    private void OnDisable()
    {
        FarmingDomainEvents.CropHarvested -= HandleHarvested;
        FarmingDomainEvents.CropPlanted -= HandlePlanted;
        BindQuestManager(null);
        BindStorage(false);
    }

    private void Update()
    {
        if (_questManager != QuestManager.Instance)
            BindQuestManager(QuestManager.Instance);
        BindStorage(true);

        if (!_dirty || !CanApplyNow())
            return;

        _dirty = false;
        bool allowMature = _allowMature;
        bool justStarted = _questJustStarted;
        _allowMature = false;
        _questJustStarted = false;
        Apply(allowMature, justStarted);
    }

    // Only while live gameplay is running: never mid-restore (restore must not create state) and
    // never before the save's farm/storage/quests have all been restored into the scene.
    private static bool CanApplyNow() =>
        GameStateManager.Instance != null
        && GameStateManager.Instance.CurrentState == GameState.Playing
        && (GameSessionManager.Instance == null || !GameSessionManager.Instance.IsRestoring);

    private void BindQuestManager(QuestManager manager)
    {
        if (_questManager != null)
        {
            _questManager.QuestAccepted -= HandleQuestAccepted;
            _questManager.QuestProgressChanged -= HandleQuestProgress;
        }
        _questManager = manager;
        if (_questManager != null)
        {
            _questManager.QuestAccepted += HandleQuestAccepted;
            _questManager.QuestProgressChanged += HandleQuestProgress;
            _dirty = true;
            _allowMature = true;
        }
    }

    private FarmStorageManager _boundStorage;

    private void BindStorage(bool bind)
    {
        FarmStorageManager target = bind ? FarmStorageManager.Instance : null;
        if (_boundStorage == target)
            return;
        if (_boundStorage != null)
            _boundStorage.Changed -= HandleSupplyChanged;
        _boundStorage = target;
        if (_boundStorage != null)
            _boundStorage.Changed += HandleSupplyChanged;
    }

    private void HandleQuestAccepted(string questId)
    {
        if (questId != _questId)
            return;
        _dirty = true;
        _allowMature = true;
        _questJustStarted = true;
    }

    private void HandleQuestProgress(string questId)
    {
        if (questId == _questId)
            _dirty = true;
    }

    private void HandleHarvested(string plotId, string cropId, int quantity) => _dirty = true;
    private void HandlePlanted(string plotId, string cropId) => _dirty = true;
    private void HandleSupplyChanged() => _dirty = true;

    /// <summary>Applies the supply plan now. Returns the number of ripe plots placed and seeds
    /// added (for tests/diagnostics).</summary>
    public (int maturePlaced, int seedsAdded) Apply(bool allowMature, bool questJustStarted = false)
    {
        FarmingManager farm = _farmingManager != null ? _farmingManager : FarmingManager.Instance;
        if (_questManager == null || farm == null || farm.Catalog == null
            || _questManager.GetStatus(_questId) != QuestStatus.Active
            || !_questManager.Catalog.TryResolve(_questId, out QuestDefinition quest)
            || !_questManager.TryGetProgress(_questId, out QuestProgressSnapshot progress))
        {
            return (0, 0);
        }

        int maturePlaced = 0;
        int seedsAdded = 0;
        foreach (QuestFarmSupplyPlanner.CropNeed need in QuestFarmSupplyPlanner.RemainingNeeds(
                     quest.Objectives, progress.CurrentObjectiveIndex, progress.ObjectiveCounters))
        {
            if (!farm.Catalog.TryResolve(need.CropId, out CropDefinition crop))
                continue;

            SeedItemSO seed = FindSeed(need.CropId);
            int held = SeedCount(seed);
            QuestFarmSupplyPlanner.CropSupply plan = QuestFarmSupplyPlanner.PlanCrop(
                need, crop.MinimumHarvestQuantity, PlantedCount(farm, need.CropId), held,
                EmptyPlotCount(farm), allowMature, questJustStarted);

            for (int i = 0; i < plan.MaturePlots; i++)
            {
                if (!TryPlaceMature(farm, crop))
                    break;
                maturePlaced++;
            }

            if (plan.Seeds > 0 && seed != null && FarmStorageManager.Instance != null)
            {
                FarmStorageManager.Instance.EnsureSlotCount(InventoryManager.Instance != null
                    ? InventoryManager.Instance.Slots.Count : 0);
                if (FarmStorageManager.Instance.TryDeposit(seed, plan.Seeds))
                    seedsAdded += plan.Seeds;
            }
            else if (plan.Seeds > 0)
            {
                Debug.LogWarning($"QuestFarmSupply: no seed item/storage for '{need.CropId}'; "
                    + $"{plan.Seeds} seed(s) could not be supplied.", this);
            }
        }

        if (maturePlaced > 0 || seedsAdded > 0)
            FarmingDomainEvents.RaiseFarmStateChanged();
        // Our own storage deposit re-dirtied us; the plan is already satisfied, so skip that pass.
        _dirty = false;
        return (maturePlaced, seedsAdded);
    }

    private bool TryPlaceMature(FarmingManager farm, CropDefinition crop)
    {
        if (_starterPlots != null)
        {
            foreach (StarterPlot starter in _starterPlots)
            {
                if (starter.crop == crop && farm.TryPlaceMatureCrop(starter.plotId, crop))
                    return true;
            }
        }
        foreach (FarmPlot plot in farm.Plots)
        {
            if (plot != null && !plot.HasCrop && !IsReservedStarter(plot.PlotId, crop)
                && farm.TryPlaceMatureCrop(plot.PlotId, crop))
                return true;
        }
        return false;
    }

    // Keep another crop's authored starter plot free for that crop when possible.
    private bool IsReservedStarter(string plotId, CropDefinition crop)
    {
        if (_starterPlots == null)
            return false;
        foreach (StarterPlot starter in _starterPlots)
            if (starter.plotId == plotId && starter.crop != null && starter.crop != crop)
                return true;
        return false;
    }

    private SeedItemSO FindSeed(string cropId)
    {
        if (_seeds == null)
            return null;
        foreach (SeedItemSO seed in _seeds)
            if (seed != null && seed.Crop != null && seed.Crop.CropId == cropId)
                return seed;
        return null;
    }

    private static int SeedCount(SeedItemSO seed)
    {
        if (seed == null)
            return 0;
        int count = InventoryManager.Instance != null ? InventoryManager.Instance.GetTotalQuantity(seed.itemId) : 0;
        if (FarmStorageManager.Instance != null)
        {
            foreach (FarmStorageSlot slot in FarmStorageManager.Instance.Slots)
                if (slot.item == seed) count += slot.quantity;
        }
        return count;
    }

    private static int PlantedCount(FarmingManager farm, string cropId)
    {
        int count = 0;
        foreach (FarmPlot plot in farm.Plots)
            if (plot != null && plot.HasCrop && plot.Crop.CropId == cropId) count++;
        return count;
    }

    private static int EmptyPlotCount(FarmingManager farm)
    {
        int count = 0;
        foreach (FarmPlot plot in farm.Plots)
            if (plot != null && !plot.HasCrop && !plot.IsResolving) count++;
        return count;
    }
}
