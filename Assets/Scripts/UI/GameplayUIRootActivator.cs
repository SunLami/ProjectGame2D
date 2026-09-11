using UnityEngine;

// Keeps gameplay UI panels unchecked in the Editor Hierarchy for a clean scene view,
// while still activating them at scene load so their OnEnable-driven event
// subscriptions (GameStateManager, TutorialManager, etc.) run correctly during Play.
//
// This root now lives in Bootstrap.unity (shared across every map), which stays loaded from
// MainMenu through Loading through every gameplay scene -- so without this gate the HUD would
// render on top of MainMenu and the Loading screen too. Only show it once a real map is actually
// being played.
[RequireComponent(typeof(Canvas))]
public sealed class GameplayUIRootActivator : MonoBehaviour
{
    [SerializeField] private GameObject[] _rootsToActivate;

    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();

        foreach (GameObject root in _rootsToActivate)
        {
            if (root != null)
                root.SetActive(true);
        }
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StateChanged += HandleStateChanged;
            ApplyVisibility(GameStateManager.Instance.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameStateChange change) => ApplyVisibility(change.Current.State);

    private void ApplyVisibility(GameState state)
    {
        _canvas.enabled = state switch
        {
            GameState.Booting or GameState.MainMenu or GameState.Loading or GameState.Cutscene => false,
            _ => true,
        };
    }
}
