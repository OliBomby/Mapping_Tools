using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SnappingTools.DataStructure;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using Mapping_Tools.Classes.SnappingTools.Serialization;
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
using Editor_Reader;
using Mapping_Tools.Classes;
using Mapping_Tools.Components.Domain;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

namespace Mapping_Tools.Viewmodels {
    public class SnappingToolsVm : IDisposable
    {
        #region fields

        public SnappingToolsProject Project { get; set; }
        protected SnappingToolsPreferences Preferences => Project.CurrentPreferences;

        public ObservableCollection<RelevantObjectsGenerator> Generators { get; }
        protected readonly LayerCollection LayerCollection;

        public CommandImplementation SelectedToggleCommand { get; set; }
        public CommandImplementation LockedToggleCommand { get; set; }
        public CommandImplementation InheritableToggleCommand { get; set; }

        private IRelevantObject _lastSnappedRelevantObject;
        private readonly List<IRelevantDrawable> _lastSelectedRelevantDrawables;
        private readonly List<IRelevantDrawable> _lastLockedRelevantDrawables;
        private readonly List<IRelevantDrawable> _lastInheritRelevantDrawables;
        private bool _selectedToggle;
        private bool _lockedToggle;
        private bool _inheritableToggle;

        private int _editorTime;
        private bool _osuActivated;
        private int _fetchEditorFails;

        private string _filter = "";
        public string Filter { get => _filter; set => SetFilter(value); }

        private readonly DispatcherTimer _updateTimer;
        private readonly DispatcherTimer _autoSnapTimer;
        private readonly DispatcherTimer _selectTimer;
        private readonly DispatcherTimer _lockTimer;
        private readonly DispatcherTimer _inheritTimer;

        private const double PointsBias = 3;
        private const double SpecialBias = 3;

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

        private bool SnapChangeRedrawsOverlay => Preferences.KeyDownViewMode.HasFlag(ViewMode.Parents) || Preferences.KeyDownViewMode.HasFlag(ViewMode.DirectParents) ||
                                                 Preferences.KeyDownViewMode.HasFlag(ViewMode.Children) || Preferences.KeyDownViewMode.HasFlag(ViewMode.DirectChildren);

        #endregion

        #region default constructor
        public SnappingToolsVm() {
            // Set up a coordinate converter for converting coordinates between screen and osu!
            _coordinateConverter = new CoordinateConverter();
            
            // Initialize project and preferences
            Project = new SnappingToolsProject();
            Project.PropertyChanged += ProjectOnPropertyChanged;

            Preferences.PropertyChanged += PreferencesOnPropertyChanged;

            // Get all the RelevantObjectGenerators
            var interfaceType = typeof(RelevantObjectsGenerator);
            Generators = new ObservableCollection<RelevantObjectsGenerator>(AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(x => x.GetTypes())
              .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
              .Select(Activator.CreateInstance).OfType<RelevantObjectsGenerator>());

            // Set project stuff
            Project.SetGenerators(Generators);

            // Add PropertyChanged event to all generators to listen for changes
            foreach (var gen in Generators) { gen.Settings.PropertyChanged += OnGeneratorSettingsPropertyChanged; }

            // Set up groups and filters
            var view = (CollectionView) CollectionViewSource.GetDefaultView(Generators);
            var groupDescription = new PropertyGroupDescription("GeneratorType")
                {CustomSort = new GeneratorGroupComparer()};
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

            // Setup commands
            InitializeCommands();

            // Listen for changes in the osu! user config
            _configWatcher = new FileSystemWatcher();
            SetConfigWatcherPath(SettingsManager.Settings.OsuConfigPath);
            _configWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Attributes | NotifyFilters.CreationTime;
            _configWatcher.Changed += OnChangedConfigWatcher;

            // Listen for changes in osu! user config path in the settings
            SettingsManager.Settings.PropertyChanged += OnSettingsChanged;

            _state = State.LookingForProcess;
        }
        #endregion
        
        #region main loop
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
                    catch (Exception ex) {
                        _fetchEditorFails++;
                        if (_fetchEditorFails <= 3) return;

                        MessageBox.Show("Editor Reader seems to be failing a lot. Try restarting osu! and opening Geometry Dashboard again.");
                        ex.Show();

                        _updateTimer.IsEnabled = false;
                        return;
                    }

                    _overlay = new SnappingToolsOverlay { Converter = _coordinateConverter };
                    _overlay.Initialize(_osuWindow);
                    _overlay.Enable();

                    _overlay.SetBorder(Preferences.DebugEnabled);

                    _overlay.OverlayWindow.Draw += OnDraw;

                    _updateTimer.Interval = TimeSpan.FromMilliseconds(100);
                    _state = State.Active;
                    break;
                case State.Active:
                    _updateTimer.Interval = TimeSpan.FromMilliseconds(100);

                    // It successfully fetched editor so editor reader is probably working
                    _fetchEditorFails = 0;

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
                        case UpdateMode.OsuActivated:
                            // Before not activated and after activated
                            if (!_osuActivated && osuActivated) {
                                UpdateRelevantObjects();
                            }

