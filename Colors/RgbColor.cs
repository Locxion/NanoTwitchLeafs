namespace NanoTwitchLeafs.Colors
{
    using System.Drawing;

    /// <summary>
    /// Represents a RGB color space.
    /// http://en.wikipedia.org/wiki/HSV_color_space
    /// </summary>
    public sealed class RgbColor
    {
        public RgbColor(
            int red,
            int green,
            int blue,
            int alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        /// <summary>
        /// Gets or sets the red component. Values from 0 to 255.
        /// </summary>

        public int Red { get; set; }

        /// <summary>
        /// Gets or sets the green component. Values from 0 to 255.
        /// </summary>

        public int Green { get; set; }

        /// <summary>
        /// Gets or sets the blue component. Values from 0 to 255.
        /// </summary>

        public int Blue { get; set; }

        /// <summary>
        /// Gets or sets the alpha component. Values from 0 to 255.
        /// </summary>

        public int Alpha { get; set; }

        public static RgbColor FromColor(
            Color color)
        {
            return ColorConverting.ColorToRgb(color);
        }

        public static RgbColor FromRgbColor(
            RgbColor color)
        {
            return new RgbColor(color.Red, color.Green, color.Blue, color.Alpha);
        }

        public static RgbColor FromHsbColor(
            HsbColor color)
        {
            return color.ToRgbColor();
        }

        public static RgbColor FromHslColor(
            HslColor color)
        {
            return color.ToRgbColor();
        }

        public override string ToString()
        {
            return Alpha < 255 ? $@"RGBA({Red}, {Green}, {Blue}, {Alpha / 255f})" : $@"RGB({Red}, {Green}, {Blue})";
        }

        public Color ToColor()
        {
            return ColorConverting.RgbToDrawingColor(this);
        }

        public RgbColor ToRgbColor()
        {
            return this;
        }

        public HsbColor ToHsbColor()
        {
            return ColorConverting.RgbToHsb(this);
        }

        public HslColor ToHslColor()
        {
            return ColorConverting.RgbToHsl(this);
        }

        public override bool Equals(
            object obj)
        {
            var equal = false;

            if (obj is RgbColor color)
            {
                var rgb = color;

                if (Red == rgb.Red && Blue == rgb.Blue && Green == rgb.Green)
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return $@"R:{Red}-G:{Green}-B:{Blue}-A:{Alpha}".GetHashCode();
        }
    }
}