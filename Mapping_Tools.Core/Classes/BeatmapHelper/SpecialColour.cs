namespace Mapping_Tools.Classes.BeatmapHelper {
    public class SpecialColour : ComboColour, IEquatable<SpecialColour>, ICloneable {
        private string? name;

        public string? Name {
            get => name;
            set => Set(ref name, value);
        }

        public SpecialColour() { }

        public SpecialColour(RgbaColour color) : base(color) { }

        public SpecialColour(RgbaColour color, string name) : base(color) {
            Name = name;
        }

        public object Clone() => new SpecialColour(Color, Name ?? string.Empty);

        public bool Equals(SpecialColour? other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return name == other.name && Color == other.Color;
        }

        public override bool Equals(object? obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SpecialColour)obj);
        }

        public override int GetHashCode() => name?.GetHashCode() ?? 0;
    }
}
