using UnityEngine;

// Procedurally draws a filled white circle (soft 1px antialiased edge) into a Texture2D and wraps
// it as a Sprite, cached per requested pixel size. Used as a Mask shape for the round Minimap
// frame/viewport until Codex delivers real Dark Inventory Style circular frame art -- this is
// runtime pixel math, not an authored/generated bitmap asset, so it doesn't cross the "Claude
// doesn't generate art" line.
public static class RuntimeCircleSprite
{
    private static readonly System.Collections.Generic.Dictionary<int, Sprite> Cache = new();

    public static Sprite Get(int diameter)
    {
        if (Cache.TryGetValue(diameter, out Sprite cached) && cached != null)
            return cached;

        Texture2D texture = new(diameter, diameter, TextureFormat.RGBA32, false)
        {
            name = $"RuntimeCircle_{diameter}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float radius = diameter * 0.5f;
        Vector2 center = new(radius, radius);
        Color32[] pixels = new Color32[diameter * diameter];
        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(radius - distance);
                pixels[y * diameter + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, diameter, diameter), new Vector2(0.5f, 0.5f));
        sprite.name = texture.name;
        Cache[diameter] = sprite;
        return sprite;
    }
}
