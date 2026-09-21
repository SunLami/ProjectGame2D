using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class FishingMinigameController : MonoBehaviour
{
    private enum SessionPhase
    {
        None,
        Waiting,
        BiteReady,
        Minigame,
        Result
    }

    [SerializeField] private FishingMinigameUI _ui;
    [SerializeField, Min(0f)] private float _resultDisplaySeconds = 1.5f;

    private SessionPhase _phase;
    private FishingSpotDefinition _definition;
    private FishingSpotInteractable _spot;
    private FishDefinitionSO _selectedFish;
    private Coroutine _waitingRoutine;
    private float _remainingTime;
    private float _progress;
    private float _fishPosition;
    private float _fishTarget;
    private float _fishTargetTimer;
    private float _catchZonePosition;
    private float _catchZoneVelocity;

    public static FishingMinigameController Instance { get; private set; }
    public bool IsSessionActive => _phase != SessionPhase.None;
    public bool IsMinigameActive => _phase == SessionPhase.Minigame;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _ui?.HideAll();
    }

    private void Update()
    {
        if (_phase == SessionPhase.None)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndSession();
            return;
        }

        if (_phase == SessionPhase.BiteReady
            && Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            BeginMinigame();
            return;
        }

        if (_phase == SessionPhase.Minigame)
            TickMinigame(Time.unscaledDeltaTime);
    }

    public bool TryBeginFishing(FishingSpotInteractable spot, FishingSpotDefinition definition)
    {
        if (IsSessionActive
            || spot == null
            || definition == null
            || GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing
            || InventoryManager.Instance == null)
        {
            return false;
        }

        if (!InventoryManager.Instance.HasEmptySlot)
        {
            _spot = spot;
            _definition = definition;
            _phase = SessionPhase.Result;
            GameStateManager.Instance.PushState(GameState.FishingWaiting);
            _ui?.ShowResult("Inventory full. Fishing needs one empty slot.");
            StartCoroutine(FinishAfterResult());
            return true;
        }

        if (!definition.TryRollFish(out _selectedFish))
        {
            Debug.LogError($"Fishing spot '{definition.name}' has no valid fish entries.", definition);
            return false;
        }

        _spot = spot;
        _definition = definition;
        _phase = SessionPhase.Waiting;
        GameStateManager.Instance.PushState(GameState.FishingWaiting);
        _ui?.ShowWaiting();
        _waitingRoutine = StartCoroutine(WaitForBiteLoop());
        return true;
    }

    private IEnumerator WaitForBiteLoop()
    {
        while (_phase is SessionPhase.Waiting or SessionPhase.BiteReady)
        {
            _phase = SessionPhase.Waiting;
            _ui?.ShowWaiting();
            yield return new WaitForSecondsRealtime(Random.Range(
                _definition.MinimumWaitSeconds,
                _definition.MaximumWaitSeconds));

            if (_phase != SessionPhase.Waiting)
                yield break;

            _phase = SessionPhase.BiteReady;
            _ui?.ShowBitePrompt();
            float elapsed = 0f;
            while (_phase == SessionPhase.BiteReady && elapsed < _definition.HookWindowSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_phase == SessionPhase.BiteReady)
                _phase = SessionPhase.Waiting;
        }
    }

    private void BeginMinigame()
    {
        if (_phase != SessionPhase.BiteReady)
            return;

        if (_waitingRoutine != null)
        {
            StopCoroutine(_waitingRoutine);
            _waitingRoutine = null;
        }

        _phase = SessionPhase.Minigame;
        _remainingTime = _definition.TimeLimitSeconds;
        _progress = 0f;
        _fishPosition = Random.value;
        _fishTarget = Random.value;
        _fishTargetTimer = _definition.FishTargetInterval;
        _catchZonePosition = 0.5f;
        _catchZoneVelocity = 0f;
        GameStateManager.Instance.ReplaceState(GameState.FishingMinigame);
        _ui?.ShowMinigame(_selectedFish, _remainingTime);
        UpdatePresentation();
    }

    private void TickMinigame(float deltaTime)
    {
        _remainingTime -= deltaTime;
        _fishTargetTimer -= deltaTime;
        if (_fishTargetTimer <= 0f)
        {
            _fishTarget = Random.value;
            _fishTargetTimer = _definition.FishTargetInterval * Random.Range(0.65f, 1.35f);
        }

        _fishPosition = Mathf.MoveTowards(
            _fishPosition,
            _fishTarget,
            _definition.FishSpeed * deltaTime);

        bool holding = Mouse.current != null && Mouse.current.leftButton.isPressed;
        _catchZoneVelocity += (holding ? _definition.LiftAcceleration : -_definition.Gravity) * deltaTime;
        _catchZoneVelocity = Mathf.Clamp(
            _catchZoneVelocity,
            -_definition.MaximumCatchZoneSpeed,
            _definition.MaximumCatchZoneSpeed);
        _catchZonePosition += _catchZoneVelocity * deltaTime;
        if (_catchZonePosition <= 0f || _catchZonePosition >= 1f)
        {
            _catchZonePosition = Mathf.Clamp01(_catchZonePosition);
            _catchZoneVelocity = 0f;
        }

        bool touchingFish = Mathf.Abs(_fishPosition - _catchZonePosition)
            <= (_definition.CatchZoneSize * 0.5f + 0.035f);
        _progress += (touchingFish
            ? _definition.CatchProgressPerSecond
            : -_definition.ProgressLossPerSecond) * deltaTime;
        _progress = Mathf.Clamp01(_progress);

        UpdatePresentation();
        if (_progress >= 1f)
            CompleteCatch();
        else if (_remainingTime <= 0f)
            CompleteFailure("The fish got away!");
    }

    private void CompleteCatch()
    {
        int weightGrams = _selectedFish.RollWeightGrams();
        if (InventoryManager.Instance == null
            || !InventoryManager.Instance.TryAddFish(_selectedFish, weightGrams))
        {
            CompleteFailure("Inventory full. The fish could not be stored.");
            return;
        }

        int value = _selectedFish.GetValueForWeight(weightGrams);
        BeginResult($"Caught {_selectedFish.itemName}\n{weightGrams / 1000f:0.00} kg  -  {value} gold");
    }

    private void CompleteFailure(string message) => BeginResult(message);

    private void BeginResult(string message)
    {
        if (_phase != SessionPhase.Minigame)
            return;
        _phase = SessionPhase.Result;
        _ui?.ShowResult(message);
        StartCoroutine(FinishAfterResult());
    }

    private IEnumerator FinishAfterResult()
    {
        yield return new WaitForSecondsRealtime(_resultDisplaySeconds);
        EndSession();
    }

    private void UpdatePresentation()
    {
        _ui?.SetFishPosition(_fishPosition);
        _ui?.SetCatchZonePosition(_catchZonePosition, _definition.CatchZoneSize);
        _ui?.SetProgress(_progress);
        _ui?.SetTime(_remainingTime);
    }

    public void EndSession()
    {
        if (_waitingRoutine != null)
        {
            StopCoroutine(_waitingRoutine);
            _waitingRoutine = null;
        }

        _phase = SessionPhase.None;
        _definition = null;
        _selectedFish = null;
        _spot = null;
        _ui?.HideAll();

        if (GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState is GameState.FishingWaiting or GameState.FishingMinigame)
        {
            GameStateManager.Instance.ReturnToPreviousState();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            EndSession();
            Instance = null;
        }
    }
}
