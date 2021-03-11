using Mapping_Tools.Annotations;
using Newtonsoft.Json;
using System.Windows.Media;

namespace Mapping_Tools.Classes.Tools.ComboColourStudio {
    public class NamedColour : BindableBase, IComboColour {
        private Color _colour;
        public Color Colour {
            get => _colour;
            set => Set(ref _colour, value);
        }

        private string _name;
        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }

        [UsedImplicitly]
        public NamedColour() { }

        public NamedColour(Color colour) : this(colour, null) { }

        public NamedColour(IComboColour colour, string name) : this(Color.FromRgb(colour.R, colour.G, colour.B), name) { }

        public NamedColour(Color colour, string name) {
            Colour = colour;
            Name = name;
        }

        public object Clone() {
            return MemberwiseClone();
        }

        [JsonIgnore]
        public byte R => Colour.R;
        [JsonIgnore]
        public byte G => Colour.G;
        [JsonIgnore]
        public byte B => Colour.B;
    }
}