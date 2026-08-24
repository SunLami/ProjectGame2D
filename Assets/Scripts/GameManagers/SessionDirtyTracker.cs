using UnityEngine;

/// <summary>
/// Scene service (lives on _SceneContext like PlayerSpawnReadinessSource/WorldObjectRegistry):
/// marks the active GameSessionManager session dirty whenever real gameplay changes progression,
/// inventory/equipment, tutorial/quest state or persistent world state. Event-driven only -- never
/// polls or serializes GameSaveData to diff state every frame.
///
/// GameSessionManager.MarkDirty() itself no-ops while IsRestoring is true, so every subscription
/// here is safe to leave unconditional: restore-time RestoreProgression/RestoreEquipped/
/// LoadFromSaveData/RestoreState calls fire the same change events real gameplay does, and the
/// single guard in GameSessionManager is what keeps a freshly loaded/New Game session starting
/// clean (see GameSessionManager.IsRestoring, RuntimeArchitecture.md "Event rules").
///
/// D-024 (see DecisionRegister.md): player movement/position alone does not dirty the session --
/// only progression (level/XP), inventory/equipment/gold, tutorial/quest progress, world object
/// state, and Shop/Crafting transactions (which already mutate inventory, so InventoryManager's
/// event alone covers them without a separate subscription).
/// </summary>
public sealed class SessionDirtyTracker : MonoBehaviour
{
    private InventoryManager _inventoryManager;
    private EquipmentManager _equipmentManager;
    private PlayerStat _playerStat;
    private TutorialManager _tutorialManager;
    private QuestManager _questManager;
    private bool _subscribed;

    internal void ConfigureForTests(
        InventoryManager inventoryManager, EquipmentManager equipmentManager, PlayerStat playerStat,
        TutorialManager tutorialManager, QuestManager questManager)
    {
        _inventoryManager = inventoryManager;
        _equipmentManager = equipmentManager;
        _playerStat = playerStat;
        _tutorialManager = tutorialManager;
        _questManager = questManager;
    }

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed)
            return;

        if (_inventoryManager == null) _inventoryManager = InventoryManager.Instance;
        if (_equipmentManager == null) _equipmentManager = EquipmentManager.Instance;
        if (_playerStat == null) _playerStat = PlayerStat.Instance;
        if (_tutorialManager == null) _tutorialManager = TutorialManager.Instance;
        if (_questManager == null) _questManager = QuestManager.Instance;

        if (_inventoryManager != null) _inventoryManager.OnInventoryChanged += HandleDirtyingEvent;
        if (_equipmentManager != null) _equipmentManager.OnEquipmentChanged += HandleDirtyingEvent;
        if (_playerStat != null)
        {
            _playerStat.OnLevelUp += HandleLevelUp;
            _playerStat.OnExperienceChanged += HandleExperienceChanged;
        }
        if (_tutorialManager != null)
        {
            _tutorialManager.OnStepChanged += HandleTutorialStepChanged;
            _tutorialManager.OnTutorialCompleted += HandleDirtyingEvent;
        }
        if (_questManager != null)
        {
            _questManager.QuestAccepted += HandleQuestEvent;
            _questManager.QuestProgressChanged += HandleQuestEvent;
            _questManager.QuestCompleted += HandleQuestEvent;
            _questManager.MainQuestUnlocked += HandleDirtyingEvent;
        }
        WorldDomainEvents.WorldObjectChanged += HandleDirtyingEvent;

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        if (_inventoryManager != null) _inventoryManager.OnInventoryChanged -= HandleDirtyingEvent;
        if (_equipmentManager != null) _equipmentManager.OnEquipmentChanged -= HandleDirtyingEvent;
        if (_playerStat != null)
        {
            _playerStat.OnLevelUp -= HandleLevelUp;
            _playerStat.OnExperienceChanged -= HandleExperienceChanged;
        }
        if (_tutorialManager != null)
        {
            _tutorialManager.OnStepChanged -= HandleTutorialStepChanged;
            _tutorialManager.OnTutorialCompleted -= HandleDirtyingEvent;
        }
        if (_questManager != null)
        {
            _questManager.QuestAccepted -= HandleQuestEvent;
            _questManager.QuestProgressChanged -= HandleQuestEvent;
            _questManager.QuestCompleted -= HandleQuestEvent;
            _questManager.MainQuestUnlocked -= HandleDirtyingEvent;
        }
        WorldDomainEvents.WorldObjectChanged -= HandleDirtyingEvent;

        _subscribed = false;
    }

    private void HandleDirtyingEvent() => GameSessionManager.Instance?.MarkDirty();
    private void HandleLevelUp(int level) => GameSessionManager.Instance?.MarkDirty();
    private void HandleExperienceChanged(int current, int toNext) => GameSessionManager.Instance?.MarkDirty();
    private void HandleTutorialStepChanged(TutorialStepDefinition step) => GameSessionManager.Instance?.MarkDirty();
    private void HandleQuestEvent(string questId) => GameSessionManager.Instance?.MarkDirty();
}
