using System.Windows.Media;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class SpecialColour : ComboColour {
        private string _name;

        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }

        [UsedImplicitly]
        public SpecialColour() {}

        public SpecialColour(Color color) : base(color) {}

        public SpecialColour(Color color, string name) : base(color) {
            Name = name;
        }
    }
}