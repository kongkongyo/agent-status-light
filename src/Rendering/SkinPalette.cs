using System.Drawing;

namespace WorkStatusLight
{

    internal sealed class SkinPalette
    {
        public SkinPalette(bool drawBody, Color bodyStart, Color bodyEnd, Color border, Color rim, Color inactiveLens, Color lensShadow, Color shine, int inactiveShineAlpha)
        {
            DrawBody = drawBody;
            BodyStart = bodyStart;
            BodyEnd = bodyEnd;
            Border = border;
            Rim = rim;
            InactiveLens = inactiveLens;
            LensShadow = lensShadow;
            Shine = shine;
            InactiveShineAlpha = inactiveShineAlpha;
        }

        public bool DrawBody { get; private set; }
        public Color BodyStart { get; private set; }
        public Color BodyEnd { get; private set; }
        public Color Border { get; private set; }
        public Color Rim { get; private set; }
        public Color InactiveLens { get; private set; }
        public Color LensShadow { get; private set; }
        public Color Shine { get; private set; }
        public int InactiveShineAlpha { get; private set; }

        public static SkinPalette Dark()
        {
            return new SkinPalette(
                true,
                Color.FromArgb(48, 52, 55),
                Color.FromArgb(12, 14, 15),
                Color.FromArgb(6, 8, 9),
                Color.FromArgb(6, 8, 9),
                Color.FromArgb(38, 43, 45),
                Color.FromArgb(13, 15, 16),
                Color.White,
                30);
        }

        public static SkinPalette Light()
        {
            return new SkinPalette(
                true,
                SystemColors.ControlLightLight,
                SystemColors.Control,
                SystemColors.ControlDark,
                SystemColors.ControlDarkDark,
                SystemColors.ControlLight,
                SystemColors.ControlDark,
                Color.White,
                85);
        }

        public static SkinPalette Transparent()
        {
            return new SkinPalette(
                false,
                Color.Transparent,
                Color.Transparent,
                Color.Transparent,
                Color.FromArgb(135, 92, 104, 112),
                Color.FromArgb(120, 214, 222, 228),
                Color.FromArgb(110, 126, 138, 146),
                Color.White,
                70);
        }
    }
}
