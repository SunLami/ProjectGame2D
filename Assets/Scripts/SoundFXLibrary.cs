using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Per-ID playback profile read from a library group. A group with all-zero values is a "plain" one-shot (the
/// original behaviour: main AudioSource, no pitch variation, no limits).</summary>
public readonly struct SfxProfile
{
    public readonly float Volume;       // 0 in the asset = 1
    public readonly float PitchJitter;  // +- fraction of pitch
    public readonly float MinInterval;  // seconds between two starts of this id (0 = none)
    public readonly int MaxVoices;      // simultaneous instances (0 = unlimited)

    public SfxProfile(float volume, float pitchJitter, float minInterval, int maxVoices)
    {
        Volume = volume <= 0f ? 1f : volume;
        PitchJitter = Mathf.Max(0f, pitchJitter);
        MinInterval = Mathf.Max(0f, minInterval);
        MaxVoices = Mathf.Max(0, maxVoices);
    }

    public bool IsPlain => Mathf.Approximately(Volume, 1f) && PitchJitter <= 0f && MinInterval <= 0f && MaxVoices <= 0;
}

public class SoundFXLibrary : MonoBehaviour
{
    [SerializeField] private SoundFXGroup[] _soundFXGroups;
    private Dictionary<string, List<AudioClip>> _soundFXDictionary;
    private Dictionary<string, SfxProfile> _profiles;
    private Dictionary<string, string> _categories;
    private readonly Dictionary<string, int> _lastIndex = new Dictionary<string, int>();

    [Serializable]
    public struct SoundFXGroup
    {
        public string groupName;
        public List<AudioClip> audioClips;
        [Tooltip("Volume trim of this id (0 = 1). The catalog builder fills it from Tools/sfx.")]
        public float volume;
        [Tooltip("Random pitch variation, +- fraction (0.05 = +-5%).")]
        public float pitchJitter;
        [Tooltip("Minimum seconds between two starts of this id (0 = no limit).")]
        public float minInterval;
        [Tooltip("Maximum simultaneous instances of this id (0 = no limit).")]
        public int maxVoices;
        [Tooltip("Informational: the clips are seamless loops for SoundFXManager.StartLoop.")]
        public bool loop;
    }

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        _soundFXDictionary = new Dictionary<string, List<AudioClip>>();
        _profiles = new Dictionary<string, SfxProfile>();
        _categories = new Dictionary<string, string>();
        if (_soundFXGroups != null)
        {
            foreach (SoundFXGroup soundFXGroup in _soundFXGroups)
            {
                _soundFXDictionary[soundFXGroup.groupName] = soundFXGroup.audioClips;
                _profiles[soundFXGroup.groupName] = new SfxProfile(
                    soundFXGroup.volume, soundFXGroup.pitchJitter, soundFXGroup.minInterval, soundFXGroup.maxVoices);
            }
        }

        // SfxBank assets (Resources/Audio/SfxBanks, edited in the Inspector or Tools > SFX > SFX Manager) win over scene groups.
        foreach (SfxBank bank in Resources.LoadAll<SfxBank>(SfxBank.ResourceFolder))
        {
            foreach (SfxBank.Row row in bank.rows)
            {
                if (string.IsNullOrEmpty(row.id))
                    continue;

                _categories[row.id] = row.category;
                if (row.clips == null || row.clips.Count == 0)
                    continue;

                _soundFXDictionary[row.id] = row.clips;
                _profiles[row.id] = new SfxProfile(row.volume, row.pitchJitter, row.minInterval, row.maxVoices);
            }
        }
    }

    /// <summary>Category of an id from the database ("" when unknown); used for per-scene category levels.</summary>
    public string CategoryOf(string name)
    {
        EnsureInitialized();
        return name != null && _categories.TryGetValue(name, out string category) ? category : string.Empty;
    }

    public bool HasGroup(string name)
    {
        EnsureInitialized();
        return _soundFXDictionary.TryGetValue(name, out List<AudioClip> clips) && clips != null && clips.Count > 0;
    }

    public AudioClip GetRandomClip(string name)
    {
        return TryGetClip(name, out AudioClip clip, out _) ? clip : null;
    }

    /// <summary>Random clip of the group, never the same clip twice in a row when the group has variants.</summary>
    public bool TryGetClip(string name, out AudioClip clip, out SfxProfile profile)
    {
        EnsureInitialized();
        clip = null;
        profile = default;
        if (name == null || !_soundFXDictionary.TryGetValue(name, out List<AudioClip> audioClips)
            || audioClips == null || audioClips.Count == 0)
            return false;

        int index = UnityEngine.Random.Range(0, audioClips.Count);
        if (audioClips.Count > 1 && _lastIndex.TryGetValue(name, out int last) && index == last)
            index = (index + 1 + UnityEngine.Random.Range(0, audioClips.Count - 1)) % audioClips.Count;
        _lastIndex[name] = index;

        clip = audioClips[index];
        profile = _profiles.TryGetValue(name, out SfxProfile found) ? found : new SfxProfile(0f, 0f, 0f, 0);
        return clip != null;
    }

    private void EnsureInitialized()
    {
        if (_soundFXDictionary == null)
            InitializeDictionary();
    }
}
