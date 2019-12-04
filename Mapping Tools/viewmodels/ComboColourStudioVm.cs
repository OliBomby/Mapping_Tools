using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Mapping_Tools.Viewmodels {

    public class ComboColourStudioVm : BindableBase {
        private ObservableCollection<SpecialColour> _comboColours;
        private ObservableCollection<SpecialColour> _specialColours;

        public ComboColourStudioVm() {
            ComboColours = new ObservableCollection<SpecialColour>();
            SpecialColours = new ObservableCollection<SpecialColour>();

            AddCommand = new CommandImplementation(_ => {
                if (ComboColours.Count >= 8) return;
                ComboColours.Add(ComboColours.Count > 0
                    ? new SpecialColour(ComboColours[ComboColours.Count - 1].Color)
                    : new SpecialColour(Colors.White));
            });

            RemoveCommand = new CommandImplementation(_ => {
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

        private void ImportFromBeatmap(string importPath) {
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


        public ObservableCollection<SpecialColour> ComboColours {
            get => _comboColours;
            set => Set(ref _comboColours, value);
        }

        public ObservableCollection<SpecialColour> SpecialColours {
            get => _specialColours;
            set => Set(ref _specialColours, value);
        }

        public string ExportPath { get; set; }

        public CommandImplementation AddCommand { get; }
        public CommandImplementation RemoveCommand { get; }
        public CommandImplementation AddSpecialCommand { get; }
        public CommandImplementation RemoveSpecialCommand { get; }
    }
}