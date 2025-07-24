using Editor_Reader;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Views.Sliderator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

namespace Mapping_Tools.Viewmodels {
    public class SlideratorVm : BindableBase {
        private ObservableCollection<HitObject> loadedHitObjects;
        private HitObject visibleHitObject;
        private int visibleHitObjectIndex;
        private double pixelLength;
        private double globalSv;
        private double graphBeats;
        private double beatsPerMinute;
        private int beatSnapDivisor;
        private TimeSpan graphDuration;
        private double svGraphMultiplier;
        private ImportMode importModeSetting;
        private string timeCode;
        private Visibility timeCodeBoxVisibility;
        private double exportTime;
        private ExportMode exportModeSetting;
        private GraphMode graphModeSetting;
        private double velocityLimit;
        private bool showRedAnchors;
        private bool showGraphAnchors;
        private bool manualVelocity;
        private double newVelocity;
        private double minDendrite;
        private double distanceTraveled;
        private bool delegateToBpm;
        private bool removeSliderTicks;
        private bool exportAsNormal;
        private bool exportAsStream;
        private bool exportAsInvisibleSlider;

        #region Properties

        public ObservableCollection<HitObject> LoadedHitObjects {
            get => loadedHitObjects;
            set => SetLoadedHitObjects(value);
        }

        [JsonIgnore]
        public HitObject VisibleHitObject {
            get => visibleHitObject;
            set => SetCurrentHitObject(value);
        }

        public int VisibleHitObjectIndex {
            get => visibleHitObjectIndex;
            set => SetCurrentHitObjectIndex(value);
        }
        
        [JsonIgnore]
        public double PixelLength {
            get => pixelLength;
            set {
                if (!Set(ref pixelLength, value)) return;
                UpdateSvGraphMultiplier();
                if (VisibleHitObject == null) return;
                VisibleHitObject.PixelLength = value;
            } 
        }

        public double GlobalSv {
            get => globalSv;
            set {
                if (!Set(ref globalSv, value)) return;
                UpdateSvGraphMultiplier();
                RaisePropertyChanged(nameof(ExpectedSegments));
            } 
        }
        
        [JsonIgnore]
        public double GraphBeats {
            get => graphBeats;
            set => Set(ref graphBeats, value, action: () => {
                if (double.IsNaN(value)) graphBeats = 0;
                UpdateAnimationDuration();
                RaisePropertyChanged(nameof(ExpectedSegments));
                if (VisibleHitObject == null) return;
                VisibleHitObject.TemporalLength = graphBeats / BeatsPerMinute * 60000;
            });
        }
        
        [JsonIgnore]
        public double BeatsPerMinute {
            get => beatsPerMinute;
            set {
                if (!Set(ref beatsPerMinute, value)) return;
                UpdateAnimationDuration();
                if (VisibleHitObject == null) return;
                VisibleHitObject.TemporalLength = GraphBeats / value * 60000;
                VisibleHitObject.UnInheritedTimingPoint.SetBpm(value);
            } 
        }

        public int BeatSnapDivisor {
            get => beatSnapDivisor;
            set => Set(ref beatSnapDivisor, value);
        }
        
        [JsonIgnore]
        public TimeSpan GraphDuration {
            get => graphDuration;
            set => Set(ref graphDuration, value);
        }
        
        [JsonIgnore]
        public double SvGraphMultiplier {
            get => svGraphMultiplier;
            set => Set(ref svGraphMultiplier, value);
        }

        public ImportMode ImportModeSetting {
            get => importModeSetting;
            set => SetImportMode(value);
        }

        [JsonIgnore]
        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        public string TimeCode {
            get => timeCode;
            set => Set(ref timeCode, value);
        }

        public Visibility TimeCodeBoxVisibility {
            get => timeCodeBoxVisibility;
            set => Set(ref timeCodeBoxVisibility, value);
        }

        public double ExportTime {
            get => exportTime;
            set {
                if (!Set(ref exportTime, value)) return;
                if (VisibleHitObject == null) return;
                VisibleHitObject.Time = value;
            }
        }

        public ExportMode ExportModeSetting {
            get => exportModeSetting;
            set => Set(ref exportModeSetting, value);
        }
        
        [JsonIgnore]
        public IEnumerable<ExportMode> ExportModes => Enum.GetValues(typeof(ExportMode)).Cast<ExportMode>();

        public GraphMode GraphModeSetting {
            get => graphModeSetting;
            set => Set(ref graphModeSetting, value);
        }

        public double VelocityLimit {
            get => velocityLimit;
            set => Set(ref velocityLimit, value);
        }

        public bool ShowRedAnchors {
            get => showRedAnchors;
            set => Set(ref showRedAnchors, value);
        }

