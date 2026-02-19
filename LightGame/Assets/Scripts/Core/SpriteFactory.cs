using UnityEngine;

public static class SpriteFactory
{
    private static int ClampResolution(int resolution)
    {
        if (resolution < 1)
        {
            Debug.LogWarning($"SpriteFactory: resolution {resolution} clamped to 1");
            return 1;
        }
        if (resolution > 512)
        {
            Debug.LogWarning($"SpriteFactory: resolution {resolution} clamped to 512");
            return 512;
        }
        return resolution;
    }

    private static Sprite ToSprite(Texture2D texture)
    {
        return Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), texture.width);
    }

    public static Sprite CreateCircle(int resolution, Color color)
    {
        resolution = ClampResolution(resolution);
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (resolution - 1) / 2f;
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center;
                float dy = y - center;
                tex.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? color : Color.clear);
            }
        }
        tex.Apply();
        return ToSprite(tex);
    }

    public static Sprite CreateDiamond(int resolution, Color color)
    {
        resolution = ClampResolution(resolution);
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float center = (resolution - 1) / 2f;
        float half = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);
                tex.SetPixel(x, y, dx / half + dy / half <= 1f ? color : Color.clear);
            }
        }
        tex.Apply();
        return ToSprite(tex);
    }

    public static Sprite CreateTriangle(int resolution, Color color)
    {
        resolution = ClampResolution(resolution);
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Right-pointing triangle: for each row y, fill from x=0 up to a width
                // proportional to distance from center row
                float normalizedY = Mathf.Abs(y - (resolution - 1) / 2f) / (resolution / 2f);
                float maxX = resolution * (1f - normalizedY);
                tex.SetPixel(x, y, x < maxX ? color : Color.clear);
            }
        }
        tex.Apply();
        return ToSprite(tex);
    }

    public static Sprite CreateRectangle(int width, int height, Color color)
    {
        width = ClampResolution(width);
        height = ClampResolution(height);
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, color);

        tex.Apply();
        return Sprite.Create(tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f), Mathf.Max(width, height));
    }

    public static Sprite CreateSquare(int resolution, Color color)
    {
        resolution = ClampResolution(resolution);
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                tex.SetPixel(x, y, color);

        tex.Apply();
        return ToSprite(tex);
    }
}
