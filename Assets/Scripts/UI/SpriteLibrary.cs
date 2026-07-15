using System.Collections.Generic;
using UnityEngine;

namespace Tier9.UI
{
    /// <summary>
    /// Sprites are looked up by content id from Resources/Sprites (drop your art there,
    /// named after the id, e.g. "puffshroom.png"). Missing art gets a generated
    /// colored placeholder so the game is fully playable without assets.
    /// </summary>
    public static class SpriteLibrary
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) id = "unknown";
            if (Cache.TryGetValue(id, out var sprite) && sprite != null) return sprite;
            sprite = Resources.Load<Sprite>("Sprites/" + id);
            if (sprite == null) sprite = GeneratePlaceholder(id);
            Cache[id] = sprite;
            return sprite;
        }

        static Sprite GeneratePlaceholder(string id)
        {
            int hash = 23;
            foreach (char c in id) hash = hash * 31 + c;
            float hue = Mathf.Abs(hash % 360) / 360f;
            Color main = Color.HSVToRGB(hue, 0.50f, 0.85f);
            Color dark = Color.HSVToRGB(hue, 0.62f, 0.50f);

            const int size = 48;
            const int border = 3;
            const int corner = 6;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = Mathf.Min(x, size - 1 - x);
                    int cy = Mathf.Min(y, size - 1 - y);
                    if (cx + cy < corner)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (cx + cy < corner + border || cx < border || cy < border)
                    {
                        tex.SetPixel(x, y, dark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, main);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
