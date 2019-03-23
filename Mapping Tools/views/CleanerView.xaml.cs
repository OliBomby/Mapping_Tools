using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.TimeLine;

namespace Mapping_Tools.Views {
    public partial class CleanerView :UserControl {
        private readonly BackgroundWorker backgroundWorker;
        private readonly BackgroundWorker backgroundLoader;
        List<double> TimingpointsRemoved;
        List<double> TimingpointsAdded;
        List<double> TimingpointsChanged;
        double EndTime_monitor;
        TimeLine TL;

        public CleanerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;

            backgroundWorker = ( (BackgroundWorker) this.FindResource("backgroundWorker") );
            backgroundLoader = ( (BackgroundWorker) this.FindResource("backgroundLoader") );
        }

        private void CompileTimeLine(string fileToCopy) {
            if( fileToCopy != "" || fileToCopy != null)
                backgroundLoader.RunWorkerAsync(GetArgumentsFromWindow());
        }

        private void BackgroundLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
            }
            else {
                if( TL != null ) {
                    TL.mainCanvas.Children.Clear();
                }
                try {
                    TL = new TimeLine((int) MainWindow.AppWindow.ActualWidth, 100, EndTime_monitor);
                    foreach( Double timing_s in TimingpointsAdded ) {
                        TL.AddElement(timing_s, 1);
                    }
                    foreach( Double timing_s in TimingpointsChanged ) {
                        TL.AddElement(timing_s, 2);
                    }
                    foreach( Double timing_s in TimingpointsRemoved ) {
                        TL.AddElement(timing_s, 3);
                    }
                    tl_host.Children.Add(TL);
                }
                catch( Exception ex ) {
                    Console.WriteLine(ex.Message);
                    return;
                }
                finally {

                }
            }
        }

        private void BackgroundLoader_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            loader_progress.Value = e.ProgressPercentage;
        }

        private void BackgroundLoader_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            Monitor_Program((Arguments) e.Argument, bgw);
        }

        private void Monitor_Program(Arguments arguments, BackgroundWorker worker) {
            Editor editor = new Editor(arguments.Path);

            List<TimingPoint> originalTimingPoints = new List<TimingPoint>();
            foreach (TimingPoint tp in editor.Beatmap.BeatmapTiming.TimingPoints) { originalTimingPoints.Add(tp.Copy()); }

            MapCleaner.CleanMap(editor.Beatmap, arguments.CleanerArguments, worker);
            List<TimingPoint> newTimingPoints = editor.Beatmap.BeatmapTiming.TimingPoints;

            Monitor_Differences(originalTimingPoints, newTimingPoints);
        }

        private void Monitor_Differences(List<TimingPoint> originalTimingPoints, List<TimingPoint> newTimingPoints) {
            // Take note of all the changes
            TimingpointsChanged = new List<double>();

            var originalInNew = (from first in originalTimingPoints
                                 join second in newTimingPoints
                                 on first.Offset equals second.Offset
                                 select first).ToList();

            var newInOriginal = (from first in originalTimingPoints
                                 join second in newTimingPoints
                                 on first.Offset equals second.Offset
                                 select second).ToList();

            for (int i = 0; i < originalInNew.Count(); i++) {
                if (originalInNew[i] != newInOriginal[i]) {
                    TimingpointsChanged.Add(originalInNew[i].Offset);
                }
            }

            List<double> originalOffsets = new List<double>();
            List<double> newOffsets = new List<double>();
            originalTimingPoints.ForEach(o => originalOffsets.Add(o.Offset));
            newTimingPoints.ForEach(o => newOffsets.Add(o.Offset));

            TimingpointsRemoved = originalOffsets.Except(newOffsets).ToList();
            TimingpointsAdded = newOffsets.Except(originalOffsets).ToList();
            double endTimeOriginal = originalTimingPoints.Last().Offset;
            double endTimeNew = newTimingPoints.Last().Offset;
            EndTime_monitor = endTimeOriginal > endTimeNew ? endTimeOriginal : endTimeNew;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Run_Program((Arguments) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            BackgroundLoader_RunWorkerCompleted(sender, e);

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
            Arguments arguments = GetArgumentsFromWindow();

            DateTime now = DateTime.Now;
            string fileToCopy = arguments.Path;
            string destinationDirectory = MainWindow.AppWindow.BackupPath;
            try {
                File.Copy(fileToCopy, Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + System.IO.Path.GetFileName(fileToCopy)));
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return;
            }

            backgroundWorker.RunWorkerAsync(arguments);

            start.IsEnabled = false;
        }

        private struct Arguments {
            public string Path;
            public MapCleaner.Arguments CleanerArguments;

            public Arguments(string path, MapCleaner.Arguments cleanerArguments) {
                Path = path;
                CleanerArguments = cleanerArguments;
            }
        }

        private Arguments GetArgumentsFromWindow() {
            string fileToCopy = MainWindow.AppWindow.currentMap.Text;
            Arguments arguments = new Arguments(fileToCopy,
                                                new MapCleaner.Arguments((bool)VolumeSliders.IsChecked, (bool)SamplesetSliders.IsChecked,
                                                                         (bool)VolumeSpinners.IsChecked, (bool)RemoveSliderendMuting.IsChecked,
                                                                         (bool)ResnapObjects.IsChecked, (bool)ResnapBookmarks.IsChecked,
                                                                         int.Parse(Snap1.Text.Split('/')[1]), int.Parse(Snap2.Text.Split('/')[1]),
                                                                         (bool)RemoveUnclickableHitsounds.IsChecked));
            return arguments;
        }

        private string Run_Program(Arguments arguments, BackgroundWorker worker, DoWorkEventArgs e) {
            Editor editor = new Editor(arguments.Path);

            List<TimingPoint> orgininalTimingPoints = new List<TimingPoint>();
            foreach (TimingPoint tp in editor.Beatmap.BeatmapTiming.TimingPoints) { orgininalTimingPoints.Add(tp.Copy()); }
            int oldTimingPointsCount = editor.Beatmap.BeatmapTiming.TimingPoints.Count;

            int objectsResnapped = MapCleaner.CleanMap(editor.Beatmap, arguments.CleanerArguments, worker);

            List<TimingPoint> newTimingPoints = editor.Beatmap.BeatmapTiming.TimingPoints;
            Monitor_Differences(orgininalTimingPoints, newTimingPoints);

            // Save the file
            editor.SaveFile();
            

            // Make an accurate message (Softwareporn)
            int removed = oldTimingPointsCount - editor.Beatmap.BeatmapTiming.TimingPoints.Count;
            string message = "";
            if( removed < 0 ) {
                message += "Succesfully added " + Math.Abs(removed);
            }
            else {
                message += "Succesfully removed " + removed;
            }
            if( Math.Abs(removed) == 1 ) {
                message += " greenline and resnapped " + objectsResnapped;
            }
            else {
                message += " greenlines and resnapped " + objectsResnapped;
            }
            if( Math.Abs(objectsResnapped) == 1 ) {
                message += " object!";
            }
            else {
                message += " objects!";
            }
            return message;
        }
    }
}
