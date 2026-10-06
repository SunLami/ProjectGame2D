using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Static consistency of the SFX content: every id the code can request exists in the catalog written by
/// Tools/sfx, and every catalog clip is an imported mono AudioClip with the import mode the pipeline expects.</summary>
public sealed class SfxCatalogTests
{
    private const string CatalogPath = "Assets/Editor/Sfx/sfx_catalog.json";

    // Hand-made UI ids (not produced by Tools/sfx).
    private static readonly string[] LegacyIds =
    {
        "sfx.ui.hover", "sfx.ui.click_primary", "sfx.ui.click_secondary", "sfx.ui.toggle",
        "sfx.ui.error", "sfx.ui.popup_open", "sfx.ui.popup_close",
    };

    [Serializable] private class CatalogFile { public List<Entry> entries = new List<Entry>(); }

    [Serializable]
    private class Entry
    {
        public string id;
        public string[] files;
        public bool loop;
    }

    private static CatalogFile Load() => JsonUtility.FromJson<CatalogFile>(File.ReadAllText(CatalogPath));

    [Test]
    public void EveryIdConstantExistsInCatalogOrLegacyList()
    {
        var known = new HashSet<string>(LegacyIds);
        foreach (Entry entry in Load().entries)
            known.Add(entry.id);

        foreach (FieldInfo field in typeof(SfxIds).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            string id = (string)field.GetRawConstantValue();
            Assert.IsTrue(known.Contains(id), "SfxIds." + field.Name + " = '" + id + "' has no catalog entry");
        }
    }

    [Test]
    public void EveryCatalogClipIsAnImportedMonoClip()
    {
        foreach (Entry entry in Load().entries)
        {
            Assert.IsNotEmpty(entry.files, entry.id);
            foreach (string file in entry.files)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(file);
                Assert.IsNotNull(clip, entry.id + ": missing clip " + file);
                Assert.AreEqual(1, clip.channels, file + " must be mono");
                Assert.Greater(clip.length, 0.05f, file + " is too short");
                if (entry.loop)
                    Assert.Greater(clip.length, 1f, file + " loop is too short");
            }
        }
    }

    [Test]
    public void LiteralSfxIdsInScriptsExistInCatalog()
    {
        var known = new HashSet<string>(LegacyIds);
        foreach (Entry entry in Load().entries)
            known.Add(entry.id);

        var literal = new System.Text.RegularExpressions.Regex("\"(sfx\\.[a-z0-9_.]+)\"");
        foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains("Tests") || path.EndsWith("SfxIds.cs"))
                continue;
            foreach (System.Text.RegularExpressions.Match match in literal.Matches(File.ReadAllText(path)))
                Assert.IsTrue(known.Contains(match.Groups[1].Value), path + ": unknown sfx id " + match.Groups[1].Value);
        }
    }
}
