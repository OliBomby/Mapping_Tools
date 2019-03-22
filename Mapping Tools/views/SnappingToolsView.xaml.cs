using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.Tools;
using OsuMemoryDataProvider;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class SnappingToolsView :UserControl {
        private BackgroundWorker backgroundWorker;

        public SnappingToolsView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker") ;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Complete_Sliders((Arguments) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
                MessageBox.Show(e.Error.Message);
            }
            else {
                MessageBox.Show(e.Result.ToString());
                progress.Value = 0;
            }
            start.IsEnabled = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            /*DataProvider.Initalize();
            var reader = DataProvider.Instance;
            Console.WriteLine(reader.ReadPlayTime());*/

            DateTime now = DateTime.Now;
            string fileToCopy = MainWindow.AppWindow.currentMap.Text;
            string destinationDirectory = MainWindow.AppWindow.BackupPath;
            try {
                File.Copy(fileToCopy, Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + System.IO.Path.GetFileName(fileToCopy)));
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return;
            }
            backgroundWorker.RunWorkerAsync(new Arguments(fileToCopy, TemporalBox.GetDouble(), SpatialBox.GetDouble(), (bool) ReqBookmBox.IsChecked));
            start.IsEnabled = false;
        }

        private struct Arguments {
            public string Path;
            public double TemporalLength;
            public double SpatialLength;
            public bool RequireBookmarks;
            public Arguments(string path, double temporal, double spatial, bool requireBookmarks)
            {
                Path = path;
                TemporalLength = temporal;
                SpatialLength = spatial;
                RequireBookmarks = requireBookmarks;
            }
        }

        private string Complete_Sliders(Arguments arg, BackgroundWorker worker, DoWorkEventArgs e) {
            int slidersCompleted = 0;

            Editor editor = new Editor(arg.Path);
            Timing timing = editor.Beatmap.BeatmapTiming;
            List<HitObject> markedObjects = arg.RequireBookmarks ? editor.GetBookmarkedObjects() : editor.Beatmap.HitObjects;

            for(int i = 0; i < markedObjects.Count; i++) {
                HitObject ho = markedObjects[i];
                if (ho.IsSlider) {
                    double oldSpatialLength = ho.PixelLength;
                    double newSpatialLength = arg.SpatialLength != 0 ? ho.GetSliderPath(fullLength: true).Distance * arg.SpatialLength : oldSpatialLength;
                    double oldTemporalLength = timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);
                    double newTemporalLength = arg.TemporalLength != 0 ? timing.GetMpBAtTime(ho.Time) * arg.TemporalLength : oldTemporalLength;
                    double oldSV = timing.GetSVAtTime(ho.Time);
                    double newSV = oldSV / ((newSpatialLength / oldSpatialLength) / (newTemporalLength / oldTemporalLength));
                    ho.SV = newSV;
                    ho.PixelLength = newSpatialLength;
                    slidersCompleted++;
                }
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(i / markedObjects.Count);
                }
            }

            // Reconstruct SV
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
            // Add Hitobject stuff
            foreach (HitObject ho in editor.Beatmap.HitObjects)
            {
                if (ho.IsSlider) // SV changes
                {
                    TimingPoint tp = ho.TP.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = ho.SV;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }
            }

            // Add the new SV changes
            timingPointsChanges = timingPointsChanges.OrderBy(o => o.MyTP.Offset).ToList();
            foreach (TimingPointsChange c in timingPointsChanges) {
                c.AddChange(timing.TimingPoints, timing);
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
            if (Math.Abs(slidersCompleted) == 1)
            {
                message += "Successfully completed " + slidersCompleted + " slider!";
            }
            else
            {
                message += "Successfully completed " + slidersCompleted + " sliders!";
            }
            return message;
        }

        private void Print(string str) {
            Console.WriteLine(str);
        }
    }
}
