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

namespace Mapping_Tools.Views {
    public partial class CleanerView :UserControl {
        private readonly BackgroundWorker backgroundWorker;
        public readonly BackgroundWorker backgroundLoader;

        public CleanerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = ( (BackgroundWorker) this.FindResource("backgroundWorker") );
            backgroundLoader = ( (BackgroundWorker) this.FindResource("backgroundLoader") );
        }

        private void UpdateLoaderProgress(BackgroundWorker worker, double fraction, int stage, int maxStages) {
            // Update progressbar
            if( worker != null && worker.WorkerReportsProgress ) {
                worker.ReportProgress((int) ( ( fraction + stage ) / maxStages * 100 ));
            }
        }

        private void BackgroundLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
            }
            else {

            }
        }

        private void BackgroundLoader_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void BackgroundLoader_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            Monitor_Program((List<string>) e.Argument, bgw);
        }

        private void Monitor_Program(List<string> arguments, BackgroundWorker worker) {

            // Retrieve all arguments
            string path = arguments[0];
            bool volumeSliders = arguments[1] == "True";
            bool samplesetSliders = arguments[2] == "True";
            bool volumeSpinners = arguments[3] == "True";
            bool removeSliderendMuting = arguments[4] == "True";
            bool resnapObjects = arguments[5] == "True";
            bool resnapBookmarks = arguments[6] == "True";
            int snap1 = int.Parse(arguments[7].Split('/')[1]);
            int snap2 = int.Parse(arguments[8].Split('/')[1]);
            bool removeUnclickabeHitsounds = arguments[9] == "True";

            Editor editor = new Editor(path);
            Timing timing = editor.Beatmap.BeatmapTiming;
            Timeline timeline = editor.Beatmap.GetTimeline();

            List<TimingPoint> original = new List<TimingPoint>();
            foreach(TimingPoint tp in timing.TimingPoints) { original.Add(tp.Copy()); }

            int mode = editor.Beatmap.General["Mode"].Value;
            int num_timingPoints = editor.Beatmap.BeatmapTiming.TimingPoints.Count;
            int objectsResnapped = 0;

            // Count total stages
            int maxStages = 11;

            // Collect Kiai toggles and SV changes for mania/taiko
            List<TimingPoint> kiaiToggles = new List<TimingPoint>();
            List<TimingPoint> svChanges = new List<TimingPoint>();
            bool lastKiai = false;
            double lastSV = -100;
            for (int i = 0; i < timing.TimingPoints.Count; i++)
            {
                TimingPoint tp = timing.TimingPoints[i];
                if (tp.Kiai != lastKiai)
                {
                    kiaiToggles.Add(tp.Copy());
                    lastKiai = tp.Kiai;
                }
                if (tp.Inherited)
                {
                    lastSV = -100;
                }
                else
                {
                    if (tp.MpB != lastSV)
                    {
                        svChanges.Add(tp.Copy());
                        lastSV = tp.MpB;
                    }
                }
                UpdateProgressbar(worker, (double)i / timing.TimingPoints.Count, 0, maxStages);
            }

            // Resnap shit
            if (resnapObjects)
            {
                // Resnap all objects
                for (int i = 0; i < editor.Beatmap.HitObjects.Count; i++)
                {
                    HitObject ho = editor.Beatmap.HitObjects[i];
                    bool resnapped = ho.ResnapSelf(timing, snap1, snap2);
                    if (resnapped)
                    {
                        objectsResnapped += 1;
                    }
                    ho.ResnapEnd(timing, snap1, snap2);
                    UpdateProgressbar(worker, (double)i / editor.Beatmap.HitObjects.Count, 1, maxStages);
                }

                // Resnap Kiai toggles and SV changes
                for (int i = 0; i < kiaiToggles.Count; i++)
                {
                    TimingPoint tp = kiaiToggles[i];
                    tp.ResnapSelf(timing, snap1, snap2);
                    UpdateProgressbar(worker, (double)i / kiaiToggles.Count, 2, maxStages);
                }
                for (int i = 0; i < svChanges.Count; i++)
                {
                    TimingPoint tp = svChanges[i];
                    tp.ResnapSelf(timing, snap1, snap2);
                    UpdateProgressbar(worker, (double)i / svChanges.Count, 3, maxStages);
                }
            }

            if (resnapBookmarks)
            {
                // Resnap the bookmarks
                List<double> newBookmarks = new List<double>();
                List<double> bookmarks = editor.Beatmap.GetBookmarks();
                for (int i = 0; i < bookmarks.Count; i++)
                {
                    double bookmark = bookmarks[i];
                    newBookmarks.Add(Math.Floor(timing.Resnap(bookmark, snap1, snap2)));
                    UpdateProgressbar(worker, (double)i / bookmarks.Count, 4, maxStages);
                }
                editor.Beatmap.SetBookmarks(newBookmarks);
            }

            // Maybe mute unclickable timelineobjects
            if (removeUnclickabeHitsounds)
            {
                foreach (TimelineObject tlo in timeline.TimeLineObjects)
                {
                    if (!(tlo.IsCircle || tlo.IsSliderHead || tlo.IsHoldnoteHead))  // Not clickable
                    {
                        tlo.FenoSampleVolume = 5;  // 5% volume mute
                    }
                }
            }

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
            // Add redlines
            List<TimingPoint> redlines = timing.GetAllRedlines();
            for (int i = 0; i < redlines.Count; i++)
            {
                TimingPoint tp = redlines[i];
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, inherited: true));
                UpdateProgressbar(worker, (double)i / redlines.Count, 5, maxStages);
            }
            // Add SV changes for taiko and mania
            if (mode == 1 || mode == 3)
            {
                for (int i = 0; i < svChanges.Count; i++)
                {
                    TimingPoint tp = svChanges[i];
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                    UpdateProgressbar(worker, (double)i / svChanges.Count, 6, maxStages);
                }
            }
            // Add Kiai toggles
            for (int i = 0; i < kiaiToggles.Count; i++)
            {
                TimingPoint tp = kiaiToggles[i];
                timingPointsChanges.Add(new TimingPointsChange(tp, kiai: true));
                UpdateProgressbar(worker, (double)i / kiaiToggles.Count, 7, maxStages);
            }
            // Add Hitobject stuff
            for (int i = 0; i < editor.Beatmap.HitObjects.Count; i++)
            {
                HitObject ho = editor.Beatmap.HitObjects[i];
                if (ho.IsSlider) // SV changes
                {
                    TimingPoint tp = ho.TP.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = ho.SV;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }
                // Body hitsounds
                bool vol = (ho.IsSlider && volumeSliders) || (ho.IsSpinner && volumeSpinners);
                bool sam = (ho.IsSlider && samplesetSliders && ho.SampleSet == 0);
                bool ind = (ho.IsSlider && samplesetSliders);
                bool samplesetActuallyChanged = false;
                foreach (TimingPoint tp in ho.BodyHitsounds)
                {
                    if (tp.Volume == 5 && removeSliderendMuting) { vol = false; }  // Removing sliderbody silencing
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind, sampleset: sam));
                    if (tp.SampleSet != ho.HitsoundTP.SampleSet) { samplesetActuallyChanged = samplesetSliders && ho.SampleSet == 0; }  // True for sampleset change in sliderbody
                }
                if (ho.IsSlider && (!samplesetActuallyChanged) && ho.SampleSet == 0)  // Case can put sampleset on sliderbody
                {
                    ho.SampleSet = ho.HitsoundTP.SampleSet;
                    ho.SliderExtras = true;
                }
                if (ho.IsSlider && samplesetActuallyChanged) // Make it start out with the right sampleset
                {
                    TimingPoint tp = ho.HitsoundTP.Copy();
                    tp.Offset = ho.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: true));
                }
                UpdateProgressbar(worker, (double)i / editor.Beatmap.HitObjects.Count, 8, maxStages);
            }
            // Add timeline hitsounds
            for (int i = 0; i < timeline.TimeLineObjects.Count; i++)
            {
                TimelineObject tlo = timeline.TimeLineObjects[i];
                // Change the samplesets in the hitobjects
                if (tlo.Origin.IsCircle)
                {
                    tlo.Origin.SampleSet = tlo.FenoSampleSet;
                    tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                    if (mode == 3)
                    {
                        tlo.Origin.CustomIndex = tlo.FenoCustomIndex;
                        tlo.Origin.SampleVolume = tlo.FenoSampleVolume;
                    }
                }
                else if (tlo.Origin.IsSlider)
                {
                    tlo.Origin.EdgeHitsounds[tlo.Repeat] = tlo.GetHitsounds();
                    tlo.Origin.EdgeSampleSets[tlo.Repeat] = tlo.FenoSampleSet;
                    tlo.Origin.EdgeAdditionSets[tlo.Repeat] = tlo.FenoAdditionSet;
                    tlo.Origin.SliderExtras = true;
                    if (tlo.Origin.EdgeAdditionSets[tlo.Repeat] == tlo.Origin.EdgeSampleSets[tlo.Repeat])  // Simplify additions to auto
                    {
                        tlo.Origin.EdgeAdditionSets[tlo.Repeat] = 0;
                    }
                }
                else if (tlo.Origin.IsSpinner)
                {
                    if (tlo.Repeat == 1)
                    {
                        tlo.Origin.SampleSet = tlo.FenoSampleSet;
                        tlo.Origin.AdditionSet = tlo.FenoAdditionSet;

                    }
                }
                else if (tlo.Origin.IsHoldNote)
                {
                    if (tlo.Repeat == 0)
                    {
                        tlo.Origin.SampleSet = tlo.FenoSampleSet;
                        tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                        tlo.Origin.CustomIndex = tlo.FenoCustomIndex;
                        tlo.Origin.SampleVolume = tlo.FenoSampleVolume;
                    }
                }
                if (tlo.Origin.AdditionSet == tlo.Origin.SampleSet)  // Simplify additions to auto
                {
                    tlo.Origin.AdditionSet = 0;
                }
                if (mode == 0 && tlo.HasHitsound) // Add greenlines for custom indexes and volumes
                {
                    TimingPoint tp = tlo.Origin.TP.Copy();
                    tp.Offset = tlo.Time;
                    tp.SampleIndex = tlo.FenoCustomIndex;
                    tp.Volume = tlo.FenoSampleVolume;
                    bool ind = !(tlo.Filename != "" && (tlo.IsCircle || tlo.IsHoldnoteHead || tlo.IsSpinnerEnd));  // Index doesnt have to change if custom is overridden by Filename
                    bool vol = !(tp.Volume == 5 && removeSliderendMuting && (tlo.IsSliderEnd || tlo.IsSpinnerEnd));  // Remove volume change if sliderend muting or spinnerend muting
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind));
                }
                UpdateProgressbar(worker, (double)i / timeline.TimeLineObjects.Count, 9, maxStages);
            }


            // Add the new timingpoints
            timingPointsChanges = timingPointsChanges.OrderBy(o => o.TP.Offset).ToList();
            List<TimingPoint> newTimingPoints = new List<TimingPoint>();
            for (int i = 0; i < timingPointsChanges.Count; i++)
            {
                TimingPointsChange c = timingPointsChanges[i];
                c.AddChange(newTimingPoints, timing);
                UpdateProgressbar(worker, (double)i / timingPointsChanges.Count, 10, maxStages);
            }

            // Take note of all the changes
            List<double> timingpointsChanged = new List<double>();

            var originalInNew = (from first in original
                                       join second in newTimingPoints
                                       on first.Offset equals second.Offset
                                       select first).ToList();

            var newInOriginal = (from first in original
                                join second in newTimingPoints
                                on first.Offset equals second.Offset
                                select second).ToList();

            for(int i = 0; i < originalInNew.Count(); i++)
            {
                if (originalInNew[i] != newInOriginal[i])
                {
                    timingpointsChanged.Add(originalInNew[i].Offset);
                }
            }

            List<double> originalOffsets = new List<double>();
            List<double> newOffsets = new List<double>();
            original.ForEach(o => originalOffsets.Add(o.Offset));
            newTimingPoints.ForEach(o => newOffsets.Add(o.Offset));

            List<double> timingpointsRemoved = originalOffsets.Except(newOffsets).ToList();
            List<double> timingpointsAdded = newOffsets.Except(originalOffsets).ToList();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress)
            {
                worker.ReportProgress(100);
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Run_Program((List<string>) e.Argument, bgw, e);
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
            string destinationDirectory = System.Environment.CurrentDirectory + "\\Backups\\";
            try {
                File.Copy(fileToCopy, destinationDirectory + now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + System.IO.Path.GetFileName(fileToCopy));
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return;
            }

            backgroundWorker.RunWorkerAsync(new List<string> {fileToCopy, VolumeSliders.IsChecked.ToString(), SamplesetSliders.IsChecked.ToString(),
                                                    VolumeSpinners.IsChecked.ToString(), RemoveSliderendMuting.IsChecked.ToString(),
                                                    ResnapObjects.IsChecked.ToString(), ResnapBookmarks.IsChecked.ToString(), Snap1.Text, Snap2.Text, RemoveUnclickableHitsounds.IsChecked.ToString()});

            backgroundLoader.RunWorkerAsync(new List<string> {fileToCopy, VolumeSliders.IsChecked.ToString(), SamplesetSliders.IsChecked.ToString(),
                                                    VolumeSpinners.IsChecked.ToString(), RemoveSliderendMuting.IsChecked.ToString(),
                                                    ResnapObjects.IsChecked.ToString(), ResnapBookmarks.IsChecked.ToString(), Snap1.Text, Snap2.Text, RemoveUnclickableHitsounds.IsChecked.ToString()});
            start.IsEnabled = false;
        }

        private string Run_Program(List<string> arguments, BackgroundWorker worker, DoWorkEventArgs e) {
            // Retrieve all arguments
            string path = arguments[0];
            bool volumeSliders = arguments[1] == "True";
            bool samplesetSliders = arguments[2] == "True";
            bool volumeSpinners = arguments[3] == "True";
            bool removeSliderendMuting = arguments[4] == "True";
            bool resnapObjects = arguments[5] == "True";
            bool resnapBookmarks = arguments[6] == "True";
            int snap1 = int.Parse(arguments[7].Split('/')[1]);
            int snap2 = int.Parse(arguments[8].Split('/')[1]);
            bool removeUnclickabeHitsounds = arguments[9] == "True";

            Editor editor = new Editor(path);
            Timing timing = editor.Beatmap.BeatmapTiming;
            Timeline timeline = editor.Beatmap.GetTimeline();

            int mode = editor.Beatmap.General["Mode"].Value;
            int num_timingPoints = editor.Beatmap.BeatmapTiming.TimingPoints.Count;
            int objectsResnapped = 0;

            // Count total stages
            int maxStages = 11;

            // Collect Kiai toggles and SV changes for mania/taiko
            List<TimingPoint> kiaiToggles = new List<TimingPoint>();
            List<TimingPoint> svChanges = new List<TimingPoint>();
            bool lastKiai = false;
            double lastSV = -100;
            for( int i = 0; i < timing.TimingPoints.Count; i++ ) {
                TimingPoint tp = timing.TimingPoints[i];
                if( tp.Kiai != lastKiai ) {
                    kiaiToggles.Add(tp.Copy());
                    lastKiai = tp.Kiai;
                }
                if( tp.Inherited ) {
                    lastSV = -100;
                }
                else {
                    if( tp.MpB != lastSV ) {
                        svChanges.Add(tp.Copy());
                        lastSV = tp.MpB;
                    }
                }
                UpdateProgressbar(worker, (double) i / timing.TimingPoints.Count, 0, maxStages);
            }

            // Resnap shit
            if( resnapObjects ) {
                // Resnap all objects
                for( int i = 0; i < editor.Beatmap.HitObjects.Count; i++ ) {
                    HitObject ho = editor.Beatmap.HitObjects[i];
                    bool resnapped = ho.ResnapSelf(timing, snap1, snap2);
                    if( resnapped ) {
                        objectsResnapped += 1;
                    }
                    ho.ResnapEnd(timing, snap1, snap2);
                    UpdateProgressbar(worker, (double) i / editor.Beatmap.HitObjects.Count, 1, maxStages);
                }

                // Resnap Kiai toggles and SV changes
                for( int i = 0; i < kiaiToggles.Count; i++ ) {
                    TimingPoint tp = kiaiToggles[i];
                    tp.ResnapSelf(timing, snap1, snap2);
                    UpdateProgressbar(worker, (double) i / kiaiToggles.Count, 2, maxStages);
                }
                for( int i = 0; i < svChanges.Count; i++ ) {
                    TimingPoint tp = svChanges[i];
                    tp.ResnapSelf(timing, snap1, snap2);
                    UpdateProgressbar(worker, (double) i / svChanges.Count, 3, maxStages);
                }
            }

            if( resnapBookmarks ) {
                // Resnap the bookmarks
                List<double> newBookmarks = new List<double>();
                List<double> bookmarks = editor.Beatmap.GetBookmarks();
                for( int i = 0; i < bookmarks.Count; i++ ) {
                    double bookmark = bookmarks[i];
                    newBookmarks.Add(Math.Floor(timing.Resnap(bookmark, snap1, snap2)));
                    UpdateProgressbar(worker, (double) i / bookmarks.Count, 4, maxStages);
                }
                editor.Beatmap.SetBookmarks(newBookmarks);
            }

            // Maybe mute unclickable timelineobjects
            if( removeUnclickabeHitsounds ) {
                foreach( TimelineObject tlo in timeline.TimeLineObjects ) {
                    if( !( tlo.IsCircle || tlo.IsSliderHead || tlo.IsHoldnoteHead ) )  // Not clickable
                    {
                        tlo.FenoSampleVolume = 5;  // 5% volume mute
                    }
                }
            }

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
            // Add redlines
            List<TimingPoint> redlines = timing.GetAllRedlines();
            for( int i = 0; i < redlines.Count; i++ ) {
                TimingPoint tp = redlines[i];
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, inherited: true));
                UpdateProgressbar(worker, (double) i / redlines.Count, 5, maxStages);
            }
            // Add SV changes for taiko and mania
            if( mode == 1 || mode == 3 ) {
                for( int i = 0; i < svChanges.Count; i++ ) {
                    TimingPoint tp = svChanges[i];
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                    UpdateProgressbar(worker, (double) i / svChanges.Count, 6, maxStages);
                }
            }
            // Add Kiai toggles
            for( int i = 0; i < kiaiToggles.Count; i++ ) {
                TimingPoint tp = kiaiToggles[i];
                timingPointsChanges.Add(new TimingPointsChange(tp, kiai: true));
                UpdateProgressbar(worker, (double) i / kiaiToggles.Count, 7, maxStages);
            }
            // Add Hitobject stuff
            for( int i = 0; i < editor.Beatmap.HitObjects.Count; i++ ) {
                HitObject ho = editor.Beatmap.HitObjects[i];
                if( ho.IsSlider ) // SV changes
                {
                    TimingPoint tp = ho.TP.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = ho.SV;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }
                // Body hitsounds
                bool vol = ( ho.IsSlider && volumeSliders ) || ( ho.IsSpinner && volumeSpinners );
                bool sam = ( ho.IsSlider && samplesetSliders && ho.SampleSet == 0 );
                bool ind = ( ho.IsSlider && samplesetSliders );
                bool samplesetActuallyChanged = false;
                foreach( TimingPoint tp in ho.BodyHitsounds ) {
                    if( tp.Volume == 5 && removeSliderendMuting ) { vol = false; }  // Removing sliderbody silencing
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind, sampleset: sam));
                    if( tp.SampleSet != ho.HitsoundTP.SampleSet ) { samplesetActuallyChanged = samplesetSliders && ho.SampleSet == 0; }  // True for sampleset change in sliderbody
                }
                if( ho.IsSlider && ( !samplesetActuallyChanged ) && ho.SampleSet == 0 )  // Case can put sampleset on sliderbody
                {
                    ho.SampleSet = ho.HitsoundTP.SampleSet;
                    ho.SliderExtras = true;
                }
                if( ho.IsSlider && samplesetActuallyChanged ) // Make it start out with the right sampleset
                {
                    TimingPoint tp = ho.HitsoundTP.Copy();
                    tp.Offset = ho.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: true));
                }
                UpdateProgressbar(worker, (double) i / editor.Beatmap.HitObjects.Count, 8, maxStages);
            }
            // Add timeline hitsounds
            for( int i = 0; i < timeline.TimeLineObjects.Count; i++ ) {
                TimelineObject tlo = timeline.TimeLineObjects[i];
                // Change the samplesets in the hitobjects
                if( tlo.Origin.IsCircle ) {
                    tlo.Origin.SampleSet = tlo.FenoSampleSet;
                    tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                    if( mode == 3 ) {
                        tlo.Origin.CustomIndex = tlo.FenoCustomIndex;
                        tlo.Origin.SampleVolume = tlo.FenoSampleVolume;
                    }
                }
                else if( tlo.Origin.IsSlider ) {
                    tlo.Origin.EdgeHitsounds[tlo.Repeat] = tlo.GetHitsounds();
                    tlo.Origin.EdgeSampleSets[tlo.Repeat] = tlo.FenoSampleSet;
                    tlo.Origin.EdgeAdditionSets[tlo.Repeat] = tlo.FenoAdditionSet;
                    tlo.Origin.SliderExtras = true;
                    if( tlo.Origin.EdgeAdditionSets[tlo.Repeat] == tlo.Origin.EdgeSampleSets[tlo.Repeat] )  // Simplify additions to auto
                    {
                        tlo.Origin.EdgeAdditionSets[tlo.Repeat] = 0;
                    }
                }
                else if( tlo.Origin.IsSpinner ) {
                    if( tlo.Repeat == 1 ) {
                        tlo.Origin.SampleSet = tlo.FenoSampleSet;
                        tlo.Origin.AdditionSet = tlo.FenoAdditionSet;

                    }
                }
                else if( tlo.Origin.IsHoldNote ) {
                    if( tlo.Repeat == 0 ) {
                        tlo.Origin.SampleSet = tlo.FenoSampleSet;
                        tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                        tlo.Origin.CustomIndex = tlo.FenoCustomIndex;
                        tlo.Origin.SampleVolume = tlo.FenoSampleVolume;
                    }
                }
                if( tlo.Origin.AdditionSet == tlo.Origin.SampleSet )  // Simplify additions to auto
                {
                    tlo.Origin.AdditionSet = 0;
                }
                if( mode == 0 && tlo.HasHitsound ) // Add greenlines for custom indexes and volumes
                {
                    TimingPoint tp = tlo.Origin.TP.Copy();
                    tp.Offset = tlo.Time;
                    tp.SampleIndex = tlo.FenoCustomIndex;
                    tp.Volume = tlo.FenoSampleVolume;
                    bool ind = !( tlo.Filename != "" && ( tlo.IsCircle || tlo.IsHoldnoteHead || tlo.IsSpinnerEnd ) );  // Index doesnt have to change if custom is overridden by Filename
                    bool vol = !( tp.Volume == 5 && removeSliderendMuting && ( tlo.IsSliderEnd || tlo.IsSpinnerEnd ) );  // Remove volume change if sliderend muting or spinnerend muting
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind));
                }
                UpdateProgressbar(worker, (double) i / timeline.TimeLineObjects.Count, 9, maxStages);
            }


            // Add the new timingpoints
            timingPointsChanges = timingPointsChanges.OrderBy(o => o.TP.Offset).ToList();
            List<TimingPoint> newTimingPoints = new List<TimingPoint>();
            for( int i = 0; i < timingPointsChanges.Count; i++ ) {
                TimingPointsChange c = timingPointsChanges[i];
                c.AddChange(newTimingPoints, timing);
                UpdateProgressbar(worker, (double) i / timingPointsChanges.Count, 10, maxStages);
            }

            // Replace the old timingpoints
            timing.TimingPoints = newTimingPoints;

            // Save the file
            editor.SaveFile();

            // Complete progressbar
            if( worker != null && worker.WorkerReportsProgress ) {
                worker.ReportProgress(100);
            }

            // Make an accurate message (Softwareporn)
            int removed = num_timingPoints - newTimingPoints.Count;
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

        private void UpdateProgressbar(BackgroundWorker worker, double fraction, int stage, int maxStages) {
            // Update progressbar
            if( worker != null && worker.WorkerReportsProgress ) {
                worker.ReportProgress((int) ( ( fraction + stage ) / maxStages * 100 ));
            }
        }

        public void Print(string str) {
            Debug.WriteLine(str);
        }
    }
}
