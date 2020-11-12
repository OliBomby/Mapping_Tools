using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.TimeLine;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools.Views.AutoFailDetector {
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class AutoFailDetectorView : IQuickRun {
        private List<double> _unloadingObjects;
        private List<double> _potentialUnloadingObjects;
        private List<double> _potentialDisruptors;
        private double _endTimeMonitor;
        private TimeLine _tl;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler RunFinished;

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolName = "Auto-fail Detector";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolDescription = $"Detects cases of incorrect object loading in a beatmap which makes osu! unable to calculate scores correctly.{Environment.NewLine} Auto-fail is most often caused by placing other hit objects during sliders, so there are multiple hit objects going on at the same time also known as \"2B\" patterns.{Environment.NewLine} Use the AR and OD override options to see what would happen when you use hardrock mod on the map.";

        /// <summary>
        /// Initializes the Map Cleaner view to <see cref="MainWindow"/>
        /// </summary>
        public AutoFailDetectorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new AutoFailDetectorVm();

            // It's important to see the results
            Verbose = true;
        }

        public AutoFailDetectorVm ViewModel => (AutoFailDetectorVm) DataContext;

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

            //BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Run_Program((AutoFailDetectorVm) e.Argument, bgw, e);
        }

        protected override void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error == null) {
                FillTimeLine();
            }
            base.BackgroundWorker_RunWorkerCompleted(sender, e);
        }

        private string Run_Program(AutoFailDetectorVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();
            var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);
            var beatmap = editor.Beatmap;

            // Get approach time and radius of the 50 score hit window
            var ar = args.ApproachRateOverride == -1
                ? editor.Beatmap.Difficulty["ApproachRate"].DoubleValue
                : args.ApproachRateOverride;
            var approachTime = (int) Beatmap.GetApproachTime(ar);

            var od = args.OverallDifficultyOverride == -1
                ? editor.Beatmap.Difficulty["OverallDifficulty"].DoubleValue
                : args.OverallDifficultyOverride;
            var window50 = (int) Math.Ceiling(200 - 10 * od);

            // Start time and end time
            var mapStartTime = (int) beatmap.GetMapStartTime();
            var mapEndTime = (int) beatmap.GetMapEndTime();
            var autoFailTime = (int) beatmap.GetAutoFailCheckTime();

            // Detect auto-fail
            var autoFailDetector = new Classes.Tools.AutoFailDetector(beatmap.HitObjects,
                mapStartTime, mapEndTime, autoFailTime,
                approachTime, window50, args.PhysicsUpdateLeniency);

            var autoFail = autoFailDetector.DetectAutoFail();

            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(33);

            // Fix auto-fail
            if (args.GetAutoFailFix) {
                var placedFix = autoFailDetector.AutoFailFixDialogue(args.AutoPlaceFix);

                if (placedFix) {
                    editor.SaveFile();
                }
            }

            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(67);

            // Set the timeline lists
            _unloadingObjects = args.ShowUnloadingObjects ? autoFailDetector.UnloadingObjects : new List<double>();
            _potentialUnloadingObjects = args.ShowPotentialUnloadingObjects ? autoFailDetector.PotentialUnloadingObjects : new List<double>();
            _potentialDisruptors = args.ShowPotentialDisruptors ? autoFailDetector.Disruptors : new List<double>();

            // Set end time for the timeline
            _endTimeMonitor = mapEndTime;

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, false, args.Quick));

            return autoFail ? $"{autoFailDetector.UnloadingObjects.Count} unloading objects detected and {autoFailDetector.PotentialUnloadingObjects.Count} potential unloading objects detected!" :
                autoFailDetector.PotentialUnloadingObjects.Count > 0 ? $"No auto-fail, but {autoFailDetector.PotentialUnloadingObjects.Count} potential unloading objects detected." : 
                "No auto-fail detected.";
        }


        private void FillTimeLine() {
            _tl?.mainCanvas.Children.Clear();
            try {
                _tl = new TimeLine(MainWindow.AppWindow.ActualWidth, 100.0, _endTimeMonitor);
                foreach (double timingS in _potentialUnloadingObjects) {
                    _tl.AddElement(timingS, 1);
                }
                foreach (double timingS in _potentialDisruptors) {
                    _tl.AddElement(timingS, 4);
                }
                foreach (double timingS in _unloadingObjects) {
                    _tl.AddElement(timingS, 3);
                }
                tl_host.Children.Clear();
                tl_host.Children.Add(_tl);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
