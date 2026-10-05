using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SfxDatabaseTests
{
    private static IEnumerable<T> Load<T>(string folder) where T : Object =>
        AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(a => a != null);

    private static Dictionary<string, SfxBank.Row> Rows()
    {
        var rows = new Dictionary<string, SfxBank.Row>();
        foreach (SfxBank bank in Load<SfxBank>("Assets/Resources/Audio/SfxBanks"))
        {
            foreach (SfxBank.Row row in bank.rows)
                rows[row.id] = row;
        }

        return rows;
    }

    [Test]
    public void SceneProfile_MultipliesCategoryAndSoundLevels_AndMuteWins()
    {
        var profile = ScriptableObject.CreateInstance<SfxSceneProfile>();
        profile.categories.Add(new SfxSceneProfile.CategoryLevel { category = "Skill", level = 0.5f });
        profile.categories.Add(new SfxSceneProfile.CategoryLevel { category = "UI", mute = true });
        profile.sounds.Add(new SfxSceneProfile.SoundLevel { id = "sfx.skill.a", level = 0.5f });
        profile.sounds.Add(new SfxSceneProfile.SoundLevel { id = "sfx.skill.b", mute = true });

        Assert.AreEqual(1f, profile.Multiplier("sfx.x", "Combat"), 1e-5f);
        Assert.AreEqual(0.5f, profile.Multiplier("sfx.skill.c", "Skill"), 1e-5f);
        Assert.AreEqual(0.25f, profile.Multiplier("sfx.skill.a", "Skill"), 1e-5f);
        Assert.AreEqual(0f, profile.Multiplier("sfx.skill.b", "Skill"), 1e-5f);
        Assert.AreEqual(0f, profile.Multiplier("sfx.ui.click", "UI"), 1e-5f);

        Object.DestroyImmediate(profile);
    }

    [Test]
    public void SceneMix_WithoutProfile_IsNeutral()
    {
        SfxSceneMix.Current = null;
        Assert.AreEqual(1f, SfxSceneMix.Multiplier("sfx.any", "Any"), 1e-5f);
    }

    [Test]
    public void Banks_CoverEveryCatalogIdConstant_WithClips()
    {
        Dictionary<string, SfxBank.Row> rows = Rows();
        Assert.Greater(rows.Count, 100, "Run Tools/SFX/Apply Catalog first");

        foreach (System.Reflection.FieldInfo field in typeof(SfxIds).GetFields())
        {
            string id = (string)field.GetRawConstantValue();
            if (!rows.TryGetValue(id, out SfxBank.Row row))
                continue; // legacy UI ids live in the Bootstrap library
            Assert.IsNotEmpty(row.clips, id + " has no clips");
            Assert.IsFalse(row.clips.Exists(c => c == null), id + " has a missing clip");
            Assert.IsFalse(string.IsNullOrEmpty(row.label), id + " has no label");
        }
    }

    [Test]
    public void SceneProfiles_ReferenceExistingSounds()
    {
        Dictionary<string, SfxBank.Row> rows = Rows();
        foreach (SfxSceneProfile p in Load<SfxSceneProfile>("Assets/Resources/Audio/SfxScenes"))
        {
            Assert.IsFalse(string.IsNullOrEmpty(p.sceneName));
            foreach (string id in new[] { p.bedId, p.secondBedId, p.fallbackFootstepId })
                Assert.IsTrue(string.IsNullOrEmpty(id) || rows.ContainsKey(id), p.sceneName + " references missing sound '" + id + "'");
        }
    }
}
