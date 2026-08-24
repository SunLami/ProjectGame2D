using UnityEngine;

/// <summary>Immutable authored content for one tutorial step. stepId is the save contract --
/// renaming displayed text never changes it.</summary>
[CreateAssetMenu(fileName = "NewTutorialStep", menuName = "Game/Tutorial/Tutorial Step")]
public sealed class TutorialStepDefinition : ScriptableObject
{
    [SerializeField] private string _stepId;
    [SerializeField] private TutorialStepType _type;
    [SerializeField] private string _targetAreaId;
    [SerializeField, TextArea] private string _instructionText;

    public string StepId => _stepId;
    public TutorialStepType Type => _type;

    /// <summary>Only meaningful when Type == ReachArea.</summary>
    public string TargetAreaId => _targetAreaId;
    public string InstructionText => _instructionText;
}
