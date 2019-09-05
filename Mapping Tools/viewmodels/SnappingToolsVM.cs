using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace Mapping_Tools.Viewmodels {
    public class SnappingToolsVM {
        public Hotkey SnapHotkey { get; set; }

        public ObservableCollection<IGenerateRelevantObjects> Generators { get; }
        private readonly List<IRelevantObject> relevantObjects = new List<IRelevantObject>();

        private string _filter = "";
        public string Filter { get => _filter; set => SetFilter(value); }

        private readonly DispatcherTimer AutoSnapTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(10) };
        public bool AutoSnapTimerEnabled { get => AutoSnapTimer.IsEnabled; set => AutoSnapTimer.IsEnabled = value; }

        public SnappingToolsVM() {
            var interfaceType = typeof(IGenerateRelevantObjects);
            Generators = new ObservableCollection<IGenerateRelevantObjects>(AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(x => x.GetTypes())
              .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
              .Select(x => Activator.CreateInstance(x)).OfType<IGenerateRelevantObjects>());

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(Generators);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("GeneratorType");
            view.GroupDescriptions.Add(groupDescription);
            view.Filter = UserFilter;

            AutoSnapTimer.Tick += Timer_Tick;

            GenerateCommand = new CommandImplementation(
                _ => {
                    GenerateRelevantObjects();
                });
        }

        private void GenerateRelevantObjects() {
            if (EditorReaderStuff.TryGetFullEditorReader(out var reader)) {
                var editor = EditorReaderStuff.GetNewestVersion(reader, out var _);

                // Get the visible hitobjects using approach rate
                var editorTime = reader.EditorTime();
                var approachTime = ApproachRateToMs(reader.ApproachRate);
                var visibleObjects = editor.Beatmap.HitObjects.Where(o => Math.Abs(o.Time - editorTime) < approachTime).ToList();

                // Get all the active generators
                var activeGenerators = Generators.Where(o => o.IsActive);

                // Reset the old RelevantObjects
                relevantObjects.Clear();

                // Generate RelevantObjects based on the visible hitobjects
                foreach (var gen in activeGenerators.OfType<IGenerateRelevantObjectsFromHitObjects>()) {
                    relevantObjects.AddRange(gen.GetRelevantObjects(visibleObjects));
                }

                // Seperate the RelevantObjects
                var relevantPoints = new List<RelevantPoint>();
                var relevantLines = new List<RelevantLine>();
                var relevantCircles = new List<RelevantCircle>();

                foreach (var ro in relevantObjects) {
                    if (ro is RelevantPoint rp)
                        relevantPoints.Add(rp);
                    else if (ro is RelevantLine rl)
                        relevantLines.Add(rl);
                    else if (ro is RelevantCircle rc)
                        relevantCircles.Add(rc);
                }

                // Generate more RelevantObjects
                foreach (var gen in activeGenerators.OfType<IGenerateRelevantObjectsFromRelevantPoints>()) {
                    relevantObjects.AddRange(gen.GetRelevantObjects(relevantPoints));
                }
            }
        }

        private double ApproachRateToMs(double approachRate) {
            if (approachRate < 5) {
                return 1800 - 120 * approachRate;
            } else {
                return 1200 - 150 * (approachRate - 5);
            }
        }

        void Timer_Tick(object sender, EventArgs e) {
            if (IsHotkeyDown(SnapHotkey)) {
                // Move the cursor's Position
                // System.Windows.Forms.Cursor.Position = new Point();
                var cursorPoint = System.Windows.Forms.Cursor.Position;
                // CONVERT THIS CURSOR POSITION TO EDITOR POSITION
                var cursorPos = ToEditorPosition(new Vector2(cursorPoint.X, cursorPoint.Y));

                if (relevantObjects.Count == 0)
                    return;

                IRelevantObject nearest = null;
                double smallestDistance = double.PositiveInfinity;
                foreach (IRelevantObject o in relevantObjects) {
                    double dist = o.DistanceTo(cursorPos);
                    if (dist < smallestDistance) {
                        smallestDistance = dist;
                        nearest = o;
                    }
                }

                // CONVERT THIS TO CURSOR POSITION
                var nearestPoint = ToMonitorPosition(nearest.NearestPoint(cursorPos));
                System.Windows.Forms.Cursor.Position = new Point((int)Math.Round(nearestPoint.X), (int)Math.Round(nearestPoint.Y));
            }
        }

        private Vector2 ToEditorPosition(Vector2 pos) {
            // (400, 189) -> (0, 0)
            // (1520, 1028) -> (512, 384)
            // Console.WriteLine(pos);
            return (pos - new Vector2(400, 189)) / 2.186;
        }

        private Vector2 ToMonitorPosition(Vector2 pos) {
            return pos * 2.186 + new Vector2(400, 189);
        }

        private bool IsHotkeyDown(Hotkey hotkey) {
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

        private bool UserFilter(object item) {
            if (string.IsNullOrEmpty(Filter))
                return true;
            else
                return ((item as IGenerateRelevantObjects).Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void SetFilter(string value) {
            _filter = value;
            CollectionViewSource.GetDefaultView(Generators).Refresh();
        }

        public CommandImplementation GenerateCommand { get; }
    }
}
