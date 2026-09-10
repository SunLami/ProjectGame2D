using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Lets developers keep hitting Play directly on any content scene (MapNhat, IntroCutscene,
/// MainMenu, future maps) for fast iteration, even though those scenes no longer carry their own
/// _Managers/_UI -- those now live only in Bootstrap.unity. Before entering Play Mode, this
/// records whichever scene was open and forces Unity to always actually start from Bootstrap;
/// BootstrapSceneLoader then reads the recorded scene back and additively loads it.
/// </summary>
[InitializeOnLoad]
internal static class PlayModeBootstrap
{
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
    public const string LastEditedScenePrefKey = "PlayModeBootstrap.LastEditedScene";

    static PlayModeBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        string activeScenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(activeScenePath) || activeScenePath == BootstrapScenePath)
            SessionState.EraseString(LastEditedScenePrefKey);
        else
            SessionState.SetString(LastEditedScenePrefKey, activeScenePath);

        SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        EditorSceneManager.playModeStartScene = bootstrapScene;
    }
}
