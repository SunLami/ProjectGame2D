using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A group of related sounds in ONE asset (e.g. "Skill - Thuy", "Boss Earth", "UI"). Each row is one sound the code
/// can play: a readable label, the clips, a volume, and an advanced block. Every SfxBank under Resources/Audio/SfxBanks is
/// loaded automatically by <see cref="SoundFXLibrary"/> (it wins over the groups serialized in scenes). Open the asset and edit
/// the table - nothing to register.</summary>
[CreateAssetMenu(menuName = "Audio/SFX Bank", fileName = "New SFX Bank")]
public sealed class SfxBank : ScriptableObject
{
    public const string ResourceFolder = "Audio/SfxBanks";

    [Serializable]
    public class Row
    {
        [Tooltip("Readable name shown in the table.")] public string label;
        [Tooltip("The id gameplay code plays (do not rename unless the code is changed too).")] public string id;
        [Tooltip("Mixing group used by scene profiles (Combat, Skill, Boss, World, UI...).")] public string category;
        public string description;
        [Tooltip("One is picked at random on every play, never the same twice in a row.")] public List<AudioClip> clips = new List<AudioClip>();
        [Range(0f, 2f)] public float volume = 1f;
        [Range(0f, 0.3f), Tooltip("Random pitch change per play, +- fraction.")] public float pitchJitter;
        [Min(0f), Tooltip("Minimum seconds between two starts of this sound.")] public float minInterval;
        [Min(0), Tooltip("Simultaneous instances (0 = unlimited).")] public int maxVoices;
        [Tooltip("The clips are seamless loops (played with SoundFXManager.StartLoop).")] public bool loop;
    }

    public string title;
    public List<Row> rows = new List<Row>();
}

/// <summary>Draws a string field as a dropdown of every sound id in the project's SFX banks (editor only drawer).</summary>
public sealed class SfxIdAttribute : PropertyAttribute { }
