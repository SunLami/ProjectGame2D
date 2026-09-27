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
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
    }
}
