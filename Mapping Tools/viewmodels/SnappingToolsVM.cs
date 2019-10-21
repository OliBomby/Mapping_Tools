using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Views.SnappingTools;
using Process.NET;
using Process.NET.Memory;
using Process.NET.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Mapping_Tools.Classes.SnappingTools.DataStructure;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

namespace Mapping_Tools.Viewmodels {
    public class SnappingToolsVm
    {
        public SnappingToolsPreferences Preferences { get; }

        public ObservableCollection<RelevantObjectsGenerator> Generators { get; }
        protected readonly LayerCollection LayerCollection;
        private int _editorTime;

        private string _filter = "";
        public string Filter { get => _filter; set => SetFilter(value); }

        private bool _listenersEnabled;
        public bool ListenersEnabled {
            get => _listenersEnabled;
            set {
                _listenersEnabled = value;

                _updateTimer.IsEnabled = value;

                try {
                    _configWatcher.EnableRaisingEvents = value;
                } catch {
                    MessageBox.Show("Can't enable filesystem watcher. osu! config path is probably incorrect.", "Warning");
                }

                if (!value) {
                    _state = State.Disabled;
                    _overlay.Dispose();
                }
                else {
                    _state = State.LookingForProcess;
                }
            }
        }

        private readonly DispatcherTimer _updateTimer;
        private readonly DispatcherTimer _autoSnapTimer;

        private const double PointsBias = 3;

        private readonly CoordinateConverter _coordinateConverter;
        private readonly FileSystemWatcher _configWatcher;

        private SnappingToolsOverlay _overlay;
        private ProcessSharp _processSharp;
        private IWindow _osuWindow;

        private State _state;

        private enum State {
            Disabled,
            LookingForProcess,
            LookingForEditor,
            Active
        }

        private bool HotkeyRedrawsOverlay {
            get => Preferences.KeyDownViewMode != Preferences.KeyUpViewMode;
        }

        public SnappingToolsVm() {
            // Set up a coordinate converter for converting coordinates between screen and osu!
            _coordinateConverter = new CoordinateConverter();

            // Initialize layer collection
            LayerCollection = new LayerCollection() {
                AllGenerators = new RelevantObjectsGeneratorCollection(Generators),
                AcceptableDifference = 10
            };

            // Get preferences
            Preferences = new SnappingToolsPreferences();
            Preferences.PropertyChanged += PreferencesOnPropertyChanged;

            // Get all the RelevantObjectGenerators
            var interfaceType = typeof(RelevantObjectsGenerator);
            Generators = new ObservableCollection<RelevantObjectsGenerator>(AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(x => x.GetTypes())
              .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
              .Select(Activator.CreateInstance).OfType<RelevantObjectsGenerator>());

            // Add PropertyChanged event to all generators to listen for changes
            foreach (var gen in Generators) { gen.PropertyChanged += OnGeneratorPropertyChanged; }

            // Set up groups and filters
            var view = (CollectionView) CollectionViewSource.GetDefaultView(Generators);
            var groupDescription = new PropertyGroupDescription("GeneratorType");
            view.GroupDescriptions.Add(groupDescription);
            view.Filter = UserFilter;

            // Set up timers for responding to hotkey presses and beatmap changes
            _updateTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(100) };
            _updateTimer.Tick += UpdateTimerTick;
            _autoSnapTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            _autoSnapTimer.Tick += AutoSnapTimerTick;

            // Listen for changes in the osu! user config
            _configWatcher = new FileSystemWatcher();
            SetConfigWatcherPath(SettingsManager.Settings.OsuConfigPath);
            _configWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Attributes | NotifyFilters.CreationTime;
            _configWatcher.Changed += OnChangedConfigWatcher;

            // Listen for changes in osu! user config path in the settings
            SettingsManager.Settings.PropertyChanged += OnSettingsChanged;

            _state = State.LookingForProcess;
        }

