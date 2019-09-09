using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Viewmodels {
    public class SnappingToolsVM {
        public Hotkey SnapHotkey { get; set; }

        public ObservableCollection<RelevantObjectsGenerator> Generators { get; }
        private readonly List<IRelevantObject> _relevantObjects = new List<IRelevantObject>();
        private int _editorTime;
        private double[] _visibleObjectTimes;

        private string _filter = "";
        public string Filter { get => _filter; set => SetFilter(value); }

        public bool ListenersEnabled
        {
            set
            {
                UpdateTimer.IsEnabled = value;
                _configWatcher.EnableRaisingEvents = value;
            }
        }

        private readonly DispatcherTimer UpdateTimer;
        private readonly DispatcherTimer AutoSnapTimer;

        private const double PointsBias = 3;

        private readonly CoordinateConverter _coordinateConverter;
        public readonly FileSystemWatcher _configWatcher;

        public SnappingToolsVM() {
            // Get all the RelevantObjectGenerators
            var interfaceType = typeof(RelevantObjectsGenerator);
            Generators = new ObservableCollection<RelevantObjectsGenerator>(AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(x => x.GetTypes())
              .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
              .Select(Activator.CreateInstance).OfType<RelevantObjectsGenerator>());

            // Add PropertyChanged event to all generators to listen for changes
            foreach (var gen in Generators) { gen.PropertyChanged += OnGeneratorPropertyChanged; }

            // Set up groups and filters
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(Generators);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("GeneratorType");
            view.GroupDescriptions.Add(groupDescription);
            view.Filter = UserFilter;

            // Set up timers for responding to hotkey presses and beatmap changes
            UpdateTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(100) };
            UpdateTimer.Tick += UpdateTimerTick;
            AutoSnapTimer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(16) };
            AutoSnapTimer.Tick += AutoSnapTimerTick;

            // Set up a coordinate converter for converting coordinates between screen and osu!
            _coordinateConverter = new CoordinateConverter();

            // Listen for changes in the osu! user config
            _configWatcher = new FileSystemWatcher();
            SetConfigWatcherPath(SettingsManager.Settings.OsuConfigPath);
            _configWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Attributes | NotifyFilters.CreationTime;
            _configWatcher.Changed += OnChangedConfigWatcher;

            // Listen for changes in osu! user config path in the settings
            SettingsManager.Settings.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "OsuConfigPath") return;
            SetConfigWatcherPath(SettingsManager.Settings.OsuConfigPath);
            _coordinateConverter.ReadConfig();
        }

        private void SetConfigWatcherPath(string path)
        {
            try {
                _configWatcher.Path = Path.GetDirectoryName(path);
                _configWatcher.Filter = Path.GetFileName(path);
            } catch (Exception ex) { Console.WriteLine(@"Can't set ConfigWatcher Path/Filter: " + ex.Message); }
        }

        private void OnChangedConfigWatcher(object sender, FileSystemEventArgs e)
        {
            _coordinateConverter.ReadConfig();
        }

        private void OnGeneratorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsActive")
            {
                // Reload relevant objects when a generator gets enabled/disabled
                GenerateRelevantObjects();
            }
        }

        private void UpdateTimerTick(object sender, EventArgs e)
        {
            var reader = EditorReaderStuff.GetEditorReader();
            if (reader.ProcessNeedsReload())
            {
                try
                {
                    reader.FetchHOM();
                }
                catch
                {
                    return;
                }
            }

            var time = reader.EditorTime();
            if (time != _editorTime)
            {
                _editorTime = time;
                UpdateRelevantObjects();
            }

            if (!AutoSnapTimer.IsEnabled && IsHotkeyDown(SnapHotkey))
            {
                AutoSnapTimer.Start();
            }
        }

        private void UpdateRelevantObjects() {
            if (!EditorReaderStuff.TryGetFullEditorReader(out var reader)) return;

            var editor = EditorReaderStuff.GetNewestVersion(reader, out _);

            // Get the visible hitobjects using approach rate
            var approachTime = ApproachRateToMs(reader.ApproachRate);
            var visibleObjects = editor.Beatmap.HitObjects.Where(o => Math.Abs(o.Time - _editorTime) < approachTime).ToList();
            var visibleObjectTimes = visibleObjects.Select(o => o.Time).ToArray();

            if (_visibleObjectTimes != null && visibleObjectTimes.SequenceEqual(_visibleObjectTimes))
            {
                // Visible Objects didn't change. Return to avoid redundant updates
                return;
            }
            // Set the new times
            _visibleObjectTimes = visibleObjectTimes;

            GenerateRelevantObjects(visibleObjects);
        }

        private void GenerateRelevantObjects(List<HitObject> visibleObjects=null)
        {
            if (visibleObjects == null)
            {
                if (!EditorReaderStuff.TryGetFullEditorReader(out var reader)) return;

                var editor = EditorReaderStuff.GetNewestVersion(reader, out _);

                // Get the visible hitobjects using approach rate
                var approachTime = ApproachRateToMs(reader.ApproachRate);
                visibleObjects = editor.Beatmap.HitObjects.Where(o => Math.Abs(o.Time - _editorTime) < approachTime).ToList();
            }

            // Get all the active generators
            var activeGenerators = Generators.Where(o => o.IsActive).ToList();

            // Reset the old RelevantObjects
            _relevantObjects.Clear();

            // Generate RelevantObjects based on the visible hitobjects
            foreach (var gen in activeGenerators.OfType<IGenerateRelevantObjectsFromHitObjects>()) {
                _relevantObjects.AddRange(gen.GetRelevantObjects(visibleObjects));
            }

            // Seperate the RelevantObjects
            var relevantPoints = new List<RelevantPoint>();
            var relevantLines = new List<RelevantLine>();
            var relevantCircles = new List<RelevantCircle>();

            foreach (var ro in _relevantObjects) {
                if (ro is RelevantPoint rp)
                {
                    relevantPoints.Add(rp);
                    Views.SnappingTools.SnappingOverlay.CreateOverlayPoint(_coordinateConverter.EditorToScreenCoordinate(rp.child));
                }
                else if (ro is RelevantLine rl)
                    relevantLines.Add(rl);
                else if (ro is RelevantCircle rc)
                    relevantCircles.Add(rc);
            }

            // Generate more RelevantObjects
            foreach (var gen in activeGenerators.OfType<IGenerateRelevantObjectsFromRelevantObjects>()) {
                _relevantObjects.AddRange(gen.GetRelevantObjects(_relevantObjects));
            }
            foreach (var gen in activeGenerators.OfType<IGenerateRelevantObjectsFromRelevantPoints>()) {
                _relevantObjects.AddRange(gen.GetRelevantObjects(relevantPoints));
            }
        }

        private double ApproachRateToMs(double approachRate)
        {
            if (approachRate < 5) {
                return 1800 - 120 * approachRate;
            }

            return 1200 - 150 * (approachRate - 5);
        }

        private void AutoSnapTimerTick(object sender, EventArgs e) {
            if (!IsHotkeyDown(SnapHotkey))
            {
                AutoSnapTimer.Stop();
                return;
            }

            // Move the cursor's Position
            // System.Windows.Forms.Cursor.Position = new Point();
            var cursorPoint = System.Windows.Forms.Cursor.Position;
            // CONVERT THIS CURSOR POSITION TO EDITOR POSITION
            var cursorPos = _coordinateConverter.ScreenToEditorCoordinate(new Vector2(cursorPoint.X, cursorPoint.Y));

            if (_relevantObjects.Count == 0)
                return;

            IRelevantObject nearest = null;
            double smallestDistance = double.PositiveInfinity;
            foreach (IRelevantObject o in _relevantObjects) {
                double dist = o.DistanceTo(cursorPos);
                if (o is RelevantPoint) // Prioritize points to be able to snap to intersections
                    dist -= PointsBias;
                if (dist < smallestDistance) {
                    smallestDistance = dist;
                    nearest = o;
                }
            }

            // CONVERT THIS TO CURSOR POSITION
            if (nearest == null) return;
            var nearestPoint = _coordinateConverter.EditorToScreenCoordinate(nearest.NearestPoint(cursorPos));
            System.Windows.Forms.Cursor.Position = new Point((int)Math.Round(nearestPoint.X), (int)Math.Round(nearestPoint.Y));
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
            if (hotkey.Modifiers.HasFlag(ModifierKeys.Windows) && !(Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)))
                return false;

            return true;
        }

        private bool UserFilter(object item)
        {
            if (string.IsNullOrEmpty(Filter))
                return true;
            return ((RelevantObjectsGenerator) item).Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetFilter(string value) {
            _filter = value;
            CollectionViewSource.GetDefaultView(Generators).Refresh();
        }
    }
}
