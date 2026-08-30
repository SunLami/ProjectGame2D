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

    public event Action<string> QuestAccepted;
    public event Action<string> QuestProgressChanged;
    public event Action<string> QuestCompleted;
    public event Action MainQuestUnlocked;

    public IQuestResolver Catalog => _catalog;
    public bool IsMainQuestUnlocked => _mainQuestUnlocked;

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

            if (state.TryProgressCurrentObjective(amount))
                QuestProgressChanged?.Invoke(questId);
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

        if (GetStatus(questId) != QuestStatus.Available)
            return false;

        _runtime[questId] = new QuestRuntimeState(definition);
        QuestAccepted?.Invoke(questId);
        QuestProgressChanged?.Invoke(questId);
        return true;
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
        QuestProgressChanged?.Invoke(questId);
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
        var data = new QuestSaveData();
        foreach (KeyValuePair<string, QuestRuntimeState> pair in _runtime)
        {
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

                var state = new QuestRuntimeState(definition);
                state.RestoreProgress(entry.status, entry.currentObjectiveIndex, entry.objectiveCounters);
                _runtime[entry.questId] = state;
            }
        }

        ReconcileMainQuestUnlock(fireEvent: false);
    }
}