        private void PreferencesOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "OffsetLeft":
                    _coordinateConverter.EditorBoxOffset.Left = Preferences.OffsetLeft;
                    break;
                case "OffsetTop":
                    _coordinateConverter.EditorBoxOffset.Top = Preferences.OffsetTop;
                    break;
                case "OffsetRight":
                    _coordinateConverter.EditorBoxOffset.Right = Preferences.OffsetRight;
                    break;
                case "OffsetBottom":
                    _coordinateConverter.EditorBoxOffset.Bottom = Preferences.OffsetBottom;
                    break;
                case "DebugEnabled":
                    _overlay.SetBorder(Preferences.DebugEnabled);
                    break;
            }
        }

        private void OnDraw(object sender, DrawingContext context) {
            if (IsHotkeyDown(Preferences.SnapHotkey)) {
                switch (Preferences.KeyDownViewMode) {
                    case ViewMode.Everything:
                        foreach (var obj in RelevantObjects)
                            obj.DrawYourself(context, _coordinateConverter, Preferences);
                        break;
                    case ViewMode.ParentsOnly:
                        throw new NotImplementedException();
                    case ViewMode.Nothing:
                        break;
                }
            } else {
                switch (Preferences.KeyUpViewMode) {
                    case ViewMode.Everything:
                        foreach (var obj in RelevantObjects)
                            obj.DrawYourself(context, _coordinateConverter, Preferences);
                        break;
                    case ViewMode.Nothing:
                        break;
                }
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != "OsuConfigPath") return;
            SetConfigWatcherPath(SettingsManager.Settings.OsuConfigPath);
            _coordinateConverter.ReadConfig();
        }

        private void SetConfigWatcherPath(string path) {
            try {
                _configWatcher.Path = Path.GetDirectoryName(path);
                _configWatcher.Filter = Path.GetFileName(path);
            }
            catch (Exception ex) { Console.WriteLine(@"Can't set ConfigWatcher Path/Filter: " + ex.Message); }
        }

        private void OnChangedConfigWatcher(object sender, FileSystemEventArgs e) {
            _coordinateConverter.ReadConfig();
        }

        private void OnGeneratorPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "IsActive" && _state == State.Active) {
                // Reload relevant objects when a generator gets enabled/disabled
                GenerateRelevantObjects();
                _overlay.OverlayWindow.InvalidateVisual();
            }
        }

        private void UpdateTimerTick(object sender, EventArgs e) {
            var reader = EditorReaderStuff.GetEditorReader();
            switch (_state) {
                case State.Disabled:
                    break;
                case State.LookingForProcess:
                    _updateTimer.Interval = TimeSpan.FromSeconds(5);

                    // Set up objects/overlay
                    var process = System.Diagnostics.Process.GetProcessesByName("osu!").FirstOrDefault();
                    if (process == null) {
                        return;
                    }

                    try {
                        reader.SetProcess();
                    }
                    catch (Win32Exception) {
                        return;
                    }

                    _processSharp = new ProcessSharp(process, MemoryType.Remote);
                    _osuWindow = _processSharp.WindowFactory.MainWindow;

                    _updateTimer.Interval = TimeSpan.FromSeconds(1);
                    _state = State.LookingForEditor;
                    break;
                case State.LookingForEditor:
                    _updateTimer.Interval = TimeSpan.FromSeconds(1);
                    if (reader.ProcessNeedsReload()) {
                        _state = State.LookingForProcess;
                        _overlay?.Dispose();
                        return;
                    }

                    try {
                        if (!_osuWindow.Title.EndsWith(@".osu")) {
                            return;
                        }
                    }
                    catch (ArgumentException) {
                        _state = State.LookingForProcess;
                        _overlay?.Dispose();
                        return;
                    }

                    try {
                        reader.FetchEditor();
                    }
                    catch {
                        return;
                    }

                    _overlay = new SnappingToolsOverlay { Converter = _coordinateConverter };
                    _overlay.Initialize(_osuWindow);
                    _overlay.Enable();

                    _overlay.OverlayWindow.Draw += OnDraw;

                    _updateTimer.Interval = TimeSpan.FromMilliseconds(100);
                    _state = State.Active;
                    break;
                case State.Active:
                    _updateTimer.Interval = TimeSpan.FromMilliseconds(100);

                    if (reader.ProcessNeedsReload()) {
                        ClearRelevantObjects();
                        _state = State.LookingForProcess;
                        _overlay.Dispose();
                        return;
                    }
                    if (reader.EditorNeedsReload()) {
                        ClearRelevantObjects();
                        _state = State.LookingForEditor;
                        _overlay.Dispose();
                        return;
                    }

                    var editorTime = reader.EditorTime();
                    if (editorTime != _editorTime) {
                        _editorTime = editorTime;
                        UpdateRelevantObjects();
                    }

                    _coordinateConverter.OsuWindowPosition = new Vector2(_osuWindow.X, _osuWindow.Y);
                    _overlay.Update();

                    if (!_autoSnapTimer.IsEnabled && IsHotkeyDown(Preferences.SnapHotkey)) {
                        if (HotkeyRedrawsOverlay)
                            _overlay.OverlayWindow.InvalidateVisual();
                        _autoSnapTimer.Start();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ClearRelevantObjects(bool redraw = true) {
            if (RelevantObjects.Count == 0) return;
            RelevantPoints.Clear();
            RelevantLines.Clear();
            RelevantCircles.Clear();
            RelevantObjects.Clear();
            if (redraw)
                _overlay.OverlayWindow.InvalidateVisual();
        }

        private List<HitObject> GetVisibleHitObjects()
        {
            if (!EditorReaderStuff.TryGetFullEditorReader(out var reader)) return new List<HitObject>();

            var hitObjects = EditorReaderStuff.GetHitObjects(reader);

            // Get the visible hitobjects using approach rate
            var approachTime = ApproachRateToMs(reader.ApproachRate);
            var thereAreSelected = hitObjects.Any(o => o.IsSelected);
            return hitObjects.Where(o => thereAreSelected ? o.IsSelected : _editorTime > o.Time - approachTime && _editorTime < o.EndTime + approachTime).ToList();
        }
      
        private void UpdateRelevantObjects()
        {
            var visibleObjects = GetVisibleHitObjects();
            
            var comparer = new HitObjectComparer();
            var rootLayer = LayerCollection.GetRootLayer();
            var existingHitObjects = LayerCollection.GetRootRelevantHitObjects();
            var added = visibleObjects.Where(o => !existingHitObjects.Select(x => x.HitObject).Contains(o, comparer)).ToArray();
            var removed = existingHitObjects.Where(o => !visibleObjects.Contains(o.HitObject, comparer)).ToArray();

            rootLayer.Remove(removed);
            rootLayer.Add(added.Select(o => new RelevantHitObject(o)));

            if (added.Length == 0 && removed.Length == 0)
            {
                // Root bjects didn't change. Return to avoid redundant updates
                return;
            }

            // Update relevant objects
            GenerateRelevantObjects(visibleObjects);

            _overlay.OverlayWindow.InvalidateVisual();
        }

        private static double ApproachRateToMs(double approachRate) {
            if (approachRate < 5) {
                return 1800 - 120 * approachRate;
            }

            return 1200 - 150 * (approachRate - 5);
        }

        private void AutoSnapTimerTick(object sender, EventArgs e) {
            if (!IsHotkeyDown(Preferences.SnapHotkey)) {
                if (HotkeyRedrawsOverlay)
                    _overlay?.OverlayWindow.InvalidateVisual();
                _autoSnapTimer.Stop();
                return;
            }

            // Move the cursor's Position
            // System.Windows.Forms.Cursor.Position = new Point();
            var cursorPoint = System.Windows.Forms.Cursor.Position;
            // CONVERT THIS CURSOR POSITION TO EDITOR POSITION
            var cursorPos = _coordinateConverter.ScreenToEditorCoordinate(new Vector2(cursorPoint.X, cursorPoint.Y));

            if (RelevantObjects.Count == 0)
                return;

            IRelevantDrawable nearest = null;
            var smallestDistance = double.PositiveInfinity;
            foreach (var o in RelevantObjects) {
                var dist = o.DistanceTo(cursorPos);
                if (o is RelevantPoint) // Prioritize points to be able to snap to intersections
                    dist -= PointsBias;
                if (!(dist < smallestDistance)) continue;
                smallestDistance = dist;
                nearest = o;
            }

            // CONVERT THIS TO CURSOR POSITION
            if (nearest == null) return;
            var nearestPoint = _coordinateConverter.EditorToScreenCoordinate(nearest.NearestPoint(cursorPos));
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int) Math.Round(nearestPoint.X), (int) Math.Round(nearestPoint.Y));
        }

        private static bool IsHotkeyDown(Hotkey hotkey) {
            if (hotkey == null)
                return false;
            if (!Keyboard.IsKeyDown(hotkey.Key))
                return false;
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Alt) && !(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
                return false;
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Control) && !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                return false;
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Shift) && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                return false;
            return !hotkey.Modifiers.HasFlag(ModifierKeys.Windows) || (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin));
        }

        private bool UserFilter(object item) {
            if (string.IsNullOrEmpty(Filter))
                return true;
            return ((RelevantObjectsGenerator)item).Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetFilter(string value) {
            _filter = value;
            CollectionViewSource.GetDefaultView(Generators).Refresh();
        }
    }
}
