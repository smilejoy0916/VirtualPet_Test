using UnityEngine;

namespace VirtualPet.UI
{
    public static class UITheme
    {
        public static readonly Color Background = new(0.98f, 0.96f, 0.92f);
        public static readonly Color Surface = Color.white;
        public static readonly Color Primary = new(1f, 0.54f, 0.30f);
        public static readonly Color Secondary = new(0.47f, 0.40f, 0.95f);
        public static readonly Color TextPrimary = new(0.16f, 0.15f, 0.20f);
        public static readonly Color TextSecondary = new(0.45f, 0.44f, 0.50f);
        public static readonly Color TextOnDark = Color.white;
        public static readonly Color Coin = new(1f, 0.78f, 0.18f);
        public static readonly Color CoinDark = new(0.85f, 0.58f, 0.05f);
        public static readonly Color Success = new(0.25f, 0.72f, 0.45f);
        public static readonly Color Danger = new(0.90f, 0.30f, 0.30f);
        public static readonly Color TabInactive = new(0.62f, 0.61f, 0.66f);
        public static readonly Color Scrim = new(0.08f, 0.07f, 0.12f, 0.96f);
        public static readonly Color Sky = new(0.70f, 0.86f, 0.98f);

        public const int TitleSize = 64;
        public const int HeadingSize = 48;
        public const int BodySize = 38;
        public const int CaptionSize = 30;

        static Font font;
        public static Font Font => font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
