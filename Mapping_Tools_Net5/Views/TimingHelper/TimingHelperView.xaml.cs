using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.TimingHelper {
    /// <summary>
    /// Interactielogica voor TimingHelperView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class TimingHelperView : IQuickRun, ISavable<TimingHelperVm> {
        public static readonly string ToolName = "Timing Helper";

        public static readonly string ToolDescription = $@"Timing Helper is meant to speed up your timing job by placing the redlines for you. You only have to tell it exactly where all the sounds are.{Environment.NewLine}What you do is place 'markers' exactly on the correct timing of sounds. These markers can be hit objects, bookmarks, greenlines and redlines.{Environment.NewLine}Timing Helper will then adjust BPM and/or add redlines to make every marker be snapped.";

        public TimingHelperView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new TimingHelperVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public TimingHelperVm ViewModel => (TimingHelperVm) DataContext;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Adjust_Timing((TimingHelperVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps());
        }

        public void QuickRun() {
            RunTool(new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}, true);
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

        private string Adjust_Timing(TimingHelperVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            // Count
            int redlinesAdded = 0;
            
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (string path in arg.Paths) {
                // Open beatmap
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                Beatmap beatmap = editor.Beatmap;
                Timing timing = beatmap.BeatmapTiming;

                // Get all the times to snap
                List<Marker> markers = new List<Marker>();
                if (arg.Objects) {
                    markers.AddRange(beatmap.HitObjects.Select(ho => new Marker(ho.Time)));
                }
                if (arg.Bookmarks) {
                    markers.AddRange(beatmap.GetBookmarks().Select(time => new Marker(time)));
                }
                if (arg.Greenlines) {
                    // Get the offsets of greenlines
                    markers.AddRange(from tp in timing.TimingPoints where !tp.Uninherited select new Marker(tp.Offset));
                }
                if (arg.Redlines) {
                    // Get the offsets of redlines
                    markers.AddRange(from tp in timing.TimingPoints where tp.Uninherited select new Marker(tp.Offset));
                }

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(20);
                }

                // Sort the markers
                markers = markers.OrderBy(o => o.Time).ToList();

                // If there are no redlines add one with a default BPM
                if (!timing.TimingPoints.Any(tp => tp.Uninherited)) {
                    timing.Add(new TimingPoint(0, 1000, 4, SampleSet.Soft, 0, 100, true, false, false));
                }

                // Remove multiple markers on the same tick
                var newMarkers = new List<Marker>(markers.Count);
                newMarkers.AddRange(markers.Where((t, i) => i == 0 || Math.Abs(t.Time - markers[i - 1].Time) >= arg.Leniency + Precision.DOUBLE_EPSILON));
                markers = newMarkers;

                // Calculate the beats between time and the last time or redline for each time
                // Time the same is 0
                // Time a little after is smallest snap
                foreach (var marker in markers) {
                    double time = marker.Time;

                    TimingPoint redline = timing.GetRedlineAtTime(time - 1);

                    // Resnap to that redline only
                    double resnappedTime = timing.Resnap(time, arg.BeatDivisors, false, tp: redline);

                    // Calculate beats from the redline
                    double beatsFromRedline = (resnappedTime - redline.Offset) / redline.MpB;

                    // Avoid problems
                    if (MathHelper.ApproximatelyEquivalent(beatsFromRedline, 0, 0.0001)) {
                        beatsFromRedline = arg.BeatDivisors.Min(o => o.GetValue());
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
                        double resnappedTimeL = timing.Resnap(lastTime, arg.BeatDivisors, false);

                        // Change the beats from last marker
                        beatsFromLastMarker = (resnappedTime - resnappedTimeL) / redline.MpB;

                        // Avoid problems
                        if (MathHelper.ApproximatelyEquivalent(beatsFromLastMarker, 0, 0.0001)) {
                            beatsFromLastMarker = arg.BeatDivisors.Min(o => o.GetValue());
                        }
                        if (lastTime == time) {
                            beatsFromLastMarker = 0;
                        }
                    }

                    // Set the variable
                    marker.BeatsFromLastMarker = beatsFromLastMarker;

                    if (arg.BeatsBetween != -1) {
                        marker.BeatsFromLastMarker = arg.BeatsBetween;
                    }
                }

                // Remove redlines except the first redline
                if (!arg.Redlines) {
                    var first = timing.TimingPoints.FirstOrDefault(o => o.Uninherited);
                    timing.RemoveAll(o => o.Uninherited && o != first);
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

                    double beatsFromLastMarker = marker.BeatsFromLastMarker;

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
                        timing.Add(newRedline);

                        // Set the MpB
                        newRedline.MpB = GetMpB(time - lastTime, beatsFromLastMarker, arg.Leniency);

                        // Update the counter
                        redlinesAdded++;
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

            // Do QuickRun stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, arg.Quick));

            // Make an accurate message
            string message = "Successfully added ";
            message += redlinesAdded;
            if (Math.Abs(redlinesAdded) == 1) {
                message += " redlines!";
            } else {
                message += " redlines!";
            }

            return arg.Quick ? string.Empty : message;
        }

        private static bool CheckMpB(double mpbNew, IEnumerable<Marker> markers, TimingPoint redline, TimingHelperVm arg) {
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
                double resnappedTimeBa = redline.Offset + redline.MpB * beatsFromRedline;
                double beatsFromRedlineBa = (resnappedTimeBa - redline.Offset) / redline.MpB;

                // Change MpB back so the redline doesn't get changed
                redline.MpB = mpbOld;

                // Check changes
                if (MathHelper.ApproximatelyEquivalent(beatsFromRedlineBa, beatsFromRedline, 0.1) && IsSnapped(timeB, resnappedTimeBa, arg.Leniency)) {
                    continue;
                }
                canChangeRedline = false;
            }
            return canChangeRedline;
        }

        private static double HumanRoundMpB(double mpb, IReadOnlyCollection<Marker> markers, TimingPoint redline, TimingHelperVm arg) {
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

        private static double GetMpB(double timeFromRedline, double beatsFromRedline, double leniency) {
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

        public event EventHandler RunFinished;

        public TimingHelperVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(TimingHelperVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "timinghelperproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Timing Helper Projects");
    }
}
