using UnityEditor;

public sealed class QuestTrackerAssetImporter : AssetPostprocessor
{
    private const string Folder = "Assets/Resources/UI/Quest/Tracker1920/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder, System.StringComparison.Ordinal))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        bool divider = assetPath.EndsWith("quest_tracker_divider_pixel_v2.png", System.StringComparison.Ordinal);
        importer.spriteImportMode = divider ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = divider ? UnityEngine.FilterMode.Point : UnityEngine.FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = divider ? 4096 : 512;
    }
}
