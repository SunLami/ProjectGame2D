using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds / updates one SceneAmbience object (ambience bed + fallback footstep surface) per gameplay scene.
/// Idempotent: re-running only refreshes the settings of the existing object.</summary>
public static class SceneAmbienceInstaller
{
    private readonly struct Plan
    {
        public readonly string Scene, Bed, SecondBed, Footstep;
        public readonly float BedLevel, SecondLevel;

        public Plan(string scene, string bed, float bedLevel, string secondBed, float secondLevel, string footstep)
        {
            Scene = scene; Bed = bed; BedLevel = bedLevel; SecondBed = secondBed; SecondLevel = secondLevel; Footstep = footstep;
        }
    }

    private static readonly Plan[] Plans =
    {
        new Plan("Assets/Scenes/MainMenu.unity", SfxIds.AmbCampfire, 0.7f, SfxIds.AmbNight, 0.4f, ""),
        new Plan("Assets/Scenes/DemoScene.unity", SfxIds.AmbForest, 0.6f, SfxIds.AmbWind, 0.25f, ""),
        new Plan("Assets/Scenes/MapNhat.unity", SfxIds.AmbVillage, 0.6f, SfxIds.AmbForest, 0.3f, ""),
        new Plan("Assets/Scenes/MapDuy.unity", SfxIds.AmbForest, 0.6f, SfxIds.AmbWind, 0.25f, ""),
        new Plan("Assets/Scenes/BossArena_Earth.unity", SfxIds.AmbCave, 0.7f, SfxIds.AmbWind, 0.2f, SfxIds.StepStone),
        new Plan("Assets/Scenes/BossArena_Water.unity", SfxIds.AmbBeach, 0.7f, SfxIds.AmbWind, 0.2f, SfxIds.StepSand),
        new Plan("Assets/Scenes/BossArena_Wind.unity", SfxIds.AmbWind, 0.75f, "", 0f, SfxIds.StepStone),
    };

    [MenuItem("Tools/SFX/Install Scene Ambience")]
    public static void InstallAll()
    {
        string previous = SceneManager.GetActiveScene().path;
        foreach (Plan plan in Plans)
        {
            string sceneName = Path.GetFileNameWithoutExtension(plan.Scene);
            if (SfxDatabaseTools.AllScenes().All(p => p.sceneName != sceneName))
            {
                SfxSceneProfile profile = SfxDatabaseTools.CreateSceneProfile(sceneName);
                profile.bedId = plan.Bed;
                profile.bedLevel = plan.BedLevel;
                profile.secondBedId = plan.SecondBed;
                profile.secondBedLevel = plan.SecondLevel;
                profile.fallbackFootstepId = plan.Footstep;
                EditorUtility.SetDirty(profile);
            }

            Scene scene = EditorSceneManager.OpenScene(plan.Scene, OpenSceneMode.Single);
            SceneAmbience ambience = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ambience = root.GetComponentInChildren<SceneAmbience>(true);
                if (ambience != null)
                    break;
            }

            if (ambience == null)
                ambience = new GameObject("SceneAmbience").AddComponent<SceneAmbience>();

            ambience.Configure(plan.Bed, plan.SecondBed, plan.BedLevel, plan.SecondLevel, plan.Footstep);
            EditorUtility.SetDirty(ambience);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(previous))
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        Debug.Log("SceneAmbienceInstaller: done (scene profiles live in the SFX Manager > Scenes tab).");
    }
}
