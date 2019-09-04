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

                relevantObjects.Clear();

                var activeGenerators = Generators.Where(o => o.IsActive);

                foreach (var gen in activeGenerators.OfType<IGenerateRelevantObjectsFromHitObjects>()) {
                    relevantObjects.AddRange(gen.GetRelevantObjects(editor.Beatmap.HitObjects));
                }
                foreach (var gen in activeGenerators.OfType<IGenerateRelevantObjectsFromRelevantPoints>()) {
                    relevantObjects.AddRange(gen.GetRelevantObjects(relevantObjects.OfType<RelevantPoint>().ToList()));
                }
            }
        }

        void Timer_Tick(object sender, EventArgs e) {
            if (IsHotkeyDown(SnapHotkey)) {
                // Move the cursor's Position
                // System.Windows.Forms.Cursor.Position = new Point();
                var cursorPoint = System.Windows.Forms.Cursor.Position;
                // CONVERT THIS CURSOR POSITION TO EDITOR POSITION
                var cursorPos = new Vector2(cursorPoint.X, cursorPoint.Y);

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

                var nearestPoint = nearest.NearestPoint(cursorPos);
                System.Windows.Forms.Cursor.Position = new Point((int)Math.Round(nearestPoint.X), (int)Math.Round(nearestPoint.Y));
            }
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
