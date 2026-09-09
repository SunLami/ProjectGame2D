using UnityEngine;

// Keeps gameplay UI panels unchecked in the Editor Hierarchy for a clean scene view,
// while still activating them at scene load so their OnEnable-driven event
// subscriptions (GameStateManager, TutorialManager, etc.) run correctly during Play.
public sealed class GameplayUIRootActivator : MonoBehaviour
{
    [SerializeField] private GameObject[] _rootsToActivate;

    private void Awake()
    {
        foreach (GameObject root in _rootsToActivate)
        {
            if (root != null)
                root.SetActive(true);
        }
    }
}
