using UnityEngine;

public sealed class GameplaySceneLifetime : MonoBehaviour
{
    [SerializeField] private GameObject[] _persistentGameplayRoots;

    public void ReleaseForSceneExit()
    {
        if (_persistentGameplayRoots == null)
            return;

        UnityEngine.SceneManagement.Scene gameplayScene = gameObject.scene;
        foreach (GameObject root in _persistentGameplayRoots)
        {
            if (root != null && root.scene != gameplayScene)
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameplayScene);
        }
    }
}
