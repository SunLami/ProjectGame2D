using UnityEngine;

/// <summary>
/// Generates a simple solid-color, upward-pointing triangle Sprite at runtime so direction
/// indicators (QuestDirectionIndicator, MannequinAttackIndicator) have a working placeholder shape
/// with no external art dependency. This is intentionally crude -- swap the consuming Image's
/// sprite for an authored arrow asset later; nothing else about those scripts needs to change,
/// since they only rotate/position the Image, never draw it themselves.
/// </summary>
public static class ProceduralArrowSprite
{
    private static Sprite _cached;

    public static Sprite GetUpArrow()
    {
        if (_cached != null)
            return _cached;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color white = Color.white;
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            // Apex at the top (y = size-1), base at the bottom (y = 0) -- widens going down so the
            // shape reads as an arrow pointing toward +Y (this method's "up").
            float ny = y / (float)(size - 1);
            float halfWidth = (1f - ny) * 0.5f;

            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)(size - 1);
                bool inside = Mathf.Abs(nx - 0.5f) <= halfWidth;
                pixels[y * size + x] = inside ? white : clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        _cached = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return _cached;
    }
}
