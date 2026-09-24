using UnityEngine;

namespace Game2048
{
    /// <summary>Classic 2048 palette.</summary>
    public static class TileColors
    {
        public static readonly Color Background = Hex(0xFAF8EF);
        public static readonly Color Board = Hex(0xBBADA0);
        public static readonly Color EmptyCell = Hex(0xCDC1B4);
        public static readonly Color DarkText = Hex(0x776E65);
        public static readonly Color LightText = Hex(0xF9F6F2);
        public static readonly Color ScoreBox = Hex(0xBBADA0);
        public static readonly Color ScoreLabel = Hex(0xEEE4DA);
        public static readonly Color Button = Hex(0x8F7A66);
        public static readonly Color OverlayLost = new Color(0.93f, 0.89f, 0.85f, 0.73f);
        public static readonly Color OverlayWon = new Color(0.93f, 0.76f, 0.18f, 0.5f);

        private static readonly Color[] TileBackgrounds =
        {
            Hex(0xEEE4DA), // 2
            Hex(0xEDE0C8), // 4
            Hex(0xF2B179), // 8
            Hex(0xF59563), // 16
            Hex(0xF67C5F), // 32
            Hex(0xF65E3B), // 64
            Hex(0xEDCF72), // 128
            Hex(0xEDCC61), // 256
            Hex(0xEDC850), // 512
            Hex(0xEDC53F), // 1024
            Hex(0xEDC22E), // 2048
        };

        private static readonly Color SuperTile = Hex(0x3C3A32);

        public static Color TileBackground(int value)
        {
            int index = Log2(value) - 1;
            if (index < 0) return EmptyCell;
            return index < TileBackgrounds.Length ? TileBackgrounds[index] : SuperTile;
        }

        public static Color TileText(int value) => value <= 4 ? DarkText : LightText;

        private static int Log2(int value)
        {
            int log = 0;
            while (value > 1)
            {
                value >>= 1;
                log++;
            }
            return log;
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
