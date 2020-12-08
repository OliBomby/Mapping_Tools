using System;
using System.Collections.Generic;

namespace Mapping_Tools_Core.Tools.ComboColourStudio {
    public class ColourPoint : IColourPoint, ICloneable, IEquatable<ColourPoint> {
        private readonly List<int> _colourSequence;

        public double Time { get; }
        public ColourPointMode Mode { get; }
        public IReadOnlyList<int> ColourSequence => _colourSequence;

        public ColourPoint(double time, ColourPointMode mode, IEnumerable<int> colourSequence) {
            Time = time;
            Mode = mode;
            _colourSequence = new List<int>(colourSequence);
        }

        public object Clone() {
            return new ColourPoint(Time, Mode, ColourSequence);
        }

        public bool Equals(ColourPoint other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(_colourSequence, other._colourSequence) && Time.Equals(other.Time) && Mode == other.Mode;
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ColourPoint) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (_colourSequence != null ? _colourSequence.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Time.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Mode;
                return hashCode;
            }
        }
    }
}