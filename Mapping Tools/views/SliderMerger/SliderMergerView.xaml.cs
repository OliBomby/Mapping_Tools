using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class SliderMergerView : MappingTool, IQuickRun {
        private readonly BackgroundWorker backgroundWorker;
        private bool canRun = true;

        public event EventHandler RunFinished;

        public SliderMergerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker") ;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Merge_Sliders((Arguments) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
                MessageBox.Show(string.Format("{0}{1}{2}", e.Error.Message, Environment.NewLine, e.Error.StackTrace), "Error");
            }
            else {
                if (e.Result.ToString() != "")
                    MessageBox.Show(e.Result.ToString());
                progress.Value = 0;
            }
            start.IsEnabled = true;
            canRun = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps(), quick:false);
        }

        public void QuickRun() {
            RunTool(new[] { IOHelper.GetCurrentBeatmap() }, quick:true);
        }

        private void RunTool(string[] paths, bool quick=false) {
            if (!canRun) return;

            IOHelper.SaveMapBackup(paths);

            backgroundWorker.RunWorkerAsync(new Arguments(paths, LeniencyBox.GetDouble(0), SelectionModeBox.SelectedIndex, quick));
            start.IsEnabled = false;
            canRun = false;
        }

        private struct Arguments {
            public string[] Paths;
            public double Leniency;
            public int SelectionMode;
            public bool Quick;
            public Arguments(string[] paths, double leniency, int selectionMode, bool quick)
            {
                Paths = paths;
                Leniency = leniency;
                SelectionMode = selectionMode;
                Quick = quick;
            }
        }

        private string Merge_Sliders(Arguments arg, BackgroundWorker worker, DoWorkEventArgs _) {
            int slidersMerged = 0;

            bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (string path in arg.Paths) {
                var selected = new List<HitObject>();
                BeatmapEditor editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, out selected, reader) : new BeatmapEditor(path);
                Beatmap beatmap = editor.Beatmap;
                List<HitObject> markedObjects = arg.SelectionMode == 0 ? selected : 
                                                arg.SelectionMode == 1 ? beatmap.GetBookmarkedObjects() : 
                                                                         beatmap.HitObjects;

                bool mergeLast = false;
                for (int i = 0; i < markedObjects.Count - 1; i++) {
                    HitObject ho1 = markedObjects[i];
                    HitObject ho2 = markedObjects[i + 1];
                    if (ho1.IsSlider && ho2.IsSlider && (ho1.CurvePoints.Last() - ho2.Pos).Length <= arg.Leniency) {
                        ho2.Move(ho1.CurvePoints.Last() - ho2.Pos);

                        SliderPath sp1 = BezierConverter.ConvertToBezier(ho1.SliderPath);
                        SliderPath sp2 = BezierConverter.ConvertToBezier(ho2.SliderPath);
                        Vector2[] mergedAnchors = sp1.ControlPoints.Concat(sp2.ControlPoints).ToArray();
                        mergedAnchors.Round();

                        SliderPath mergedPath = new SliderPath(PathType.Bezier, mergedAnchors, ho1.PixelLength + ho2.PixelLength);
                        ho1.SliderPath = mergedPath;

                        beatmap.HitObjects.Remove(ho2);
                        markedObjects.Remove(ho2);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) { slidersMerged++; }
                        mergeLast = true;
                    } else {
                        mergeLast = false;
                    }
                    if (worker != null && worker.WorkerReportsProgress) {
                        worker.ReportProgress(i / markedObjects.Count);
                    }
                }

                // Save the file
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress)
            {
                worker.ReportProgress(100);
            }

            // Do stuff
            if (arg.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, editorRead));

            // Make an accurate message
            string message = "";
            if (Math.Abs(slidersMerged) == 1)
            {
                message += "Successfully merged " + slidersMerged + " slider!";
            }
            else
            {
                message += "Successfully merged " + slidersMerged + " sliders!";
            }
            return arg.Quick ? "" : message;
        }
    }
}
