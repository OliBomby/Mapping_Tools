﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views {
    public partial class SnappingToolsView : IQuickRun {
        private readonly BackgroundWorker _backgroundWorker;
        private bool _canRun = true;
        public static SnappingTools.SnappingOverlay overlay { get; set; }

        public event EventHandler RunFinished;

        public SnappingToolsView() {
            DataContext = new SnappingToolsVM();
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            _backgroundWorker = (BackgroundWorker)FindResource("BackgroundWorker");

            overlay = new SnappingTools.SnappingOverlay();
            overlay.Show();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Complete_Sliders((Arguments)e.Argument, bgw);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show($"{e.Error.Message}{Environment.NewLine}{e.Error.StackTrace}", "Error");
            }
            else {
                if (e.Result.ToString() != "")
                    MessageBox.Show(e.Result.ToString());
                Progress.Value = 0;
            }
            Start.IsEnabled = true;
            _canRun = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            Progress.Value = e.ProgressPercentage;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps());
        }

        public void QuickRun() {
            RunTool(new[] { IOHelper.GetCurrentBeatmap() }, true);
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!_canRun) return;

            IOHelper.SaveMapBackup(paths);

            _backgroundWorker.RunWorkerAsync(new Arguments(paths, quick));
            Start.IsEnabled = false;
            _canRun = false;
        }

        private struct Arguments {
            public readonly string[] Paths;
            public readonly bool Quick;
            public Arguments(string[] paths, bool quick) {
                Paths = paths;
                Quick = quick;
            }
        }

        private string Complete_Sliders(Arguments arg, BackgroundWorker worker) {
            int circlesAdded = 0;

            bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (string path in arg.Paths) {
                var selected = new List<HitObject>();
                BeatmapEditor editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, out selected, reader) : new BeatmapEditor(path);
                Beatmap beatmap = editor.Beatmap;
                List<HitObject> markedObjects = selected;

                for (int i = 0; i < markedObjects.Count; i++) {
                    var ho = markedObjects[i];

                    for (int k = i + 1; k < markedObjects.Count; k++) {
                        var otherHo = markedObjects[k];

                        if (!(ho.IsSlider & ho.SliderType == PathType.Linear & otherHo.IsSlider & otherHo.SliderType == PathType.Linear))
                            continue;

                        Line line1 = new Line(ho.Pos, ho.CurvePoints.Last());
                        Line line2 = new Line(otherHo.Pos, otherHo.CurvePoints.Last());

                        if (Line.Intersection(line1, line2, out var intersection)) {
                            if (intersection != Vector2.NaN)
                                beatmap.HitObjects.Add(new HitObject((ho.Time + otherHo.Time) / 2, 0, SampleSet.Auto, SampleSet.Auto) { Pos = intersection });
                        }
                    }
                }

                beatmap.SortHitObjects();

                // Save the file
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }

            // Do stuff
            if (arg.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, editorRead));

            // Make an accurate message
            string message = "";
            if (Math.Abs(circlesAdded) == 1) {
                message += "Successfully added " + circlesAdded + " circle!";
            }
            else {
                message += "Successfully added " + circlesAdded + " circles!";
            }
            return arg.Quick ? "" : message;
        }
    }
}
