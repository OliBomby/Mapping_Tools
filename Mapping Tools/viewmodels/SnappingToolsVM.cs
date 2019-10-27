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

        private IRelevantObject _lastSnappedRelevantObject;
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

        private bool HotkeyDownRedrawsOverlay => Preferences.KeyDownViewMode != Preferences.KeyUpViewMode && !SnapChangeRedrawsOverlay;

        private bool HotkeyUpRedrawsOverlay => Preferences.KeyDownViewMode != Preferences.KeyUpViewMode;

        private bool SnapChangeRedrawsOverlay => Preferences.KeyDownViewMode.HasFlag(ViewMode.Parents) ||
                                                  Preferences.KeyDownViewMode.HasFlag(ViewMode.Children);

        public SnappingToolsVm() {
            // Set up a coordinate converter for converting coordinates between screen and osu!
            _coordinateConverter = new CoordinateConverter();

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

            // Initialize layer collection
            LayerCollection = new LayerCollection(new RelevantObjectsGeneratorCollection(Generators), 2);
            LayerCollection.SetInceptionLevel(4);
            
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
                case "AcceptableDifference":
                    LayerCollection.AcceptableDifference = Preferences.AcceptableDifference;
                    break;
                case "DebugEnabled":
                    _overlay.SetBorder(Preferences.DebugEnabled);
                    break;
            }
        }

        private void OnDraw(object sender, DrawingContext context) {
            if (IsHotkeyDown(Preferences.SnapHotkey)) {
                // Handle key down rendering
                if (Preferences.KeyDownViewMode.HasFlag(ViewMode.Everything)) {
                    foreach (var relevantDrawable in LayerCollection.GetAllRelevantDrawables()) {
                        relevantDrawable.DrawYourself(context, _coordinateConverter, Preferences);
                    }
                    // It has already drawn everything so return
                    return;
                }

                var objectsToRender = new HashSet<IRelevantObject>();

                if (Preferences.KeyDownViewMode.HasFlag(ViewMode.Parents) && _lastSnappedRelevantObject != null) {
                    // Get the parents of the relevant object which is being snapped to
                    objectsToRender.UnionWith(_lastSnappedRelevantObject.GetParentage());
                }

                if (Preferences.KeyDownViewMode.HasFlag(ViewMode.Children) && _lastSnappedRelevantObject != null) {
                    // Get the parents of the relevant object which is being snapped to
                    objectsToRender.UnionWith(_lastSnappedRelevantObject.GetDescendants());
                }

                foreach (var relevantObject in objectsToRender) {
                    if (relevantObject is IRelevantDrawable relevantDrawable) {
                        relevantDrawable.DrawYourself(context, _coordinateConverter, Preferences);
                    }
                }
            } else {
                // Handle key up rendering
                if (Preferences.KeyUpViewMode.HasFlag(ViewMode.Everything)) {
                    foreach (var relevantDrawable in LayerCollection.GetAllRelevantDrawables()) {
                        relevantDrawable.DrawYourself(context, _coordinateConverter, Preferences);
                    }
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
            switch (e.PropertyName) {
                case "IsActive":
                    if (_state == State.Active) {
                        // Reload relevant objects when a generator gets enabled/disabled
                        if (((RelevantObjectsGenerator)sender).IsActive) {
                            // Generate new objects for all layers
                            LayerCollection.GetRootLayer().GenerateNewObjects(true);
                        } else {
                            // Delete all relevant objects generated by this generator
                            foreach (var objectLayerObject in LayerCollection.ObjectLayers.SelectMany(objectLayer => objectLayer.Objects.Values)) {
                                for (var i = 0; i < objectLayerObject.Count; i++) {
                                    if (objectLayerObject[i].Generator != sender) continue;
                                    objectLayerObject[i].Dispose();
                                    i--;
                                }
                            }
                        }

                        // Redraw the overlay
                        _overlay.OverlayWindow.InvalidateVisual();
                    }

                    break;
                case "IsConcurrent":
                    if (_state == State.Active) {
                        // Delete all relevant objects generated by this generator
                        foreach (var objectLayerObject in LayerCollection.ObjectLayers.SelectMany(objectLayer => objectLayer.Objects.Values)) {
                            for (var i = 0; i < objectLayerObject.Count; i++) {
                                if (objectLayerObject[i].Generator != sender) continue;
                                objectLayerObject[i].Dispose();
                                i--;
                            }
                        }

                        // Generate new objects for all layers
                        LayerCollection.GetRootLayer().GenerateNewObjects(true);

                        // Redraw the overlay
                        _overlay.OverlayWindow.InvalidateVisual();
                    }

                    break;
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
                        _state = State.LookingForProcess;
                        _overlay.Dispose();
                        return;
                    }
                    if (reader.EditorNeedsReload()) {
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
                        // Update overlay but not on parents only view mode, because that one updates on his own terms
                        if (HotkeyDownRedrawsOverlay)
                            _overlay.OverlayWindow.InvalidateVisual();

                        // Reset last snapped relevant object to trigger an overlay update in the snap timer tick
                        _lastSnappedRelevantObject = null;
                        
                        _autoSnapTimer.Start();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private List<HitObject> GetVisibleHitObjects()
        {
            if (!EditorReaderStuff.TryGetFullEditorReader(out var reader)) return new List<HitObject>();

            var hitObjects = EditorReaderStuff.GetHitObjects(reader);

            // Get the visible hitobjects using approach rate
            var approachTime = Beatmap.ApproachRateToMs(reader.ApproachRate);
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

            _overlay.OverlayWindow.InvalidateVisual();
        }

        private void AutoSnapTimerTick(object sender, EventArgs e) {
            if (!IsHotkeyDown(Preferences.SnapHotkey)) {
                if (HotkeyUpRedrawsOverlay)
                    _overlay?.OverlayWindow.InvalidateVisual();
                _autoSnapTimer.Stop();
                return;
            }

            // Move the cursor's Position
            // System.Windows.Forms.Cursor.Position = new Point();
            var cursorPoint = System.Windows.Forms.Cursor.Position;
            // CONVERT THIS CURSOR POSITION TO EDITOR POSITION
            var cursorPos = _coordinateConverter.ScreenToEditorCoordinate(new Vector2(cursorPoint.X, cursorPoint.Y));

            // Get all the relevant drawables
            var drawables = LayerCollection.GetAllRelevantDrawables().ToArray();

            if (drawables.Length == 0)
                return;

            // Get the relevant object nearest to the cursor
            IRelevantDrawable nearest = null;
            var smallestDistance = double.PositiveInfinity;
            foreach (var o in drawables) {
                var dist = o.DistanceTo(cursorPos);
                if (o is RelevantPoint) // Prioritize points to be able to snap to intersections
                    dist -= PointsBias;

                if (!(dist < smallestDistance)) continue;
                smallestDistance = dist;
                nearest = o;
            }

            // Update overlay if the last snapped changed and parentview is on
            if (nearest != _lastSnappedRelevantObject && SnapChangeRedrawsOverlay) {
                // Set the last snapped relevant object
                _lastSnappedRelevantObject = nearest;
                // Update overlay
                _overlay.OverlayWindow.InvalidateVisual();
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
