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

        /// <summary>A flat single-color 1x1-unit sprite (HP bars, tinted quads).</summary>
        public static Sprite Solid(Color color)
        {
            string key = "solid_" + ColorUtility.ToHtmlStringRGBA(color);
            if (Cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Selectable player appearances: any user sprite named player_* plus generated fallbacks.</summary>
        public static System.Collections.Generic.List<string> PlayerSpriteIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            foreach (var s in Resources.LoadAll<Sprite>("Sprites"))
                if (s.name.StartsWith("player_") && !ids.Contains(s.name)) ids.Add(s.name);
            foreach (var fallback in new[] { "player_a", "player_b", "player_c", "player_d" })
                if (!ids.Contains(fallback)) ids.Add(fallback);
            return ids;
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
