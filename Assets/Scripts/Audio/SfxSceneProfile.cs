using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Sound settings of one scene as an asset (Resources/Audio/SfxScenes, matched by scene name): ambience beds,
/// fallback footsteps, and a volume / mute per SFX category or per single sound. Read by <see cref="SceneAmbience"/>
/// when the scene loads. Sound fields are dropdowns of every id in the SFX banks.</summary>
[CreateAssetMenu(menuName = "Audio/SFX Scene Profile", fileName = "SceneName")]
public sealed class SfxSceneProfile : ScriptableObject
{
    public const string ResourceFolder = "Audio/SfxScenes";

    [Serializable]
    public class CategoryLevel
    {
        public string category;
        [Range(0f, 2f)] public float level = 1f;
        public bool mute;
    }

    [Serializable]
    public class SoundLevel
    {
        [SfxId] public string id;
        [Range(0f, 2f)] public float level = 1f;
        public bool mute;
    }

    [Tooltip("Exact scene name (MainMenu, DemoScene, BossArena_Earth...).")] public string sceneName;

    [Header("Ambience (looping beds, SFX channel)")]
    [SfxId] public string bedId;
    [Range(0f, 1f)] public float bedLevel = 0.6f;
    [SfxId] public string secondBedId;
    [Range(0f, 1f)] public float secondBedLevel = 0.3f;
    [Min(0f)] public float fadeInSeconds = 2f;
    [Tooltip("Footstep played when the walked tile has no clip (empty = silent there).")]
    [SfxId] public string fallbackFootstepId;

    [Header("Mix of this scene")]
    public List<CategoryLevel> categories = new List<CategoryLevel>();
    public List<SoundLevel> sounds = new List<SoundLevel>();

    private static Dictionary<string, SfxSceneProfile> _byScene;

    public static SfxSceneProfile Find(string scene)
    {
        if (_byScene == null || !Application.isPlaying)
        {
            _byScene = new Dictionary<string, SfxSceneProfile>();
            foreach (SfxSceneProfile p in Resources.LoadAll<SfxSceneProfile>(ResourceFolder))
            {
                if (!string.IsNullOrEmpty(p.sceneName))
                    _byScene[p.sceneName] = p;
            }
        }

        return _byScene.TryGetValue(scene, out SfxSceneProfile found) ? found : null;
    }

    public float Multiplier(string id, string category)
    {
        float level = 1f;
        foreach (CategoryLevel c in categories)
        {
            if (c.category == category)
            {
                if (c.mute)
                    return 0f;
                level *= c.level;
            }
        }

        foreach (SoundLevel s in sounds)
        {
            if (s.id == id)
            {
                if (s.mute)
                    return 0f;
                level *= s.level;
            }
        }

        return level;
    }
}

/// <summary>The scene profile that is currently active (set by <see cref="SceneAmbience"/>).</summary>
public static class SfxSceneMix
{
    public static SfxSceneProfile Current { get; set; }

    public static float Multiplier(string id, string category) => Current == null ? 1f : Current.Multiplier(id, category);
}
