using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
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
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

namespace Mapping_Tools.Viewmodels {
    public class SnappingToolsVm : IDisposable
    {
        public SnappingToolsPreferences Preferences { get; }

        public ObservableCollection<RelevantObjectsGenerator> Generators { get; }
        protected readonly LayerCollection LayerCollection;

        private IRelevantObject _lastSnappedRelevantObject;
        private readonly List<IRelevantDrawable> _lastSelectedRelevantDrawables;
        private readonly List<IRelevantDrawable> _lastLockedRelevantDrawables;
        private readonly List<IRelevantDrawable> _lastInheritRelevantDrawables;
        private bool _selectedToggle;
        private bool _lockedToggle;
        private bool _inheritableToggle;

        private int _editorTime;
        private bool _osuActivated;

        private string _filter = "";
        public string Filter { get => _filter; set => SetFilter(value); }

        private readonly DispatcherTimer _updateTimer;
        private readonly DispatcherTimer _autoSnapTimer;
        private readonly DispatcherTimer _selectTimer;
        private readonly DispatcherTimer _lockTimer;
        private readonly DispatcherTimer _inheritTimer;

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
            foreach (var gen in Generators) { gen.Settings.PropertyChanged += OnGeneratorSettingsPropertyChanged; }

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
            _selectTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            _selectTimer.Tick += SelectTimerTick;
            _lockTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            _lockTimer.Tick += LockTimerTick;
            _inheritTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            _inheritTimer.Tick += InheritTimerTick;

