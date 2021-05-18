using System;
using System.Windows.Media;
using JetBrains.Annotations;

namespace Mapping_Tools.Classes.BeatmapHelper {

    public class SpecialColour :ComboColour, IEquatable<SpecialColour>, ICloneable {
        private string _name;

        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }

        [UsedImplicitly]
        public SpecialColour() { }

        public SpecialColour(Color color) : base(color) {
        }

        public SpecialColour(Color color, string name) : base(color) {
            Name = name;
        }

        public object Clone() {
            return new SpecialColour(Color, Name);
        }

        public bool Equals(SpecialColour other) {
            if( other is null )
                return false;
            if( ReferenceEquals(this, other) )
                return true;
            return _name == other._name && Color == other.Color;
        }

        public override bool Equals(object obj) {
            if( obj is null )
                return false;
            if( ReferenceEquals(this, obj) )
                return true;
            return obj.GetType() == GetType() && Equals((SpecialColour) obj);
        }

        public override int GetHashCode() {
            return ( _name != null ? _name.GetHashCode() : 0 );
        }
    }
}