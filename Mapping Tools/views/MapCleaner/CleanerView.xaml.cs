using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Classes.Tools.MapCleanerStuff;
using Mapping_Tools.Components.TimeLine;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.MapCleaner {
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class CleanerView : IQuickRun, ISavable<MapCleanerVm> {
        private List<double> _timingpointsRemoved;
        private List<double> _timingpointsAdded;
        private List<double> _timingpointsChanged;
        private double _endTimeMonitor;
        private TimeLine _tl;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler RunFinished;

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolName = "Map Cleaner";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolDescription = $@"It cleans the current map of useless greenlines and it also lets you do some other stuff regarding the whole map.{Environment.NewLine}Map cleaner cleans useless greenline stuff by storing all the influences of the timingpoints and then removing all the timingpoints and then rebuilding all the timingpoints in a good way. This means the greenlines automatically get resnapped to the objects that use them.";

        /// <summary>
        /// Initializes the Map Cleaner view to <see cref="MainWindow"/>
        /// </summary>
        public CleanerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new MapCleanerVm();
            ProjectManager.LoadProject(this, message: false);

            // It's important to see the results of map cleaner
            Verbose = true;
        }

        public MapCleanerVm ViewModel => (MapCleanerVm) DataContext;

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps(), quick: false);
        }

        /// <summary>
        /// 
        /// </summary>
        public void QuickRun() {
            RunTool(new[] { IOHelper.GetCurrentBeatmapOrCurrentBeatmap() }, quick: true);
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Run_Program((MapCleanerVm) e.Argument, bgw, e);
        }

        protected override void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error == null) {
                FillTimeLine();
            }
            base.BackgroundWorker_RunWorkerCompleted(sender, e);
        }

        private string Run_Program(MapCleanerVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            var result = new MapCleanerResult();

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            if (args.Paths.Length == 1) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);

                List<TimingPoint> orgininalTimingPoints = editor.Beatmap.BeatmapTiming.TimingPoints.Select(tp => tp.Copy()).ToList();
                int oldTimingPointsCount = editor.Beatmap.BeatmapTiming.TimingPoints.Count;

                result.Add(Classes.Tools.MapCleanerStuff.MapCleaner.CleanMap(editor, args.MapCleanerArgs, worker));

                // Update result with removed count
                int removed = oldTimingPointsCount - editor.Beatmap.BeatmapTiming.TimingPoints.Count;
                result.TimingPointsRemoved += removed;

                var newTimingPoints = editor.Beatmap.BeatmapTiming.TimingPoints;
                Monitor_Differences(orgininalTimingPoints, newTimingPoints);

                // Save the file
                editor.SaveFile();
            } else {
                foreach (string path in args.Paths) {
                    var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);

                    int oldTimingPointsCount = editor.Beatmap.BeatmapTiming.TimingPoints.Count;

                    result.Add(Classes.Tools.MapCleanerStuff.MapCleaner.CleanMap(editor, args.MapCleanerArgs, worker));

                    // Update result with removed count
                    int removed = oldTimingPointsCount - editor.Beatmap.BeatmapTiming.TimingPoints.Count;
                    result.TimingPointsRemoved += removed;

                    // Save the file
                    editor.SaveFile();
                }
            }

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, args.Quick));

            // Make an accurate message
            string message = $"Successfully {(result.TimingPointsRemoved < 0 ? "added" : "removed")} {Math.Abs(result.TimingPointsRemoved)} {(Math.Abs(result.TimingPointsRemoved) == 1 ? "greenline" : "greenlines")}" +
                (args.MapCleanerArgs.ResnapObjects ? $" and resnapped {result.ObjectsResnapped} {(result.ObjectsResnapped == 1 ? "object" : "objects")}" : "") + 
                (args.MapCleanerArgs.RemoveUnusedSamples ? $" and removed {result.SamplesRemoved} unused {(result.SamplesRemoved == 1 ? "sample" : "samples")}" : "") + "!";
            return args.Quick ? string.Empty : message;
        }

        private void Monitor_Differences(IReadOnlyList<TimingPoint> originalTimingPoints, IReadOnlyList<TimingPoint> newTimingPoints) {
            // Take note of all the changes
            _timingpointsChanged = new List<double>();

            var originalInNew = (from first in originalTimingPoints
                                 join second in newTimingPoints
                                 on first.Offset equals second.Offset
                                 select first).ToList();

            var newInOriginal = (from first in originalTimingPoints
                                 join second in newTimingPoints
                                 on first.Offset equals second.Offset
                                 select second).ToList();
            
            foreach (TimingPoint tp in originalInNew) {
                bool different = true;
                List<TimingPoint> newTPs = newInOriginal.Where(o => Math.Abs(o.Offset - tp.Offset) < Precision.DOUBLE_EPSILON).ToList();
                if (newTPs.Count == 0) { different = false; }
                foreach (TimingPoint newTp in newTPs) {
                    if (tp.Equals(newTp)) { different = false; }
                }
                if (different) { _timingpointsChanged.Add(tp.Offset); }
            }

            List<double> originalOffsets = new List<double>();
            List<double> newOffsets = new List<double>();
            foreach (var originalTimingPoint in originalTimingPoints) {
                originalOffsets.Add(originalTimingPoint.Offset);
            }
            foreach (var newTimingPoint in newTimingPoints) {
                newOffsets.Add(newTimingPoint.Offset);
            }

            _timingpointsRemoved = originalOffsets.Except(newOffsets).ToList();
            _timingpointsAdded = newOffsets.Except(originalOffsets).ToList();
            double endTimeOriginal = originalTimingPoints.Count > 0 ? originalTimingPoints.Last().Offset : 0;
            double endTimeNew = newTimingPoints.Count > 0 ? newTimingPoints.Last().Offset : 0;
            _endTimeMonitor = Math.Max(endTimeOriginal, endTimeNew);
        }

        private void FillTimeLine() {
            _tl?.mainCanvas.Children.Clear();
            try {
                _tl = new TimeLine(MainWindow.AppWindow.ActualWidth, 100.0, _endTimeMonitor);
                foreach (double timingS in _timingpointsAdded) {
                    _tl.AddElement(timingS, 1);
                }
                foreach (double timingS in _timingpointsChanged) {
                    _tl.AddElement(timingS, 2);
                }
                foreach (double timingS in _timingpointsRemoved) {
                    _tl.AddElement(timingS, 3);
                }
                tl_host.Children.Clear();
                tl_host.Children.Add(_tl);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public MapCleanerVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(MapCleanerVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "mapcleanerproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Map Cleaner Projects");
    }
}
