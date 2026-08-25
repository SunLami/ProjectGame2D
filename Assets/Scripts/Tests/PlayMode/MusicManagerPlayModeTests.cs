using NUnit.Framework;
using UnityEngine;

public sealed class MusicManagerPlayModeTests
{
    [TearDown]
    public void TearDown()
    {
        if (MusicManager.Instance != null)
            Object.DestroyImmediate(MusicManager.Instance.gameObject);
    }

    [Test]
    public void SceneTrackVolume_IsMultipliedBySettingsVolume_WhenTrackChanges()
    {
        AudioClip menuClip = AudioClip.Create("MenuTrack", 64, 1, 44100, false);
        AudioClip gameplayClip = AudioClip.Create("GameplayTrack", 64, 1, 44100, false);

        GameObject menuObject = CreateMusicManager("MenuMusic", menuClip, 0.5f);
        AudioSource persistentSource = menuObject.GetComponent<AudioSource>();

        MusicManager.SetVolume(0.4f);
        Assert.AreEqual(0.2f, persistentSource.volume, 0.0001f);

        GameObject gameplayObject = CreateMusicManager("GameplayMusic", gameplayClip, 0.35f);

        Assert.AreSame(gameplayClip, persistentSource.clip);
        Assert.AreEqual(0.14f, persistentSource.volume, 0.0001f);

        Object.DestroyImmediate(gameplayObject);
        Object.DestroyImmediate(menuClip);
        Object.DestroyImmediate(gameplayClip);
    }

    private static GameObject CreateMusicManager(string name, AudioClip clip, float trackVolume)
    {
        var musicObject = new GameObject(name);
        musicObject.SetActive(false);

        AudioSource source = musicObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = trackVolume;
        musicObject.AddComponent<MusicManager>();

        musicObject.SetActive(true);
        return musicObject;
    }
}
