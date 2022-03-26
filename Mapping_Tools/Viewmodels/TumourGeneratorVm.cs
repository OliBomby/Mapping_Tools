using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Options;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Components.Graph;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class TumourGeneratorVm : BindableBase {
        #region Properties

        private HitObject _previewHitObject;
        public HitObject PreviewHitObject {
            get => _previewHitObject;
            set => Set(ref _previewHitObject, value);
        }

        private ImportMode _importModeSetting;
        public ImportMode ImportModeSetting {
            get => _importModeSetting;
            set {
                if (Set(ref _importModeSetting, value)) {
                    RaisePropertyChanged(nameof(TimeCodeBoxVisibility));
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        private string _timeCode;
        public string TimeCode {
            get => _timeCode;
            set => Set(ref _timeCode, value);
        }

        [JsonIgnore]
        public Visibility TimeCodeBoxVisibility => ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;

        private ObservableCollection<TumourLayer> _tumourLayers;
        public ObservableCollection<TumourLayer> TumourLayers {
            get => _tumourLayers;
            set => Set(ref _tumourLayers, value);
        }

        private bool _justMiddleAnchors;
        public bool JustMiddleAnchors {
            get => _justMiddleAnchors;
            set => Set(ref _justMiddleAnchors, value);
        }

        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        [JsonIgnore]
        public bool Reload { get; set; }

        [JsonIgnore]
        public CommandImplementation ImportCommand { get; }

        #endregion

        public TumourGeneratorVm() {
            PreviewHitObject = new HitObject("95,319,75,2,0,P|271:304|421:48,1,475");
            ImportModeSetting = ImportMode.Selected;
            TumourLayers = new ObservableCollection<TumourLayer> { new TumourLayer() };
            JustMiddleAnchors = false;

            ImportCommand = new CommandImplementation(_ => Import(ImportModeSetting == ImportMode.Selected ?
                IOHelper.GetCurrentBeatmapOrCurrentBeatmap() :
                MainWindow.AppWindow.GetCurrentMaps()[0])
            );
        }

        public void Import(string path) {
            try {
                Editor_Reader.EditorReader reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

                if (ImportModeSetting == ImportMode.Selected && editorReaderException1 != null) {
                    throw new Exception("Could not fetch selected hit objects.", editorReaderException1);
                }

                BeatmapEditor editor = null;
                List<HitObject> markedObjects = null;

                switch (ImportModeSetting) {
                    case ImportMode.Selected:
                        editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                        if (editorReaderException2 != null) {
                            throw new Exception("Could not fetch selected hit objects.", editorReaderException2);
                        }

                        markedObjects = selected;
                        break;
                    case ImportMode.Bookmarked:
                        editor = new BeatmapEditor(path);
                        markedObjects = editor.Beatmap.GetBookmarkedObjects();
                        break;
                    case ImportMode.Time:
                        editor = new BeatmapEditor(path);
                        markedObjects = editor.Beatmap.QueryTimeCode(TimeCode).ToList();
                        break;
                    default:
                        break;
                }

                if (markedObjects == null || !markedObjects.Any(o => o.IsSlider)) return;

                PreviewHitObject = markedObjects.First(s => s.IsSlider);
            } catch (Exception ex) {
                ex.Show();
            }
        }

        public enum ImportMode {
            Selected,
            Bookmarked,
            Time
        }
    }
}