using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Lives on the Bootstrap scene (the one that owns _Managers/_UI and never unloads during
/// gameplay). On a real build, Bootstrap is always build index 0 and this additively loads
/// MainMenu right after it. In the Editor, hitting Play on any scene routes through Bootstrap
/// first (see PlayModeBootstrap) -- this reads which scene the developer actually had open and
/// loads that instead, so direct Play-on-MapNhat iteration keeps working unchanged.
/// </summary>
[DefaultExecutionOrder(-950)]
public sealed class BootstrapSceneLoader : MonoBehaviour
{
    private const string DefaultEntryScene = "MainMenu";

#if UNITY_EDITOR
    // Must match PlayModeBootstrap.LastEditedScenePrefKey (Assets/Editor/PlayModeBootstrap.cs).
    // Duplicated as a literal because scripts outside a folder named "Editor" cannot reference
    // types defined inside one -- they compile into separate assemblies.
    private const string LastEditedScenePrefKey = "PlayModeBootstrap.LastEditedScene";
#endif

    private void Start()
    {
        string targetScene = ResolveTargetScene();
        if (!SceneManager.GetSceneByName(targetScene).isLoaded)
            SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
    }

    private static string ResolveTargetScene()
    {
#if UNITY_EDITOR
        string storedScenePath = SessionState.GetString(LastEditedScenePrefKey, string.Empty);
        if (!string.IsNullOrEmpty(storedScenePath))
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(storedScenePath);
            if (!string.IsNullOrEmpty(sceneName))
                return sceneName;
        }
#endif
        return DefaultEntryScene;
    }
}
