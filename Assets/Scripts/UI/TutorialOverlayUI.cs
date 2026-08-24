using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    private void OnEnable()
    {
        _skipButton.onClick.AddListener(OpenSkipConfirmation);
        _confirmSkipButton.onClick.AddListener(ConfirmSkip);
        _cancelSkipButton.onClick.AddListener(CloseSkipConfirmation);
        BindTutorialManager();
    }

    private void Start()
    {
        // All scene Awake calls have completed by Start. This covers an earlier OnEnable caused by
        // Script Execution Order without delaying the normal immediate CurrentStep refresh.
        if (_tutorialManager == null)
            BindTutorialManager();
    }

    private void OnDisable()
    {
        _skipButton.onClick.RemoveListener(OpenSkipConfirmation);
        _confirmSkipButton.onClick.RemoveListener(ConfirmSkip);
        _cancelSkipButton.onClick.RemoveListener(CloseSkipConfirmation);
        UnbindTutorialManager();
    }

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
        _skipConfirmation.SetActive(false);
        _instructionPanel.SetActive(false);
    }

    private void Refresh(TutorialStepDefinition step)
    {
        _skipConfirmation.SetActive(false);
        bool hasActiveStep = step != null;
        _instructionPanel.SetActive(hasActiveStep);

        if (hasActiveStep)
            _instructionText.text = step.InstructionText;
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
