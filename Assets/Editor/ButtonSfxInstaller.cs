using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
    private static readonly string[] SecondaryNameHints =
    {
        "close", "cancel", "back", "decline", "quit", "delete", "return", "exit"
    };

    [MenuItem("Tools/ProjectGame2D/Audio/Attach Button SFX To Open Scene")]
    public static void AttachToOpenScene()
    {
        int buttonsAdded = 0;
        int togglesAdded = 0;
        int alreadyPresent = 0;

        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.GetComponent<ButtonSfx>() != null)
            {
                alreadyPresent++;
                continue;
            }

            ButtonSfx sfx = button.gameObject.AddComponent<ButtonSfx>();
            SetClickSound(sfx, LooksSecondary(button.gameObject.name)
                ? ButtonSfx.ClickSound.Secondary
                : ButtonSfx.ClickSound.Primary);
            EditorUtility.SetDirty(button.gameObject);
            buttonsAdded++;
        }

        foreach (Toggle toggle in Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (toggle.GetComponent<ButtonSfx>() != null)
            {
                alreadyPresent++;
                continue;
            }

            ButtonSfx sfx = toggle.gameObject.AddComponent<ButtonSfx>();
            SetClickSound(sfx, ButtonSfx.ClickSound.Toggle);
            EditorUtility.SetDirty(toggle.gameObject);
            togglesAdded++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[ButtonSfxInstaller] Added ButtonSfx to {buttonsAdded} Button(s) and "
            + $"{togglesAdded} Toggle(s) in '{EditorSceneManager.GetActiveScene().name}'. "
            + $"{alreadyPresent} already had one and were left untouched. Save the scene, then "
            + "review click-sound classification in the Inspector.");
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

    private static void SetClickSound(ButtonSfx sfx, ButtonSfx.ClickSound clickSound)
    {
        SerializedObject serialized = new(sfx);
        serialized.FindProperty("_clickSound").enumValueIndex = (int)clickSound;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
