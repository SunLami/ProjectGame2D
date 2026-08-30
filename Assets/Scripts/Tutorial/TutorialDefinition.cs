using System.Collections.Generic;
using UnityEngine;

/// <summary>Ordered sequence of tutorial steps. tutorialId is the catalog identity; step order in
/// the array is the progression order.</summary>
[CreateAssetMenu(fileName = "NewTutorialDefinition", menuName = "Game/Tutorial/Tutorial Definition")]
public sealed class TutorialDefinition : ScriptableObject
{
    [SerializeField] private string _tutorialId;
    [SerializeField] private TutorialStepDefinition[] _steps;

    public string TutorialId => _tutorialId;
    public IReadOnlyList<TutorialStepDefinition> Steps => _steps;
}
