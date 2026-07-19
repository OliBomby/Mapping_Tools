using System.ComponentModel;
using System.Runtime.CompilerServices;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// The british alternative because main developer wants to keep the spelling.
    /// Its spelled "Colours" in the game.
    /// </summary>
    public class ComboColour : INotifyPropertyChanged {
        private RgbaColour color;
        private bool hasAlpha;

        public RgbaColour Color {
            get => color;
            set => Set(ref color, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ComboColour() {
            Color = default;
        }

        public ComboColour(RgbaColour color) {
            Color = color;
        }

        public ComboColour(byte r, byte g, byte b) {
            Color = RgbaColour.FromRgb(r, g, b);
        }

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

        public ComboColour Copy() => (ComboColour)MemberwiseClone();

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public override string ToString() {
            if (hasAlpha)
                return $"{Color.R.ToInvariant()},{Color.G.ToInvariant()},{Color.B.ToInvariant()},{Color.A.ToInvariant()}";
            return $"{Color.R.ToInvariant()},{Color.G.ToInvariant()},{Color.B.ToInvariant()}";
        }

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
