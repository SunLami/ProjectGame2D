using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXLibrary : MonoBehaviour
{
    [SerializeField] private SoundFXGroup[] _soundFXGroups;
    private Dictionary<string, List<AudioClip>> _soundFXDictionary;

    [Serializable]
    public struct SoundFXGroup
    {
        public string groupName;
        public List<AudioClip> audioClips;
    }

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        _soundFXDictionary = new Dictionary<string, List<AudioClip>>();
        foreach (SoundFXGroup soundFXGroup in _soundFXGroups)
        {
            _soundFXDictionary[soundFXGroup.groupName] = soundFXGroup.audioClips;
        }
    }

    public AudioClip GetRandomClip(string name)
    {
        if (_soundFXDictionary.ContainsKey(name))
        {
            List<AudioClip> audioClips = _soundFXDictionary[name];
            if (audioClips.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, audioClips.Count);
                return audioClips[randomIndex];
            }
        }
        return null;
    }

}
