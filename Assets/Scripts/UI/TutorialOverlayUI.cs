using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Presentation adapter for the input tutorial. It observes TutorialManager but does not own
/// tutorial progression, game state, or time scale.
/// </summary>
public sealed class TutorialOverlayUI : MonoBehaviour
{
    [Header("Tutorial Prompt")]
    [SerializeField] private GameObject _instructionPanel;
    [SerializeField] private TMP_Text _instructionText;
    [SerializeField] private Button _skipButton;

    [Header("Skip Confirmation")]
    [SerializeField] private GameObject _skipConfirmation;
    [SerializeField] private Button _confirmSkipButton;
    [SerializeField] private Button _cancelSkipButton;

    private TutorialManager _tutorialManager;
    private TutorialStepDefinition _pendingStep;
    private bool _hiddenByPlayer;

    private void OnEnable()
    {
        _skipButton.onClick.AddListener(OpenSkipConfirmation);
        _confirmSkipButton.onClick.AddListener(ConfirmSkip);
        _cancelSkipButton.onClick.AddListener(CloseSkipConfirmation);
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged += HandleGameStateChanged;
        BindTutorialManager();
    }

    private void Start()
    {
        // All scene Awake calls have completed by Start. This covers an earlier OnEnable caused by
        // Script Execution Order without delaying the normal immediate CurrentStep refresh.
        if (_tutorialManager == null)
            BindTutorialManager();
    }

    private void LateUpdate()
    {
        bool freeGameplay = GameStateManager.Instance?.CurrentState == GameState.Playing
            && !IsCommerceWindowOpen;
        if (freeGameplay && !_skipConfirmation.activeSelf
            && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            _hiddenByPlayer = !_hiddenByPlayer;
        }

        UpdateBanner(freeGameplay);
    }

    private void UpdateBanner(bool freeGameplay)
    {
        bool hasActiveStep = _pendingStep != null && _pendingStep.Type != TutorialStepType.WaitForQuest;
        bool showStep = hasActiveStep && !_hiddenByPlayer && !IsGameplayMenuOpen;
        string hint = !hasActiveStep && !_hiddenByPlayer && freeGameplay
            ? QuestManager.Instance?.GetTrackedControlHint()
            : null;
        bool showHint = !string.IsNullOrEmpty(hint);

        _instructionPanel.SetActive(showStep || showHint);
        _skipButton.gameObject.SetActive(showStep);

        if (showHint && _instructionText.text != hint)
            _instructionText.text = hint;
    }

    private void OnDisable()
    {
        _skipButton.onClick.RemoveListener(OpenSkipConfirmation);
        _confirmSkipButton.onClick.RemoveListener(ConfirmSkip);
        _cancelSkipButton.onClick.RemoveListener(CloseSkipConfirmation);
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= HandleGameStateChanged;
        UnbindTutorialManager();
    }

    // Tutorial instruction/skip banner shares screen space with the bottom-right HUD area and sits
    // behind gameplay menus in sibling order except CharacterPopup, which renders nested inside
    // UnifiedGameplayHUD (above Tutorial in sibling order). Hiding this presentation-only banner
    // while any GameplayMenu is open keeps it from drawing over popup content without touching
    // TutorialManager progression, step binding, or the Skip callback.
    // Re-read the manager rather than the cached step: TutorialManager.RestoreState (Continue/load)
    // raises no OnStepChanged, so the cached step can still be the pre-restore first step (e.g.
    // "Move") for a save whose tutorial is already completed.
    private void HandleGameStateChanged(GameStateChange change)
    {
        if (_tutorialManager == null)
            BindTutorialManager();
        else
            Refresh(_tutorialManager.CurrentStep);
    }

    private static bool IsCommerceWindowOpen =>
        (ShopCraftingUI.Instance != null && ShopCraftingUI.Instance.IsOpen)
        || (FarmStorageUI.Instance != null && FarmStorageUI.Instance.IsOpen);

    private static bool IsGameplayMenuOpen =>
        GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.GameplayMenu;

    private void BindTutorialManager()
    {
        TutorialManager manager = TutorialManager.Instance;
        if (_tutorialManager != manager)
        {
            UnbindTutorialManager();
            _tutorialManager = manager;

            if (_tutorialManager != null)
            {
                _tutorialManager.OnStepChanged += HandleStepChanged;
                _tutorialManager.OnTutorialCompleted += HandleTutorialCompleted;
            }
        }

        Refresh(_tutorialManager != null ? _tutorialManager.CurrentStep : null);
    }

    private void UnbindTutorialManager()
    {
        if (_tutorialManager == null)
            return;

        _tutorialManager.OnStepChanged -= HandleStepChanged;
        _tutorialManager.OnTutorialCompleted -= HandleTutorialCompleted;
        _tutorialManager = null;
    }

    private void HandleStepChanged(TutorialStepDefinition step) => Refresh(step);

    private void HandleTutorialCompleted()
    {
        _pendingStep = null;
        _skipConfirmation.SetActive(false);
        _instructionPanel.SetActive(false);
        _skipButton.gameObject.SetActive(false);
    }

    private void Refresh(TutorialStepDefinition step)
    {
        _pendingStep = step;
        _skipConfirmation.SetActive(false);

        if (step != null && step.Type != TutorialStepType.WaitForQuest)
            _instructionText.text = step.InstructionText;

        UpdateBanner(GameStateManager.Instance?.CurrentState == GameState.Playing && !IsCommerceWindowOpen);
    }

    private void OpenSkipConfirmation()
    {
        if (_tutorialManager == null || _tutorialManager.CurrentStep == null)
            return;

        _skipConfirmation.SetActive(true);
        Select(_cancelSkipButton);
    }

    private void CloseSkipConfirmation()
    {
        _skipConfirmation.SetActive(false);
        Select(_skipButton);
    }

    private void ConfirmSkip()
    {
        _skipConfirmation.SetActive(false);
        _tutorialManager?.Skip();
    }

    private static void Select(Selectable selectable)
    {
        if (EventSystem.current != null && selectable != null && selectable.IsInteractable())
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
