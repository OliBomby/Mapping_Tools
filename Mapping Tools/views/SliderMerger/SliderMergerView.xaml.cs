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
using Mapping_Tools.Views.Standard;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class SliderMergerView :UserControl {
        private readonly BackgroundWorker backgroundWorker;

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
                new MessageWindow(ErrorType.Error, eventArg: e).Show();
            }
            else {
                new MessageWindow(ErrorType.Success, e.Result.ToString()).Show();
                progress.Value = 0;
            }
            start.IsEnabled = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            string fileToCopy = MainWindow.AppWindow.currentMap.Text;
            IOHelper.SaveMapBackup(fileToCopy);

            backgroundWorker.RunWorkerAsync(new Arguments(fileToCopy, LeniencyBox.GetDouble(0), (bool) ReqBookmBox.IsChecked));
            start.IsEnabled = false;
        }

        private struct Arguments {
            public string Path;
            public double Leniency;
            public bool RequireBookmarks;
            public Arguments(string path, double leniency, bool requireBookmarks)
            {
                Path = path;
                Leniency = leniency;
                RequireBookmarks = requireBookmarks;
            }
        }

        private string Merge_Sliders(Arguments arg, BackgroundWorker worker, DoWorkEventArgs _) {
            int slidersMerged = 0;
            bool mergeLast = false;

            BeatmapEditor editor = new BeatmapEditor(arg.Path);
            Beatmap beatmap = editor.Beatmap;
            List<HitObject> markedObjects = arg.RequireBookmarks ? beatmap.GetBookmarkedObjects() : beatmap.HitObjects;

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
                    if(!mergeLast) { slidersMerged++; }
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

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress)
            {
                worker.ReportProgress(100);
            }

            // Make an accurate message
            string message = "";
            if (Math.Abs(slidersMerged) == 1)
            {
                message += $"Successfully merged {slidersMerged} slider!";
            }
            else
            {
                message += $"Successfully merged {slidersMerged} sliders!";
            }
            return message;
        }
    }
}