        public bool ShowGraphAnchors {
            get => showGraphAnchors;
            set => Set(ref showGraphAnchors, value);
        }

        public bool ManualVelocity {
            get => manualVelocity;
            set => Set(ref manualVelocity, value);
        }

        public double NewVelocity {
            get => newVelocity;
            set  {
                if (Set(ref newVelocity, value)) {
                    RaisePropertyChanged(nameof(ExpectedSegments));
                }
            }
        }

        public double MinDendrite {
            get => minDendrite;
            set {
                if (Set(ref minDendrite, value)) {
                    RaisePropertyChanged(nameof(ExpectedSegments));
                }
            }
        }

        public double DistanceTraveled {
            get => distanceTraveled;
            set {
                if (Set(ref distanceTraveled, value)) {
                    RaisePropertyChanged(nameof(ExpectedSegments));
                }
            }
        }

        public long ExpectedSegments {
            get {
                if (ExportAsStream) {
                    return (long) (GraphBeats * BeatSnapDivisor) + 1;
                }
                if (ExportAsInvisibleSlider) {
                    var timeLength = (long)(GraphBeats / BeatsPerMinute * 60000);
                    return 16 + 7 * (timeLength - 1);
                }
                var newLength = NewVelocity * 100 * GlobalSv * GraphBeats;
                return (long) ((newLength - DistanceTraveled) / MinDendrite * 2 + DistanceTraveled / 10);
            }
        }

        public bool DelegateToBpm {
            get => delegateToBpm;
            set => Set(ref delegateToBpm, value);
        }

        public bool RemoveSliderTicks {
            get => removeSliderTicks;
            set => Set(ref removeSliderTicks, value);
        }

        public bool ExportAsNormal {
            get => exportAsNormal;
            set {
                if (Set(ref exportAsNormal, value)) {
                    RaisePropertyChanged(nameof(ExpectedSegments));
                }
            }
        }

        public bool ExportAsStream {
            get => exportAsStream;
            set {
                if (Set(ref exportAsStream, value)) {
                    RaisePropertyChanged(nameof(ExpectedSegments));
                }
            }
        }

        public bool ExportAsInvisibleSlider {
            get => exportAsInvisibleSlider;
            set {
                if (Set(ref exportAsInvisibleSlider, value)) {
                    RaisePropertyChanged(nameof(ExpectedSegments));
                }
            }
        }

        public bool DoEditorRead { get; set; }

        [JsonIgnore]
        public CommandImplementation ImportCommand { get; }
        [JsonIgnore]
        public CommandImplementation MoveLeftCommand { get; }
        [JsonIgnore]
        public CommandImplementation MoveRightCommand { get; }
        [JsonIgnore]
        public CommandImplementation GraphToggleCommand { get; }

        public GraphState GraphState { get; set; }

        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public SlideratorView SlideratorView { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        [JsonIgnore]
        public bool Reload { get; set; }

        #endregion

        public SlideratorVm() {
            LoadedHitObjects = new ObservableCollection<HitObject>();
            PixelLength = 100;
            BeatsPerMinute = 180;
            GlobalSv = 1.4;
            GraphBeats = 3;
            BeatSnapDivisor = 4;
            ImportModeSetting = ImportMode.Selected;
            TimeCodeBoxVisibility = Visibility.Collapsed;
            VelocityLimit = 10;
            GraphModeSetting = GraphMode.Position;
            ShowRedAnchors = false;
            ShowGraphAnchors = false;
            ManualVelocity = false;
            NewVelocity = 1;
            MinDendrite = 2;
            DistanceTraveled = 0;
            DelegateToBpm = false;
            RemoveSliderTicks = false;
            ExportAsStream = false;
            DoEditorRead = false;
            Quick = false;
            ExportAsNormal = true;

            ImportCommand = new CommandImplementation(_ => Import(ImportModeSetting == ImportMode.Selected ? 
                IOHelper.GetCurrentBeatmapOrCurrentBeatmap(false) :
                MainWindow.AppWindow.GetCurrentMaps()[0])
            );
            MoveLeftCommand = new CommandImplementation(_ => {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    SlideratorView.RunFast();
                    SlideratorView.RunFinished += SlideratorViewOnRunFinishedMoveLeftOnce;
                } else {
                    MoveLeftOnce();
                }
            });
            MoveRightCommand = new CommandImplementation(_ => {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    SlideratorView.RunFast();
                    SlideratorView.RunFinished += SlideratorViewOnRunFinishedMoveRightOnce;
                } else {
                    MoveRightOnce();
                }
            });
            GraphToggleCommand = new CommandImplementation(ToggleGraphMode);
        }

