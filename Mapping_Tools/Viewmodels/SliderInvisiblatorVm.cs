﻿using Editor_Reader;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Views.SliderInvisiblator;
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
    public class SliderInvisiblatorVm : BindableBase {
        private ObservableCollection<HitObject> _loadedHitObjects;
        private HitObject _visibleHitObject;
        private int _visibleHitObjectIndex;
        private double _pixelLength;
        private double _globalSv;
        private double _graphBeats;
        private double _beatsPerMinute;
        private int _beatSnapDivisor;
        private TimeSpan _graphDuration;
        private double _svGraphMultiplier;
        private ImportMode _importModeSetting;
        private string _timeCode;
        private Visibility _timeCodeBoxVisibility;
        private double _exportTime;
        private ExportMode _exportModeSetting;
        private GraphMode _graphModeSetting;
        private double _velocityLimit;
        private bool _showRedAnchors;
        private bool _showGraphAnchors;
        private bool _manualVelocity;
        private bool _singleSliderMode;
        private double _newVelocity;
        private double _distanceTraveled;

        #region Properties

        public ObservableCollection<HitObject> LoadedHitObjects {
            get => _loadedHitObjects;
            set => SetLoadedHitObjects(value);
        }

        [JsonIgnore]
        public HitObject VisibleHitObject {
            get => _visibleHitObject;
            set => SetCurrentHitObject(value);
        }

        public int VisibleHitObjectIndex {
            get => _visibleHitObjectIndex;
            set => SetCurrentHitObjectIndex(value);
        }
        
        [JsonIgnore]
        public double PixelLength {
            get => _pixelLength;
            set {
                if (!Set(ref _pixelLength, value)) return;
                UpdateSvGraphMultiplier();
                if (VisibleHitObject == null) return;
                VisibleHitObject.PixelLength = value;
            } 
        }

        public double GlobalSv {
            get => _globalSv;
            set {
                if (!Set(ref _globalSv, value)) return;
                UpdateSvGraphMultiplier();
            } 
        }
        
        [JsonIgnore]
        public double GraphBeats {
            get => _graphBeats;
            set {
                if (!Set(ref _graphBeats, value)) return;
                UpdateAnimationDuration();
                if (VisibleHitObject == null) return;
                VisibleHitObject.TemporalLength = value / BeatsPerMinute * 60000;
            }
        }
        
        [JsonIgnore]
        public double BeatsPerMinute {
            get => _beatsPerMinute;
            set {
                if (!Set(ref _beatsPerMinute, value)) return;
                UpdateAnimationDuration();
                if (VisibleHitObject == null) return;
                VisibleHitObject.TemporalLength = GraphBeats / value * 60000;
                VisibleHitObject.UnInheritedTimingPoint.SetBpm(value);
            } 
        }

        public int BeatSnapDivisor {
            get => _beatSnapDivisor;
            set => Set(ref _beatSnapDivisor, value);
        }
        
        [JsonIgnore]
        public TimeSpan GraphDuration {
            get => _graphDuration;
            set => Set(ref _graphDuration, value);
        }
        
        [JsonIgnore]
        public double SvGraphMultiplier {
            get => _svGraphMultiplier;
            set {
                if (Set(ref _svGraphMultiplier, value)) {
                }
            }
        }

        public ImportMode ImportModeSetting {
            get => _importModeSetting;
            set => SetImportMode(value);
        }

        [JsonIgnore]
        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        public string TimeCode {
            get => _timeCode;
            set => Set(ref _timeCode, value);
        }

        public Visibility TimeCodeBoxVisibility {
            get => _timeCodeBoxVisibility;
            set => Set(ref _timeCodeBoxVisibility, value);
        }

        public double ExportTime {
            get => _exportTime;
            set {
                if (!Set(ref _exportTime, value)) return;
                if (VisibleHitObject == null) return;
                VisibleHitObject.Time = value;
            }
        }

        public ExportMode ExportModeSetting {
            get => _exportModeSetting;
            set => Set(ref _exportModeSetting, value);
        }
        
        [JsonIgnore]
        public IEnumerable<ExportMode> ExportModes => Enum.GetValues(typeof(ExportMode)).Cast<ExportMode>();

        public GraphMode GraphModeSetting {
            get => _graphModeSetting;
            set => Set(ref _graphModeSetting, value);
        }

        public double VelocityLimit {
            get => _velocityLimit;
            set => Set(ref _velocityLimit, value);
        }

        public bool ShowRedAnchors {
            get => _showRedAnchors;
            set => Set(ref _showRedAnchors, value);
        }

        public bool ShowGraphAnchors {
            get => _showGraphAnchors;
            set => Set(ref _showGraphAnchors, value);
        }

        public bool ManualVelocity {
            get => _manualVelocity;
            set => Set(ref _manualVelocity, value);
        }

        public bool SingleSliderMode {
            get => _singleSliderMode;
            set => Set(ref _singleSliderMode, value);
        }

        public bool MassInvisiblationMode {
            get => !_singleSliderMode;
            set => Set(ref _singleSliderMode, !value);
        }

        public double NewVelocity
        {
            get => _newVelocity;
            set => Set(ref _newVelocity, value);
        }
        public double DistanceTraveled
        {
            get => _distanceTraveled;
            set => Set(ref _distanceTraveled, value);
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
        public string[] Paths { get; set; }

        [JsonIgnore]
        public SliderInvisiblatorView SliderInvisiblatorView { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        [JsonIgnore]
        public bool Reload { get; set; }

        #endregion

        public SliderInvisiblatorVm() {
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
            SingleSliderMode = true;
            DoEditorRead = false;
            Quick = false;

            ImportCommand = new CommandImplementation(_ => Import(ImportModeSetting == ImportMode.Selected ? 
                IOHelper.GetCurrentBeatmapOrCurrentBeatmap() : 
                MainWindow.AppWindow.GetCurrentMaps()[0])
            );
            MoveLeftCommand = new CommandImplementation(_ => {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    SliderInvisiblatorView.RunFast();
                    SliderInvisiblatorView.RunFinished += SliderInvisiblatorViewOnRunFinishedMoveLeftOnce;
                } else {
                    MoveLeftOnce();
                }
            });
            MoveRightCommand = new CommandImplementation(_ => {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    SliderInvisiblatorView.RunFast();
                    SliderInvisiblatorView.RunFinished += SliderInvisiblatorViewOnRunFinishedMoveRightOnce;
                } else {
                    MoveRightOnce();
                }
            });
            GraphToggleCommand = new CommandImplementation(ToggleGraphMode);
        }

        private void SliderInvisiblatorViewOnRunFinishedMoveLeftOnce(object sender, EventArgs e) {
            Application.Current.Dispatcher?.Invoke(() => {
                SliderInvisiblatorView.RunFinished -= SliderInvisiblatorViewOnRunFinishedMoveLeftOnce;
                MoveLeftOnce();
            });
        }

        private void SliderInvisiblatorViewOnRunFinishedMoveRightOnce(object sender, EventArgs e) {
            Application.Current.Dispatcher?.Invoke(() => {
                SliderInvisiblatorView.RunFinished -= SliderInvisiblatorViewOnRunFinishedMoveRightOnce;
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
            Set(ref _loadedHitObjects, value, nameof(LoadedHitObjects), () => {
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
            Set(ref _visibleHitObject, value, nameof(VisibleHitObject), () => {
                if (VisibleHitObject.UnInheritedTimingPoint == null) return;
                // Gotta watch out because the BPM and GraphBeats edit the TemporalLength of the VisibleHitObject
                var temporalLengthTemp = VisibleHitObject.TemporalLength;
                BeatsPerMinute = VisibleHitObject.UnInheritedTimingPoint.GetBpm();
                GraphBeats = temporalLengthTemp * BeatsPerMinute / 60000;
                ExportTime = VisibleHitObject.Time;
                PixelLength = VisibleHitObject.PixelLength;
            });
        }

        private void SetCurrentHitObjectIndex(int value) {
            Set(ref _visibleHitObjectIndex, value, nameof(VisibleHitObjectIndex), () => {
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
            if (!Set(ref _importModeSetting, value, nameof(ImportMode))) return;
            TimeCodeBoxVisibility = ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;
        }

        public enum ImportMode
        {
            Selected,
            Bookmarked,
            Time,
            Everything
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