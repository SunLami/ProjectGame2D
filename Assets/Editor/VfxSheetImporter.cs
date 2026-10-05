using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Slices the horizontal 176 px VFX strips that Tools/fetch_vfx_sheet.py writes (Resources/VFX/Skills/Water/*.png)
/// into sprites named &lt;Sheet&gt;_&lt;n&gt; (pixel-art import: point filter, no compression, 48 PPU, centre pivot).
/// Idempotent: re-running only refreshes the settings.</summary>
public static class VfxSheetImporter
{
    private const string Folder = "Assets/Resources/VFX/Skills/Water/";
    private static readonly string[] Sheets = { "SandMound_Move", "SandBurst_Hit", "ClawSlash_Hit", "TidalWall_Flow", "TideEdge_Flow", "TentacleBody_Flow", "TentacleTip_Flow" };

    [MenuItem("Tools/Project Game/VFX/Import Water Boss Sheets")]
    public static void ImportAll()
    {
        foreach (string sheet in Sheets)
            Import(Folder + sheet + ".png", sheet, 176);
        AssetDatabase.SaveAssets();
        Debug.Log("VfxSheetImporter: " + Sheets.Length + " sheets sliced.");
    }

    public static void Import(string path, string name, int frameSize)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
        {
            Debug.LogWarning("VfxSheetImporter: missing " + path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 48f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int count = texture.width / frameSize;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        for (int i = 0; i < count; i++)
        {
            rects.Add(new SpriteRect
            {
                name = name + "_" + i,
                rect = new Rect(i * frameSize, 0, frameSize, frameSize),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate(),
            });
        }

        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();
    }
}