        private void SlideratorViewOnRunFinishedMoveLeftOnce(object sender, EventArgs e) {
            Application.Current.Dispatcher?.Invoke(() => {
                SlideratorView.RunFinished -= SlideratorViewOnRunFinishedMoveLeftOnce;
                MoveLeftOnce();
            });
        }

        private void SlideratorViewOnRunFinishedMoveRightOnce(object sender, EventArgs e) {
            Application.Current.Dispatcher?.Invoke(() => {
                SlideratorView.RunFinished -= SlideratorViewOnRunFinishedMoveRightOnce;
                MoveRightOnce();
            });
        }

        private void MoveRightOnce() {
            var newIndex = VisibleHitObjectIndex + 1;
            if (newIndex < LoadedHitObjects.Count) {
                VisibleHitObjectIndex = newIndex;
            } else {
                MessageBox.Show("You've reached the end of the slider list.");
            }
        }

        private void MoveLeftOnce() {
            var newIndex = VisibleHitObjectIndex - 1;
            if (newIndex >= 0) {
                VisibleHitObjectIndex = newIndex;
            } else {
                MessageBox.Show("You've reached the start of the slider list.");
            }
        }

        public void Import(string path) {
            try {
                EditorReader reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);
                
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
                }

                if (markedObjects == null || markedObjects.Count(o => o.IsSlider) == 0) return;

                LoadedHitObjects = new ObservableCollection<HitObject>(markedObjects.Where(s => s.IsSlider));

                if (editor != null) {
                    GlobalSv = editor.Beatmap.Difficulty["SliderMultiplier"].GetDouble();
                }

                DoEditorRead = true;
            } catch (Exception ex) {
                ex.Show();
            }
        }

        private void ToggleGraphMode(object _) {
            switch (GraphModeSetting) {
                case GraphMode.Position:
                    GraphModeSetting = GraphMode.Velocity;
                    break;
                case GraphMode.Velocity:
                    GraphModeSetting = GraphMode.Position;
                    break;
                default:
                    GraphModeSetting = GraphMode.Position;
                    break;
            }
        }

        private void SetLoadedHitObjects(ObservableCollection<HitObject> value) {
            Set(ref loadedHitObjects, value, nameof(LoadedHitObjects), () => {
                LoadedHitObjects.CollectionChanged += LoadedHitObjectsOnCollectionChanged;
                if (LoadedHitObjects.Count == 0) return;
                if (VisibleHitObjectIndex == 0) {
                    VisibleHitObject = LoadedHitObjects[0];
                } else {
                    VisibleHitObjectIndex = 0;
                }
            });
        }

        private void LoadedHitObjectsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (VisibleHitObject == null && LoadedHitObjects.Count > 0) {
                VisibleHitObject = LoadedHitObjects[0];
                VisibleHitObjectIndex = 0;
            }
        }

        private void SetCurrentHitObject(HitObject value) {
            Set(ref visibleHitObject, value, nameof(VisibleHitObject), () => {
                if (VisibleHitObject.UnInheritedTimingPoint == null) return;
                // Gotta watch out because the BPM and GraphBeats edit the TemporalLength of the VisibleHitObject
                var temporalLengthTemp = VisibleHitObject.TemporalLength;
                BeatsPerMinute = VisibleHitObject.UnInheritedTimingPoint.GetBpm() > 0
                    ? VisibleHitObject.UnInheritedTimingPoint.GetBpm()
                    : 180; // Default BPM if the hitobject has no valid timing
                GraphBeats = temporalLengthTemp * BeatsPerMinute / 60000;
                ExportTime = VisibleHitObject.Time;
                PixelLength = VisibleHitObject.PixelLength;
            });
        }

        private void SetCurrentHitObjectIndex(int value) {
            Set(ref visibleHitObjectIndex, value, nameof(VisibleHitObjectIndex), () => {
                if (VisibleHitObjectIndex < 0 || VisibleHitObjectIndex >= LoadedHitObjects.Count) return;
                VisibleHitObject = LoadedHitObjects[VisibleHitObjectIndex];
            });
        }

        private void UpdateAnimationDuration() {
            if (BeatsPerMinute < 1) return;
            GraphDuration = TimeSpan.FromMinutes(GraphBeats / BeatsPerMinute);
        }

        private void UpdateSvGraphMultiplier() {
            SvGraphMultiplier = 100 * GlobalSv / PixelLength;
        }

        private void SetImportMode(ImportMode value) {
            if (!Set(ref importModeSetting, value, nameof(ImportMode))) return;
            TimeCodeBoxVisibility = ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;
        }

        public enum ImportMode
        {
            Selected,
            Bookmarked,
            Time
        }

        public enum ExportMode
        {
            Add,
            Override
        }

        public enum GraphMode
        {
            Position,
            Velocity
        }
    }
}