using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.Domain;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools.SnappingTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;
using MessageBox = System.Windows.MessageBox;
using System.Diagnostics;

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

        private IRelevantObject lastSnappedRelevantObject;
        private HitObject heldHitObject;
        private HitObject[] heldHitObjects;
        private Vector2 heldHitObjectMouseOffset;
        private readonly List<IRelevantDrawable> lastSelectedRelevantDrawables;
        private readonly List<IRelevantDrawable> lastLockedRelevantDrawables;
        private readonly List<IRelevantDrawable> lastInheritRelevantDrawables;
        private bool selectedToggle;
        private bool lockedToggle;
        private bool inheritableToggle;
        private bool unlockedSomething;

        private int editorTime;
        private bool osuActivated;
        private int fetchEditorFails;

        private string filter = "";
        public string Filter { get => filter; set => SetFilter(value); }

        private readonly DispatcherTimer updateTimer;
        private readonly DispatcherTimer autoSnapTimer;
        private readonly DispatcherTimer selectTimer;
        private readonly DispatcherTimer lockTimer;
        private readonly DispatcherTimer inheritTimer;

        private const double RelevancyBias = 4;
        private const double PointsBias = 3;
        private const double SpecialBias = 2;
        private const double SelectionRange = 80;

        private readonly CoordinateConverter coordinateConverter;
        private readonly FileSystemWatcher configWatcher;

        private SnappingToolsOverlay overlay;
        private ProcessSharp processSharp;
        private IWindow osuWindow;

        private State state;

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
            coordinateConverter = new CoordinateConverter();
            
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
            updateTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(100) };
            updateTimer.Tick += UpdateTimerTick;
            autoSnapTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            autoSnapTimer.Tick += AutoSnapTimerTick;
            selectTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            selectTimer.Tick += SelectTimerTick;
            lockTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            lockTimer.Tick += LockTimerTick;
            inheritTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            inheritTimer.Tick += InheritTimerTick;

            // Setup some lists for the hotkey controls
            lastSelectedRelevantDrawables = new List<IRelevantDrawable>();
            lastLockedRelevantDrawables = new List<IRelevantDrawable>();
            lastInheritRelevantDrawables = new List<IRelevantDrawable>();

            // Setup commands
            InitializeCommands();

            // Listen for changes in the osu! user config
            configWatcher = new FileSystemWatcher();
            SetConfigWatcherPath(SettingsManager.Settings.OsuConfigPath);
            configWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Attributes | NotifyFilters.CreationTime;
            configWatcher.Changed += OnChangedConfigWatcher;

            // Listen for changes in osu! user config path in the settings
            SettingsManager.Settings.PropertyChanged += OnSettingsChanged;

            state = State.LookingForProcess;
        }
        #endregion
        
        #region main loop
        private void UpdateTimerTick(object sender, EventArgs e) {
            var reader = EditorReaderStuff.GetEditorReader();
            switch (state) {
                case State.Disabled:
                    break;
                case State.LookingForProcess:
                    updateTimer.Interval = TimeSpan.FromSeconds(5);

                    // Set up objects/overlay
                    var process = EditorReaderStuff.GetOsuProcess();
                    if (process == null) {
                        return;
                    }

                    try {
                        reader.SetProcess(process);
                    }
                    catch {
                        return;
                    }

                    processSharp = new ProcessSharp(process, MemoryType.Remote);
                    osuWindow = processSharp.WindowFactory.MainWindow;

                    updateTimer.Interval = TimeSpan.FromSeconds(1);
                    state = State.LookingForEditor;
                    break;
                case State.LookingForEditor:
                    updateTimer.Interval = TimeSpan.FromSeconds(1);
                    if (reader.ProcessNeedsReload()) {
                        state = State.LookingForProcess;
                        overlay?.Dispose();
                        return;
                    }

                    try {
                        if (!osuWindow.Title.EndsWith(@".osu")) {
                            return;
                        }
                    }
                    catch (ArgumentException) {
                        state = State.LookingForProcess;
                        overlay?.Dispose();
                        return;
                    }

                    try {
                        reader.FetchEditor();
                    }
                    catch (Exception ex) {
                        fetchEditorFails++;
                        if (fetchEditorFails <= 3) return;

                        MessageBox.Show("Editor Reader seems to be failing a lot. Try restarting osu! and opening Geometry Dashboard again or refer to the FAQ.");
                        ex.Show();

                        updateTimer.IsEnabled = false;
                        return;
                    }

                    overlay = new SnappingToolsOverlay { Converter = coordinateConverter };
                    overlay.Initialize(osuWindow);
                    overlay.Enable();

                    overlay.SetBorder(Preferences.DebugEnabled);

                    overlay.OverlayWindow.Draw += OnDraw;

                    updateTimer.Interval = TimeSpan.FromMilliseconds(100);
                    state = State.Active;
                    break;
                case State.Active:
                    updateTimer.Interval = TimeSpan.FromMilliseconds(100);

                    // It successfully fetched editor so editor reader is probably working
                    fetchEditorFails = 0;

                    if (reader.ProcessNeedsReload()) {
                        state = State.LookingForProcess;
                        overlay.Dispose();
                        return;
                    }
                    if (reader.EditorNeedsReload()) {
                        state = State.LookingForEditor;
                        overlay.Dispose();
                        return;
                    }

                    // Get osu! state
                    var osuActivated = osuWindow.IsActivated;

                    // Get editor time
                    var editorTime = reader.EditorTime();

                    // Handle updating of relevant objects
                    switch (Preferences.UpdateMode) {
                        case UpdateMode.AnyChange:
                            UpdateRelevantObjects();
                            break;
                        case UpdateMode.TimeChange:
                            if (this.editorTime != editorTime) {
                                UpdateRelevantObjects();
                            }

                            break;
                        case UpdateMode.OsuActivated:
                            // Before not activated and after activated
                            if (!this.osuActivated && osuActivated) {
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
                    this.editorTime = editorTime;
                    // Update old osu activated variable
                    this.osuActivated = osuActivated;

                    // Try to get the current osu window position, this can throw an exception
                    try {
                        coordinateConverter.OsuWindowPosition = new Vector2(osuWindow.X, osuWindow.Y);
                    } catch (Exception ex) {
                        Debug.WriteLine(ex);
                    }
                    
                    overlay.Update();

                    // Don't do hotkeys if osu is deactivated
                    if (!osuActivated)
                        break;

                    if (!autoSnapTimer.IsEnabled && IsHotkeyDown(Preferences.SnapHotkey)) {
                        // Update overlay but not on parents only view mode, because that one updates on his own terms
                        if (HotkeyDownRedrawsOverlay)
                            overlay.OverlayWindow.InvalidateVisual();

                        // Reset last snapped relevant object to trigger an overlay update in the snap timer tick
                        lastSnappedRelevantObject = null;

                        // Find any possible held hit object
                        FetchHeldHitObject();
                        var cursorPos = GetCursorPosition();
                        heldHitObjectMouseOffset = heldHitObject != null ? heldHitObject.Pos - cursorPos : Vector2.Zero;
                        
                        autoSnapTimer.Start();
                    }
                    if (!selectTimer.IsEnabled && IsHotkeyDown(Preferences.SelectHotkey)) {
                        selectTimer.Start();
                    }
                    if (!lockTimer.IsEnabled && IsHotkeyDown(Preferences.LockHotkey)) {
                        lockTimer.Start();
                    }
                    if (!inheritTimer.IsEnabled && IsHotkeyDown(Preferences.InheritHotkey)) {
                        inheritTimer.Start();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region geometry dashboard helpers

        private Point GetRelativeDpiPoint(Vector2 pos, Vector2 offset) {
            var dpi = coordinateConverter.ToDpi(coordinateConverter.EditorToRelativeCoordinate(pos));
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
                        relevantDrawable.DrawYourself(context, coordinateConverter, Preferences);
                    }
                    // It has already drawn everything so return
                    return;
                }

                var objectsToRender = new HashSet<IRelevantObject>();

                if (Preferences.KeyDownViewMode.HasFlag(ViewMode.Parents) && lastSnappedRelevantObject != null) {
                    // Get the parents of the relevant object which is being snapped to
                    objectsToRender.UnionWith(lastSnappedRelevantObject.GetParentage(int.MaxValue));
                } else if (Preferences.KeyDownViewMode.HasFlag(ViewMode.DirectParents) && lastSnappedRelevantObject != null) {
                    objectsToRender.UnionWith(lastSnappedRelevantObject.GetParentage(1));
                }

                if (Preferences.KeyDownViewMode.HasFlag(ViewMode.Children) && lastSnappedRelevantObject != null) {
                    // Get the parents of the relevant object which is being snapped to
                    objectsToRender.UnionWith(lastSnappedRelevantObject.GetDescendants(int.MaxValue));
                } else if (Preferences.KeyDownViewMode.HasFlag(ViewMode.DirectChildren) && lastSnappedRelevantObject != null) {
                    objectsToRender.UnionWith(lastSnappedRelevantObject.GetDescendants(1));
                }

                foreach (var relevantObject in objectsToRender) {
                    if (relevantObject is IRelevantDrawable relevantDrawable) {
                        relevantDrawable.DrawYourself(context, coordinateConverter, Preferences);
                    }
                }
            } else {
                // Handle key up rendering
                if (Preferences.KeyUpViewMode.HasFlag(ViewMode.Everything)) {
                    foreach (var relevantDrawable in LayerCollection.GetAllRelevantDrawables()) {
                        relevantDrawable.DrawYourself(context, coordinateConverter, Preferences);
                    }
                }
            }
        }
        
        private List<HitObject> GetHitObjects(SelectedHitObjectMode selectionMode) {
            // We want the actual position as seen on screen
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(false);

            if (reader == null)
                return new List<HitObject>();

            var hitObjects = EditorReaderStuff.GetHitObjects(reader);

            // Get the visible hitobjects using approach rate
            var approachTime = Beatmap.GetApproachTime(reader.ApproachRate);

            switch (selectionMode) {
                case SelectedHitObjectMode.AllwaysAllVisible:
                    return hitObjects.Where(o => editorTime > o.Time - approachTime && editorTime < o.EndTime + approachTime).ToList();
                case SelectedHitObjectMode.VisibleOrSelected:
                    var thereAreSelected = hitObjects.Any(o => o.IsSelected);
                    return hitObjects.Where(o => thereAreSelected ? o.IsSelected : editorTime > o.Time - approachTime && editorTime < o.EndTime + approachTime).ToList();
                case SelectedHitObjectMode.OnlySelected:
                    return hitObjects.Where(o => o.IsSelected).ToList();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void FetchHeldHitObject() {
            if (Mouse.LeftButton != MouseButtonState.Pressed && !Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                heldHitObject = null;
                heldHitObjects = new HitObject[0];
                return;
            }

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            if (reader == null) {
                heldHitObject = null;
                heldHitObjects = new HitObject[0];
                return;
            }

            var selectedHitObjects = EditorReaderStuff.GetHitObjects(reader).Where(o => o.IsSelected).ToArray();

            if (selectedHitObjects.Length == 0) {
                heldHitObject = null;
                heldHitObjects = new HitObject[0];
                return;
            }

            var mousePos = GetCursorPosition();
            var circleRadius = Beatmap.GetHitObjectRadius(reader.CircleSize);

            HitObject closest = null;
            double bestDist = double.PositiveInfinity;
            foreach (var ho in selectedHitObjects) {
                var dist = Vector2.Distance(ho.Pos, mousePos);
                if (dist < bestDist) {
                    bestDist = dist;
                    closest = ho;
                }
            }

            if (closest != null && bestDist <= circleRadius) {
                heldHitObject = closest;
                heldHitObjects = selectedHitObjects;
            } else {
                heldHitObject = null;
                heldHitObjects = new HitObject[0];
            }
        }
      
        private void UpdateRelevantObjects()
        {
            var hitObjects = GetHitObjects(Preferences.SelectedHitObjectMode);
            
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

            overlay.OverlayWindow.InvalidateVisual();
        }


        [Flags]
        private enum DrawableFetchPriority {
            Selected = 1,
            Locked = 2,
            Inheritable = 4,
        }


        private IRelevantDrawable GetNearestDrawable(Vector2 cursorPos, DrawableFetchPriority specialPriority = 0, HitObject[] heldHitObjects = null, double range = double.PositiveInfinity) {
            // Get all the relevant drawables
            var drawables = LayerCollection.GetAllRelevantDrawables();

            // Hit object comparer for finding a parent held hit object
            var comparer = new HitObjectComparer(checkPosition:false);

            // Get the relevant object nearest to the cursor
            IRelevantDrawable nearest = null;
            var smallestDistance = double.PositiveInfinity;
            foreach (var o in drawables) {
                var dist = o.DistanceTo(cursorPos);

                if (dist > range) {
                    continue;
                }

                // Prioritize relevant points
                dist -= RelevancyBias * MathHelper.Clamp(o.Relevancy, 0 ,1);

                if (o is RelevantPoint) {
                    // Prioritize points to be able to snap to intersections
                    dist -= PointsBias;
                }

                if (specialPriority.HasFlag(DrawableFetchPriority.Selected) && o.IsSelected ||
                    specialPriority.HasFlag(DrawableFetchPriority.Locked) && o.IsLocked ||
                    specialPriority.HasFlag(DrawableFetchPriority.Inheritable) && o.IsInheritable) {
                    // Prioritize selected and locked to be able to unselect them easily
                    dist -= SpecialBias;
                }

                // Exclude any drawables which are the direct child of the held hit objects
                // Checks if any and all parents of the drawable are one of the held hit objects.
                if (heldHitObjects != null && o.ParentObjects.Count > 0 && o.ParentObjects.All(p =>
                        p is RelevantHitObject rho && heldHitObjects.Any(hho => comparer.Equals(rho.HitObject, hho)))) {
                    continue;
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
            var cursorPos = coordinateConverter.ScreenToEditorCoordinate(new Vector2(cursorPoint.X, cursorPoint.Y));

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
                    overlay?.OverlayWindow.InvalidateVisual();
                autoSnapTimer.Stop();
                return;
            }
            
            // Get nearest drawable
            var cursorPos = GetCursorPosition();
            // Get the offset if a hit object is being held so the center of that object gets snapped
            var nearest = GetNearestDrawable(cursorPos + heldHitObjectMouseOffset, heldHitObjects:heldHitObjects);

            // Update overlay if the last snapped changed and parentview is on
            if (nearest != lastSnappedRelevantObject && SnapChangeRedrawsOverlay) {
                // Set the last snapped relevant object
                lastSnappedRelevantObject = nearest;
                // Update overlay
                overlay.OverlayWindow.InvalidateVisual();
            }

            // CONVERT THIS TO CURSOR POSITION
            if (nearest == null) return;

            var nearestPoint = coordinateConverter.EditorToScreenCoordinate(
                nearest.NearestPoint(cursorPos + heldHitObjectMouseOffset) - heldHitObjectMouseOffset);
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int) Math.Round(nearestPoint.X), (int) Math.Round(nearestPoint.Y));
        }

        private void SelectTimerTick(object sender, EventArgs e) {
            // Check timer stop
            if (!IsHotkeyDown(Preferences.SelectHotkey)) {
                selectTimer.Stop();
                lastSelectedRelevantDrawables.Clear();
                return;
            }

            // Get nearest drawable
            var cursorPos = GetCursorPosition();
            var nearest = GetNearestDrawable(cursorPos, DrawableFetchPriority.Selected, range: SelectionRange);

            if (nearest == null) return;

            // Check if this drawable was already handled with this keypress
            if (lastSelectedRelevantDrawables.Contains(nearest)) return;

            // Get the selecting mode
            if (lastSelectedRelevantDrawables.Count == 0) {
                selectedToggle = !nearest.IsSelected;
            }

            // Set the selected variable of the nearest drawable
            nearest.IsSelected = selectedToggle;

            // Add nearest drawable to the list so it doesnt get toggled later
            lastSelectedRelevantDrawables.Add(nearest);

            // Redraw overlay
            overlay.OverlayWindow.InvalidateVisual();
        }

        private void LockTimerTick(object sender, EventArgs e) {
            // Check timer stop
            if (!IsHotkeyDown(Preferences.LockHotkey)) {
                lockTimer.Stop();
                lastLockedRelevantDrawables.Clear();
                unlockedSomething = false;
                return;
            }

            // Get nearest drawable
            var cursorPos = GetCursorPosition();
            var nearest = GetNearestDrawable(cursorPos, DrawableFetchPriority.Locked, range: SelectionRange);

            if (nearest == null) return;

            // Check if this drawable was already handled with this keypress
            if (lastLockedRelevantDrawables.Contains(nearest)) return;

            // Get the locking mode
            if (lastLockedRelevantDrawables.Count == 0) {
                lockedToggle = !nearest.IsLocked;
            }

            // Set the locked variable of the nearest drawable
            if (lockedToggle) {
                if (!nearest.IsLocked) {
                    LayerCollection.GetRootLayer().Add(nearest.GetLockedRelevantObject());
                }
            } else {
                if (nearest.IsLocked && !unlockedSomething) {
                    nearest.Dispose();
                    unlockedSomething = true;
                }
            }

            // Add nearest drawable to the list so it doesnt get toggled later
            lastLockedRelevantDrawables.Add(nearest);

            // Redraw overlay
            overlay.OverlayWindow.InvalidateVisual();
        }

        private void InheritTimerTick(object sender, EventArgs e) {
            // Check timer stop
            if (!IsHotkeyDown(Preferences.InheritHotkey)) {
                inheritTimer.Stop();
                lastInheritRelevantDrawables.Clear();
                return;
            }

            // Get nearest drawable
            var cursorPos = GetCursorPosition();
            var nearest = GetNearestDrawable(cursorPos, range: SelectionRange);

            if (nearest == null) return;

            // Check if this drawable was already handled with this keypress
            if (lastInheritRelevantDrawables.Contains(nearest)) return;

            // Get the inherit mode
            if (lastInheritRelevantDrawables.Count == 0) {
                inheritableToggle = !nearest.IsInheritable;
            }

            // Set the inheritable variable of the nearest drawable
            nearest.IsInheritable = inheritableToggle;

            // Add nearest drawable to the list so it doesnt get toggled later
            lastInheritRelevantDrawables.Add(nearest);

            // Redraw overlay
            overlay.OverlayWindow.InvalidateVisual();
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
                    coordinateConverter.EditorBoxOffset.Left = Preferences.OffsetLeft;
                    break;
                case "OffsetTop":
                    coordinateConverter.EditorBoxOffset.Top = Preferences.OffsetTop;
                    break;
                case "OffsetRight":
                    coordinateConverter.EditorBoxOffset.Right = Preferences.OffsetRight;
                    break;
                case "OffsetBottom":
                    coordinateConverter.EditorBoxOffset.Bottom = Preferences.OffsetBottom;
                    break;
                case "AcceptableDifference":
                    LayerCollection.AcceptableDifference = Preferences.AcceptableDifference;
                    break;
                case "DebugEnabled":
                    overlay?.SetBorder(Preferences.DebugEnabled);
                    break;
                case "VisiblePlayfieldBoundary":
                    overlay?.OverlayWindow.InvalidateVisual();
                    break;
                case "InceptionLevel":
                    LayerCollection.SetInceptionLevel(Preferences.InceptionLevel);
                    overlay?.OverlayWindow.InvalidateVisual();
                    break;
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != "OsuConfigPath") return;
            SetConfigWatcherPath(SettingsManager.Settings.OsuConfigPath);
            coordinateConverter.ReadConfig();
        }

        private void OnGeneratorSettingsPropertyChanged(object sender, PropertyChangedEventArgs e) {
            var settings = (GeneratorSettings) sender;
            var generator = settings.Generator;

            switch (e.PropertyName) {
                case "IsActive":
                    if (state == State.Active) {
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
                        overlay.OverlayWindow.InvalidateVisual();
                    }

                    break;
                default:
                    if (state == State.Active) {
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
                        overlay.OverlayWindow.InvalidateVisual();
                    }

                    break;
            }
            
        }

        #endregion

        #region osu config watcher

        private void SetConfigWatcherPath(string path) {
            try {
                configWatcher.Path = Path.GetDirectoryName(path);
                configWatcher.Filter = Path.GetFileName(path);
            }
            catch (Exception ex) { Console.WriteLine(@"Can't set ConfigWatcher Path/Filter: " + ex.Message); }
        }

        private void OnChangedConfigWatcher(object sender, FileSystemEventArgs e) {
            coordinateConverter.ReadConfig();
        }

        #endregion

        #region UI helpers
        
        private bool UserFilter(object item) {
            if (string.IsNullOrEmpty(Filter))
                return true;
            return ((RelevantObjectsGenerator)item).Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetFilter(string value) {
            filter = value;
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
                overlay.OverlayWindow.InvalidateVisual();
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

                LayerCollection.GetRootLayer().Add(lockedObjectsToAdd.Select(o => o.GetLockedRelevantObject()));
                foreach (var relevantObject in lockedObjectsToDispose) {
                    relevantObject.Dispose();
                }
                LayerCollection.GetRootLayer().GenerateNewObjects(true);
                overlay.OverlayWindow.InvalidateVisual();
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
                overlay.OverlayWindow.InvalidateVisual();
            });
        }

        #endregion

        #region serialization stuff

        public void UpdateEverything() {
            coordinateConverter.EditorBoxOffset.Left = Preferences.OffsetLeft;
            coordinateConverter.EditorBoxOffset.Top = Preferences.OffsetTop;
            coordinateConverter.EditorBoxOffset.Right = Preferences.OffsetRight;
            coordinateConverter.EditorBoxOffset.Bottom = Preferences.OffsetBottom;
            LayerCollection.AcceptableDifference = Preferences.AcceptableDifference;
            LayerCollection.SetInceptionLevel(Preferences.InceptionLevel);
            if (overlay != null) {
                overlay.SetBorder(Preferences.DebugEnabled);
                overlay.OverlayWindow.InvalidateVisual();
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

        public RelevantObjectCollection GetLockedObjects() {
            return LayerCollection.GetRootLayer().Objects.ObjectsWhere(o => o.IsLocked);
        }

        public void SetLockedObjects(RelevantObjectCollection objects) {
            LayerCollection.GetRootLayer().Objects.MergeWith(objects);

            overlay.OverlayWindow.InvalidateVisual();
        }

        #endregion

        #region tool management helpers

        public void Dispose() {
            updateTimer.Stop();
            overlay?.Dispose();
            configWatcher?.Dispose();
            processSharp?.Dispose();
            osuWindow?.Dispose();
        }

        public void Activate() {
            updateTimer.IsEnabled = true;

            try {
                configWatcher.EnableRaisingEvents = true;
            } catch (Exception ex) {
                MessageBox.Show("Can not enable filesystem watcher. osu! config path is probably incorrect. Please set the correct path in the Preferences or your overlay might have the wrong position.", "Warning");
                ex.Show();
            }

            state = State.LookingForProcess;

            Project?.Activate();
        }

        public void Deactivate() {
            if (Preferences.KeepRunning) return;

            updateTimer.IsEnabled = false;

            try {
                configWatcher.EnableRaisingEvents = false;
            } catch (Exception ex) {
                MessageBox.Show("Can not enable filesystem watcher. osu! config path is probably incorrect. Please set the correct path in the Preferences or your overlay might have the wrong position.", "Warning");
                ex.Show();
            }

            state = State.Disabled;
            overlay?.Dispose();

            Project?.Deactivate();
        }
        #endregion
    }
}
