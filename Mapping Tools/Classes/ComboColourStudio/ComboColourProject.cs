using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;

namespace Mapping_Tools.Classes.ComboColourStudio {
    public class ComboColourProject : BindableBase {
        private ObservableCollection<ColourPoint> _colourPoints;
        private ObservableCollection<SpecialColour> _comboColours;

        private int _maxBurstLength;

        public ComboColourProject() {
            ColourPoints = new ObservableCollection<ColourPoint>();
            ComboColours = new ObservableCollection<SpecialColour>();

            MaxBurstLength = 1;

            AddColourPointCommand = new CommandImplementation(_ => {
                ColourPoints.Add(ColourPoints.Count > 0
                    ? (ColourPoint)ColourPoints[ColourPoints.Count - 1].Clone()
                    : GenerateNewColourPoint());
            });

            RemoveColourPointCommand = new CommandImplementation(_ => {
                if (ColourPoints.Any(o => o.IsSelected)) {
                    ColourPoints.RemoveAll(o => o.IsSelected);
                    return;
                }
                if (ColourPoints.Count > 0) {
                    ColourPoints.RemoveAt(ColourPoints.Count - 1);
                }
            });

            AddComboCommand = new CommandImplementation(_ => {
                if (ComboColours.Count >= 8) return;
                ComboColours.Add(ComboColours.Count > 0
                    ? new SpecialColour(ComboColours[ComboColours.Count - 1].Color, $"Combo{ComboColours.Count + 1}")
                    : new SpecialColour(Colors.White, $"Combo{ComboColours.Count + 1}"));
            });

            RemoveComboCommand = new CommandImplementation(_ => {
                if (ComboColours.Count > 0) {
                    ComboColours.RemoveAt(ComboColours.Count - 1);
                }
            });
        }

        private void ComboColoursOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            /*if (e.OldItems != null) {
                foreach (var oldItem in e.OldItems) {
                    var removed = (SpecialColour) oldItem;
                    foreach (var colourPoint in ColourPoints) {
                        colourPoint.ColourSequence.Remove(removed);
                    }
                }
            }*/

            MatchComboColourReferences();
        }

        private void ColourPointsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (var oldItem in e.OldItems) {
                    ((ColourPoint) oldItem).ParentProject = null;
                }
            }
            if (e.NewItems == null) return;
            foreach (var newItem in e.NewItems) {
                var newColourPoint = (ColourPoint) newItem;
                newColourPoint.ParentProject = this;
                // Match object references with the combo colours
                MatchComboColourReferences();
            }
        }

        /// <summary>
        /// This method makes sure the SpecialColour objects in the colour sequences are the same objects as in the combo colours.
        /// With this the colours of the colour sequences update when the combo colours get changed.
        /// </summary>
        private void MatchComboColourReferences() {
            foreach (var colourPoint in ColourPoints) {
                for (int i = 0; i < colourPoint.ColourSequence.Count; i++) {
                    colourPoint.ColourSequence[i] =
                        ComboColours.FirstOrDefault(o => o.Name == colourPoint.ColourSequence[i].Name) ??
                        colourPoint.ColourSequence[i];
                }
            }
        }

        private ColourPoint GenerateNewColourPoint(double time = 0, IEnumerable<SpecialColour> colours = null) {
            return new ColourPoint(time, colours ?? new SpecialColour[0], ColourPointMode.Normal, this);
        }

        public void ImportFromBeatmap(string importPath) {
            try {
                var editor = new BeatmapEditor(importPath);
                var beatmap = editor.Beatmap;

                ComboColours.Clear();
                for (int i = 0; i < beatmap.ComboColours.Count; i++) {
                    ComboColours.Add(new SpecialColour(beatmap.ComboColours[i].Color, $"Combo{i + 1}"));
                }
            }
            catch( Exception ex ) {
                MessageBox.Show($"{ex.Message}{Environment.NewLine}{ex.StackTrace}", "Error");
            }
        }

        
        public ObservableCollection<ColourPoint> ColourPoints {
            get => _colourPoints;
            set { Set(ref _colourPoints, value);
                ColourPoints.CollectionChanged += ColourPointsOnCollectionChanged;
            }
        }

        public ObservableCollection<SpecialColour> ComboColours {
            get => _comboColours;
            set { Set(ref _comboColours, value);
                ComboColours.CollectionChanged += ComboColoursOnCollectionChanged;
            }
        }

        public int MaxBurstLength {
            get => _maxBurstLength;
            set => Set(ref _maxBurstLength, value);
        }

        public CommandImplementation AddColourPointCommand { get; }
        public CommandImplementation RemoveColourPointCommand { get; }
        public CommandImplementation AddComboCommand { get; }
        public CommandImplementation RemoveComboCommand { get; }
    }
}