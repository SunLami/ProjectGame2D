using UnityEngine;

public enum GameBootstrapMode
{
    MainMenu,
    DevelopmentGameplay
}

[DefaultExecutionOrder(-900)]
public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameBootstrapMode _mode;

    private void Awake()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager == null)
        {
            Debug.LogError("GameBootstrap requires GameStateManager to be initialized.", this);
            return;
        }

        switch (_mode)
        {
            case GameBootstrapMode.MainMenu:
                if (stateManager.CurrentState == GameState.Booting)
                    stateManager.ResetToMainMenu();
                break;
            case GameBootstrapMode.DevelopmentGameplay:
                if (stateManager.CurrentState != GameState.Booting)
                    break;

                if (!GameSessionManager.Instance.TryStartDevelopment(gameObject.scene.name))
                {
                    Debug.LogError("Could not create the development game session.", this);
                    break;
                }

                stateManager.ResetToPlaying();
                break;
            default:
                Debug.LogError($"Unsupported bootstrap mode: {_mode}.", this);
                break;
        }
    }
}
