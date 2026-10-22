using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Reproducible metadata-only import: generated PNG pixels are never rewritten.
public static class SkillIconForegroundImporter
{
    [Serializable] private sealed class Measurements { public MeasuredIcon[] icons; }
    [Serializable] private sealed class MeasuredIcon
    {
        public string name;
        public PixelRect rect;
        public Vector2[] hull;
    }
    [Serializable] private sealed class PixelRect
    {
        public float x, y, width, height;
    }

    [MenuItem("Tools/ProjectGame2D/UI/Import Shared Skill Foregrounds")]
    public static void Import()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Import skill presentation in Edit Mode only.");
        const string folder = "Assets/Resources/UI/Skills/Icons/V3Foreground/";
        const string dataPath = "Assets/Documentation/DevelopmentPlan/SkillIconForegroundMeasurements.json";
        var data = JsonUtility.FromJson<Measurements>(System.IO.File.ReadAllText(dataPath));
        if (data?.icons == null || data.icons.Length != 13)
            throw new InvalidOperationException("Expected all twelve skills plus Shield.");
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var icon in data.icons)
            {
                string path = folder + icon.name + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
#pragma warning disable CS0618
                importer.spritesheet = new[] { new SpriteMetaData
                {
                    name = icon.name + "_motif", rect = new Rect(icon.rect.x, icon.rect.y, icon.rect.width, icon.rect.height),
                    alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f)
                } };
#pragma warning restore CS0618
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 512;
                importer.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }

        var catalog = AssetDatabase.LoadAssetAtPath<SkillIconVisualCatalog>(SkillIconPresentationBuilder.VisualsPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<SkillIconVisualCatalog>();
            AssetDatabase.CreateAsset(catalog, SkillIconPresentationBuilder.VisualsPath);
        }
        var serialized = new SerializedObject(catalog);
        var entries = serialized.FindProperty("_entries");
        entries.arraySize = data.icons.Length;
        for (int i = 0; i < data.icons.Length; i++)
        {
            var measured = data.icons[i];
            var entry = entries.GetArrayElementAtIndex(i);
            var source = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Skills/Icons/V2/" + measured.name + ".png");
            var foreground = AssetDatabase.LoadAllAssetsAtPath(folder + measured.name + ".png").OfType<Sprite>().Single();
            if (source == null || foreground == null || measured.hull.Length < 3)
                throw new InvalidOperationException("Invalid icon: " + measured.name);
            entry.FindPropertyRelative("source").objectReferenceValue = source;
            entry.FindPropertyRelative("foreground").objectReferenceValue = foreground;
            var hull = entry.FindPropertyRelative("hull");
            hull.arraySize = measured.hull.Length;
            for (int h = 0; h < measured.hull.Length; h++)
                hull.GetArrayElementAtIndex(h).vector2Value = measured.hull[h];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        catalog.Validate();
        AssetDatabase.SaveAssets();
        Debug.Log("Imported 13 transparent motifs and alpha hulls for shared skill presentation.");
    }
}
