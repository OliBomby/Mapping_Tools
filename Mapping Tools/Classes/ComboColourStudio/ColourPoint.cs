using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using System.Collections.ObjectModel;
using System.Linq;
using Mapping_Tools.Annotations;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.ComboColourStudio {
    public class ColourPoint : BindableBase, IEquatable<ColourPoint>, ICloneable {
        private double _time;
        private ObservableCollection<SpecialColour> _colourSequence; 
        private ColourPointMode _mode;
        private bool _isSelected;
        private ComboColourProject _parentProject;

        public ColourPoint() : this(0, new ObservableCollection<SpecialColour>(), ColourPointMode.Normal, null) {}

        public ColourPoint(double time, IEnumerable<SpecialColour> colourSequence, ColourPointMode mode, ComboColourProject parentProject) {
            Time = time;
            ColourSequence = new ObservableCollection<SpecialColour>(colourSequence);
            Mode = mode;
            ParentProject = parentProject;

            
            AddCommand = new CommandImplementation(_ => {

            });

            RemoveCommand = new CommandImplementation(_ => {

            });
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

        public bool IsSelected {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        [CanBeNull]
        [JsonIgnore]
        public ComboColourProject ParentProject {
            get => _parentProject;
            set => Set(ref _parentProject, value);
        }
        
        public IEnumerable<ColourPointMode> ColourPointModes => Enum.GetValues(typeof(ColourPointMode)).Cast<ColourPointMode>();
        public CommandImplementation AddCommand { get; }
        public CommandImplementation RemoveCommand { get; }

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
            return new ColourPoint(Time, colours, Mode, ParentProject);
        }
    }
}