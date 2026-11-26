using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns quest acceptance, objective progress and turn-in for the active session. Session-scoped
/// persistent singleton like TutorialManager/InventoryManager, torn down by GameplaySceneLifetime
/// on scene reload. Subscribes to QuestDomainEvents only -- never polls world state, never reads
/// UI. NPC components must go through QuestNpcInteractionService, not this class's internals
/// directly (TutorialAndQuestProgression.md NPC roles).
/// </summary>
public sealed class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private QuestCatalog _catalog;

    private readonly Dictionary<string, QuestRuntimeState> _runtime = new(StringComparer.Ordinal);
    private IItemResolver _itemResolver;
    private bool _subscribed;
    private bool _mainQuestUnlocked;
    private string _trackedQuestId;

    public event Action<string> QuestAccepted;
    public event Action<string> QuestProgressChanged;
    public event Action<string> QuestCompleted;
    public event Action<string> QuestTrackingChanged;
    public event Action<string> QuestAbandoned;
    public event Action MainQuestUnlocked;

    /// <summary>Static mirror of QuestCompleted so cross-system consumers (e.g. TutorialManager,
    /// another Bootstrap-scoped singleton with no guaranteed Awake order relative to this one) can
    /// subscribe without depending on QuestManager.Instance already existing -- same reasoning as
    /// EquipmentManager.ItemEquipped being static.</summary>
    public static event Action<string> QuestTurnedIn;

    public IQuestResolver Catalog => _catalog;
    public bool IsMainQuestUnlocked => _mainQuestUnlocked;
    public string TrackedQuestId => _trackedQuestId;

    // Act 1 (Heorte) station gates: no physical Smithing/Black Arts station exists yet, so future
    // station interactables read these instead of duplicating their own completion check
    // (TutorialAndQuestProgression.md Act I -- Heorte Intro Quest Flow, Quest 4/5).
    public bool IsSmithingStationUnlocked => GetStatus("quest.act1.ring_of_forge") == QuestStatus.Completed;
    public bool IsBlackArtsStationUnlocked => GetStatus("quest.act1.far_from_your_kind") == QuestStatus.Completed;

    private IItemResolver ItemResolver => _itemResolver ??= new ResourcesItemResolver();

    internal void ConfigureForTests(QuestCatalog catalog, IItemResolver itemResolver = null)
    {
        _catalog = catalog;
        _itemResolver = itemResolver;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Editor-only parent (e.g. "_Managers") keeps the Hierarchy tidy; detach before
            // DontDestroyOnLoad, which only works on root GameObjects.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();

    private void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this)
            Instance = null;
    }

    private void Subscribe()
    {
        if (_subscribed)
            return;

        QuestDomainEvents.NpcConversationCompleted += HandleTalk;
        QuestDomainEvents.InventoryItemAdded += HandleObtain;
        QuestDomainEvents.ItemCrafted += HandleCraft;
        QuestDomainEvents.ItemPurchased += HandlePurchase;
        QuestDomainEvents.ResourceGathered += HandleGather;
        QuestDomainEvents.EnemyKilled += HandleKill;
        QuestDomainEvents.ItemEquipped += HandleEquip;
        QuestDomainEvents.CropReplanted += HandleReplant;
        QuestDomainEvents.FishCaught += HandleFish;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        QuestDomainEvents.NpcConversationCompleted -= HandleTalk;
        QuestDomainEvents.InventoryItemAdded -= HandleObtain;
        QuestDomainEvents.ItemCrafted -= HandleCraft;
        QuestDomainEvents.ItemPurchased -= HandlePurchase;
        QuestDomainEvents.ResourceGathered -= HandleGather;
        QuestDomainEvents.EnemyKilled -= HandleKill;
        QuestDomainEvents.ItemEquipped -= HandleEquip;
        QuestDomainEvents.CropReplanted -= HandleReplant;
        QuestDomainEvents.FishCaught -= HandleFish;
        _subscribed = false;
    }

    private void HandleTalk(string npcId, string outcomeId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesTalk(o, npcId, outcomeId), 1);

    private void HandleCraft(string itemId, int quantity, string stationId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesCraft(o, itemId, stationId), quantity);

    private void HandlePurchase(string itemId, int quantity, string shopId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesPurchase(o, itemId), quantity);

    private void HandleGather(string resourceId, int quantity, string areaId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesGather(o, resourceId, areaId), quantity);

    private void HandleKill(string enemyId, string areaId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesKill(o, enemyId, areaId), 1);

    private void HandleEquip(string itemId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesEquip(o, itemId), 1);

    private void HandleReplant(string cropId, string areaId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesReplant(o, cropId, areaId), 1);

    private void HandleFish(string fishId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesFish(o, fishId), 1);

    // Obtain needs its own handler: RequirePossession is a boolean gate re-checked against live
    // inventory, not an incrementing counter (ObtainObjectiveMode / D-014).
    private void HandleObtain(string itemId, int quantity)
    {
        foreach (string questId in SnapshotActiveQuestIds())
        {
            QuestRuntimeState state = _runtime[questId];
            QuestObjectiveDefinition objective = state.CurrentObjective;
            bool progressed = false;
            if (QuestObjectiveMatchers.MatchesObtain(objective, itemId))
            {
                progressed = objective.ObtainMode == ObtainObjectiveMode.RequirePossession
                    ? HasInventoryItem(itemId, objective.TargetCount) && state.CompleteCurrentObjective()
                    : state.TryProgressCurrentObjective(quantity);
            }

            if (state.ReconcileGatherPossession(HasInventoryItem))
                NotifyObjectiveProgress(questId, state);
            else if (progressed)
            {
                SoundFXManager.PlaySfx(SfxIds.QuestObjective);
                QuestProgressChanged?.Invoke(questId);
            }
        }
    }

    private static bool HasInventoryItem(string itemId, int quantity) =>
        InventoryManager.Instance != null && InventoryManager.Instance.HasItemId(itemId, quantity);

    private void ProgressMatching(Func<QuestObjectiveDefinition, bool> matches, int amount)
    {
        foreach (string questId in SnapshotActiveQuestIds())
        {
            QuestRuntimeState state = _runtime[questId];
            if (!state.TryProgressMatchingObjective(matches, amount))
                continue;

            state.ReconcileGatherPossession(HasInventoryItem);
            NotifyObjectiveProgress(questId, state);
        }
    }

    internal void ReconcilePlantingCounts(string questId, Func<string, string, int> countPlants)
    {
        if (_runtime.TryGetValue(questId, out QuestRuntimeState state) && state.ReconcilePlantingCounts(countPlants))
            NotifyObjectiveProgress(questId, state);
    }

    private void NotifyObjectiveProgress(string questId, QuestRuntimeState state)
    {
        SoundFXManager.PlaySfx(SfxIds.QuestObjective);
        QuestProgressChanged?.Invoke(questId);
        if (state.Status == QuestStatus.ReadyToTurnIn && state.Definition.AutoTurnIn)
            TryTurnIn(questId, out _);
    }

    // Snapshot of keys for Active quests only -- a handler mutating _runtime mid-dispatch (not
    // currently possible, but keeps iteration safe against future changes) never corrupts this.
    private List<string> SnapshotActiveQuestIds()
    {
        var ids = new List<string>();
        foreach (KeyValuePair<string, QuestRuntimeState> pair in _runtime)
        {
            if (pair.Value.Status == QuestStatus.Active)
                ids.Add(pair.Key);
        }
        return ids;
    }

    /// <summary>Read-model for a direction indicator: every npcId that currently has something
    /// actionable for the player, in priority order -- a quest ready to turn in, then an Active
    /// quest whose current objective is literally "go talk to someone" (the objective's own
    /// TargetId, not the quest's GiverNpcId/TurnInNpcId -- this is mid-quest, not an offer/turn-in
    /// moment), then any quest ready to offer. A direction indicator resolves each id to a world
    /// position itself (e.g. via a scene registry) and should walk this list in order, since not
    /// every actionable NPC is necessarily present/resolvable in the player's current scene -- this
    /// method only knows quest state, not world placement, so it can't filter for that itself.</summary>
    public IEnumerable<string> GetActionableNpcIds()
    {
        if (_catalog == null)
            yield break;

        if (TryGetTrackedState(out QuestRuntimeState trackedState))
        {
            QuestDefinition trackedQuest = trackedState.Definition;
            if (trackedState.Status == QuestStatus.ReadyToTurnIn
                && !string.IsNullOrEmpty(trackedQuest.TurnInNpcId))
            {
                yield return trackedQuest.TurnInNpcId;
            }

            QuestObjectiveDefinition objective = trackedState.CurrentObjective;
            if (objective != null && objective.Type == QuestObjectiveType.Talk && !string.IsNullOrEmpty(objective.TargetId))
                yield return objective.TargetId;
            // Non-Talk objectives that still happen at an NPC (e.g. Purchase at a shop keeper) name
            // that NPC explicitly as authored navigation data instead of guessing from the type.
            if (!string.IsNullOrEmpty(objective?.NavigationNpcId))
                yield return objective.NavigationNpcId;
        }

        foreach (QuestDefinition quest in _catalog.AllQuests)
        {
            if (!string.IsNullOrEmpty(quest.GiverNpcId) && GetStatus(quest.QuestId) == QuestStatus.Available)
                yield return quest.GiverNpcId;
        }
    }

    /// <summary>Read-model for a direction indicator: every areaId the current objective of some
    /// Active quest points at (Kill/Gather with a TargetAreaId set) -- a location the player needs
    /// to go to make progress, as opposed to an NPC they need to talk to. Checked as a fallback
    /// after GetActionableNpcIds finds nothing resolvable, since talking to someone usually takes
    /// priority over "go stand somewhere."</summary>
    public IEnumerable<string> GetActionableAreaIds()
    {
        if (!TryGetTrackedState(out QuestRuntimeState state) || state.Status != QuestStatus.Active)
            yield break;

        QuestObjectiveDefinition objective = state.CurrentObjective;
        if (!string.IsNullOrEmpty(objective?.NavigationAreaId))
        {
            yield return objective.NavigationAreaId;
            yield break;
        }

        if (objective != null
            && !string.IsNullOrEmpty(objective.TargetAreaId)
            && (objective.Type == QuestObjectiveType.Kill || objective.Type == QuestObjectiveType.Gather))
        {
            yield return objective.TargetAreaId;
        }
    }

    /// <summary>How many more units of itemId the Active quest questId still needs for a Purchase
    /// objective that is its current step. 0 when the quest is not Active, its current objective is
    /// something else, or that objective is already full. Read-only: lets a quest-scoped shop
    /// allowance (ShopStockEntry.QuestAllowanceId) cap a complimentary line to exactly the quest's
    /// need, so a 0 G quest gift is never an unlimited item source.</summary>
    public int GetRemainingPurchaseAllowance(string questId, string itemId)
    {
        if (string.IsNullOrEmpty(questId)
            || !_runtime.TryGetValue(questId, out QuestRuntimeState state)
            || !QuestObjectiveMatchers.MatchesPurchase(state.CurrentObjective, itemId))
        {
            return 0;
        }

        return Mathf.Max(0, state.CurrentObjective.TargetCount - state.ObjectiveCounters[state.CurrentObjectiveIndex]);
    }

    public string GetTrackedControlHint()
    {
        if (TryGetTrackedState(out QuestRuntimeState state))
        {
            if (state.Status == QuestStatus.ReadyToTurnIn && state.Definition.ShowOfferTutorial)
                return state.Definition.TurnInDirections;

            string hint = state.CurrentObjective?.ControlHint;
            if (!string.IsNullOrEmpty(hint))
                return hint;
        }

        if (_catalog != null)
            foreach (QuestDefinition quest in _catalog.AllQuests)
                if (quest.ShowOfferTutorial && GetStatus(quest.QuestId) == QuestStatus.Available)
                    return quest.OfferDirections;

        return null;
    }

    private bool TryGetTrackedState(out QuestRuntimeState state)
    {
        state = null;
        return !string.IsNullOrEmpty(_trackedQuestId)
            && _runtime.TryGetValue(_trackedQuestId, out state)
            && (state.Status == QuestStatus.Active || state.Status == QuestStatus.ReadyToTurnIn);
    }

    /// <summary>Computed status: Locked/Available are derived from prerequisites every call
    /// (TutorialAndQuestProgression.md Main Quest gate -- never trust a single cached bool).</summary>
    public QuestStatus GetStatus(string questId)
    {
        if (_runtime.TryGetValue(questId, out QuestRuntimeState state) && state.Status != QuestStatus.Locked)
            return state.Status;

        if (_catalog == null || !_catalog.TryResolve(questId, out QuestDefinition definition))
            return QuestStatus.Locked;

        return ArePrerequisitesMet(definition) ? QuestStatus.Available : QuestStatus.Locked;
    }

    /// <summary>Read-only presentation read-model for UI (e.g. "1/2 killed"). Returns false if
    /// questId has no runtime entry yet (Locked/Available -- nothing accepted, nothing to show
    /// progress for). ObjectiveCounters in the snapshot is a defensive copy, never the live array,
    /// so UI can never mutate QuestRuntimeState through it.</summary>
    public bool TryGetProgress(string questId, out QuestProgressSnapshot snapshot)
    {
        if (string.IsNullOrEmpty(questId) || !_runtime.TryGetValue(questId, out QuestRuntimeState state))
        {
            snapshot = default;
            return false;
        }

        snapshot = new QuestProgressSnapshot(
            state.Status, state.CurrentObjectiveIndex, (int[])state.ObjectiveCounters.Clone());
        return true;
    }

    private bool ArePrerequisitesMet(QuestDefinition definition)
    {
        foreach (string prerequisiteId in definition.PrerequisiteQuestIds)
        {
            if (GetStatus(prerequisiteId) != QuestStatus.Completed)
                return false;
        }
        return true;
    }

    /// <summary>Accepts a quest exactly once: only succeeds while GetStatus == Available.</summary>
    public bool TryAcceptQuest(string questId)
    {
        if (_catalog == null || !_catalog.TryResolve(questId, out QuestDefinition definition))
            return false;

        if (definition.IsDebugQuest && !CanUseDebugQuest)
            return false;

        if (GetStatus(questId) != QuestStatus.Available)
            return false;

        if (!TryGrantAcceptanceSupplies(definition.AcceptanceSupplies))
            return false;

        if (!_runtime.TryGetValue(questId, out QuestRuntimeState state))
            _runtime[questId] = state = new QuestRuntimeState(definition);
        else
        {
            QuestStatus resumedStatus = state.CurrentObjectiveIndex >= definition.Objectives.Count
                ? QuestStatus.ReadyToTurnIn : QuestStatus.Active;
            state.RestoreProgress(resumedStatus, state.CurrentObjectiveIndex, state.ObjectiveCounters);
        }

        state.ReconcileGatherPossession(HasInventoryItem);
        SoundFXManager.PlaySfx(SfxIds.QuestAccept);
        QuestAccepted?.Invoke(questId);
        QuestProgressChanged?.Invoke(questId);
        if (string.IsNullOrEmpty(_trackedQuestId))
            SetTrackedQuest(questId);
        if (state.Status == QuestStatus.ReadyToTurnIn && definition.AutoTurnIn)
            TryTurnIn(questId, out _);
        return true;
    }

    private bool TryGrantAcceptanceSupplies(QuestRewardDefinition supplies)
    {
        if (supplies == null)
            return true;

        var grants = new List<InventoryItemGrant>();
        foreach (QuestRewardItemEntry entry in supplies.Items)
        {
            if (!ItemResolver.TryResolve(entry.ItemId, out ItemSO item))
                return false;
            grants.Add(new InventoryItemGrant(item, entry.Quantity));
        }

        InventoryManager inventory = InventoryManager.Instance;
        if ((grants.Count > 0 || supplies.Gold > 0) && inventory == null)
            return false;
        if (grants.Count > 0 && !inventory.TryAddBatch(grants))
            return false;
        if (supplies.Gold > 0)
            inventory.AddGold(supplies.Gold);
        if (supplies.Experience > 0)
            PlayerStat.Instance?.AddExperience(supplies.Experience);
        return true;
    }

    public bool IsTracked(string questId) =>
        !string.IsNullOrEmpty(questId)
        && string.Equals(_trackedQuestId, questId, StringComparison.Ordinal);

    public bool TryTrackQuest(string questId)
    {
        QuestStatus status = GetStatus(questId);
        if (status != QuestStatus.Active && status != QuestStatus.ReadyToTurnIn)
            return false;
        if (IsTracked(questId))
            return false;

        SetTrackedQuest(questId);
        return true;
    }

    public bool TryUntrackQuest(string questId)
    {
        if (!IsTracked(questId))
            return false;

        SetTrackedQuest(null);
        return true;
    }

    public bool TryAbandonQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)
            || !_runtime.TryGetValue(questId, out QuestRuntimeState state)
            || (state.Status != QuestStatus.Active && state.Status != QuestStatus.ReadyToTurnIn)
            || !state.Definition.CanAbandon)
        {
            return false;
        }

        _runtime.Remove(questId);
        if (IsTracked(questId))
            SetTrackedQuest(null);
        QuestAbandoned?.Invoke(questId);
        return true;
    }

    private void SetTrackedQuest(string questId)
    {
        _trackedQuestId = questId;
        QuestTrackingChanged?.Invoke(questId);
    }

    private static bool CanUseDebugQuest
    {
        get
        {
#if UNITY_EDITOR
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }
    }

    /// <summary>Atomic turn-in transaction (TutorialAndQuestProgression.md reward transaction):
    /// validates ReadyToTurnIn and full reward capacity before granting anything, then grants and
    /// marks Completed. Already-Completed is rejected up front so a double-submit never regrants.</summary>
    public bool TryTurnIn(string questId, out QuestTurnInResult result)
    {
        if (!_runtime.TryGetValue(questId, out QuestRuntimeState state))
        {
            result = QuestTurnInResult.QuestNotFound;
            return false;
        }

        if (state.Status == QuestStatus.Completed)
        {
            result = QuestTurnInResult.AlreadyCompleted;
            return false;
        }

        if (state.Status != QuestStatus.ReadyToTurnIn)
        {
            result = QuestTurnInResult.ObjectivesIncomplete;
            return false;
        }

        QuestRewardDefinition rewards = state.Definition.Rewards;
        if (!HasCapacityForRewards(rewards))
        {
            result = QuestTurnInResult.InsufficientInventoryCapacity;
            return false;
        }

        GrantRewards(rewards);
        state.MarkCompleted();
        result = QuestTurnInResult.Success;
        SoundFXManager.PlaySfx(SfxIds.QuestComplete);
        // Clear tracking before completion listeners run so a newly accepted quest can be tracked.
        if (IsTracked(questId))
            SetTrackedQuest(null);
        QuestCompleted?.Invoke(questId);
        QuestTurnedIn?.Invoke(questId);
        QuestProgressChanged?.Invoke(questId);
        ReconcileMainQuestUnlock(fireEvent: true);
        if (!string.IsNullOrEmpty(state.Definition.AutoStartQuestId))
            TryAcceptQuest(state.Definition.AutoStartQuestId);
        return true;
    }

    private bool HasCapacityForRewards(QuestRewardDefinition rewards)
    {
        if (rewards == null)
            return true;

        foreach (QuestRewardItemEntry entry in rewards.Items)
        {
            if (string.IsNullOrEmpty(entry.ItemId))
                continue;

            if (!ItemResolver.TryResolve(entry.ItemId, out ItemSO item))
            {
                Debug.LogWarning($"QuestManager: reward item '{entry.ItemId}' could not be resolved.", this);
                return false;
            }

            if (InventoryManager.Instance != null && !InventoryManager.Instance.HasCapacityFor(item, entry.Quantity))
                return false;
        }
        return true;
    }

    private void GrantRewards(QuestRewardDefinition rewards)
    {
        if (rewards == null)
            return;

        foreach (QuestRewardItemEntry entry in rewards.Items)
        {
            if (string.IsNullOrEmpty(entry.ItemId))
                continue;

            if (ItemResolver.TryResolve(entry.ItemId, out ItemSO item))
                InventoryManager.Instance?.AddItem(item, entry.Quantity);
        }

        if (rewards.Gold > 0)
            InventoryManager.Instance?.AddGold(rewards.Gold);
        if (rewards.Experience > 0)
            PlayerStat.Instance?.AddExperience(rewards.Experience);
    }

    private void ReconcileMainQuestUnlock(bool fireEvent)
    {
        bool unlocked = ComputeMainQuestUnlocked();
        bool justUnlocked = unlocked && !_mainQuestUnlocked;
        _mainQuestUnlocked = unlocked;

        if (justUnlocked && fireEvent)
            MainQuestUnlocked?.Invoke();
    }

    private bool ComputeMainQuestUnlocked()
    {
        if (_catalog == null)
            return false;

        bool hasTutorialQuest = false;
        foreach (QuestDefinition quest in _catalog.AllQuests)
        {
            if (!quest.IsTutorialQuest)
                continue;

            hasTutorialQuest = true;
            if (GetStatus(quest.QuestId) != QuestStatus.Completed)
                return false;
        }
        return hasTutorialQuest;
    }

    public QuestSaveData ToSaveData()
    {
        var data = new QuestSaveData { trackedQuestId = _trackedQuestId };
        foreach (KeyValuePair<string, QuestRuntimeState> pair in _runtime)
        {
            if (pair.Value.Definition.IsDebugQuest)
                continue;

            data.quests.Add(new QuestProgressSaveData
            {
                questId = pair.Key,
                status = pair.Value.Status,
                currentObjectiveIndex = pair.Value.CurrentObjectiveIndex,
                objectiveCounters = pair.Value.ObjectiveCounters
            });
        }
        return data;
    }

    /// <summary>Restore-only: rebuilds runtime state with no progression side effects -- never
    /// fires QuestAccepted/QuestProgressChanged/QuestCompleted/MainQuestUnlocked.</summary>
    public void RestoreState(QuestSaveData data)
    {
        _runtime.Clear();
        _trackedQuestId = null;

        if (_catalog != null && data?.quests != null)
        {
            foreach (QuestProgressSaveData entry in data.quests)
            {
                if (string.IsNullOrEmpty(entry.questId) || !_catalog.TryResolve(entry.questId, out QuestDefinition definition))
                {
                    Debug.LogWarning(
                        $"QuestManager: quest '{entry.questId}' not found in catalog; progress dropped.", this);
                    continue;
                }

                if (definition.IsDebugQuest)
                    continue;

                var state = new QuestRuntimeState(definition);
                state.RestoreProgress(entry.status, entry.currentObjectiveIndex, entry.objectiveCounters);
                state.ReconcileGatherPossession(HasInventoryItem);
                _runtime[entry.questId] = state;
            }
        }

        if (!string.IsNullOrEmpty(data?.trackedQuestId)
            && _runtime.TryGetValue(data.trackedQuestId, out QuestRuntimeState tracked)
            && (tracked.Status == QuestStatus.Active || tracked.Status == QuestStatus.ReadyToTurnIn))
        {
            _trackedQuestId = data.trackedQuestId;
        }

        ReconcileMainQuestUnlock(fireEvent: false);
    }
}
