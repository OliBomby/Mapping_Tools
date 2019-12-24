using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Mapping_Tools.Components.Graph;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class SlideratorVm : BindableBase {
        private ObservableCollection<HitObject> _loadedHitObjects;
        private HitObject _visibleHitObject;
        private int _visibleHitObjectIndex;
        private double _graphBeats;
        private double _beatsPerMinute;
        private int _beatSnapDivisor;
        private TimeSpan _graphDuration;
        private ImportMode _importMode;
        private double _exactTime;
        private Visibility _exactTimeBoxVisibility;
        private double _exportTime;
        private ExportMode _exportMode;
        private double _velocityLimit;

        #region Properties

        public ObservableCollection<HitObject> LoadedHitObjects {
            get => _loadedHitObjects;
            set => SetLoadedHitObjects(value);
        }

        public HitObject VisibleHitObject {
            get => _visibleHitObject;
            set => SetCurrentHitObject(value);
        }

        public int VisibleHitObjectIndex {
            get => _visibleHitObjectIndex;
            set => SetCurrentHitObjectIndex(value);
        }

        public double GraphBeats {
            get => _graphBeats;
            set {
                if (Set(ref _graphBeats, value)) {
                    UpdateAnimationDuration();
                }
            }
        }

        public double BeatsPerMinute {
            get => _beatsPerMinute;
            set {
                if (Set(ref _beatsPerMinute, value)) {
                    UpdateAnimationDuration();
                }
            } 
        }

        public int BeatSnapDivisor {
            get => _beatSnapDivisor;
            set => Set(ref _beatSnapDivisor, value);
        }

        public TimeSpan GraphDuration {
            get => _graphDuration;
            set => Set(ref _graphDuration, value);
        }

        public ImportMode ImportMode {
            get => _importMode;
            set => SetImportMode(value);
        }

        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        public double ExactTime {
            get => _exactTime;
            set => Set(ref _exactTime, value);
        }

        public Visibility ExactTimeBoxVisibility {
            get => _exactTimeBoxVisibility;
            set => Set(ref _exactTimeBoxVisibility, value);
        }

        public double ExportTime {
            get => _exportTime;
            set => Set(ref _exportTime, value);
        }

        public ExportMode ExportMode {
            get => _exportMode;
            set => Set(ref _exportMode, value);
        }
        
        public IEnumerable<ExportMode> ExportModes => Enum.GetValues(typeof(ExportMode)).Cast<ExportMode>();

        public double VelocityLimit {
            get => _velocityLimit;
            set => Set(ref _velocityLimit, value);
        }

        public CommandImplementation ImportCommand { get; }
        public CommandImplementation MoveLeftCommand { get; }
        public CommandImplementation MoveRightCommand { get; }

        [JsonIgnore]
        public GraphState GraphState { get; set; }

        [JsonIgnore]
        public string Path { get; set; }

        #endregion

        public SlideratorVm() {
            LoadedHitObjects = new ObservableCollection<HitObject>();
            BeatsPerMinute = 180;
            GraphBeats = 3;
            BeatSnapDivisor = 4;
            ImportMode = ImportMode.Selected;
            ExactTimeBoxVisibility = Visibility.Collapsed;
            VelocityLimit = 10;

            ImportCommand = new CommandImplementation(Import);
            MoveLeftCommand = new CommandImplementation(_ => {
                VisibleHitObjectIndex = MathHelper.Clamp(VisibleHitObjectIndex - 1, 0, LoadedHitObjects.Count - 1);
            });
            MoveRightCommand = new CommandImplementation(_ => {
                VisibleHitObjectIndex = MathHelper.Clamp(VisibleHitObjectIndex + 1, 0, LoadedHitObjects.Count - 1);
            });
    }

        private void Import(object _) {
            try {
                bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);
                string path = MainWindow.AppWindow.GetCurrentMaps()[0];
                BeatmapEditor editor;
                List<HitObject> markedObjects = null;

                switch (ImportMode) {
                    case ImportMode.Selected:
                        if (!editorRead) break;
                        EditorReaderStuff.GetEditor(out var selected, reader);
                        markedObjects = selected;
                        break;
                    case ImportMode.Bookmarked:
                        editor = new BeatmapEditor(path);
                        markedObjects = editor.Beatmap.GetBookmarkedObjects();
                        break;
                    case ImportMode.Time:
                        editor = new BeatmapEditor(path);
                        markedObjects =
                            new List<HitObject>(editor.Beatmap.GetHitObjectsWithRangeInRange(
                                ExactTime - 5,
                                ExactTime + 5));
                        break;
                }

                if (markedObjects == null || markedObjects.Count(o => o.IsSlider) == 0) return;

                LoadedHitObjects = new ObservableCollection<HitObject>(markedObjects.Where(s => s.IsSlider));
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + ex.StackTrace, "Error");
            }
        }

        private void SetLoadedHitObjects(ObservableCollection<HitObject> value) {
            if (!Set(ref _loadedHitObjects, value, nameof(LoadedHitObjects))) return;
            if (LoadedHitObjects.Count == 0) return;
            VisibleHitObject = LoadedHitObjects[0];
            VisibleHitObjectIndex = 0;
        }

        private void SetCurrentHitObject(HitObject value) {
            if (!Set(ref _visibleHitObject, value, nameof(VisibleHitObject))) return;
            BeatsPerMinute = VisibleHitObject.UnInheritedTimingPoint.GetBPM();
            GraphBeats = VisibleHitObject.TemporalLength * BeatsPerMinute / 60000;
            ExportTime = VisibleHitObject.Time;
        }

        private void SetCurrentHitObjectIndex(int value) {
            if (!Set(ref _visibleHitObjectIndex, value, nameof(VisibleHitObjectIndex))) return;
            if (VisibleHitObjectIndex < 0 || VisibleHitObjectIndex >= LoadedHitObjects.Count) return;
            VisibleHitObject = LoadedHitObjects[VisibleHitObjectIndex];
        }

        private void UpdateAnimationDuration() {
            if (BeatsPerMinute < 1) return;
            GraphDuration = TimeSpan.FromMinutes(GraphBeats / BeatsPerMinute);
        }

        private void SetImportMode(ImportMode value) {
            if (!Set(ref _importMode, value, nameof(ImportMode))) return;
            ExactTimeBoxVisibility = ImportMode == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public enum ImportMode {
        Selected,
        Bookmarked,
        Time
    }

    public enum ExportMode {
        Add,
        Override
    }
}