                            break;
                        case UpdateMode.HotkeyDown:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (IsHotkeyDown(Preferences.RefreshHotkey)) {
                        UpdateRelevantObjects();
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
        #endregion

        #region geometry dashboard helpers

        private Point GetRelativeDpiPoint(Vector2 pos, Vector2 offset) {
            var dpi = _coordinateConverter.ToDpi(_coordinateConverter.EditorToRelativeCoordinate(pos));
            return new Point(dpi.X + offset.X, dpi.Y + offset.Y);
        }
        
        private void OnDraw(object sender, DrawingContext context) {
            if (Preferences.VisiblePlayfieldBoundary) {
                const double thickness = 2;
                context.DrawRectangle(null, new Pen(Brushes.DarkOrange, thickness), 
                    new Rect(GetRelativeDpiPoint(new Vector2(-65, -57), new Vector2(-thickness / 2)), 
                        GetRelativeDpiPoint(new Vector2(576, 423), new Vector2(thickness / 2))));
            }

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
                    objectsToRender.UnionWith(_lastSnappedRelevantObject.GetParentage(int.MaxValue));
                } else if (Preferences.KeyDownViewMode.HasFlag(ViewMode.DirectParents) && _lastSnappedRelevantObject != null) {
                    objectsToRender.UnionWith(_lastSnappedRelevantObject.GetParentage(1));
                }

                if (Preferences.KeyDownViewMode.HasFlag(ViewMode.Children) && _lastSnappedRelevantObject != null) {
                    // Get the parents of the relevant object which is being snapped to
                    objectsToRender.UnionWith(_lastSnappedRelevantObject.GetDescendants(int.MaxValue));
                } else if (Preferences.KeyDownViewMode.HasFlag(ViewMode.DirectChildren) && _lastSnappedRelevantObject != null) {
                    objectsToRender.UnionWith(_lastSnappedRelevantObject.GetDescendants(1));
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
        
        private List<HitObject> GetHitObjects() {
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            if (reader == null)
                return new List<HitObject>();

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


        private IRelevantDrawable GetNearestDrawable(Vector2 cursorPos, bool specialPriority = false) {
            // Get all the relevant drawables
            var drawables = LayerCollection.GetAllRelevantDrawables().ToArray();

            // Get the relevant object nearest to the cursor
            IRelevantDrawable nearest = null;
            var smallestDistance = double.PositiveInfinity;
            foreach (var o in drawables) {
                var dist = o.DistanceTo(cursorPos);
                if (o is RelevantPoint) {
                    // Prioritize points to be able to snap to intersections
                    dist -= PointsBias;
                }
                if (specialPriority && (o.IsSelected || o.IsLocked)) {
                    // Prioritize selected and locked to be able to unselect them easily
                    dist -= SpecialBias;
                }

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
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Alt) != (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
                return false;
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Control) != (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                return false;
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Shift) != (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                return false;
            return hotkey.Modifiers.HasFlag(ModifierKeys.Windows) == (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin));
        }
        #endregion

        #region hotkey loop ticks
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
            var nearest = GetNearestDrawable(cursorPos, true);

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
            var nearest = GetNearestDrawable(cursorPos, true);

            if (nearest == null) return;

            // Check if this drawable was already handled with this keypress
            if (_lastLockedRelevantDrawables.Contains(nearest)) return;

            // Get the locking mode
            if (_lastLockedRelevantDrawables.Count == 0) {
                _lockedToggle = !nearest.IsLocked;
            }

            // Set the locked variable of the nearest drawable
            if (_lockedToggle) {
                if (!nearest.IsLocked) {
                    LayerCollection.LockedLayer.Add(nearest.GetLockedRelevantObject());
                    LayerCollection.LockedLayer.NextLayer?.GenerateNewObjects(true);
                }
            } else {
                if (nearest.IsLocked)
                    nearest.Dispose();
            }

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
        
        #endregion

        #region change listeners

        private void ProjectOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != "CurrentPreferences") return;
            
            UpdateEverything();
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
                    _overlay?.SetBorder(Preferences.DebugEnabled);
                    break;
                case "VisiblePlayfieldBoundary":
                    _overlay?.OverlayWindow.InvalidateVisual();
                    break;
                case "InceptionLevel":
                    LayerCollection.SetInceptionLevel(Preferences.InceptionLevel);
                    _overlay?.OverlayWindow.InvalidateVisual();
                    break;
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != "OsuConfigPath") return;
            SetConfigWatcherPath(SettingsManager.Settings.OsuConfigPath);
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
                default:
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

        #endregion

        #region osu config watcher

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

        #endregion

        #region UI helpers
        
        private bool UserFilter(object item) {
            if (string.IsNullOrEmpty(Filter))
                return true;
            return ((RelevantObjectsGenerator)item).Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetFilter(string value) {
            _filter = value;
            CollectionViewSource.GetDefaultView(Generators).Refresh();
        }
        

        #endregion
        
        #region command makers

        private void InitializeCommands() {
            SelectedToggleCommand = new CommandImplementation(_ => {
                var virtualObjects = LayerCollection.GetAllRelevantDrawables();
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    foreach (var relevantObject in virtualObjects) {
                        relevantObject.AutoPropagate = false;
                        relevantObject.IsSelected = true;
                        relevantObject.AutoPropagate = true;
                    }
                } else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                    foreach (var relevantObject in virtualObjects) {
                        relevantObject.AutoPropagate = false;
                        relevantObject.IsSelected = false;
                        relevantObject.AutoPropagate = true;
                    }
                } else {
                    foreach (var relevantObject in virtualObjects) {
                        relevantObject.AutoPropagate = false;
                        relevantObject.IsSelected = !relevantObject.IsSelected;
                        relevantObject.AutoPropagate = true;
                    }
                }
                LayerCollection.GetRootLayer().GenerateNewObjects(true);
                _overlay.OverlayWindow.InvalidateVisual();
            });
            LockedToggleCommand = new CommandImplementation(_ => {
                var virtualObjects = LayerCollection.GetAllRelevantDrawables();
                var lockedObjectsToDispose = new List<IRelevantObject>();
                var lockedObjectsToAdd = new List<IRelevantObject>();
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    lockedObjectsToAdd.AddRange(from relevantObject in virtualObjects where !relevantObject.IsLocked select relevantObject.GetLockedRelevantObject());
                } else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                    lockedObjectsToDispose.AddRange(virtualObjects.Where(relevantObject => relevantObject.IsLocked));
                } else {
                    foreach (var relevantObject in virtualObjects) {
                        if (relevantObject.IsLocked) {
                            lockedObjectsToDispose.Add(relevantObject);
                        } else {
                            lockedObjectsToAdd.Add(relevantObject.GetLockedRelevantObject());
                        }
                    }
                }

                foreach (var relevantObject in lockedObjectsToAdd) {
                    LayerCollection.LockedLayer.Add(relevantObject.GetLockedRelevantObject());
                }
                foreach (var relevantObject in lockedObjectsToDispose) {
                    relevantObject.Dispose();
                }
                LayerCollection.GetRootLayer().GenerateNewObjects(true);
                _overlay.OverlayWindow.InvalidateVisual();
            });
            InheritableToggleCommand = new CommandImplementation(_ => {
                var virtualObjects = LayerCollection.GetAllRelevantDrawables();
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    foreach (var relevantObject in virtualObjects) {
                        relevantObject.AutoPropagate = false;
                        relevantObject.IsInheritable = true;
                        relevantObject.AutoPropagate = true;
                    }
                } else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                    foreach (var relevantObject in virtualObjects) {
                        relevantObject.AutoPropagate = false;
                        relevantObject.IsInheritable = false;
                        relevantObject.AutoPropagate = true;
                    }
                } else {
                    foreach (var relevantObject in virtualObjects) {
                        relevantObject.AutoPropagate = false;
                        relevantObject.IsInheritable = !relevantObject.IsInheritable;
                        relevantObject.AutoPropagate = true;
                    }
                }
                LayerCollection.GetRootLayer().GenerateNewObjects(true);
                _overlay.OverlayWindow.InvalidateVisual();
            });
        }

        #endregion

        #region serialization stuff

        public void UpdateEverything() {
            _coordinateConverter.EditorBoxOffset.Left = Preferences.OffsetLeft;
            _coordinateConverter.EditorBoxOffset.Top = Preferences.OffsetTop;
            _coordinateConverter.EditorBoxOffset.Right = Preferences.OffsetRight;
            _coordinateConverter.EditorBoxOffset.Bottom = Preferences.OffsetBottom;
            LayerCollection.AcceptableDifference = Preferences.AcceptableDifference;
            LayerCollection.SetInceptionLevel(Preferences.InceptionLevel);
            if (_overlay != null) {
                _overlay.SetBorder(Preferences.DebugEnabled);
                _overlay.OverlayWindow.InvalidateVisual();
            }
        }

        public void SetProject(SnappingToolsProject project) {
            // Dispose old project
            Project?.Dispose();

            // Load in new project
            LoadNewProject(project);
        }

        private void LoadNewProject(SnappingToolsProject project) {
            Project = project;
            Project.SetGenerators(Generators);
            Project.Activate();
            Project.PropertyChanged += ProjectOnPropertyChanged;
            UpdateEverything();
        }

        public SnappingToolsProject GetProject() {
            return Project.GetThis();
        }

        #endregion

        #region tool management helpers

        public void Dispose() {
            _updateTimer.Stop();
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
                MessageBox.Show("Can't enable filesystem watcher. osu! config path is probably incorrect. You can fix this in the Options > Preferences.", "Warning");
            }

            _state = State.LookingForProcess;

            Project?.Activate();
        }

        public void Deactivate() {
            if (Preferences.KeepRunning) return;

            _updateTimer.IsEnabled = false;

            try {
                _configWatcher.EnableRaisingEvents = false;
            } catch {
                MessageBox.Show("Can't disable filesystem watcher. osu! config path is probably incorrect. You can fix this in the Options > Preferences.", "Warning");
            }

            _state = State.Disabled;
            _overlay?.Dispose();

            Project?.Deactivate();
        }
        #endregion
    }
}
