using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;
using static System.Console;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor TimingCopierView.xaml
    /// </summary>
    public partial class TimingCopierView : MappingTool, ISavable<TimingCopierVM> {
        private readonly BackgroundWorker backgroundWorker;

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "timingcopierproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Timing Copier Projects");

        public TimingCopierView() {
            InitializeComponent();
            DataContext = new TimingCopierVM();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker");
            ProjectManager.LoadProject(this, message: false);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Timing((TimingCopierVM) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
                MessageBox.Show(string.Format("{0}{1}{2}", e.Error.Message, Environment.NewLine, e.Error.StackTrace), "Error");
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
            string filesToCopy = ((TimingCopierVM)DataContext).ExportPath;
            IOHelper.SaveMapBackup(filesToCopy.Split('|'));

            backgroundWorker.RunWorkerAsync(DataContext);
            start.IsEnabled = false;
        }

        private string Copy_Timing(TimingCopierVM arg, BackgroundWorker worker, DoWorkEventArgs _) {
            string[] paths = arg.ExportPath.Split('|');
            int mapsDone = 0;

            bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (string exportPath in paths) {
                BeatmapEditor editorTo = editorRead ? EditorReaderStuff.GetNewestVersion(exportPath, reader) : new BeatmapEditor(exportPath);
                BeatmapEditor editorFrom = editorRead ? EditorReaderStuff.GetNewestVersion(arg.ImportPath, reader) : new BeatmapEditor(arg.ImportPath);

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
                foreach (TimingPoint redline in timingTo.GetAllRedlines()) {
                    TimingPoint greenlineHere = timingTo.GetGreenlineAtTime(redline.Offset);
                    if (greenlineHere.Offset == redline.Offset) {
                        removeList.Add(redline);
                    } else {
                        redline.Inherited = false;
                        redline.MpB = -100;
                    }
                }
                foreach (TimingPoint tp in removeList) {
                    timingTo.TimingPoints.Remove(tp);
                }

                // Make new timing points changes
                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

                // Add redlines
                List<TimingPoint> redlines = timingFrom.GetAllRedlines();
                foreach (TimingPoint tp in redlines) {
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, inherited: true, omitFirstBarLine: true));
                }

                // Apply timing changes
                TimingPointsChange.ApplyChanges(timingTo, timingPointsChanges);

                if (arg.ResnapMode == "Number of beats between objects stays the same") {
                    redlines = timingTo.GetAllRedlines();
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
                        newTime = timingTo.Resnap(newTime, arg.Snap1, arg.Snap2, firstTP: redlines.FirstOrDefault());
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
                        ho.ResnapSelf(timingTo, arg.Snap1, arg.Snap2, firstTP: redlines.FirstOrDefault());
                        ho.ResnapEnd(timingTo, arg.Snap1, arg.Snap2, firstTP: redlines.FirstOrDefault());
                    }

                    // Resnap greenlines
                    foreach (TimingPoint tp in timingTo.GetAllGreenlines())
                    {
                        tp.ResnapSelf(timingTo, arg.Snap1, arg.Snap2, firstTP: redlines.FirstOrDefault());
                    }
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
            string message = string.Format("Successfully copied timing to {0} {1}!", mapsDone, mapsDone == 1 ? "beatmap" : "beatmaps");
            return message;
        }

        private List<Marker> GetMarkers(Beatmap beatmap, Timing timing) {
            List<Marker> markers = new List<Marker>();
            List<TimingPoint> redlines = timing.GetAllRedlines();

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
            double lastTime = redlines.FirstOrDefault().Offset;
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
            public object Object { get; set; }
            public double BeatsFromLastMarker { get; set; }
            public double Time { get => GetTime(); set => SetTime(value); }

            public double GetTime() {
                if (Object is double) {
                    return (double)Object;
                } else if (Object is HitObject) {
                    return ((HitObject)Object).Time;
                } else if (Object is TimingPoint) {
                    return ((TimingPoint)Object).Offset;
                }
                return -1;
            }

            private void SetTime(double value) {
                if (Object is double) {
                    Object = value;
                } else if (Object is HitObject) {
                    ((HitObject)Object).Time = value;
                } else if (Object is TimingPoint) {
                    ((TimingPoint)Object).Offset = value;
                }
            }

            public Marker(object obj) {
                Object = obj;
                BeatsFromLastMarker = 0;
            }
        }

        public TimingCopierVM GetSaveData() {
            return (TimingCopierVM)DataContext;
        }

        public void SetSaveData(TimingCopierVM saveData) {
            DataContext = saveData;
        }
    }
}
