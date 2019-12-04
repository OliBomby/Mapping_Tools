using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using System.Collections.ObjectModel;

namespace Mapping_Tools.Classes.ComboColourStudio {
    public class ColourPoint : BindableBase, IEquatable<ColourPoint>, ICloneable {
        private double _time;
        private ObservableCollection<SpecialColour> _colourSequence; 
        private ColourPointMode _mode;

        public ColourPoint() : this(0, new ObservableCollection<SpecialColour>(), ColourPointMode.Normal) {}

        public ColourPoint(double time, IEnumerable<SpecialColour> colourSequence, ColourPointMode mode) {
            Time = time;
            ColourSequence = new ObservableCollection<SpecialColour>(colourSequence);
            Mode = mode;
        }

        public double Time {
            get => _time;
            set => Set(ref _time, value);
        }

        public ObservableCollection<SpecialColour> ColourSequence {
            get => _colourSequence;
            set => Set(ref _colourSequence, value);
        }

        public ColourPointMode Mode {
            get => _mode;
            set => Set(ref _mode, value);
        }

        public bool Equals(ColourPoint other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _time.Equals(other._time) && Equals(_colourSequence, other._colourSequence) && _mode == other._mode;
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ColourPoint) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = _time.GetHashCode();
                hashCode = (hashCode * 397) ^ (_colourSequence != null ? _colourSequence.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) _mode;
                return hashCode;
            }
        }

        public object Clone() {
            var colours = new SpecialColour[ColourSequence.Count];
            for (int i = 0; i < ColourSequence.Count; i++) {
                colours[i] = (SpecialColour)ColourSequence[i].Clone();
            }
            return new ColourPoint(Time, colours, Mode);
        }
    }
}