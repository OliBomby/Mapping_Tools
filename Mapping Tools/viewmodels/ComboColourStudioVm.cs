using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.ComboColourStudio;
using Mapping_Tools.Components.Dialogs;
using Mapping_Tools.Components.Domain;
using Mapping_Tools_Core;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.ToolHelpers;
using Mapping_Tools_Core.Tools.ComboColourStudio;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using ConnectedBeatmapEditor = Mapping_Tools.Classes.BeatmapHelper.ConnectedBeatmapEditor;

namespace Mapping_Tools.Viewmodels {

    public class ComboColourStudioVm : BindableBase, IComboColourProject {
        public IReadOnlyList<IColourPoint> ColourPoints => ObservableColourPoints;

        public IReadOnlyList<IComboColour> ComboColours => ObservableComboColours;

        private int _maxBurstLength;
        public int MaxBurstLength {
            get => _maxBurstLength;
            set => Set(ref _maxBurstLength, value);
        }

        /// <summary>
        /// Exposed observable collections so the UI can bind to it and get updates.
        /// </summary>
        internal ObservableCollection<ColourPointBindable> ObservableColourPoints { get; }

        internal ObservableCollection<NamedColour> ObservableComboColours { get; }

        [JsonIgnore]
        public CommandImplementation AddColourPointCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveColourPointCommand { get; }
        [JsonIgnore]
        public CommandImplementation AddComboCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveComboCommand { get; }
        [JsonIgnore]
        public CommandImplementation ImportColoursCommand { get; }
        [JsonIgnore]
        public CommandImplementation ImportColourHaxCommand { get; }
        [JsonIgnore]
        public string ExportPath { get; set; }

        public ComboColourStudioVm() {
            ObservableColourPoints = new ObservableCollection<ColourPointBindable>();
            ObservableComboColours = new ObservableCollection<NamedColour>();
            _maxBurstLength = 1;

            ObservableColourPoints.CollectionChanged += ColourPointsOnCollectionChanged;
            ObservableComboColours.CollectionChanged += ComboColoursOnCollectionChanged;

            #region CommandImplementations
            AddColourPointCommand = new CommandImplementation(_ => {
                double time = ObservableColourPoints.Count > 1 ?
                    ObservableColourPoints.Count(o => o.IsSelected) > 0 ? ObservableColourPoints.Where(o => o.IsSelected).Max(o => o.Time) :
                    ObservableColourPoints.Last().Time
                    : 0;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    try {
                        time = EditorReaderStuff.GetEditorTime();
                    } catch (Exception ex) {
                        ex.Show();
                    }
                }

                ObservableColourPoints.Add(MakeNewColourPoint(time));
            });

            RemoveColourPointCommand = new CommandImplementation(_ => {
                if (ObservableColourPoints.Any(o => o.IsSelected)) {
                    ObservableColourPoints.RemoveAll(o => o.IsSelected);
                    return;
                }
                if (ObservableColourPoints.Count > 0) {
                    ObservableColourPoints.RemoveAt(ObservableColourPoints.Count - 1);
                }
            });

            AddComboCommand = new CommandImplementation(_ => {
                if (ObservableComboColours.Count >= 8) return;
                ObservableComboColours.Add(ObservableComboColours.Count > 0
                    ? new NamedColour(ObservableComboColours[ObservableComboColours.Count - 1].Colour, $"Combo{ObservableComboColours.Count + 1}")
                    : new NamedColour(Colors.White, $"Combo{ObservableComboColours.Count + 1}"));
            });

            RemoveComboCommand = new CommandImplementation(_ => {
                if (ObservableComboColours.Count > 0) {
                    ObservableComboColours.RemoveAt(ObservableComboColours.Count - 1);
                }
            });

            ImportColoursCommand = new CommandImplementation(async _ => {
                try {
                    var sampleDialog = new BeatmapImportDialog();

                    var result = await DialogHost.Show(sampleDialog, "RootDialog");

                    if ((bool)result) {
                        ImportComboColoursFromBeatmap(sampleDialog.Path);
                    }
                }
                catch (Exception e) {
                    e.Show();
                }
            });

            ImportColourHaxCommand = new CommandImplementation(async _ => {
                try {
                    var sampleDialog = new BeatmapImportDialog();

                    var result = await DialogHost.Show(sampleDialog, "RootDialog");

                    if ((bool)result) {
                        ImportColourHaxFromBeatmap(sampleDialog.Path);
                    }
                } catch (Exception e) {
                    e.Show();
                }
            });
            #endregion
        }

        private void ComboColoursOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            
        }

        private void ColourPointsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems == null) return;
            foreach (var newItem in e.NewItems) {
                var newColourPoint = (ColourPointBindable)newItem;
                newColourPoint.ParentProject = this;
            }
        }

        private ColourPointBindable MakeNewColourPoint(double time = 0, IEnumerable<int> colours = null, ColourPointMode mode = ColourPointMode.Normal) {
            return new ColourPointBindable(time, mode, colours ?? new int[0], this);
        }

        /// <summary>
        /// Extracts all the colourhax from a beatmap and assigns it to this.
        /// </summary>
        /// <param name="importPath"></param>
        public void ImportColourHaxFromBeatmap(string importPath) {
            var editor = new ConnectedBeatmapEditor(importPath);
            var beatmap = editor.Beatmap;

            // Add default colours if there are no colours
            if (beatmap.ComboColoursList.Count == 0) {
                beatmap.ComboColoursList.AddRange(ComboColour.GetDefaultComboColours());
            }

            var importer = new ColourHaxImporter {MaxBurstLength = MaxBurstLength};
            var result = importer.ImportColourHax(beatmap);

            ImportComboColours(result);
            ImportColourPoints(result);
        }

        /// <summary>
        /// Extracts all the combo colours from a beatmap and replaces the <see cref="ComboColours"/> of this.
        /// </summary>
        /// <param name="importPath"></param>
        public void ImportComboColoursFromBeatmap(string importPath) {
            var editor = new ConnectedBeatmapEditor(importPath);
            var beatmap = editor.Beatmap;

            ImportComboColours(beatmap);
        }

        /// <summary>
        /// Replaces <see cref="ObservableComboColours"/> with provided combo colours.
        /// </summary>
        /// <param name="comboColourCollection"></param>
        private void ImportComboColours(IComboColourCollection comboColourCollection) {
            ObservableComboColours.Clear();
            for (int i = 0; i < comboColourCollection.ComboColours.Count; i++) {
                ObservableComboColours.Add(new NamedColour(comboColourCollection.ComboColours[i], $"Combo{i + 1}"));
            }
        }

        /// <summary>
        /// Replaces <see cref="ObservableColourPoints"/> with provided colour points.
        /// </summary>
        /// <param name="colourPointCollection"></param>
        private void ImportColourPoints(IColourPointCollection colourPointCollection) {
            ObservableColourPoints.Clear();
            foreach (var colourPoint in colourPointCollection.ColourPoints) {
                ObservableColourPoints.Add(new ColourPointBindable(colourPoint, this));
            }
        }
    }
}