using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bulk-attaches ButtonSfx to every Button/Toggle in the currently open scene that doesn't have
/// one yet. Run once per scene (MainMenu.unity, MapNhat.unity) with that scene open in the Editor
/// -- see AudioSfxSystem.md Phase A. Name-based heuristics pick Secondary/Toggle click sound;
/// everything else defaults to Primary. Review the result in the Inspector afterward and fix any
/// misclassified button by hand (e.g. a Delete button that should read as Secondary, not Primary).
/// </summary>
public static class ButtonSfxInstaller
{
    private static readonly string[] ProductionScenePaths =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/IntroCutscene.unity",
        "Assets/Scenes/MapNhat.unity"
    };

    private static readonly string[] SecondaryNameHints =
    {
        "close", "cancel", "back", "decline", "quit", "delete", "return", "exit"
    };

    [MenuItem("Tools/ProjectGame2D/Audio/Attach Button SFX To Open Scene")]
    public static void AttachToOpenScene()
    {
        InstallInHierarchy(
            Object.FindObjectsByType<Button>(FindObjectsInactive.Include),
            Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include),
            out int buttonsAdded,
            out int togglesAdded,
            out int reclassified);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[ButtonSfxInstaller] Added ButtonSfx to {buttonsAdded} Button(s) and "
            + $"{togglesAdded} Toggle(s) in '{EditorSceneManager.GetActiveScene().name}'. "
            + $"{reclassified} existing component(s) were reclassified. Save the scene, then "
            + "review click-sound classification in the Inspector.");
    }

    [MenuItem("Tools/ProjectGame2D/Audio/Install Phase A UI SFX Project-Wide")]
    public static void InstallProjectWide()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.isDirty)
        {
            Debug.LogError("[ButtonSfxInstaller] Save the currently open scene before running the project-wide installer.");
            return;
        }

        string currentScenePath = currentScene.path;
        int buttonsAdded = 0;
        int togglesAdded = 0;
        int reclassified = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/Resources/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                InstallInHierarchy(
                    root.GetComponentsInChildren<Button>(true),
                    root.GetComponentsInChildren<Toggle>(true),
                    out int prefabButtons,
                    out int prefabToggles,
                    out int prefabReclassified);

                buttonsAdded += prefabButtons;
                togglesAdded += prefabToggles;
                reclassified += prefabReclassified;
                if (prefabButtons > 0 || prefabToggles > 0 || prefabReclassified > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        foreach (string scenePath in ProductionScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            InstallInHierarchy(
                Object.FindObjectsByType<Button>(FindObjectsInactive.Include),
                Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include),
                out int sceneButtons,
                out int sceneToggles,
                out int sceneReclassified);

            buttonsAdded += sceneButtons;
            togglesAdded += sceneToggles;
            reclassified += sceneReclassified;
            if (sceneButtons > 0 || sceneToggles > 0 || sceneReclassified > 0)
                EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(currentScenePath))
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        Debug.Log($"[ButtonSfxInstaller] Project-wide Phase A installation complete. Added "
            + $"{buttonsAdded} Button(s), {togglesAdded} Toggle(s); {reclassified} existing "
            + "ButtonSfx component(s) were reclassified where needed.");
    }

    private static void InstallInHierarchy(
        Button[] buttons,
        Toggle[] toggles,
        out int buttonsAdded,
        out int togglesAdded,
        out int reclassified)
    {
        buttonsAdded = 0;
        togglesAdded = 0;
        reclassified = 0;

        foreach (Button button in buttons)
        {
            ButtonSfx sfx = button.GetComponent<ButtonSfx>();
            if (sfx == null)
            {
                sfx = button.gameObject.AddComponent<ButtonSfx>();
                buttonsAdded++;
            }

            if (SetClickSound(sfx, LooksSecondary(button.gameObject.name)
                ? ButtonSfx.ClickSound.Secondary
                : ButtonSfx.ClickSound.Primary))
                reclassified++;
            EditorUtility.SetDirty(button.gameObject);
        }

        foreach (Toggle toggle in toggles)
        {
            ButtonSfx sfx = toggle.GetComponent<ButtonSfx>();
            if (sfx == null)
            {
                sfx = toggle.gameObject.AddComponent<ButtonSfx>();
                togglesAdded++;
            }

            if (SetClickSound(sfx, ButtonSfx.ClickSound.Toggle))
                reclassified++;
            EditorUtility.SetDirty(toggle.gameObject);
        }

    }

    private static bool LooksSecondary(string gameObjectName)
    {
        string lowered = gameObjectName.ToLowerInvariant();
        foreach (string hint in SecondaryNameHints)
        {
            if (lowered.Contains(hint))
                return true;
        }
        return false;
    }

    private static bool SetClickSound(ButtonSfx sfx, ButtonSfx.ClickSound clickSound)
    {
        SerializedObject serialized = new(sfx);
        SerializedProperty property = serialized.FindProperty("_clickSound");
        if (property.enumValueIndex == (int)clickSound)
            return false;

        property.enumValueIndex = (int)clickSound;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }
}
