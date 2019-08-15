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
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Views {
    public partial class CleanerView :UserControl {
        private readonly BackgroundWorker backgroundWorker;
        List<double> TimingpointsRemoved;
        List<double> TimingpointsAdded;
        List<double> TimingpointsChanged;
        double EndTime_monitor;
        TimeLine TL;

        public CleanerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;

            backgroundWorker = ( (BackgroundWorker) FindResource("backgroundWorker") );
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            Arguments arguments = GetArgumentsFromWindow();
            
            string fileToCopy = arguments.Path;
            IOHelper.SaveMapBackup(fileToCopy);

            backgroundWorker.RunWorkerAsync(arguments);

            start.IsEnabled = false;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Run_Program((Arguments) e.Argument, bgw, e);
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
                MessageBox.Show(string.Format("{0}{1}{2}", e.Error.Message, Environment.NewLine, e.Error.StackTrace), "Error");
            }
            else {
                FillTimeLine();
                MessageBox.Show(e.Result.ToString());
                progress.Value = 0;
            }
            start.IsEnabled = true;
        }

        private struct Arguments {
            public string Path;
            public MapCleaner.MapCleanerArgs CleanerArguments;

            public Arguments(string path, MapCleaner.MapCleanerArgs cleanerArguments) {
                Path = path;
                CleanerArguments = cleanerArguments;
            }
        }

        private Arguments GetArgumentsFromWindow() {
            string fileToCopy = MainWindow.AppWindow.currentMap.Text;
            Arguments arguments = new Arguments(fileToCopy,
                                                new MapCleaner.MapCleanerArgs((bool)VolumeSliders.IsChecked, (bool)SamplesetSliders.IsChecked, (bool)VolumeSpinners.IsChecked,
                                                                         (bool)ResnapObjects.IsChecked, (bool)ResnapBookmarks.IsChecked,
                                                                         (bool)RemoveUnusedSamples.IsChecked,
                                                                         (bool)RemoveMuting.IsChecked,
                                                                         (bool)RemoveUnclickableHitsounds.IsChecked,
                                                                         int.Parse(Snap1.Text.Split('/')[1]), int.Parse(Snap2.Text.Split('/')[1])));
            return arguments;
        }

        private string Run_Program(Arguments args, BackgroundWorker worker, DoWorkEventArgs _) {
            BeatmapEditor editor = new BeatmapEditor(args.Path);

            List<TimingPoint> orgininalTimingPoints = new List<TimingPoint>();
            foreach (TimingPoint tp in editor.Beatmap.BeatmapTiming.TimingPoints) { orgininalTimingPoints.Add(tp.Copy()); }
            int oldTimingPointsCount = editor.Beatmap.BeatmapTiming.TimingPoints.Count;

            var result = MapCleaner.CleanMap(editor, args.CleanerArguments, worker);

            List<TimingPoint> newTimingPoints = editor.Beatmap.BeatmapTiming.TimingPoints;
            Monitor_Differences(orgininalTimingPoints, newTimingPoints);

            // Save the file
            editor.SaveFile();
            

            // Make an accurate message
            int removed = oldTimingPointsCount - editor.Beatmap.BeatmapTiming.TimingPoints.Count;
            string message = $"Successfully {(removed < 0 ? "added" : "removed")} {Math.Abs(removed)} {(Math.Abs(removed) == 1 ? "greenline" : "greenlines")}" +
                (args.CleanerArguments.ResnapObjects ? $" and resnapped {result.ObjectsResnapped} {(result.ObjectsResnapped == 1 ? "object" : "objects")}" : "") + 
                (args.CleanerArguments.RemoveUnusedSamples ? $" and removed {result.SamplesRemoved} unused {(result.SamplesRemoved == 1 ? "sample" : "samples")}" : "") + "!";
            return message;
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
            
            foreach (TimingPoint tp in originalInNew) {
                bool different = true;
                List<TimingPoint> newTPs = newInOriginal.Where(o => o.Offset == tp.Offset).ToList();
                if (newTPs.Count == 0) { different = false; }
                foreach (TimingPoint newTP in newTPs) {
                    if (tp.Equals(newTP)) { different = false; }
                }
                if (different) { TimingpointsChanged.Add(tp.Offset); }
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

        private void FillTimeLine() {
            if (TL != null) {
                TL.mainCanvas.Children.Clear();
            }
            try {
                TL = new TimeLine(MainWindow.AppWindow.ActualWidth, 100.0, EndTime_monitor);
                foreach (double timing_s in TimingpointsAdded) {
                    TL.AddElement(timing_s, 1);
                }
                foreach (double timing_s in TimingpointsChanged) {
                    TL.AddElement(timing_s, 2);
                }
                foreach (double timing_s in TimingpointsRemoved) {
                    TL.AddElement(timing_s, 3);
                }
                tl_host.Children.Clear();
                tl_host.Children.Add(TL);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return;
            } finally {

            }
        }
    }
}
