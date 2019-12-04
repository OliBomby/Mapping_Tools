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

        public ComboColourProject() {
            ColourPoints = new ObservableCollection<ColourPoint>();
            ComboColours = new ObservableCollection<SpecialColour>();

            ColourPoints.CollectionChanged += ColourPointsOnCollectionChanged;

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
                    var removing = ComboColours[ComboColours.Count - 1];
                    ComboColours.RemoveAt(ComboColours.Count - 1);
                    foreach (var colourPoint in ColourPoints) {
                        colourPoint.ColourSequence.Remove(removing);
                    }
                }
            });
        }

        private void ColourPointsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (var oldItem in e.OldItems) {
                    ((ColourPoint) oldItem).ParentProject = null;
                }
            }
            if (e.NewItems == null) return;
            foreach (var newItem in e.NewItems) {
                ((ColourPoint) newItem).ParentProject = this;
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
                    ComboColours.Add(new SpecialColour(beatmap.ComboColours[i].Color, $"Combo{i}"));
                }
            }
            catch( Exception ex ) {
                MessageBox.Show($"{ex.Message}{Environment.NewLine}{ex.StackTrace}", "Error");
            }
        }

        
        public ObservableCollection<ColourPoint> ColourPoints {
            get => _colourPoints;
            set => Set(ref _colourPoints, value);
        }

        public ObservableCollection<SpecialColour> ComboColours {
            get => _comboColours;
            set => Set(ref _comboColours, value);
        }

        public CommandImplementation AddColourPointCommand { get; }
        public CommandImplementation RemoveColourPointCommand { get; }
        public CommandImplementation AddComboCommand { get; }
        public CommandImplementation RemoveComboCommand { get; }
    }
}