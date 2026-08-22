using static Mapping_Tools.Core.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.Classes.BeatmapHelper {
    /// <summary>
    /// The british alternative because main developer wants to keep the spelling.
    /// Its spelled "Colours" in the game.
    /// </summary>
    public class ComboColour {
        private RgbaColour color;
        private bool hasAlpha;

        /// <summary>Gets or sets the RGBA value.</summary>
        public RgbaColour Color {
            get => color;
            set {
                color = value;
                if (value.A != byte.MaxValue)
                    hasAlpha = true;
            }
        }

        /// <summary>
        /// Creates a fully transparent black combo colour.
        /// </summary>
        public ComboColour() {
            Color = default;
        }

        /// <summary>
        /// Creates a combo colour from an RGBA value.
        /// </summary>
        /// <param name="color">The value exposed by <see cref="Color"/>.</param>
        public ComboColour(RgbaColour color) {
            Color = color;
            hasAlpha = color.A != byte.MaxValue;
        }

        /// <summary>
        /// Creates an opaque combo colour from individual RGB channels.
        /// </summary>
        /// <param name="r">The red channel.</param>
        /// <param name="g">The green channel.</param>
        /// <param name="b">The blue channel.</param>
        public ComboColour(byte r, byte g, byte b) {
            Color = RgbaColour.FromRgb(r, g, b);
        }

        /// <summary>
        /// Parses the value after the colon in an osu! <c>ComboN : R,G,B[,A]</c> line.
        /// </summary>
        /// <param name="line">A complete combo-colour line from the <c>[Colours]</c> section.</param>
        /// <exception cref="BeatmapParsingException">A channel is not an integer.</exception>
        public ComboColour(string line) {
            string[] split = line.Split(':');
            string[] commaSplit = split[1].Split(',');

            if (!TryParseInt(commaSplit[0], out int r))
                throw new BeatmapParsingException("Failed to parse red component of colour.", line);
            if (!TryParseInt(commaSplit[1], out int g))
                throw new BeatmapParsingException("Failed to parse green component of colour.", line);
            if (!TryParseInt(commaSplit[2], out int b))
                throw new BeatmapParsingException("Failed to parse blue component of colour.", line);

            if (commaSplit.Length > 3) {
                if (!TryParseInt(commaSplit[3], out int a))
                    throw new BeatmapParsingException("Failed to parse alpha component of colour.", line);
                Color = RgbaColour.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                hasAlpha = true;
            } else {
                Color = RgbaColour.FromRgb((byte)r, (byte)g, (byte)b);
            }
        }

        /// <summary>
        /// Creates a shallow copy suitable for editing independently in a view model.
        /// </summary>
        /// <returns>A new combo-colour instance with the same value and alpha-format flag.</returns>
        public ComboColour Copy() => (ComboColour)MemberwiseClone();

        /// <summary>
        /// Serializes the colour channels for the right-hand side of an osu! colour line.
        /// </summary>
        /// <returns><c>R,G,B</c>, or <c>R,G,B,A</c> when the source line explicitly contained alpha.</returns>
        public override string ToString() {
            if (hasAlpha)
                return $"{Color.R.ToInvariant()},{Color.G.ToInvariant()},{Color.B.ToInvariant()},{Color.A.ToInvariant()}";
            return $"{Color.R.ToInvariant()},{Color.G.ToInvariant()},{Color.B.ToInvariant()}";
        }

        /// <summary>
        /// Returns the four-colour palette used when a beatmap does not define combo colours.
        /// </summary>
        /// <returns>Fresh orange, green, blue, and red combo-colour instances.</returns>
        public static ComboColour[] GetDefaultComboColours() {
            return new[] {
                new ComboColour(255, 192, 0),
                new ComboColour(0, 202, 0),
                new ComboColour(18, 124, 255),
                new ComboColour(242, 24, 57)
            };
        }
    }
}
