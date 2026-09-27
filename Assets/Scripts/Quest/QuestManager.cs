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
        _subscribed = false;
    }

    private void HandleTalk(string npcId, string outcomeId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesTalk(o, npcId), 1);

    private void HandleCraft(string itemId, int quantity, string stationId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesCraft(o, itemId), quantity);

    private void HandlePurchase(string itemId, int quantity, string shopId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesPurchase(o, itemId), quantity);

    private void HandleGather(string resourceId, int quantity, string areaId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesGather(o, resourceId, areaId), quantity);

    private void HandleKill(string enemyId, string areaId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesKill(o, enemyId, areaId), 1);

    private void HandleEquip(string itemId) =>
        ProgressMatching(o => QuestObjectiveMatchers.MatchesEquip(o, itemId), 1);

    // Obtain needs its own handler: RequirePossession is a boolean gate re-checked against live
    // inventory, not an incrementing counter (ObtainObjectiveMode / D-014).
    private void HandleObtain(string itemId, int quantity)
    {
        foreach (string questId in SnapshotActiveQuestIds())
        {
            QuestRuntimeState state = _runtime[questId];
            QuestObjectiveDefinition objective = state.CurrentObjective;
            if (!QuestObjectiveMatchers.MatchesObtain(objective, itemId))
                continue;

            bool progressed;
            if (objective.ObtainMode == ObtainObjectiveMode.RequirePossession)
            {
                progressed = InventoryManager.Instance != null
                    && InventoryManager.Instance.HasItemId(itemId, objective.TargetCount)
                    && state.CompleteCurrentObjective();
            }
            else
            {
                progressed = state.TryProgressCurrentObjective(quantity);
            }

            if (progressed)
                QuestProgressChanged?.Invoke(questId);
        }
    }

    private void ProgressMatching(Func<QuestObjectiveDefinition, bool> matches, int amount)
    {
        foreach (string questId in SnapshotActiveQuestIds())
        {
            QuestRuntimeState state = _runtime[questId];
            if (!matches(state.CurrentObjective))
                continue;

            if (!state.TryProgressCurrentObjective(amount))
                continue;

            QuestProgressChanged?.Invoke(questId);

            // A quest authored with AutoTurnIn (e.g. "equip a weapon") completes itself the
            // instant its objectives are done -- no NPC visit to hand it in. TryTurnIn re-validates
            // ReadyToTurnIn/reward capacity itself, so this is just as safe as any other caller.
            if (state.Status == QuestStatus.ReadyToTurnIn && state.Definition.AutoTurnIn)
                TryTurnIn(questId, out _);
        }
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
        if (objective != null
            && !string.IsNullOrEmpty(objective.TargetAreaId)
            && (objective.Type == QuestObjectiveType.Kill || objective.Type == QuestObjectiveType.Gather))
        {
            yield return objective.TargetAreaId;
        }
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
        if (_runtime.TryGetValue(questId, out QuestRuntimeState state))
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

        _runtime[questId] = new QuestRuntimeState(definition);
        QuestAccepted?.Invoke(questId);
        QuestProgressChanged?.Invoke(questId);
        if (string.IsNullOrEmpty(_trackedQuestId))
            SetTrackedQuest(questId);
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
            || string.IsNullOrEmpty(state.Definition.GiverNpcId))
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
        QuestCompleted?.Invoke(questId);
        QuestTurnedIn?.Invoke(questId);
        QuestProgressChanged?.Invoke(questId);
        if (IsTracked(questId))
            SetTrackedQuest(null);
        ReconcileMainQuestUnlock(fireEvent: true);
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
