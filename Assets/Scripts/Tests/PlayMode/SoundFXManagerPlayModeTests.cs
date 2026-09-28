using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SoundFXManagerPlayModeTests
{
    [SetUp]
    public void SetUp() => DestroySoundFxManagers();

    [TearDown]
    public void TearDown() => DestroySoundFxManagers();

    private static void DestroySoundFxManagers()
    {
        foreach (SoundFXManager manager in
                 Object.FindObjectsByType<SoundFXManager>(FindObjectsInactive.Include))
            Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void PlaySfx_PlaysResolvedClip_WhenIdExistsInLibrary()
    {
        AudioClip clip = AudioClip.Create("sfx.ui.click_primary", 44100, 1, 44100, false);
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

    [Test]
    public void ButtonSfx_OnSelect_PlaysHoverForKeyboardOrGamepadFocus()
    {
        AudioClip clip = AudioClip.Create("sfx.ui.hover", 44100, 1, 44100, false);
        GameObject managerObject = CreateSoundFXManager("sfx.ui.hover", clip);
        ButtonSfx buttonSfx = new GameObject("ButtonSfxUnderTest").AddComponent<ButtonSfx>();

        buttonSfx.OnSelect(null);

        Assert.IsTrue(managerObject.GetComponent<AudioSource>().isPlaying);
        Object.DestroyImmediate(buttonSfx.gameObject);
        Object.DestroyImmediate(clip);
    }

    [Test]
    public void ButtonSfx_OnSubmit_PlaysConfiguredClickForKeyboardOrGamepadSubmit()
    {
        AudioClip clip = AudioClip.Create("sfx.ui.click_primary", 44100, 1, 44100, false);
        GameObject managerObject = CreateSoundFXManager("sfx.ui.click_primary", clip);
        ButtonSfx buttonSfx = new GameObject("ButtonSfxUnderTest").AddComponent<ButtonSfx>();

        buttonSfx.OnSubmit(null);

        Assert.IsTrue(managerObject.GetComponent<AudioSource>().isPlaying);
        Object.DestroyImmediate(buttonSfx.gameObject);
        Object.DestroyImmediate(clip);
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
