using System;
using UnityEngine;

/// <summary>
/// Advances an authored TutorialDefinition from typed domain events (never raw input/key checks,
/// so remapping still completes the tutorial). Session-scoped like InventoryManager/
/// EquipmentManager: persistent singleton torn down by GameplaySceneLifetime on scene reload.
/// Input tutorial only (Phase 5) -- Tutorial Quest chain is Phase 6.
/// </summary>
public sealed class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private TutorialDefinition _tutorialDefinition;

    private int _currentIndex;
    private bool _completed;
    private bool _subscribed;

    public event Action<TutorialStepDefinition> OnStepChanged;
    public event Action OnTutorialCompleted;

    public bool IsCompleted => _completed;

    public TutorialStepDefinition CurrentStep =>
        _completed || _tutorialDefinition == null
        || _currentIndex < 0 || _currentIndex >= _tutorialDefinition.Steps.Count
            ? null
            : _tutorialDefinition.Steps[_currentIndex];

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

        Player.PlayerMoved += HandlePlayerMoved;
        Player.PlayerSprinted += HandlePlayerSprinted;
        Player.PlayerAttacked += HandlePlayerAttacked;
        InventoryWindowUI.InventoryOpened += HandleInventoryOpened;
        EquipmentManager.ItemEquipped += HandleItemEquipped;
        AreaTriggerZone.PlayerEnteredArea += HandleAreaEntered;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        Player.PlayerMoved -= HandlePlayerMoved;
        Player.PlayerSprinted -= HandlePlayerSprinted;
        Player.PlayerAttacked -= HandlePlayerAttacked;
        InventoryWindowUI.InventoryOpened -= HandleInventoryOpened;
        EquipmentManager.ItemEquipped -= HandleItemEquipped;
        AreaTriggerZone.PlayerEnteredArea -= HandleAreaEntered;
        _subscribed = false;
    }

    private void HandlePlayerMoved() => TryAdvance(TutorialStepType.Move);
    private void HandlePlayerSprinted() => TryAdvance(TutorialStepType.Sprint);
    private void HandlePlayerAttacked() => TryAdvance(TutorialStepType.Attack);
    private void HandleInventoryOpened() => TryAdvance(TutorialStepType.OpenInventory);
    private void HandleItemEquipped(EquipmentItemSO item) => TryAdvance(TutorialStepType.EquipItem);

    private void HandleAreaEntered(string areaId)
    {
        TutorialStepDefinition step = CurrentStep;
        if (step != null && step.Type == TutorialStepType.ReachArea && step.TargetAreaId == areaId)
            Advance();
    }

    private void TryAdvance(TutorialStepType type)
    {
        TutorialStepDefinition step = CurrentStep;
        if (step != null && step.Type == type)
            Advance();
    }

    private void Advance()
    {
        if (_completed || _tutorialDefinition == null)
            return;

        _currentIndex++;
        if (_currentIndex >= _tutorialDefinition.Steps.Count)
        {
            _completed = true;
            OnTutorialCompleted?.Invoke();
        }
        else
        {
            OnStepChanged?.Invoke(CurrentStep);
        }
    }

    /// <summary>Skip with confirm is a UI concern; this is the backend half -- marks completed
    /// without granting anything (there is nothing to grant, input tutorial has no rewards).</summary>
    public void Skip()
    {
        if (_completed)
            return;

        _completed = true;
        _currentIndex = _tutorialDefinition != null ? _tutorialDefinition.Steps.Count : 0;
        OnTutorialCompleted?.Invoke();
    }

    /// <summary>Restore-only: sets state without firing OnStepChanged/OnTutorialCompleted (restore
    /// must not look like fresh progression).</summary>
    public void RestoreState(string stepId, bool completed)
    {
        _completed = completed;

        if (completed || _tutorialDefinition == null)
        {
            _currentIndex = _tutorialDefinition != null ? _tutorialDefinition.Steps.Count : 0;
            return;
        }

        _currentIndex = 0;
        if (string.IsNullOrEmpty(stepId))
            return;

        for (int i = 0; i < _tutorialDefinition.Steps.Count; i++)
        {
            if (_tutorialDefinition.Steps[i].StepId == stepId)
            {
                _currentIndex = i;
                return;
            }
        }
    }

    public TutorialSaveData ToSaveData() => new()
    {
        currentStepId = CurrentStep?.StepId,
        completed = _completed
    };
}
