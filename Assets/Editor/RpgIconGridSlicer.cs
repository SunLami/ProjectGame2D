using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Slices the RPG icon spritesheets into individual Sprites via grid,
// using Unity's own TextureImporter so the result is a normal, editable
// Multiple-sprite import (fine-tune further in Sprite Editor if a sheet
// mixes icon sizes on the same grid).
public static class RpgIconGridSlicer
{
    private class SliceTarget
    {
        public string path;
        public int cellWidth;
        public int cellHeight;
    }

    private static readonly List<SliceTarget> Targets = new List<SliceTarget>
    {
        new SliceTarget { path = "Assets/Resources/Icons/Armor and Weapons/PNG/Armor.png", cellWidth = 32, cellHeight = 32 },
        new SliceTarget { path = "Assets/Resources/Icons/Armor and Weapons/PNG/Weapons.png", cellWidth = 32, cellHeight = 32 },
        new SliceTarget { path = "Assets/Resources/Icons/Armor and Weapons/PNG/Icons.png", cellWidth = 32, cellHeight = 32 },
        new SliceTarget { path = "Assets/Resources/Icons/Game Resources/PNG/Icons.png", cellWidth = 32, cellHeight = 32 },
        new SliceTarget { path = "Assets/Resources/Icons/UI/PNG/Gui_icons2.png", cellWidth = 24, cellHeight = 24 },
        new SliceTarget { path = "Assets/Resources/Icons/Game Resources/PNG/Objects.png", cellWidth = 32, cellHeight = 32 },
    };

    [MenuItem("Tools/RPG Icons/Slice Configured Sheets")]
    public static void SliceConfiguredSheets()
    {
        int sliced = 0;
        foreach (SliceTarget target in Targets)
        {
            if (SliceTexture(target.path, target.cellWidth, target.cellHeight))
            {
                sliced++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"RpgIconGridSlicer: sliced {sliced}/{Targets.Count} texture(s). " +
                   "Open each in Sprite Editor to verify alignment before wiring into ItemSO assets.");
    }

    public static bool SliceTexture(string assetPath, int cellWidth, int cellHeight)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"RpgIconGridSlicer: texture not found at {assetPath}");
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.isReadable = true;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            Debug.LogWarning($"RpgIconGridSlicer: could not load texture at {assetPath}");
            return false;
        }

        int cols = texture.width / cellWidth;
        int rows = texture.height / cellHeight;
        string baseName = Path.GetFileNameWithoutExtension(assetPath);

        var metas = new List<SpriteMetaData>(cols * rows);
        int index = 0;
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = $"{baseName}_{index}",
                    rect = new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                });
                index++;
            }
        }

#pragma warning disable CS0618 // TextureImporter.spritesheet is the simplest supported way to grid-slice via script
        importer.spritesheet = metas.ToArray();
#pragma warning restore CS0618
        importer.SaveAndReimport();

        Debug.Log($"RpgIconGridSlicer: sliced {assetPath} into {metas.Count} sprites ({cols}x{rows} grid, {cellWidth}x{cellHeight}px cells).");
        return true;
    }
}
