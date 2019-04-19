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
    /// Interactielogica voor TimingHelperView.xaml
    /// </summary>
    public partial class TimingHelperView :UserControl {
        private BackgroundWorker backgroundWorker;

        public TimingHelperView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker") ;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Hitsounds((Arguments) e.Argument, bgw, e);
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
            backgroundWorker.RunWorkerAsync(new Arguments(fileToCopy, (bool)BookmarkBox.IsChecked, (bool)GreenlinesBox.IsChecked, (bool)OmitBarlineBox.IsChecked,
                                                          int.Parse(Snap1.Text.Split('/')[1]), int.Parse(Snap2.Text.Split('/')[1])));
            start.IsEnabled = false;
        }

        private struct Arguments {
            public string Path;
            public bool Bookmarks;
            public bool Greenlines;
            public bool OmitBarline;
            public int Snap1;
            public int Snap2;
            public Arguments(string path, bool bookmarks, bool greenlines, bool omitBarline, int snap1, int snap2)
            {
                Path = path;
                Bookmarks = bookmarks;
                Greenlines = greenlines;
                OmitBarline = omitBarline;
                Snap1 = snap1;
                Snap2 = snap2;
            }
        }

        private string Copy_Hitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs e) {
            // Open beatmap
            Editor editor = new Editor(arg.Path);
            Beatmap beatmap = editor.Beatmap;
            Timing timing = beatmap.BeatmapTiming;

            // Get all the times to snap
            List<double> times = new List<double>();
            if (arg.Bookmarks) {
                times.AddRange(beatmap.GetBookmarks());
            }
            if (arg.Greenlines) {
                // Get the offsets of greenlines
                times.AddRange(beatmap.BeatmapTiming.TimingPoints.Where(o => o.Inherited == false).Select(o => o.Offset));
            }

            // Loop through all the times
            for (int i = 0; i < times.Count; i++) {
                double time = times[i];
                double resnappedTime = timing.Resnap(time, arg.Snap1, arg.Snap2, false);

                TimingPoint redline = timing.GetRedlineAtTime(resnappedTime);
                double beatsFromRedline = (resnappedTime - redline.Offset) / redline.MpB;

                // Get the times between redline and this time
                List<double> timesBefore = times.Where(o => o < time && o > redline.Offset).ToList();

                // For each their beatsFromRedline must stay the same AND their time must be within 3-6 ms of their resnapped time
                // If any of these times becomes incompatible, place a new anchor on the last time and not change the previous redline

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(i * 100 / times.Count);
                }
            }
            
            // Save the file
            editor.SaveFile();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }

            // Make an accurate message
            string message = "";
            message += "Done!";
            return message;
        }

        private void Print(string str) {
            Console.WriteLine(str);
        }
    }
}
