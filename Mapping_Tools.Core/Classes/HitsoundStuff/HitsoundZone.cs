using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// Defines a positional hitsound-mapping rule in which -1 coordinates act as wildcards.
    /// </summary>
    public class HitsoundZone : INotifyPropertyChanged
    {
        private bool isSelected;
        private string name;
        private string filename;
        private double xPos;
        private double yPos;
        private Hitsound hitsound;
        private SampleSet sampleSet;
        private SampleSet additionsSet;
        private int customIndex;

        /// <summary>
        /// Creates an unselected wildcard zone using inherited sample settings.
        /// </summary>
        public HitsoundZone() {
            isSelected = false;
            name = "";
            filename = "";
            xPos = -1;
            yPos = -1;
            hitsound = Hitsound.Normal;
            sampleSet = SampleSet.None;
            additionsSet = SampleSet.None;
            customIndex = 0;
        }

        /// <summary>
        /// Creates a complete zone for persistence or editing.
        /// </summary>
        /// <param name="isSelected">Transient editor selection state.</param>
        /// <param name="name">The user-facing rule name.</param>
        /// <param name="filename">An optional explicit sample filename.</param>
        /// <param name="xPos">The target playfield X coordinate, or -1 to ignore X.</param>
        /// <param name="yPos">The target playfield Y coordinate, or -1 to ignore Y.</param>
        /// <param name="hitsound">The target hitsound layer.</param>
        /// <param name="sampleSet">The normal-layer sample family.</param>
        /// <param name="additionsSet">The addition-layer sample family.</param>
        /// <param name="customIndex">The custom sample index.</param>
        public HitsoundZone(bool isSelected, string name, string filename, double xPos, double yPos, Hitsound hitsound, SampleSet sampleSet, SampleSet additionsSet, int customIndex) {
            this.isSelected = isSelected;
            this.name = name;
            this.filename = filename;
            this.xPos = xPos;
            this.yPos = yPos;
            this.hitsound = hitsound;
            this.sampleSet = sampleSet;
            this.additionsSet = additionsSet;
            this.customIndex = customIndex;
        }

        /// <summary>
        /// Calculates Euclidean distance from a playfield point while ignoring wildcard axes.
        /// </summary>
        /// <param name="pos">The point to test.</param>
        /// <returns>Distance in playfield pixels over the constrained axes.</returns>
        public double Distance(Vector2 pos) {
            double dx = XPos == -1 ? 0 : XPos - pos.X;
            double dy = YPos == -1 ? 0 : YPos - pos.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Copies persisted and editor state into a new zone.
        /// </summary>
        /// <returns>An independently mutable zone.</returns>
        public HitsoundZone Copy() {
            return new HitsoundZone(IsSelected, Name, Filename, XPos, YPos, Hitsound, SampleSet, AdditionsSet, CustomIndex);
        }

        /// <summary>
        /// Gets or sets transient editor selection state; this value is excluded from JSON.
        /// </summary>
        [JsonIgnore]
        public bool IsSelected {
            get => isSelected;
            set {
                if (isSelected == value) return;
                isSelected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the explicit custom sample filename.
        /// </summary>
        public string Filename {
            get => filename;
            set {
                if (filename == value) return;
                filename = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the user-facing rule name.
        /// </summary>
        public string Name {
            get => name;
            set {
                if (name == value) return;
                name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the target X coordinate, or -1 to match any X.
        /// </summary>
        public double XPos {
            get => xPos;
            set {
                if (xPos == value) return;
                xPos = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the target Y coordinate, or -1 to match any Y.
        /// </summary>
        public double YPos {
            get => yPos;
            set {
                if (yPos == value) return;
                yPos = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the normal, whistle, finish, or clap layer matched by the zone.
        /// </summary>
        public Hitsound Hitsound {
            get => hitsound;
            set {
                if (hitsound == value) return;
                hitsound = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the normal-layer sample family.
        /// </summary>
        public SampleSet SampleSet {
            get => sampleSet;
            set {
                if (sampleSet == value) return;
                sampleSet = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the whistle/finish/clap sample family.
        /// </summary>
        public SampleSet AdditionsSet {
            get => additionsSet;
            set {
                if (additionsSet == value) return;
                additionsSet = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the custom sample index assigned by the zone.
        /// </summary>
        public int CustomIndex {
            get => customIndex;
            set {
                if (customIndex == value) return;
                customIndex = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Notifies binding clients after a zone property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for a mutated property.
        /// </summary>
        /// <param name="propertyName">The property name; supplied automatically by the caller.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
