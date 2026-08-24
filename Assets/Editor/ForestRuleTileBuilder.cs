using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ForestRuleTileBuilder
{
    private const string SourceFolder = "Assets/Tiles/Tilesets/Forest Top-Down Tileset Pixel Art";
    private const string TmxPath = SourceFolder + "/Forest.tmx";
    private const string OutputFolder = "Assets/Tiles/RuleTiles/Forest";
    private const uint FlipMask = 0xE0000000u;
    private const uint GidMask = 0x1FFFFFFFu;

    private static readonly Vector3Int[] Neighbors =
    {
        new(-1, 1), new(0, 1), new(1, 1), new(-1, 0),
        new(1, 0), new(-1, -1), new(0, -1), new(1, -1)
    };

    private sealed class TilesetInfo
    {
        public uint FirstGid;
        public uint LastGid;
        public Dictionary<int, Sprite> Sprites;
    }

    private sealed class LayerInfo
    {
        public string Name;
        public int Width;
        public int Height;
        public uint[] Gids;
    }

    [MenuItem("Tools/Project Game/Tiles/Rebuild Forest RuleTiles")]
    public static void Rebuild()
    {
        EnsureFolder(OutputFolder);
        var document = XDocument.Load(ToAbsolutePath(TmxPath));
        var map = document.Root ?? throw new InvalidDataException("Forest.tmx has no map root.");
        var tilesets = ReadTilesets(map);
        var layers = ReadLayers(map).ToDictionary(layer => layer.Name, StringComparer.OrdinalIgnoreCase);

        var report = new List<string>();
        BuildLearned("Forest_Water", GetLayers(layers, "water"), tilesets, Tile.ColliderType.None, report);
        BuildLearned("Forest_Ground", GetLayers(layers, "ground"), tilesets, Tile.ColliderType.None, report);
        BuildLearned("Forest_MainSpace", GetLayers(layers, "main_space", "main_space2"), tilesets, Tile.ColliderType.None, report);
        BuildLearned("Forest_ElevatedSpace", GetLayers(layers, "elevated_space"), tilesets, Tile.ColliderType.None, report);
        BuildLearned("Forest_Lianas", GetLayers(layers, "lianas", "lianas2", "lianas3", "lianas4", "lianas5"), tilesets, Tile.ColliderType.None, report);

        BuildRandom("Forest_GroundSpots", GetLayers(layers, "spots1", "spots2"), tilesets, report);
        BuildRandom("Forest_RockSpots", GetLayers(layers, "spots3"), tilesets, report);
        BuildRandom("Forest_WaterLilies", GetLayers(layers, "water_lilies"), tilesets, report);
        BuildRandom("Forest_GrassElements", GetLayers(layers, "grass_elements", "grass_elements2", "grass_elements3"), tilesets, report);
        BuildRandom("Forest_Reeds", GetLayers(layers, "reeds"), tilesets, report);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Forest RuleTiles rebuilt from Forest.tmx:\n" + string.Join("\n", report));
    }

    [MenuItem("Tools/Project Game/Tiles/Validate Forest RuleTiles")]
    public static void Validate()
    {
        var expected = new[]
        {
            "Forest_Water", "Forest_Ground", "Forest_MainSpace", "Forest_ElevatedSpace", "Forest_Lianas",
            "Forest_GroundSpots", "Forest_RockSpots", "Forest_WaterLilies", "Forest_GrassElements", "Forest_Reeds"
        };
        var failures = expected.Select(name => AssetDatabase.LoadAssetAtPath<RuleTile>($"{OutputFolder}/{name}.asset"))
            .Where(tile => tile == null || tile.m_DefaultSprite == null || tile.m_TilingRules.Count == 0)
            .Select(tile => tile == null ? "missing asset" : tile.name).ToArray();
        if (failures.Length > 0) throw new InvalidDataException("Forest RuleTile validation failed: " + string.Join(", ", failures));
        Debug.Log($"Forest RuleTile validation passed: {expected.Length} assets are usable.");
    }

    private static void BuildLearned(string assetName, IReadOnlyList<LayerInfo> layers,
        IReadOnlyList<TilesetInfo> tilesets, Tile.ColliderType colliderType, ICollection<string> report)
    {
        var samples = new Dictionary<byte, List<Sprite>>();
        var frequency = new Dictionary<Sprite, int>();
        var flipped = 0;

        foreach (var layer in layers)
        for (var y = 0; y < layer.Height; y++)
        for (var x = 0; x < layer.Width; x++)
        {
            var raw = layer.Gids[y * layer.Width + x];
            if ((raw & GidMask) == 0) continue;
            if ((raw & FlipMask) != 0)
            {
                flipped++;
                continue;
            }

            var sprite = ResolveSprite(raw, tilesets);
            if (sprite == null) continue;
            var mask = GetOccupancyMask(layer, x, y);
            if (!samples.TryGetValue(mask, out var sprites)) samples[mask] = sprites = new List<Sprite>();
            sprites.Add(sprite);
            frequency[sprite] = frequency.TryGetValue(sprite, out var count) ? count + 1 : 1;
        }

        if (frequency.Count == 0)
        {
            report.Add($"{assetName}: skipped (no unflipped samples)");
            return;
        }

        var tile = LoadOrCreate<RuleTile>($"{OutputFolder}/{assetName}.asset");
        tile.m_DefaultSprite = frequency.OrderByDescending(pair => pair.Value).First().Key;
        tile.m_DefaultColliderType = colliderType;
        tile.m_TilingRules.Clear();

        foreach (var sample in samples.OrderBy(pair => pair.Key))
        {
            var sprites = sample.Value.Distinct().ToArray();
            var rule = new RuleTile.TilingRule
            {
                m_Sprites = sprites,
                m_Output = sprites.Length > 1
                    ? RuleTile.TilingRuleOutput.OutputSprite.Random
                    : RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_ColliderType = colliderType
            };
            rule.m_NeighborPositions = Neighbors.ToList();
            rule.m_Neighbors = Enumerable.Range(0, 8)
                .Select(index => (sample.Key & (1 << index)) != 0
                    ? RuleTile.TilingRuleOutput.Neighbor.This
                    : RuleTile.TilingRuleOutput.Neighbor.NotThis)
                .ToList();
            tile.m_TilingRules.Add(rule);
        }

        EditorUtility.SetDirty(tile);
        report.Add($"{assetName}: {tile.m_TilingRules.Count} learned rules, {frequency.Count} sprites, {flipped} flipped samples ignored");
    }

    private static void BuildRandom(string assetName, IReadOnlyList<LayerInfo> layers,
        IReadOnlyList<TilesetInfo> tilesets, ICollection<string> report)
    {
        var rawGids = layers.SelectMany(layer => layer.Gids).Where(raw => (raw & GidMask) != 0).ToArray();
        var flipped = rawGids.Count(raw => (raw & FlipMask) != 0);
        var sprites = rawGids
            .Select(raw => ResolveSprite(raw, tilesets)).Where(sprite => sprite != null).Distinct().ToArray();
        if (sprites.Length == 0)
        {
            report.Add($"{assetName}: skipped (no unflipped sprites)");
            return;
        }

        var tile = LoadOrCreate<RuleTile>($"{OutputFolder}/{assetName}.asset");
        tile.m_DefaultSprite = sprites[0];
        tile.m_DefaultColliderType = Tile.ColliderType.None;
        tile.m_TilingRules.Clear();
        tile.m_TilingRules.Add(new RuleTile.TilingRule
        {
            m_NeighborPositions = new List<Vector3Int>(),
            m_Neighbors = new List<int>(),
            m_Sprites = sprites,
            m_Output = sprites.Length > 1
                ? RuleTile.TilingRuleOutput.OutputSprite.Random
                : RuleTile.TilingRuleOutput.OutputSprite.Single,
            m_ColliderType = Tile.ColliderType.None
        });
        EditorUtility.SetDirty(tile);
        report.Add($"{assetName}: random tile with {sprites.Length} variants ({flipped} flipped references normalized to source sprites)");
    }

    private static byte GetOccupancyMask(LayerInfo layer, int x, int y)
    {
        byte mask = 0;
        for (var i = 0; i < Neighbors.Length; i++)
        {
            var nx = x + Neighbors[i].x;
            var ny = y - Neighbors[i].y;
            if (nx >= 0 && nx < layer.Width && ny >= 0 && ny < layer.Height &&
                (layer.Gids[ny * layer.Width + nx] & GidMask) != 0)
                mask |= (byte)(1 << i);
        }
        return mask;
    }

    private static Sprite ResolveSprite(uint rawGid, IReadOnlyList<TilesetInfo> tilesets)
    {
        var gid = rawGid & GidMask;
        var tileset = tilesets.LastOrDefault(item => gid >= item.FirstGid && gid <= item.LastGid);
        if (tileset == null) return null;
        tileset.Sprites.TryGetValue((int)(gid - tileset.FirstGid), out var sprite);
        return sprite;
    }

    private static List<TilesetInfo> ReadTilesets(XElement map)
    {
        var result = new List<TilesetInfo>();
        foreach (var reference in map.Elements("tileset"))
        {
            var firstGid = (uint)reference.Attribute("firstgid");
            var definition = reference;
            var definitionFolder = SourceFolder;
            var source = (string)reference.Attribute("source");
            if (!string.IsNullOrEmpty(source))
            {
                var tsxAssetPath = NormalizeAssetPath(Path.Combine(SourceFolder, source));
                definition = XDocument.Load(ToAbsolutePath(tsxAssetPath)).Root;
                definitionFolder = NormalizeAssetPath(Path.GetDirectoryName(tsxAssetPath));
            }

            var imageSource = (string)definition?.Element("image")?.Attribute("source");
            if (string.IsNullOrEmpty(imageSource)) continue;
            var imagePath = NormalizeAssetPath(Path.Combine(definitionFolder, imageSource));
            var tileWidth = (int?)definition?.Attribute("tilewidth") ?? 16;
            var tileHeight = (int?)definition?.Attribute("tileheight") ?? 16;
            var spacing = (int?)definition?.Attribute("spacing") ?? 0;
            var margin = (int?)definition?.Attribute("margin") ?? 0;
            var declaredColumns = (int?)definition?.Attribute("columns") ?? 0;
            var sprites = AssetDatabase.LoadAllAssetsAtPath(imagePath).OfType<Sprite>()
                .Select(sprite => new
                {
                    Sprite = sprite,
                    Id = GetTiledLocalId(sprite, tileWidth, tileHeight, spacing, margin, declaredColumns)
                })
                .Where(item => item.Id >= 0).GroupBy(item => item.Id)
                .ToDictionary(group => group.Key, group => group.First().Sprite);
            if (sprites.Count == 0) continue;
            var tileCount = (uint?)definition?.Attribute("tilecount") ?? (uint)(sprites.Keys.Max() + 1);
            result.Add(new TilesetInfo
            {
                FirstGid = firstGid,
                LastGid = firstGid + tileCount - 1,
                Sprites = sprites
            });
        }
        return result.OrderBy(item => item.FirstGid).ToList();
    }

    private static IEnumerable<LayerInfo> ReadLayers(XElement map)
    {
        foreach (var layer in map.Elements("layer"))
        {
            var data = layer.Element("data");
            if ((string)data?.Attribute("encoding") != "csv") continue;
            var chunks = data.Elements("chunk").ToList();
            if (chunks.Count > 0)
            {
                var minX = chunks.Min(chunk => (int)chunk.Attribute("x"));
                var minY = chunks.Min(chunk => (int)chunk.Attribute("y"));
                var maxX = chunks.Max(chunk => (int)chunk.Attribute("x") + (int)chunk.Attribute("width"));
                var maxY = chunks.Max(chunk => (int)chunk.Attribute("y") + (int)chunk.Attribute("height"));
                var width = maxX - minX;
                var height = maxY - minY;
                var gids = new uint[width * height];
                foreach (var chunk in chunks)
                {
                    var chunkX = (int)chunk.Attribute("x") - minX;
                    var chunkY = (int)chunk.Attribute("y") - minY;
                    var chunkWidth = (int)chunk.Attribute("width");
                    var chunkHeight = (int)chunk.Attribute("height");
                    var values = Regex.Matches(chunk.Value, @"\d+")
                        .Select(match => uint.Parse(match.Value)).ToArray();
                    if (values.Length != chunkWidth * chunkHeight)
                        throw new InvalidDataException($"Layer '{(string)layer.Attribute("name")}' has an invalid TMX chunk.");
                    for (var y = 0; y < chunkHeight; y++)
                    for (var x = 0; x < chunkWidth; x++)
                        gids[(chunkY + y) * width + chunkX + x] = values[y * chunkWidth + x];
                }
                yield return new LayerInfo
                {
                    Name = (string)layer.Attribute("name"), Width = width, Height = height, Gids = gids
                };
                continue;
            }
            yield return new LayerInfo
            {
                Name = (string)layer.Attribute("name"),
                Width = (int)layer.Attribute("width"),
                Height = (int)layer.Attribute("height"),
                Gids = Regex.Matches(data.Value, @"\d+").Select(match => uint.Parse(match.Value)).ToArray()
            };
        }
    }

    private static IReadOnlyList<LayerInfo> GetLayers(IReadOnlyDictionary<string, LayerInfo> layers, params string[] names) =>
        names.Where(layers.ContainsKey).Select(name => layers[name]).ToList();

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static int ParseTrailingNumber(string value)
    {
        var match = Regex.Match(value, @"(\d+)$");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }

    private static int GetTiledLocalId(Sprite sprite, int tileWidth, int tileHeight, int spacing, int margin,
        int declaredColumns)
    {
        if (sprite.texture == null) return ParseTrailingNumber(sprite.name);
        var columns = declaredColumns > 0
            ? declaredColumns
            : (sprite.texture.width - margin * 2 + spacing) / (tileWidth + spacing);
        var column = Mathf.RoundToInt((sprite.rect.xMin - margin) / (tileWidth + spacing));
        var rowFromTop = Mathf.RoundToInt(
            (sprite.texture.height - margin - sprite.rect.yMax) / (tileHeight + spacing));
        return rowFromTop * columns + column;
    }

    private static void EnsureFolder(string assetPath)
    {
        var current = "Assets";
        foreach (var part in assetPath.Split('/').Skip(1))
        {
            var next = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }

    private static string NormalizeAssetPath(string path) => path.Replace('\\', '/');
    private static string ToAbsolutePath(string assetPath) => Path.GetFullPath(assetPath);
}
