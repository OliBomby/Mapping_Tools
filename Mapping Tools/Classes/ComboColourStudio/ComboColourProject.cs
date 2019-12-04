using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;

namespace Mapping_Tools.Classes.ComboColourStudio {
    public class ComboColourProject : BindableBase {
        private ObservableCollection<ColourPoint> _colourPoints;
        private ObservableCollection<SpecialColour> _comboColours;
        private ObservableCollection<SpecialColour> _specialColours;

        public ComboColourProject() {
            ColourPoints = new ObservableCollection<ColourPoint>();
            ComboColours = new ObservableCollection<SpecialColour>();
            SpecialColours = new ObservableCollection<SpecialColour>();

            AddColourPointCommand = new CommandImplementation(_ => {
                ColourPoints.Add(ColourPoints.Count > 0
                    ? GenerateNewColourPoint(ColourPoints[ColourPoints.Count - 1].Time)
                    : GenerateNewColourPoint());
            });

            RemoveColourPointCommand = new CommandImplementation(_ => {
                if (ColourPoints.Count > 0) {
                    ColourPoints.RemoveAt(ColourPoints.Count - 1);
                }
            });

            AddComboCommand = new CommandImplementation(_ => {
                if (ComboColours.Count >= 8) return;
                ComboColours.Add(ComboColours.Count > 0
                    ? new SpecialColour(ComboColours[ComboColours.Count - 1].Color)
                    : new SpecialColour(Colors.White));
            });

            RemoveComboCommand = new CommandImplementation(_ => {
                if (ComboColours.Count > 0) {
                    ComboColours.RemoveAt(ComboColours.Count - 1);
                }
            });

            AddSpecialCommand = new CommandImplementation(_ => {
                SpecialColours.Add(SpecialColours.Count > 0
                    ? new SpecialColour(SpecialColours[SpecialColours.Count - 1].Color)
                    : new SpecialColour(Colors.White));
            });

            RemoveSpecialCommand = new CommandImplementation(_ => {
                if (SpecialColours.Count > 0) {
                    SpecialColours.RemoveAt(SpecialColours.Count - 1);
                }
            });
        }

        private ColourPoint GenerateNewColourPoint(double time = 0) {
            return new ColourPoint(time, new SpecialColour[0], ColourPointMode.Normal, this);
        }

        public void ImportFromBeatmap(string importPath) {
            try {
                var editor = new BeatmapEditor(importPath);
                var beatmap = editor.Beatmap;

                ComboColours.Clear();
                for (int i = 0; i < beatmap.ComboColours.Count; i++) {
                    ComboColours.Add(new SpecialColour(beatmap.ComboColours[i].Color, $"Combo{i}"));
                }
                SpecialColours.Clear();
                foreach (var specialColour in beatmap.SpecialColours) {
                    SpecialColours.Add(new SpecialColour(specialColour.Value.Color, specialColour.Key));
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

        public ObservableCollection<SpecialColour> SpecialColours {
            get => _specialColours;
            set => Set(ref _specialColours, value);
        }

        public CommandImplementation AddColourPointCommand { get; }
        public CommandImplementation RemoveColourPointCommand { get; }
        public CommandImplementation AddComboCommand { get; }
        public CommandImplementation RemoveComboCommand { get; }
        public CommandImplementation AddSpecialCommand { get; }
        public CommandImplementation RemoveSpecialCommand { get; }
    }
}