            // Setup some lists for the hotkey controls
            _lastSelectedRelevantDrawables = new List<IRelevantDrawable>();
            _lastLockedRelevantDrawables = new List<IRelevantDrawable>();
            _lastInheritRelevantDrawables = new List<IRelevantDrawable>();

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
                case "InceptionLevel":
                    LayerCollection.SetInceptionLevel(Preferences.InceptionLevel);
                    _overlay.OverlayWindow.InvalidateVisual();
                    break;
                case "GeneratorSettings":
                    Preferences.ApplyGeneratorSettings(Generators);
                    break;
            }
        }

        public SnappingToolsPreferences GetPreferences() {
            Preferences.SaveGeneratorSettings(Generators);
            return Preferences;
        }

        public void SetPreferences(SnappingToolsPreferences preferences) {
            preferences?.CopyTo(Preferences);
        }

        private void OnDraw(object sender, DrawingContext context) {
            //Console.WriteLine($@"Drawable count: {LayerCollection.GetAllRelevantDrawables().Count()}");
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

        private void OnGeneratorSettingsPropertyChanged(object sender, PropertyChangedEventArgs e) {
            var settings = (GeneratorSettings) sender;
            var generator = settings.Generator;

            switch (e.PropertyName) {
                case "IsActive":
                    if (_state == State.Active) {
                        // Reload relevant objects when a generator gets enabled/disabled
                        if (settings.IsActive) {
                            // Generate new objects for all layers
                            LayerCollection.GetRootLayer().GenerateNewObjects(true);
                        } else {
                            // Delete all relevant objects generated by this generator
                            foreach (var objectLayerObject in LayerCollection.ObjectLayers.SelectMany(objectLayer => objectLayer.Objects.Values)) {
                                for (var i = 0; i < objectLayerObject.Count; i++) {
                                    if (objectLayerObject[i].Generator != generator) continue;
                                    objectLayerObject[i].Dispose();
                                    i--;
                                }
                            }
                        }

                        // Redraw the overlay
                        _overlay.OverlayWindow.InvalidateVisual();
                    }

                    break;
                case "IsSequential":
                    if (_state == State.Active) {
                        // Delete all relevant objects generated by this generator
                        foreach (var objectLayerObject in LayerCollection.ObjectLayers.SelectMany(objectLayer => objectLayer.Objects.Values)) {
                            for (var i = 0; i < objectLayerObject.Count; i++) {
                                if (objectLayerObject[i].Generator != generator) continue;
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
                case "IsDeep":
                    if (_state == State.Active) {
                        // Delete all relevant objects generated by this generator
                        foreach (var objectLayerObject in LayerCollection.ObjectLayers.SelectMany(objectLayer => objectLayer.Objects.Values)) {
                            for (var i = 0; i < objectLayerObject.Count; i++) {
                                if (objectLayerObject[i].Generator != generator) continue;
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
                    catch {
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

                    // Get editor time
                    var editorTime = reader.EditorTime();
                    // Get osu! state
                    var osuActivated = _osuWindow.IsActivated;

                    // Handle updating of relevant objects
                    switch (Preferences.UpdateMode) {
                        case UpdateMode.AnyChange:
                            UpdateRelevantObjects();
                            break;
                        case UpdateMode.TimeChange:
                            if (_editorTime != editorTime) {
                                UpdateRelevantObjects();
                            }

                            break;
                        case UpdateMode.HotkeyDown:
                            if (IsHotkeyDown(Preferences.SnapHotkey)) {
                                UpdateRelevantObjects();
                            }

                            break;
                        case UpdateMode.OsuActivated:
                            // Before not activated and after activated
                            if (!_osuActivated && osuActivated) {
                                UpdateRelevantObjects();
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // Update old editor time variable
                    _editorTime = editorTime;
                    // Update old osu activated variable
                    _osuActivated = osuActivated;

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
                    if (!_selectTimer.IsEnabled && IsHotkeyDown(Preferences.SelectHotkey)) {
                        _selectTimer.Start();
                    }
                    if (!_lockTimer.IsEnabled && IsHotkeyDown(Preferences.LockHotkey)) {
                        _lockTimer.Start();
                    }
                    if (!_inheritTimer.IsEnabled && IsHotkeyDown(Preferences.InheritHotkey)) {
                        _inheritTimer.Start();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private List<HitObject> GetHitObjects()
        {
            if (!EditorReaderStuff.TryGetFullEditorReader(out var reader)) return new List<HitObject>();

            var hitObjects = EditorReaderStuff.GetHitObjects(reader);

            // Get the visible hitobjects using approach rate
            var approachTime = Beatmap.ApproachRateToMs(reader.ApproachRate);

            switch (Preferences.SelectedHitObjectMode) {
                case SelectedHitObjectMode.AllwaysAllVisible:
                    return hitObjects.Where(o => _editorTime > o.Time - approachTime && _editorTime < o.EndTime + approachTime).ToList();
                case SelectedHitObjectMode.VisibleOrSelected:
                    var thereAreSelected = hitObjects.Any(o => o.IsSelected);
                    return hitObjects.Where(o => thereAreSelected ? o.IsSelected : _editorTime > o.Time - approachTime && _editorTime < o.EndTime + approachTime).ToList();
                case SelectedHitObjectMode.OnlySelected:
                    return hitObjects.Where(o => o.IsSelected).ToList();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
      
        private void UpdateRelevantObjects()
        {
            var hitObjects = GetHitObjects();
            
            var comparer = new HitObjectComparer(true);
            var rootLayer = LayerCollection.GetRootLayer();
            var existingHitObjects = LayerCollection.GetRootRelevantHitObjects();
            var added = hitObjects.Where(o => !existingHitObjects.Select(x => x.HitObject).Contains(o, comparer)).ToArray();
            var removed = existingHitObjects.Where(o => !hitObjects.Contains(o.HitObject, comparer)).ToArray();

            // Dispose of all the removed hit objects
            foreach (var relevantHitObject in removed) {
                relevantHitObject.Dispose();
            }
            // Add new hit objects to root layer
            rootLayer.Add(added.Select(o => new RelevantHitObject(o)));

            if (added.Length == 0 && removed.Length == 0)
            {
                // Root objects didn't change. Return to avoid redundant updates
                return;
            }

            _overlay.OverlayWindow.InvalidateVisual();
        }

        private void AutoSnapTimerTick(object sender, EventArgs e) {
            // Check timer stop
            if (!IsHotkeyDown(Preferences.SnapHotkey)) {
                if (HotkeyUpRedrawsOverlay)
                    _overlay?.OverlayWindow.InvalidateVisual();
                _autoSnapTimer.Stop();
                return;
            }
            
            // Get nearest drawable
            var cursorPos = GetCursorPosition();
            var nearest = GetNearestDrawable(cursorPos);

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

        private void SelectTimerTick(object sender, EventArgs e) {
            // Check timer stop
            if (!IsHotkeyDown(Preferences.SelectHotkey)) {
                _selectTimer.Stop();
                _lastSelectedRelevantDrawables.Clear();
                return;
            }

            // Get nearest drawable
            var cursorPos = GetCursorPosition();
            var nearest = GetNearestDrawable(cursorPos);

            if (nearest == null) return;

            // Check if this drawable was already handled with this keypress
            if (_lastSelectedRelevantDrawables.Contains(nearest)) return;

            // Get the selecting mode
            if (_lastSelectedRelevantDrawables.Count == 0) {
                _selectedToggle = !nearest.IsSelected;
            }

            // Set the selected variable of the nearest drawable
            nearest.IsSelected = _selectedToggle;

            // Add nearest drawable to the list so it doesnt get toggled later
            _lastSelectedRelevantDrawables.Add(nearest);

            // Redraw overlay
            _overlay.OverlayWindow.InvalidateVisual();
        }

        private void LockTimerTick(object sender, EventArgs e) {
            // Check timer stop
            if (!IsHotkeyDown(Preferences.LockHotkey)) {
                _lockTimer.Stop();
                _lastLockedRelevantDrawables.Clear();
                return;
            }

            // Get nearest drawable
            var cursorPos = GetCursorPosition();
            var nearest = GetNearestDrawable(cursorPos);

            if (nearest == null) return;

            // Check if this drawable was already handled with this keypress
            if (_lastLockedRelevantDrawables.Contains(nearest)) return;

            // Get the locking mode
            if (_lastLockedRelevantDrawables.Count == 0) {
                _lockedToggle = !nearest.IsLocked;
            }

            // Set the locked variable of the nearest drawable
            nearest.IsLocked = _lockedToggle;

            // Add nearest drawable to the list so it doesnt get toggled later
            _lastLockedRelevantDrawables.Add(nearest);

            // Redraw overlay
            _overlay.OverlayWindow.InvalidateVisual();
        }

        private void InheritTimerTick(object sender, EventArgs e) {
            // Check timer stop
            if (!IsHotkeyDown(Preferences.InheritHotkey)) {
                _inheritTimer.Stop();
                _lastInheritRelevantDrawables.Clear();
                return;
            }

            // Get nearest drawable
            var cursorPos = GetCursorPosition();
            var nearest = GetNearestDrawable(cursorPos);

            if (nearest == null) return;

            // Check if this drawable was already handled with this keypress
            if (_lastInheritRelevantDrawables.Contains(nearest)) return;

            // Get the inherit mode
            if (_lastInheritRelevantDrawables.Count == 0) {
                _inheritableToggle = !nearest.IsInheritable;
            }

            // Set the inheritable variable of the nearest drawable
            nearest.IsInheritable = _inheritableToggle;

            // Add nearest drawable to the list so it doesnt get toggled later
            _lastInheritRelevantDrawables.Add(nearest);

            // Redraw overlay
            _overlay.OverlayWindow.InvalidateVisual();
        }

        private IRelevantDrawable GetNearestDrawable(Vector2 cursorPos) {
            // Get all the relevant drawables
            var drawables = LayerCollection.GetAllRelevantDrawables().ToArray();

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

            return nearest;
        }

        private Vector2 GetCursorPosition() {
            // System.Windows.Forms.Cursor.Position = new Point();
            var cursorPoint = System.Windows.Forms.Cursor.Position;
            // CONVERT THIS CURSOR POSITION TO EDITOR POSITION
            var cursorPos = _coordinateConverter.ScreenToEditorCoordinate(new Vector2(cursorPoint.X, cursorPoint.Y));

            return cursorPos;
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

        public void Dispose() {
            _overlay?.Dispose();
            _configWatcher?.Dispose();
            _processSharp?.Dispose();
            _osuWindow?.Dispose();
        }

        public void Activate() {
            _updateTimer.IsEnabled = true;

            try {
                _configWatcher.EnableRaisingEvents = true;
            } catch {
                MessageBox.Show("Can't enable filesystem watcher. osu! config path is probably incorrect.", "Warning");
            }

            _state = State.LookingForProcess;
        }

        public void Deactivate() {
            _updateTimer.IsEnabled = false;

            try {
                _configWatcher.EnableRaisingEvents = false;
            } catch {
                MessageBox.Show("Can't disable filesystem watcher. osu! config path is probably incorrect.", "Warning");
            }

            _state = State.Disabled;
            _overlay?.Dispose();
        }
    }
}
