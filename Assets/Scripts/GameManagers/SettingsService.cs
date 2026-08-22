using System;
using UnityEngine;

public readonly struct SettingsSnapshot
{
    public SettingsSnapshot(float sfxVolume, float musicVolume, bool fullScreen)
    {
        SfxVolume = sfxVolume;
        MusicVolume = musicVolume;
        FullScreen = fullScreen;
    }

    public float SfxVolume { get; }
    public float MusicVolume { get; }
    public bool FullScreen { get; }
}

[DefaultExecutionOrder(-1000)]
public sealed class SettingsService : MonoBehaviour
{
    private const string SfxVolumeKey = "Settings_SfxVolume";
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string FullScreenKey = "Settings_FullScreen";

    public static SettingsService Instance { get; private set; }

    public SettingsSnapshot Current { get; private set; }

    public event Action<SettingsSnapshot> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject serviceObject = new(nameof(SettingsService));
        serviceObject.AddComponent<SettingsService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Current = Load();
        ApplyCurrent();
    }

    public void SetSfxVolume(float value)
    {
        Replace(new SettingsSnapshot(Mathf.Clamp01(value), Current.MusicVolume, Current.FullScreen));
    }

    public void SetMusicVolume(float value)
    {
        Replace(new SettingsSnapshot(Current.SfxVolume, Mathf.Clamp01(value), Current.FullScreen));
    }

    public void SetFullScreen(bool value)
    {
        Replace(new SettingsSnapshot(Current.SfxVolume, Current.MusicVolume, value));
    }

    public void Restore(SettingsSnapshot snapshot)
    {
        Replace(new SettingsSnapshot(
            Mathf.Clamp01(snapshot.SfxVolume),
            Mathf.Clamp01(snapshot.MusicVolume),
            snapshot.FullScreen));
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Current.SfxVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, Current.MusicVolume);
        PlayerPrefs.SetInt(FullScreenKey, Current.FullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ApplyAudioSettings()
    {
        SoundFXManager.SetVolume(Current.SfxVolume);
        MusicManager.SetVolume(Current.MusicVolume);
    }

    private void Replace(SettingsSnapshot next)
    {
        Current = next;
        ApplyCurrent();
        Changed?.Invoke(Current);
    }

    private void ApplyCurrent()
    {
        ApplyAudioSettings();
        Screen.fullScreen = Current.FullScreen;
    }

    private static SettingsSnapshot Load()
    {
        return new SettingsSnapshot(
            PlayerPrefs.GetFloat(SfxVolumeKey, 1f),
            PlayerPrefs.GetFloat(MusicVolumeKey, 1f),
            PlayerPrefs.GetInt(FullScreenKey, Screen.fullScreen ? 1 : 0) == 1);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
