using System.IO;
using UnityEditor;
using UnityEngine;

// One-time (re-runnable) bake of a static top-down PNG of the whole BorderMap area, replacing the
// live Camera+RenderTexture minimap/fullmap approach with the standard genre pattern: a static map
// image plus a moving Player icon. Re-run this whenever world geometry changes meaningfully;
// the Player is hidden during the capture since a live position baked into terrain art would be
// stale the moment the player moves.
public static class MapSnapshotBaker
{
    private const string BorderMapObjectName = "BorderMap";
    private const string OutputPath = "Assets/Resources/UI/Map/map_snapshot.png";
    private const int LongEdgePixels = 2048;

    [MenuItem("Tools/ProjectGame2D/UI/Bake Map Snapshot")]
    public static void Bake()
    {
        GameObject borderMap = GameObject.Find(BorderMapObjectName);
        Collider2D collider = borderMap != null ? borderMap.GetComponent<Collider2D>() : null;
        if (collider == null)
        {
            Debug.LogError($"MapSnapshotBaker: '{BorderMapObjectName}' with a Collider2D was not found in the active scene.");
            return;
        }

        Bounds bounds = collider.bounds;
        float aspect = bounds.size.x / Mathf.Max(bounds.size.y, 0.0001f);
        int width = aspect >= 1f ? LongEdgePixels : Mathf.RoundToInt(LongEdgePixels * aspect);
        int height = aspect >= 1f ? Mathf.RoundToInt(LongEdgePixels / aspect) : LongEdgePixels;

        GameObject player = GameObject.FindWithTag("Player");
        bool playerWasActive = player != null && player.activeSelf;
        if (player != null) player.SetActive(false);

        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        GameObject cameraObject = new("MapSnapshotCamera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        try
        {
            camera.orthographic = true;
            camera.orthographicSize = bounds.extents.y;
            camera.aspect = aspect;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.14f, 0.09f, 1f);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            snapshot.Apply();
            RenderTexture.active = previousActive;

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, snapshot.EncodeToPNG());
            Object.DestroyImmediate(snapshot);
        }
        finally
        {
            camera.targetTexture = null;
            Object.DestroyImmediate(cameraObject);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            if (player != null) player.SetActive(playerWasActive);
        }

        AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Mathf.Max(width, height);
            importer.SaveAndReimport();
        }

        Debug.Log($"MapSnapshotBaker: baked {width}x{height} map snapshot to {OutputPath} (bounds center={bounds.center}, size={bounds.size}).");
    }
}
