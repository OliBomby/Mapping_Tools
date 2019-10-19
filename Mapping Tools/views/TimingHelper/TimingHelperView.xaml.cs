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
    public partial class TimingHelperView {
        public static readonly string ToolName = "Timing Helper";

        public static readonly string ToolDescription = $@"Timing Helper is meant to speed up your timing job by placing the redlines for you. You only have to tell it where exactly all the sounds are.{Environment.NewLine}What you do is place 'markers' exactly on the correct timing of sounds. These markers can be hit objects, bookmarks, greenlines and redlines.{Environment.NewLine}Timing Helper will then adjust BPM and/or add redlines to make every marker be snapped.";

        public TimingHelperView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Adjust_Timing((Arguments) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            string[] filesToCopy = MainWindow.AppWindow.GetCurrentMaps();
            IOHelper.SaveMapBackup(filesToCopy);

            BackgroundWorker.RunWorkerAsync(new Arguments(filesToCopy, (bool)ObjectsBox.IsChecked, (bool)BookmarkBox.IsChecked, (bool)GreenlinesBox.IsChecked,
                                                          (bool)RedlinesBox.IsChecked, (bool)OmitBarlineBox.IsChecked,
                                                          LeniencyBox.GetDouble(defaultValue: 3), TemporalBox.GetDouble(),
                                                          int.Parse(Snap1.Text.Split('/')[1]), int.Parse(Snap2.Text.Split('/')[1])));
            CanRun = false;
        }

        private struct Arguments {
            public string[] Paths;
            public bool Objects;
            public bool Bookmarks;
            public bool Greenlines;
            public bool Redlines;
            public bool OmitBarline;
            public double Leniency;
            public double BeatsBetween;
            public int Snap1;
            public int Snap2;
            public Arguments(string[] paths, bool objects, bool bookmarks, bool greenlines, bool redlines, bool omitBarline, double leniency, double beatsBetween, int snap1, int snap2)
            {
                Paths = paths;
                Objects = objects;
                Bookmarks = bookmarks;
                Greenlines = greenlines;
                Redlines = redlines;
                OmitBarline = omitBarline;
                Leniency = leniency;
                BeatsBetween = beatsBetween;
                Snap1 = snap1;
                Snap2 = snap2;
            }
        }

        private string Adjust_Timing(Arguments arg, BackgroundWorker worker, DoWorkEventArgs _) {
            // Count
            int RedlinesAdded = 0;

            bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (string path in arg.Paths) {
                // Open beatmap
                BeatmapEditor editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, reader) : new BeatmapEditor(path);
                Beatmap beatmap = editor.Beatmap;
                Timing timing = beatmap.BeatmapTiming;

                // Get all the times to snap
                List<Marker> markers = new List<Marker>();
                if (arg.Objects) {
                    foreach (HitObject ho in beatmap.HitObjects) {
                        markers.Add(new Marker(ho.Time));
                    }
                }
                if (arg.Bookmarks) {
                    foreach (double time in beatmap.GetBookmarks()) {
                        markers.Add(new Marker(time));
                    }
                }
                if (arg.Greenlines) {
                    // Get the offsets of greenlines
                    foreach (TimingPoint tp in timing.TimingPoints) {
                        if (tp.Inherited == false) {
                            markers.Add(new Marker(tp.Offset));
                        }
                    }
                }
                if (arg.Redlines) {
                    // Get the offsets of redlines
                    foreach (TimingPoint tp in timing.TimingPoints) {
                        if (tp.Inherited == true) {
                            markers.Add(new Marker(tp.Offset));
                        }
                    }
                }

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(20);
                }

                // Sort the markers
                markers = markers.OrderBy(o => o.Time).ToList();

                // Calculate the beats between time and the last time or redline for each time
                // Time the same is 0
                // Time a little after is smallest snap
                for (int i = 0; i < markers.Count; i++) {
                    Marker marker = markers[i];
                    double time = marker.Time;

                    TimingPoint redline = timing.GetRedlineAtTime(time - 1);

                    // Resnap to that redline only
                    double resnappedTime = timing.Resnap(time, arg.Snap1, arg.Snap2, false, redline);

                    // Calculate beats from the redline
                    double beatsFromRedline = (resnappedTime - redline.Offset) / redline.MpB;

                    // Avoid problems
                    if (MathHelper.ApproximatelyEquivalent(beatsFromRedline, 0, 0.0001)) {
                        beatsFromRedline = 1 / Math.Max(arg.Snap1, arg.Snap2);
                    }
                    if (time == redline.Offset) {
                        beatsFromRedline = 0;
                    }

                    // Initialize the beats from last marker
                    double beatsFromLastMarker = beatsFromRedline;

                    // Get the times between redline and this time
                    List<Marker> timesBefore = markers.Where(o => o.Time < time && o.Time > redline.Offset).ToList();

                    if (timesBefore.Count > 0) {
                        // Get the last time info
                        double lastTime = timesBefore.Last().Time;
                        double resnappedTimeL = timing.Resnap(lastTime, arg.Snap1, arg.Snap2, false);

                        // Change the beats from last marker
                        beatsFromLastMarker = (resnappedTime - resnappedTimeL) / redline.MpB;

                        // Avoid problems
                        if (MathHelper.ApproximatelyEquivalent(beatsFromLastMarker, 0, 0.0001)) {
                            beatsFromLastMarker = 1f / Math.Max(arg.Snap1, arg.Snap2);
                        }
                        if (lastTime == time) {
                            beatsFromLastMarker = 0;
                        }
                    }

                    // Set the variable
                    marker.BeatsFromLastMarker = beatsFromLastMarker;
                }

                // Remove redlines except the first redline
                if (!arg.Redlines) {
                    var first = timing.TimingPoints.FirstOrDefault(o => o.Inherited);
                    timing.TimingPoints.RemoveAll(o => o.Inherited && o != first);
                }

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(40);
                }

                // Loop through all the markers
                for (int i = 0; i < markers.Count; i++) {
                    Marker marker = markers[i];
                    double time = marker.Time;

                    TimingPoint redline = timing.GetRedlineAtTime(time - 1);

                    double beatsFromLastMarker = arg.BeatsBetween != -1 ? arg.BeatsBetween : marker.BeatsFromLastMarker;

                    // Skip if 0 beats from last marker
                    if (beatsFromLastMarker == 0) {
                        continue;
                    }

                    // Get the times between redline and this time including this time
                    List<Marker> markersBefore = markers.Where(o => o.Time < time && o.Time > redline.Offset).ToList();
                    markersBefore.Add(marker);

                    // Calculate MpB
                    // Average MpB from timesBefore and use time from redline
                    double mpb = 0;
                    double beatsFromRedline = 0;
                    foreach (Marker markerB in markersBefore) {
                        beatsFromRedline += markerB.BeatsFromLastMarker;
                        mpb += GetMpB(markerB.Time - redline.Offset, beatsFromRedline, 0);
                    }
                    mpb /= markersBefore.Count;

                    // Check if this MpB doesn't make the markers go offsnap too far
                    bool canChangeRedline = CheckMpB(mpb, markersBefore, redline, arg);

                    // Make changes
                    if (canChangeRedline) {
                        // Round the MpB to human values first
                        mpb = HumanRoundMpB(mpb, markersBefore, redline, arg);

                        // Change the MpB of the redline
                        redline.MpB = mpb;
                    } else {
                        // Get the last time info and not the current
                        markersBefore.Remove(marker);
                        double lastTime = markersBefore.Last().Time;

                        // Make new redline
                        TimingPoint newRedline = redline.Copy();
                        TimingPoint lastHitsounds = timing.GetTimingPointAtTime(lastTime + 5);
                        newRedline.Offset = lastTime;
                        newRedline.OmitFirstBarLine = arg.OmitBarline; // Set omit to the argument
                        newRedline.Kiai = lastHitsounds.Kiai;
                        newRedline.SampleIndex = lastHitsounds.SampleIndex;
                        newRedline.SampleSet = lastHitsounds.SampleSet;
                        newRedline.Volume = lastHitsounds.Volume;
                        timing.TimingPoints.Add(newRedline);
                        timing.Sort();

                        // Set the MpB
                        newRedline.MpB = GetMpB(time - lastTime, beatsFromLastMarker, arg.Leniency);

                        // Update the counter
                        RedlinesAdded++;
                    }

                    // Update progressbar
                    if (worker != null && worker.WorkerReportsProgress) {
                        worker.ReportProgress(i * 60 / markers.Count + 40);
                    }
                }

                // Save the file
                editor.SaveFile();
            }
            

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

        private bool CheckMpB(double mpbNew, List<Marker> markers, TimingPoint redline, Arguments arg) {
            // For each their beatsFromRedline must stay the same AND their time must be within leniency of their resnapped time
            // If any of these times becomes incompatible, place a new anchor on the last time and not change the previous redline
            double mpbOld = redline.MpB;
            double beatsFromRedline = 0;
            bool canChangeRedline = true;
            foreach (Marker markerB in markers) {
                double timeB = markerB.Time;
                beatsFromRedline += markerB.BeatsFromLastMarker;

                // Get the beatsFromRedline after changing mpb
                redline.MpB = mpbNew;
                double resnappedTimeBA = redline.Offset + redline.MpB * beatsFromRedline;
                double beatsFromRedlineBA = (resnappedTimeBA - redline.Offset) / redline.MpB;

                // Change MpB back so the redline doesn't get changed
                redline.MpB = mpbOld;

                // Check changes
                if (MathHelper.ApproximatelyEquivalent(beatsFromRedlineBA, beatsFromRedline, 0.1) && IsSnapped(timeB, resnappedTimeBA, arg.Leniency)) {
                    continue;
                }
                canChangeRedline = false;
            }
            return canChangeRedline;
        }

        private double HumanRoundMpB(double mpb, List<Marker> markers, TimingPoint redline, Arguments arg) {
            double bpm = 60000 / mpb;

            // Round bpm
            double mpbInteger = 60000 / Math.Round(bpm);
            if (CheckMpB(mpbInteger, markers, redline, arg)) {
                return mpbInteger;
            }

            // Halves bpm
            double mpbHalves = 60000 / (Math.Round(bpm * 2) / 2);
            if (CheckMpB(mpbHalves, markers, redline, arg)) {
                return mpbHalves;
            }

            // Tenths bpm
            double mpbTenths = 60000 / (Math.Round(bpm * 10) / 10);
            if (CheckMpB(mpbTenths, markers, redline, arg)) {
                return mpbTenths;
            }

            // Hundredths bpm
            double mpbHundredths = 60000 / (Math.Round(bpm * 100) / 100);
            if (CheckMpB(mpbHundredths, markers, redline, arg)) {
                return mpbHundredths;
            }

            // Thousandths bpm
            double mpbThousandths = 60000 / (Math.Round(bpm * 1000) / 1000);
            if (CheckMpB(mpbThousandths, markers, redline, arg)) {
                return mpbThousandths;
            }

            // Return exact bpm
            return mpb;
        }

        private double GetMpB(double timeFromRedline, double beatsFromRedline, double leniency) {
            // Will make human-like BPM values like integers, halves and tenths
            // If that doesn't work (like the time is really far from the redline) it will try thousandths
            
            // Exact MpB and BPM
            double mpb = timeFromRedline / beatsFromRedline;
            double bpm = 60000 / mpb;

            // Round bpm
            double mpbInteger = 60000 / Math.Round(bpm);
            if (IsSnapped(timeFromRedline, mpbInteger * beatsFromRedline, leniency)) {
                return mpbInteger;
            }

            // Halves bpm
            double mpbHalves = 60000 / (Math.Round(bpm * 2) / 2);
            if (IsSnapped(timeFromRedline, mpbHalves * beatsFromRedline, leniency)) {
                return mpbHalves;
            }

            // Tenths bpm
            double mpbTenths = 60000 / (Math.Round(bpm * 10) / 10);
            if (IsSnapped(timeFromRedline, mpbTenths * beatsFromRedline, leniency)) {
                return mpbTenths;
            }

            // Hundredths bpm
            double mpbHundredths = 60000 / (Math.Round(bpm * 100) / 100);
            if (IsSnapped(timeFromRedline, mpbHundredths * beatsFromRedline, leniency)) {
                return mpbHundredths;
            }

            // Thousandths bpm
            double mpbThousandths = 60000 / (Math.Round(bpm * 1000) / 1000);
            if (IsSnapped(timeFromRedline, mpbThousandths * beatsFromRedline, leniency)) {
                return mpbThousandths;
            }

            // Return exact bpm
            return mpb;
        }

        private static bool IsSnapped(double time, double resnappedTime, double leniency = 3) {
            return Math.Abs(resnappedTime - time) <= leniency;
        }

        private class Marker {
            public double Time { get; }
            public double BeatsFromLastMarker { get; set; }

            public Marker(double time) {
                Time = time;
                BeatsFromLastMarker = 0;
            }
        }
    }
}
