using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Applies Assets/Editor/Sfx/sfx_catalog.json (written by Tools/sfx/build_sfx.py): AudioClip import settings and
/// adds rows missing from the SfxBank assets (existing rows, i.e. your edits, are never touched). Idempotent.</summary>
public static class SfxCatalogApplier
{
    private const string CatalogPath = "Assets/Editor/Sfx/sfx_catalog.json";

    [Serializable] private class CatalogFile { public List<Entry> entries = new List<Entry>(); }

    [Serializable]
    private class Entry
    {
        public string id;
        public string[] files;
        public bool loop;
    }

    [MenuItem("Tools/SFX/Reset ALL bank rows from catalog (overwrites edits)")]
    public static void ResetAll()
    {
        if (EditorUtility.DisplayDialog("Reset SFX", "Overwrite every bank row (clips, volume...) with the Python catalog?", "Overwrite", "Cancel"))
            Debug.Log("SfxCatalogApplier: " + SfxDatabaseTools.SyncFromCatalog(true) + " rows reset.");
    }

    [MenuItem("Tools/SFX/Apply Catalog (import settings + add missing bank rows)")]
    public static void ApplyAll()
    {
        if (!File.Exists(CatalogPath))
        {
            Debug.LogError("SfxCatalogApplier: missing " + CatalogPath + " (run Tools/sfx/build_sfx.py).");
            return;
        }

        CatalogFile catalog = JsonUtility.FromJson<CatalogFile>(File.ReadAllText(CatalogPath));
        foreach (Entry entry in catalog.entries)
        {
            foreach (string file in entry.files)
                SfxDatabaseTools.ApplyImportSettings(file, entry.loop);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int changed = SfxDatabaseTools.SyncFromCatalog();
        Debug.Log("SfxCatalogApplier: " + catalog.entries.Count + " catalog ids, " + changed + " new bank rows added (existing rows untouched).");
    }
}
