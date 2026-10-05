using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>Editor helpers for SfxBank / SfxSceneProfile assets: create, fill from the Python catalog (add missing rows only),
/// readable labels, audio import settings.</summary>
public static class SfxDatabaseTools
{
    public const string BanksRoot = "Assets/Resources/Audio/SfxBanks";
    public const string ScenesRoot = "Assets/Resources/Audio/SfxScenes";
    public const string SfxRoot = "Assets/Resources/Audio/SFX";
    private const string CatalogPath = "Assets/Editor/Sfx/sfx_catalog.json";

    [System.Serializable] private class CatalogFile { public List<CatalogEntry> entries = new List<CatalogEntry>(); }

    [System.Serializable]
    private class CatalogEntry
    {
        public string id;
        public string category;
        public string[] files;
        public float volume;
        public float pitchJitter;
        public float minInterval;
        public int maxVoices;
        public bool loop;
        public string desc;
    }

    // ------------------------------------------------------------------------------------------------ queries
    public static List<SfxBank> AllBanks() =>
        AssetDatabase.FindAssets("t:SfxBank", new[] { BanksRoot }).Select(g => AssetDatabase.LoadAssetAtPath<SfxBank>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(b => b != null).OrderBy(b => b.name).ToList();

    public static List<SfxSceneProfile> AllScenes() =>
        AssetDatabase.FindAssets("t:SfxSceneProfile", new[] { ScenesRoot }).Select(g => AssetDatabase.LoadAssetAtPath<SfxSceneProfile>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null).OrderBy(s => s.sceneName).ToList();

    public static IEnumerable<SfxBank.Row> AllRows() => AllBanks().SelectMany(b => b.rows);

    public static SfxBank.Row FindRow(string id) => AllRows().FirstOrDefault(r => r.id == id);

    // ------------------------------------------------------------------------------------------------ naming
    private static readonly Dictionary<string, string> Elements = new Dictionary<string, string>
    {
        { "water", "Thủy" }, { "earth", "Địa" }, { "wind", "Phong" },
    };

    /// <summary>"sfx.skill.water_s1_cast" -> "Thủy · Skill 1 · Cast"; "sfx.enemy.slime_hurt" -> "Slime hurt".</summary>
    public static string MakeLabel(string id)
    {
        string last = id.Substring(id.LastIndexOf('.') + 1);
        string[] words = last.Split('_');
        var head = new List<string>();
        var tail = new List<string>();
        foreach (string w in words)
        {
            if (head.Count == 0 && tail.Count == 0 && Elements.TryGetValue(w, out string element))
                head.Add(element);
            else if (Regex.IsMatch(w, "^s\\d$"))
                head.Add("Skill " + w.Substring(1));
            else
                tail.Add(w);
        }

        string rest = string.Join(" ", tail);
        if (rest.Length > 0)
            rest = char.ToUpperInvariant(rest[0]) + rest.Substring(1);
        head.Add(rest);
        return string.Join(" · ", head.Where(h => h.Length > 0));
    }

    /// <summary>Which bank (asset) an id belongs to.</summary>
    public static string BankNameOf(string id)
    {
        string[] p = id.Split('.');
        string area = p.Length > 1 ? p[1] : "misc";
        string last = p.Length > 2 ? p[2] : "";
        switch (area)
        {
            case "skill":
                if (last.StartsWith("water")) return "Skill Thuy (Water)";
                if (last.StartsWith("earth")) return "Skill Dia (Earth)";
                if (last.StartsWith("wind")) return "Skill Phong (Wind)";
                return "Skill Common";
            case "combat": case "player": return "Combat Player";
            case "enemy": return "Enemy";
            case "boss": return "Boss Earth";
            case "bossw": return "Boss Water (Crab)";
            case "bossowl": return "Boss Wind (Owl)";
            case "ui": case "inventory": return "UI and Inventory";
            case "quest": case "shop": case "craft": return "Quest and Shop";
            case "world": case "step": return "World";
            case "fishing": case "farm": return "Fishing and Farming";
            case "amb": return "Ambience";
            default: return "Other";
        }
    }

    // ------------------------------------------------------------------------------------------------ create
    public static SfxBank GetOrCreateBank(string name)
    {
        Directory.CreateDirectory(BanksRoot);
        string path = BanksRoot + "/" + name + ".asset";
        var bank = AssetDatabase.LoadAssetAtPath<SfxBank>(path);
        if (bank != null)
            return bank;

        bank = ScriptableObject.CreateInstance<SfxBank>();
        bank.title = name;
        AssetDatabase.CreateAsset(bank, path);
        return bank;
    }

    public static SfxSceneProfile CreateSceneProfile(string sceneName)
    {
        Directory.CreateDirectory(ScenesRoot);
        var profile = ScriptableObject.CreateInstance<SfxSceneProfile>();
        profile.sceneName = sceneName;
        foreach (string category in AllRows().Select(r => r.category).Distinct().OrderBy(c => c))
            profile.categories.Add(new SfxSceneProfile.CategoryLevel { category = category });
        AssetDatabase.CreateAsset(profile, ScenesRoot + "/" + sceneName + ".asset");
        return profile;
    }

    // ------------------------------------------------------------------------------------------------ fill from catalog
    /// <summary>Adds rows that exist in sfx_catalog.json (written by Tools/sfx/build_sfx.py) but not in any bank yet. Existing
    /// rows are never touched, so hand edits survive; use <paramref name="overwrite"/> to reset every row from the catalog.</summary>
    public static int SyncFromCatalog(bool overwrite = false)
    {
        if (!File.Exists(CatalogPath))
        {
            Debug.LogWarning("SfxDatabaseTools: " + CatalogPath + " not found (run Tools/sfx/build_sfx.py).");
            return 0;
        }

        CatalogFile catalog = JsonUtility.FromJson<CatalogFile>(File.ReadAllText(CatalogPath));
        Dictionary<string, (SfxBank bank, SfxBank.Row row)> existing = new Dictionary<string, (SfxBank, SfxBank.Row)>();
        foreach (SfxBank b in AllBanks())
        {
            foreach (SfxBank.Row r in b.rows)
                existing[r.id] = (b, r);
        }

        int changed = 0;
        foreach (CatalogEntry source in catalog.entries)
        {
            SfxBank.Row row;
            if (existing.TryGetValue(source.id, out var found))
            {
                if (!overwrite)
                    continue;
                row = found.row;
                EditorUtility.SetDirty(found.bank);
            }
            else
            {
                SfxBank bank = GetOrCreateBank(BankNameOf(source.id));
                row = new SfxBank.Row { id = source.id };
                bank.rows.Add(row);
                EditorUtility.SetDirty(bank);
            }

            row.label = MakeLabel(source.id);
            row.category = source.category;
            row.description = source.desc;
            row.clips = source.files.Select(f => AssetDatabase.LoadAssetAtPath<AudioClip>(f)).Where(c => c != null).ToList();
            row.volume = source.volume <= 0f ? 1f : source.volume;
            row.pitchJitter = source.pitchJitter;
            row.minInterval = source.minInterval;
            row.maxVoices = source.maxVoices;
            row.loop = source.loop;
            changed++;
        }

        foreach (SfxBank b in AllBanks())
        {
            b.rows = b.rows.OrderBy(r => r.id, System.StringComparer.Ordinal).ToList();
            EditorUtility.SetDirty(b);
        }

        AssetDatabase.SaveAssets();
        return changed;
    }

    // ------------------------------------------------------------------------------------------------ audio import
    /// <summary>Mono + Decompress On Load for one-shots, compressed Vorbis for loops.</summary>
    public static void ApplyImportSettings(string assetPath, bool loop)
    {
        if (AssetImporter.GetAtPath(assetPath) is not AudioImporter importer)
            return;

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        if (loop)
        {
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
        }
        else
        {
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
        }

        bool changed = !importer.forceToMono || importer.loadInBackground
            || importer.defaultSampleSettings.loadType != settings.loadType
            || importer.defaultSampleSettings.compressionFormat != settings.compressionFormat;
        importer.forceToMono = true;
        importer.loadInBackground = false;
        importer.defaultSampleSettings = settings;
        if (changed)
            importer.SaveAndReimport();
    }

    /// <summary>Copies an audio file from anywhere on disk into the project (Resources/Audio/SFX/&lt;category&gt;) and returns the clip.</summary>
    public static AudioClip ImportExternalFile(string sourcePath, string category, string id, bool loop)
    {
        string folder = SfxRoot + "/" + (string.IsNullOrEmpty(category) ? "Custom" : category);
        Directory.CreateDirectory(folder);
        string stem = id.Replace('.', '_') + "_custom";
        string ext = Path.GetExtension(sourcePath);
        int n = 1;
        string dest;
        do
        {
            dest = folder + "/" + stem + "_" + n++ + ext;
        }
        while (File.Exists(dest));

        File.Copy(sourcePath, dest);
        AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceSynchronousImport);
        ApplyImportSettings(dest, loop);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(dest);
    }
}
