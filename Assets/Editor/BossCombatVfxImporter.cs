using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Imports the boss combat feedback art (Assets/Resources/VFX/Skills/Combat, generated with Pixellab Pixen + animate_image, D-081):
/// hit sparks and cast auras are 64 px frame strips (sliced into &lt;Sheet&gt;_&lt;n&gt;, PPU 32), the telegraph rings are single 128 px sprites (PPU 16).
/// Idempotent. Menu: Tools > Project Game > VFX > Import Boss Combat Sheets.</summary>
public static class BossCombatVfxImporter
{
    private const string Folder = "Assets/Resources/VFX/Skills/Combat/";
    private static readonly string[] Strips =
    {
        "HitSpark_Earth", "HitSpark_Water", "HitSpark_Wind", "CastAura_Earth", "CastAura_Water", "CastAura_Wind",
    };

    private static readonly string[] Rings = { "TelegraphRing_Earth", "TelegraphRing_Water", "TelegraphRing_Wind" };

    [MenuItem("Tools/Project Game/VFX/Import Boss Combat Sheets")]
    public static void ImportAll()
    {
        foreach (string strip in Strips)
            ImportStrip(Folder + strip + ".png", strip, 64, 32f);
        foreach (string ring in Rings)
            ImportSingle(Folder + ring + ".png", 16f);
        AssetDatabase.SaveAssets();
        Debug.Log("BossCombatVfxImporter: " + Strips.Length + " strips + " + Rings.Length + " rings imported.");
    }

    private static TextureImporter Configure(string path, float ppu, SpriteImportMode mode)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
        {
            Debug.LogWarning("BossCombatVfxImporter: missing " + path);
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return importer;
    }

    private static void ImportSingle(string path, float ppu) => Configure(path, ppu, SpriteImportMode.Single);

    private static void ImportStrip(string path, string name, int frameSize, float ppu)
    {
        TextureImporter importer = Configure(path, ppu, SpriteImportMode.Multiple);
        if (importer == null)
            return;

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
