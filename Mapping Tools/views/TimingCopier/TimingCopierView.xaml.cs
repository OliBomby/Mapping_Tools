using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.TimingCopier {
    /// <summary>
    /// Interactielogica voor TimingCopierView.xaml
    /// </summary>
    public partial class TimingCopierView : ISavable<TimingCopierVm> {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "timingcopierproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Timing Copier Projects");

        public static readonly string ToolName = "Timing Copier";

        public static readonly string ToolDescription = $@"Copies timing from A to B.{Environment.NewLine}There are 3 modes that describe how this program will handle moving objects (hitobjects/timingpoints/bookmarks) to the new timing:{Environment.NewLine}'Number of beats between objects stays the same' will move the objects so that the number of beats between objects stays the same. After that it will also resnap to the specified snap divisors. Make sure everything is snapped and don't use this if your new timing is supposed to change the number of beats between objects.{Environment.NewLine}'Just resnap' will snap the objects to the new timing on the specified snap divisors. This doesn't resnap bookmarks.{Environment.NewLine}'Don't move objects' will not move the objects at all.";

        public TimingCopierView() {
            InitializeComponent();
            DataContext = new TimingCopierVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Timing((TimingCopierVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            string filesToCopy = ((TimingCopierVm)DataContext).ExportPath;
            BackupManager.SaveMapBackup(filesToCopy.Split('|'));

            BackgroundWorker.RunWorkerAsync(DataContext);
            CanRun = false;
        }

        private string Copy_Timing(TimingCopierVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            string[] paths = arg.ExportPath.Split('|');
            int mapsDone = 0;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (string exportPath in paths) {
                var editorTo = EditorReaderStuff.GetNewestVersionOrNot(exportPath, reader);
                var editorFrom = EditorReaderStuff.GetNewestVersionOrNot(arg.ImportPath, reader);

                Beatmap beatmapTo = editorTo.Beatmap;
                Beatmap beatmapFrom = editorFrom.Beatmap;

                Timing timingTo = beatmapTo.BeatmapTiming;
                Timing timingFrom = beatmapFrom.BeatmapTiming;

                // Get markers for hitobjects if mode 1 is used
                List<Marker> markers = new List<Marker>();
                if (arg.ResnapMode == "Number of beats between objects stays the same") {
                    markers = GetMarkers(beatmapTo, timingTo);
                }

                // Rid the beatmap of redlines
                // If a greenline exists at the same time as a redline then the redline ceizes to exist
                // Else convert the redline to a greenline: Inherited = false & MpB = -100
                List<TimingPoint> removeList = new List<TimingPoint>();
                foreach (TimingPoint redline in timingTo.Redlines) {
                    TimingPoint greenlineHere = timingTo.GetGreenlineAtTime(redline.Offset);

                    if (greenlineHere.Offset != redline.Offset) {
                        var newGreenline = redline.Copy();
                        newGreenline.Uninherited = false;
                        newGreenline.MpB = -100;

                        timingTo.Add(newGreenline);
                    }

                    removeList.Add(redline);
                }
                foreach (TimingPoint tp in removeList) {
                    timingTo.Remove(tp);
                }

                // Make new timing points changes
                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

                // Add redlines
                var redlines = timingFrom.Redlines;
                foreach (TimingPoint tp in redlines) {
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true));
                }

                // Apply timing changes
                TimingPointsChange.ApplyChanges(timingTo, timingPointsChanges);

                if (arg.ResnapMode == "Number of beats between objects stays the same") {
                    redlines = timingTo.Redlines;
                    List<double> newBookmarks = new List<double>();
                    double lastTime = redlines.FirstOrDefault().Offset;
                    foreach (Marker marker in markers) {
                        // Get redlines between this and last marker
                        TimingPoint redline = timingTo.GetRedlineAtTime(lastTime, redlines.FirstOrDefault());

                        double beatsFromLastTime = marker.BeatsFromLastMarker;
                        while (true) {
                            List<TimingPoint> redlinesBetween = redlines.Where(o => o.Offset <= lastTime + redline.MpB * beatsFromLastTime && o.Offset > lastTime).ToList();

                            if (redlinesBetween.Count == 0) break;

                            TimingPoint first = redlinesBetween.First();
                            double diff = first.Offset - lastTime;
                            beatsFromLastTime -= diff / redline.MpB;

                            redline = first;
                            lastTime = first.Offset;
                        }

                        // Last time is the time of the last redline in between
                        double newTime = lastTime + redline.MpB * beatsFromLastTime;
                        newTime = timingTo.Resnap(newTime, arg.BeatDivisors, firstTp: redlines.FirstOrDefault());
                        marker.Time = newTime;

                        lastTime = marker.Time;
                    }

                    // Add the bookmarks
                    foreach (Marker marker in markers)
                    {
                        // Check whether the marker is a bookmark
                        if (marker.Object is double) {
                            // Don't resnap bookmarks
                            newBookmarks.Add((double)marker.Object);
                        }
                    }
                    beatmapTo.SetBookmarks(newBookmarks);
                } else if (arg.ResnapMode == "Just resnap") {
                    // Resnap hitobjects
                    foreach (HitObject ho in beatmapTo.HitObjects)
                    {
                        ho.ResnapSelf(timingTo, arg.BeatDivisors, firstTp: redlines.FirstOrDefault());
                        ho.ResnapEnd(timingTo, arg.BeatDivisors, firstTp: redlines.FirstOrDefault());
                    }

                    // Resnap greenlines
                    foreach (TimingPoint tp in timingTo.Greenlines)
                    {
                        tp.ResnapSelf(timingTo, arg.BeatDivisors, firstTP: redlines.FirstOrDefault());
                    }
                    timingTo.Sort();
                } else {
                    // Don't move objects
                }

                // Save the file
                editorTo.SaveFile();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            // Make an accurate message
            string message = $"Successfully copied timing to {mapsDone} {(mapsDone == 1 ? "beatmap" : "beatmaps")}!";
            return message;
        }

        private List<Marker> GetMarkers(Beatmap beatmap, Timing timing) {
            List<Marker> markers = new List<Marker>();
            var redlines = timing.Redlines;

            foreach (HitObject ho in beatmap.HitObjects) {
                markers.Add(new Marker(ho));
            }
            foreach (double bookmark in beatmap.GetBookmarks()) {
                markers.Add(new Marker(bookmark));
            }
            foreach (TimingPoint greenline in timing.TimingPoints) {
                markers.Add(new Marker(greenline));
            }

            // Sort the markers
            markers = markers.OrderBy(o => o.Time).ToList();

            // Calculate the beats between this marker and the last marker
            // If there is a redline in between then calculate beats from last marker to the redline and beats from redline to this marker
            // Time the same is 0
            double lastTime = redlines.First().Offset;
            foreach (Marker marker in markers) {
                // Get redlines between this and last marker
                List<TimingPoint> redlinesBetween = redlines.Where(o => o.Offset < marker.Time && o.Offset > lastTime).ToList();
                TimingPoint redline = timing.GetRedlineAtTime(lastTime);

                double beatsFromLastMarker = 0;
                foreach (TimingPoint redlineBetween in redlinesBetween) {
                    beatsFromLastMarker += (redlineBetween.Offset - lastTime) / redline.MpB;
                    redline = redlineBetween;
                    lastTime = redlineBetween.Offset;
                }
                beatsFromLastMarker += (marker.Time - lastTime) / redline.MpB;

                // Set the variable
                marker.BeatsFromLastMarker = beatsFromLastMarker;

                lastTime = marker.Time;
            }

            return markers;
        }

        private class Marker
        {
            public object Object { get; private set; }
            public double BeatsFromLastMarker { get; set; }
            public double Time { get => GetTime(); set => SetTime(value); }

            private double GetTime() {
                switch (Object) {
                    case double d:
                        return d;
                    case HitObject hitObject:
                        return hitObject.Time;
                    case TimingPoint point:
                        return point.Offset;
                    default:
                        return -1;
                }
            }

            private void SetTime(double value) {
                switch (Object) {
                    case double _:
                        Object = value;
                        break;
                    case HitObject hitObject:
                        hitObject.Time = value;
                        break;
                    case TimingPoint point:
                        point.Offset = value;
                        break;
                }
            }

            public Marker(object obj) {
                Object = obj;
                BeatsFromLastMarker = 0;
            }
        }

        public TimingCopierVm GetSaveData() {
            return (TimingCopierVm)DataContext;
        }

        public void SetSaveData(TimingCopierVm saveData) {
            DataContext = saveData;
        }
    }
}
