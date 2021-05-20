using System.Windows.Media;
using Mapping_Tools.Classes.SystemTools;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// The british alternative because main developer wants to keep the spelling.
    /// Its spelled "Colours" in the game.
    /// </summary>
    public class ComboColour : BindableBase {
        private Color _color;

        /// <summary>
        /// The color value of the colour.
        /// </summary>
        public Color Color {
            get => _color;
            set => Set(ref _color, value);
        }
        
        /// <inheritdoc />
        public ComboColour() {
            Color = new Color();
        }

        /// <inheritdoc />
        public ComboColour(Color color) {
            Color = color;
        }

        /// <inheritdoc />
        public ComboColour(byte r, byte g, byte b) {
            Color = Color.FromRgb(r, g, b);
        }

        /// <inheritdoc />
        public ComboColour(string line) {
            string[] split = line.Split(':');
            string[] commaSplit = split[1].Split(',');

            if (!TryParseInt(commaSplit[0], out int r))
                throw new BeatmapParsingException("Failed to parse red component of colour.", line);

            if (!TryParseInt(commaSplit[1], out int g))
                throw new BeatmapParsingException("Failed to parse green component of colour.", line);

            if (!TryParseInt(commaSplit[2], out int b))
                throw new BeatmapParsingException("Failed to parse blue component of colour.", line);

            Color = Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        public ComboColour Copy() {
            return (ComboColour) MemberwiseClone();
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            return $"{Color.R.ToInvariant()},{Color.G.ToInvariant()},{Color.B.ToInvariant()}";
        }

        public static ComboColour[] GetDefaultComboColours() {
            return new []{new ComboColour(255, 192, 0),
                new ComboColour(0, 202, 0),
                new ComboColour(18, 124, 255),
                new ComboColour(242, 24, 57)};
        }
    }
}
