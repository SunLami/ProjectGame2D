using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SoundFXManagerPlayModeTests
{
    [TearDown]
    public void TearDown()
    {
        if (SoundFXManager.Instance != null)
            Object.DestroyImmediate(SoundFXManager.Instance.gameObject);
    }

    [Test]
    public void PlaySfx_PlaysResolvedClip_WhenIdExistsInLibrary()
    {
        AudioClip clip = AudioClip.Create("sfx.ui.click_primary", 64, 1, 44100, false);
        GameObject managerObject = CreateSoundFXManager("sfx.ui.click_primary", clip);
        AudioSource source = managerObject.GetComponent<AudioSource>();

        SoundFXManager.PlaySfx("sfx.ui.click_primary");

        Assert.IsTrue(source.isPlaying);

        Object.DestroyImmediate(clip);
    }

    [Test]
    public void PlaySfx_DoesNotThrow_WhenIdIsMissingFromLibrary()
    {
        GameObject managerObject = CreateSoundFXManager("sfx.ui.click_primary", null);

        Assert.DoesNotThrow(() => SoundFXManager.PlaySfx("sfx.ui.does_not_exist"));
    }

    private static GameObject CreateSoundFXManager(string groupName, AudioClip clip)
    {
        var managerObject = new GameObject("SoundFXManagerUnderTest");
        managerObject.SetActive(false);

        managerObject.AddComponent<AudioSource>();

        SoundFXLibrary library = managerObject.AddComponent<SoundFXLibrary>();
        if (clip != null)
        {
            var group = new SoundFXLibrary.SoundFXGroup
            {
                groupName = groupName,
                audioClips = new System.Collections.Generic.List<AudioClip> { clip }
            };
            FieldInfo groupsField = typeof(SoundFXLibrary).GetField(
                "_soundFXGroups", BindingFlags.NonPublic | BindingFlags.Instance);
            groupsField.SetValue(library, new[] { group });
        }

        managerObject.AddComponent<SoundFXManager>();

        managerObject.SetActive(true);
        return managerObject;
    }
}
