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
            backgroundWorker.RunWorkerAsync(new Arguments(fileToCopy, (bool)BookmarkBox.IsChecked, (bool)GreenlinesBox.IsChecked, 
                                                          (bool)RedlinesBox.IsChecked, (bool)OmitBarlineBox.IsChecked,
                                                          int.Parse(Snap1.Text.Split('/')[1]), int.Parse(Snap2.Text.Split('/')[1])));
            start.IsEnabled = false;
        }

        private struct Arguments {
            public string Path;
            public bool Bookmarks;
            public bool Greenlines;
            public bool Redlines;
            public bool OmitBarline;
            public int Snap1;
            public int Snap2;
            public Arguments(string path, bool bookmarks, bool greenlines, bool redlines, bool omitBarline, int snap1, int snap2)
            {
                Path = path;
                Bookmarks = bookmarks;
                Greenlines = greenlines;
                Redlines = redlines;
                OmitBarline = omitBarline;
                Snap1 = snap1;
                Snap2 = snap2;
            }
        }

        private string Copy_Hitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs e) {
            // Count
            int RedlinesAdded = 0;

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
            if (arg.Redlines) {
                // Get the offsets of redlines
                times.AddRange(beatmap.BeatmapTiming.TimingPoints.Where(o => o.Inherited == true).Select(o => o.Offset));
            }

            // Loop through all the times
            for (int i = 0; i < times.Count; i++) {
                // Get the info of the time
                double time = times[i];
                double resnappedTime = timing.Resnap(time, arg.Snap1, arg.Snap2, false);

                TimingPoint redline = timing.GetRedlineAtTime(resnappedTime - 10);
                double beatsFromRedline = (resnappedTime - redline.Offset) / redline.MpB;

                // Avoid problems
                if (time == redline.Offset) {
                    continue;
                }
                if (MathHelper.ApproximatelyEquivalent(beatsFromRedline, 0, 0.0001)) {
                    beatsFromRedline = 1 / Math.Max(arg.Snap1, arg.Snap2);
                }

                double mpbOld = redline.MpB;
                double mpb = GetMpB(time, beatsFromRedline, redline);

                // Get the times between redline and this time
                List<double> timesBefore = times.Where(o => o < time && o > redline.Offset).ToList();

                // For each their beatsFromRedline must stay the same AND their time must be within 3-6 ms of their resnapped time
                // If any of these times becomes incompatible, place a new anchor on the last time and not change the previous redline
                bool canChangeRedline = true;
                foreach (double timeB in timesBefore) {
                    // Get the beatsFromRedline after changing mpb
                    redline.MpB = mpb;
                    double resnappedTimeBA = timing.Resnap(timeB, arg.Snap1, arg.Snap2, false);
                    double beatsFromRedlineBA = (resnappedTimeBA - redline.Offset) / redline.MpB;

                    // Get the beatsFromRedline before changing mpb
                    redline.MpB = mpbOld;
                    double resnappedTimeBB = timing.Resnap(timeB, arg.Snap1, arg.Snap2, false);
                    double beatsFromRedlineBB = (resnappedTimeBB - redline.Offset) / redline.MpB;

                    // Check changes
                    if (MathHelper.ApproximatelyEquivalent(beatsFromRedlineBA, beatsFromRedlineBB, 0.1) && IsSnapped(timeB, resnappedTimeBA)){
                        continue;
                    }
                    canChangeRedline = false;
                }

                if (canChangeRedline) {
                    redline.MpB = mpb;
                } else {
                    // Get the last time info
                    double lastTime = timesBefore.Last();
                    double resnappedTimeL = timing.Resnap(lastTime, arg.Snap1, arg.Snap2, false);
                    double beatsFromRedlineL = (resnappedTimeL - redline.Offset) / redline.MpB;

                    // Make new redline
                    TimingPoint newRedline = redline.Copy();
                    newRedline.Offset = lastTime;
                    newRedline.OmitFirstBarLine = arg.OmitBarline; // Set omit of that's the argument
                    timing.TimingPoints.Insert(timing.TimingPoints.IndexOf(redline) + 1, newRedline);

                    // BeatsFromRedline of the last time is subtracted
                    beatsFromRedline = beatsFromRedline - beatsFromRedlineL;

                    // Set the MpB
                    newRedline.MpB = GetMpB(time, beatsFromRedline, newRedline);

                    // Update the counter
                    RedlinesAdded++;
                }

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
            string message = "Successfully added ";
            message += RedlinesAdded;
            if (Math.Abs(RedlinesAdded) == 1) {
                message += " redlines!";
            } else {
                message += " redlines!";
            }
            return message;
        }

        private double GetMpB(double time, double beatsFromRedline, TimingPoint redline) {
            // Will make human-like BPM values like integers, halves and tenths
            // If that doesn't work (like the time is really far from the redline) it will try thousandths
            
            // Exact MpB and BPM
            double mpb = (time - redline.Offset) / beatsFromRedline;
            double bpm = 60000 / mpb;

            // Round bpm
            double mpbInteger = 60000 / Math.Round(bpm);
            if (IsSnapped(time, redline.Offset + mpbInteger * beatsFromRedline)) {
                return mpbInteger;
            }

            // Halves bpm
            double mpbHalves = 60000 / (Math.Round(bpm * 2) / 2);
            if (IsSnapped(time, redline.Offset + mpbHalves * beatsFromRedline)) {
                return mpbHalves;
            }

            // Tenths bpm
            double mpbTenths = 60000 / (Math.Round(bpm * 10) / 10);
            if (IsSnapped(time, redline.Offset + mpbTenths * beatsFromRedline)) {
                return mpbTenths;
            }

            // Thousandths bpm
            double mpbThousandths = 60000 / (Math.Round(bpm * 1000) / 1000);
            if (IsSnapped(time, redline.Offset + mpbThousandths * beatsFromRedline)) {
                return mpbThousandths;
            }

            // Return exact bpm
            return mpb;
        }

        private bool IsSnapped(double time, double resnappedTime, double threshold = 3) {
            if (Math.Abs(resnappedTime - time) <= threshold) {
                return true;
            } else {
                return false;
            }
        }

        private void Print(string str) {
            Console.WriteLine(str);
        }
    }
}
