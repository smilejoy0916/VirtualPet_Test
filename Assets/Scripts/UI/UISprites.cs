using UnityEngine;

namespace VirtualPet.UI
{
    /// <summary>
    /// Procedurally generated UI sprites, so the host app ships no texture assets for
    /// its chrome. Each sprite is created once and shared.
    /// </summary>
    public static class UISprites
    {
        static Sprite roundedRect;
        static Sprite circle;
        static Sprite spinnerRing;

        /// <summary>9-sliced rounded rectangle (32px corners).</summary>
        public static Sprite RoundedRect => roundedRect ??= CreateRoundedRect(96, 32);

        public static Sprite Circle => circle ??= CreateRoundedRect(128, 64);

        /// <summary>A ring whose alpha fades along its length, used for spinners.</summary>
        public static Sprite SpinnerRing => spinnerRing ??= CreateRing(128, 10);

        public static Sprite CreateRoundedRect(int size, int radius)
        {
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                var cx = Mathf.Clamp(px, radius, size - radius);
                var cy = Mathf.Clamp(py, radius, size - radius);
                var distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }

            var border = radius < size / 2 ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return ToSprite(pixels, size, "RoundedRect", border);
        }

        static Sprite CreateRing(int size, int thickness)
        {
            var pixels = new Color32[size * size];
            var center = size / 2f;
            var radius = center - thickness / 2f - 1f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var offset = new Vector2(x + 0.5f - center, y + 0.5f - center);
                var edge = Mathf.Clamp01(thickness / 2f - Mathf.Abs(offset.magnitude - radius) + 0.5f);
                var angle01 = (Mathf.Atan2(offset.y, offset.x) / (2f * Mathf.PI) + 1f) % 1f;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(edge * angle01 * 255));
            }

            return ToSprite(pixels, size, "SpinnerRing", Vector4.zero);
        }

        static Sprite ToSprite(Color32[] pixels, int size, string name, Vector4 border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }
    }
}
