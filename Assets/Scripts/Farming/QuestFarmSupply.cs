using UnityEngine;

/// <summary>Supplies missing seeds for the active planting or harvest quest. Existing crops
/// and seeds in Inventory and Farm Storage count toward coverage.</summary>
public sealed class QuestFarmSupply : MonoBehaviour
{
    [SerializeField] private string _questId = "quest.act1.back_to_fields";
    [SerializeField] private string _harvestQuestId = "quest.act1.harvest_the_fields";
    [SerializeField] private FarmingManager _farmingManager;
    [Tooltip("Seed item for each crop the quest can need (matched by SeedItemSO.Crop).")]
    [SerializeField] private SeedItemSO[] _seeds;

    private QuestManager _questManager;
    private bool _dirty = true;

    private void OnEnable()
    {
        FarmingDomainEvents.CropHarvested += HandleHarvested;
        FarmingDomainEvents.CropPlanted += HandlePlanted;
        _dirty = true;
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
        Apply();
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
        if (questId != _questId && questId != _harvestQuestId)
            return;
        _dirty = true;
    }

    private void HandleQuestProgress(string questId)
    {
        if (questId == _questId || questId == _harvestQuestId)
            _dirty = true;
    }

    private void HandleHarvested(string plotId, string cropId, int quantity) => _dirty = true;
    private void HandlePlanted(string plotId, string cropId) => _dirty = true;
    private void HandleSupplyChanged() => _dirty = true;

    private void Apply()
    {
        FarmingManager farm = _farmingManager != null ? _farmingManager : FarmingManager.Instance;
        if (_questManager == null || farm == null || farm.Catalog == null)
            return;

        // Compare against the field once it is live so old saves can recover plantings the tracker skipped.
        _questManager.ReconcilePlantingCounts(_questId, (cropId, areaId) => PlantedCount(farm, cropId, areaId));

        string activeQuestId = _questManager.GetStatus(_questId) == QuestStatus.Active ? _questId : _harvestQuestId;
        if (_questManager.GetStatus(activeQuestId) != QuestStatus.Active
            || !_questManager.Catalog.TryResolve(activeQuestId, out QuestDefinition quest)
            || !_questManager.TryGetProgress(activeQuestId, out QuestProgressSnapshot progress))
            return;

        bool supplied = false;
        foreach (QuestFarmSupplyPlanner.CropNeed need in QuestFarmSupplyPlanner.RemainingNeeds(
                     quest.Objectives, progress.CurrentObjectiveIndex, progress.ObjectiveCounters))
        {
            if (!farm.Catalog.TryResolve(need.CropId, out CropDefinition crop))
                continue;

            SeedItemSO seed = FindSeed(need.CropId);
            QuestFarmSupplyPlanner.CropSupply plan = QuestFarmSupplyPlanner.PlanCrop(
                need, crop.MinimumHarvestQuantity, PlantedCount(farm, need.CropId), SeedCount(seed),
                emptyPlotsAvailable: 0, allowMature: false);
            if (plan.Seeds > 0 && TrySupplySeeds(seed, plan.Seeds))
                supplied = true;
        }

        if (supplied)
            FarmingDomainEvents.RaiseFarmStateChanged();
        _dirty = false;
    }

    private bool TrySupplySeeds(SeedItemSO seed, int quantity)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (seed != null && inventory != null && inventory.HasCapacityFor(seed, quantity))
            return inventory.AddItem(seed, quantity);

        FarmStorageManager storage = FarmStorageManager.Instance;
        if (seed != null && storage != null)
        {
            storage.EnsureSlotCount(inventory != null ? inventory.Slots.Count : 0);
            if (storage.TryDeposit(seed, quantity))
                return true;
        }

        Debug.LogWarning($"QuestFarmSupply: could not supply {quantity} seed(s) for '{seed?.itemId}'.", this);
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

    private static int PlantedCount(FarmingManager farm, string cropId, string areaId = null)
    {
        int count = 0;
        foreach (FarmPlot plot in farm.Plots)
            if (plot != null && plot.HasCrop && plot.Crop.CropId == cropId
                && (string.IsNullOrEmpty(areaId) || plot.AreaId == areaId))
                count++;
        return count;
    }

}
