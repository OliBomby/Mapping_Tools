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

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundCopierView :UserControl {
        private readonly BackgroundWorker backgroundWorker;

        public HitsoundCopierView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker");
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Hitsounds((Arguments) e.Argument, bgw, e);
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
            foreach (string fileToCopy in BeatmapToBox.Text.Split('|')) {
                IOHelper.SaveMapBackup(fileToCopy);
            }

            backgroundWorker.RunWorkerAsync(new Arguments(BeatmapToBox.Text, BeatmapFromBox.Text, CopyModeBox.SelectedIndex, LeniencyBox.GetDouble(5),
                                                          (bool)CopyHitsoundsBox.IsChecked, (bool)CopyBodyBox.IsChecked, (bool)CopySamplesetBox.IsChecked,
                                                          (bool)CopyVolumeBox.IsChecked, (bool)AlwaysPreserve5VolumeBox.IsChecked, (bool)CopyStoryboardedSamplesBox.IsChecked, (bool)IgnoreHitsoundSatisfiedSamplesBox.IsChecked, (bool)MuteSliderendBox.IsChecked,
                                                          int.Parse(MutedSnap1.Text.Split('/')[1]), int.Parse(MutedSnap2.Text.Split('/')[1]),
                                                          MutedMinLengthBox.GetDouble(0), MutedCustomIndexBox.GetInt(-1), (SampleSet)(MutedSampleSetBox.SelectedIndex + 1)));
            start.IsEnabled = false;
        }

        private struct Arguments {
            public string PathTo;
            public string PathFrom;
            public int CopyMode;
            public double TemporalLeniency;
            public bool CopyHitsounds;
            public bool CopyBodyHitsounds;
            public bool CopySamplesets;
            public bool CopyVolumes;
            public bool AlwaysPreserve5Volume;
            public bool CopyStoryboardedSamples;
            public bool IgnoreHitsoundSatisfiedSamples;
            public bool MuteSliderends;
            public int Snap1;
            public int Snap2;
            public double MinLength;
            public int MutedIndex;
            public SampleSet MutedSampleset;
            public Arguments(string pathTo, string pathFrom, int copyMode, double temporalLeniency, bool copyHitsounds, bool copyBodyHitsounds, bool copySamplesets, bool copyVolumes, bool alwaysPreserve5Volume,
                bool copyStoryboardedSamples, bool ignoreHitsoundSatisfiedSamples, bool muteSliderends, int snap1, int snap2, double minLength, int mutedIndex, SampleSet mutedSampleset)
            {
                PathTo = pathTo;
                PathFrom = pathFrom;
                CopyMode = copyMode;
                TemporalLeniency = temporalLeniency;
                CopyHitsounds = copyHitsounds;
                CopyBodyHitsounds = copyBodyHitsounds;
                CopySamplesets = copySamplesets;
                CopyVolumes = copyVolumes;
                AlwaysPreserve5Volume = alwaysPreserve5Volume;
                CopyStoryboardedSamples = copyStoryboardedSamples;
                IgnoreHitsoundSatisfiedSamples = ignoreHitsoundSatisfiedSamples;
                MuteSliderends = muteSliderends;
                Snap1 = snap1;
                Snap2 = snap2;
                MinLength = minLength;
                MutedIndex = mutedIndex;
                MutedSampleset = mutedSampleset;
            }
        }

        private string Copy_Hitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs _) {
            int copyMode = arg.CopyMode;
            double temporalLeniency = arg.TemporalLeniency;
            bool copyHitsounds = arg.CopyHitsounds;
            bool copySliderbodychanges = arg.CopyBodyHitsounds;
            bool copyVolumes = arg.CopyVolumes;
            bool copySBSamples = arg.CopyStoryboardedSamples;
            bool ignoreHSSBSamples = arg.IgnoreHitsoundSatisfiedSamples;
            bool copySamplesets = arg.CopySamplesets;
            bool muteSliderends = arg.MuteSliderends;
            bool doMutedIndex = arg.MutedIndex >= 0;

            string[] paths = arg.PathTo.Split('|');
            int mapsDone = 0;

            foreach (string pathTo in paths) {
                BeatmapEditor editorTo = new BeatmapEditor(pathTo);
                BeatmapEditor editorFrom = new BeatmapEditor(arg.PathFrom);

                Beatmap beatmapTo = editorTo.Beatmap;
                Beatmap beatmapFrom = editorFrom.Beatmap;

                Timeline processedTimeline;

                if (copyMode == 0)
                {
                    // Every defined hitsound and sampleset on hitsound gets copied to their copyTo destination
                    // Timelines
                    Timeline tlTo = beatmapTo.GetTimeline();
                    Timeline tlFrom = beatmapFrom.GetTimeline();

                    List<double> volumeMuteTimes = arg.CopyVolumes && arg.AlwaysPreserve5Volume ? new List<double>() : null;

                    if (copyHitsounds) {
                        ResetHitObjectHitsounds(beatmapTo);
                        CopyHitsounds(arg, tlFrom, tlTo);
                    }

                    // Save tlo times where volume is 5%
                    if (volumeMuteTimes != null) {
                        // Timingpointchange all the undefined tlo from copyFrom
                        foreach (TimelineObject tloTo in tlTo.TimelineObjects) {
                            if (tloTo.canCopy && tloTo.FenoSampleVolume == 5) {
                                volumeMuteTimes.Add(tloTo.Time);
                            }
                        }
                    }

                    // Volumes and samplesets and customindices greenlines get copied with timingpointchanges and allafter enabled
                    List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

                    foreach (TimingPoint tp in beatmapFrom.BeatmapTiming.TimingPoints)
                    {
                        TimingPointsChange tpc = new TimingPointsChange(tp, sampleset: copySamplesets, index: copySamplesets, volume: copyVolumes);
                        timingPointsChanges.Add(tpc);
                    }

                    // Apply the timingpoint changes
                    TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChanges, true);

                    processedTimeline = tlTo;

                    // Return 5% volume to tlo that had it before
                    if (volumeMuteTimes != null) {
                        List<TimingPointsChange> timingPointsChangesMute = new List<TimingPointsChange>();
                        processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                        foreach (TimelineObject tloTo in processedTimeline.TimelineObjects) {
                            if (volumeMuteTimes.Contains(tloTo.Time)) {
                                // Add timingpointschange to copy timingpoint hitsounds
                                TimingPoint tp = tloTo.HitsoundTP.Copy();
                                tp.Offset = tloTo.Time;
                                tp.Volume = 5;
                                timingPointsChangesMute.Add(new TimingPointsChange(tp, volume: true));
                            } else {
                                // Add timingpointschange to preserve index and volume
                                TimingPoint tp = tloTo.HitsoundTP.Copy();
                                tp.Offset = tloTo.Time;
                                tp.Volume = tloTo.FenoSampleVolume;
                                timingPointsChangesMute.Add(new TimingPointsChange(tp, volume: true));
                            }
                        }

                        // Apply the timingpoint changes
                        TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChangesMute, false);
                    }
                }
                else
                {
                    // Smarty mode
                    // Copy the defined hitsounds literally (not feno, that will be reserved for cleaner). Only the tlo that have been defined by copyFrom get overwritten.
                    Timeline tlTo = beatmapTo.GetTimeline();
                    Timeline tlFrom = beatmapFrom.GetTimeline();

                    List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                    GameMode mode = (GameMode)beatmapTo.General["Mode"].Value;
                    string mapDir = editorTo.GetBeatmapFolder();
                    Dictionary<string, string> firstSamples = HitsoundImporter.AnalyzeSamples(mapDir);

                    if (copyHitsounds)
                    {
                        CopyHitsounds(arg, tlFrom, tlTo, timingPointsChanges, mode, mapDir, firstSamples);
                    }

                    if (copySliderbodychanges)
                    {
                        // Remove timingpoints in beatmapTo that are in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                        foreach (HitObject ho in beatmapTo.HitObjects)
                        {
                            foreach (TimingPoint tp in ho.BodyHitsounds)
                            {
                                // Every timingpoint in a body in beatmapTo
                                if (beatmapFrom.HitObjects.Any(o => o.Time < tp.Offset && o.EndTime > tp.Offset))
                                {
                                    // Timingpoint is in a body for both beatmaps
                                    beatmapTo.BeatmapTiming.TimingPoints.Remove(tp);
                                }
                            }
                        }

                        // Get timingpointschanges for every timingpoint from beatmapFrom that is in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                        foreach (HitObject ho in beatmapFrom.HitObjects)
                        {
                            foreach (TimingPoint tp in ho.BodyHitsounds)
                            {
                                // Every timingpoint in a body in beatmapFrom
                                if (beatmapTo.HitObjects.Any(o => o.Time < tp.Offset && o.EndTime > tp.Offset))
                                {
                                    // Timingpoint is in a body for both beatmaps
                                    timingPointsChanges.Add(new TimingPointsChange(tp.Copy(), sampleset: copySamplesets, index: copySamplesets, volume: copyVolumes));
                                }
                            }
                        }
                    }

                    // Apply the timingpoint changes
                    TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChanges, false);

                    processedTimeline = tlTo;
                }

                if (copySBSamples)
                {
                    if (copyMode == 0)
                    {
                        beatmapTo.StoryboardSoundSamples.Clear();
                    }

                    beatmapTo.GiveObjectsGreenlines();
                    processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                    string mapDir = editorTo.GetBeatmapFolder();
                    Dictionary<string, string> firstSamples = HitsoundImporter.AnalyzeSamples(mapDir, true);

                    var samplesTo = new HashSet<StoryboardSoundSample>(beatmapTo.StoryboardSoundSamples);
                    GameMode mode = (GameMode)beatmapTo.General["Mode"].Value;

                    foreach (StoryboardSoundSample sampleFrom in beatmapFrom.StoryboardSoundSamples)
                    {
                        if (ignoreHSSBSamples)
                        {
                            List<TimelineObject> tloHere = processedTimeline.TimelineObjects.FindAll(o => Math.Abs(o.Time - sampleFrom.Time) <= temporalLeniency);
                            HashSet<string> samplesHere = new HashSet<string>();
                            foreach (TimelineObject tlo in tloHere)
                            {
                                foreach (string filename in tlo.GetPlayingFilenames(mode))
                                {
                                    string samplePath = Path.Combine(mapDir, filename);
                                    string fullPathExtLess = Path.Combine(Path.GetDirectoryName(samplePath), Path.GetFileNameWithoutExtension(samplePath));

                                    if (firstSamples.Keys.Contains(fullPathExtLess))
                                    {
                                        samplePath = firstSamples[fullPathExtLess];
                                    }
                                    samplesHere.Add(samplePath);
                                }
                            }

                            string sbSamplePath = Path.Combine(mapDir, sampleFrom.FilePath);
                            string sbFullPathExtLess = Path.Combine(Path.GetDirectoryName(sbSamplePath), Path.GetFileNameWithoutExtension(sbSamplePath));

                            if (firstSamples.Keys.Contains(sbFullPathExtLess))
                            {
                                sbSamplePath = firstSamples[sbFullPathExtLess];
                            }

                            if (samplesHere.Contains(sbSamplePath))
                                continue;
                        }

                        // Add the StoryboardSoundSamples from beatmapFrom to beatmapTo if it doesn't already have the sample
                        if (!samplesTo.Contains(sampleFrom))
                        {
                            beatmapTo.StoryboardSoundSamples.Add(sampleFrom);
                        }
                    }
                    // Sort the storyboarded samples
                    beatmapTo.StoryboardSoundSamples.OrderBy(o => o.Time);
                }

                if (muteSliderends)
                {
                    List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                    beatmapTo.GiveObjectsGreenlines();
                    processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                    foreach (TimelineObject tloTo in processedTimeline.TimelineObjects)
                    {
                        if (FilterMuteTLO(tloTo, beatmapTo, arg))
                        {
                            // Set volume to 5%, remove all hitsounds, apply customindex and sampleset
                            tloTo.SampleSet = arg.MutedSampleset;
                            tloTo.AdditionSet = 0;
                            tloTo.Normal = false;
                            tloTo.Whistle = false;
                            tloTo.Finish = false;
                            tloTo.Clap = false;

                            tloTo.HitsoundsToOrigin();

                            // Add timingpointschange to copy timingpoint hitsounds
                            TimingPoint tp = tloTo.HitsoundTP.Copy();
                            tp.Offset = tloTo.Time;
                            tp.Volume = 5;
                            tp.SampleIndex = arg.MutedIndex;
                            timingPointsChanges.Add(new TimingPointsChange(tp, index: doMutedIndex, volume: true));
                        }
                        else
                        {
                            // Add timingpointschange to preserve index and volume
                            TimingPoint tp = tloTo.HitsoundTP.Copy();
                            tp.Offset = tloTo.Time;
                            tp.SampleIndex = tloTo.FenoCustomIndex;
                            tp.Volume = tloTo.FenoSampleVolume;
                            timingPointsChanges.Add(new TimingPointsChange(tp, index: doMutedIndex, volume: true));
                        }
                    }

                    // Apply the timingpoint changes
                    TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChanges, false);
                }

                // Save the file
                editorTo.SaveFile();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            // Make an accurate message
            string message = "";
            message += "Done!";
            return message;
        }

        private void CopyHitsounds(Arguments arg, Timeline tlFrom, Timeline tlTo) {
            foreach (TimelineObject tloFrom in tlFrom.TimelineObjects) {
                TimelineObject tloTo = tlTo.GetNearestTLO(tloFrom.Time, true);

                if (tloTo != null && Math.Abs(Math.Round(tloFrom.Time) - Math.Round(tloTo.Time)) <= arg.TemporalLeniency) {
                    // Copy to this tlo
                    CopyHitsounds(arg, tloFrom, tloTo);
                }
                tloFrom.canCopy = false;
            }
        }

        private void CopyHitsounds(Arguments arg, Timeline tlFrom, Timeline tlTo, List<TimingPointsChange> timingPointsChanges, GameMode mode, string mapDir, Dictionary<string, string> firstSamples) {
            foreach (TimelineObject tloFrom in tlFrom.TimelineObjects) {
                TimelineObject tloTo = tlTo.GetNearestTLO(tloFrom.Time, true);

                if (tloTo != null && Math.Abs(Math.Round(tloFrom.Time) - Math.Round(tloTo.Time)) <= arg.TemporalLeniency) {
                    // Copy to this tlo
                    CopyHitsounds(arg, tloFrom, tloTo);

                    // Add timingpointschange to copy timingpoint hitsounds
                    TimingPoint tp = tloFrom.HitsoundTP.Copy();
                    tp.Offset = tloTo.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: arg.CopySamplesets, index: arg.CopySamplesets, volume: arg.CopyVolumes));
                }
                tloFrom.canCopy = false;
            }

            // Timingpointchange all the undefined tlo from copyFrom
            foreach (TimelineObject tloTo in tlTo.TimelineObjects) {
                if (tloTo.canCopy) {
                    TimingPoint tp = tloTo.HitsoundTP.Copy();
                    bool holdSampleset = arg.CopySamplesets && tloTo.SampleSet == SampleSet.Auto;
                    bool holdIndex = arg.CopySamplesets && !(tloTo.CanCustoms && tloTo.CustomIndex != 0);

                    // Dont hold indexes or sampleset if the sample it plays currently is the same as the sample it would play without conserving
                    if (holdSampleset || holdIndex) {
                        List<string> nativeSamples = tloTo.GetFirstPlayingFilenames(mode, mapDir, firstSamples);

                        if (holdSampleset) {
                            SampleSet oldSampleSet = tloTo.FenoSampleSet;
                            SampleSet newSampleSet = tloTo.FenoSampleSet;
                            double latest = double.NegativeInfinity;
                            foreach (TimingPointsChange tpc in timingPointsChanges) {
                                if (tpc.Sampleset && tpc.MyTP.Offset <= tloTo.Time && tpc.MyTP.Offset >= latest) {
                                    newSampleSet = tpc.MyTP.SampleSet;
                                    latest = tpc.MyTP.Offset;
                                }
                            }

                            tp.SampleSet = newSampleSet;
                            tloTo.GiveHitsoundTimingPoint(tp);
                            List<string> newSamples = tloTo.GetFirstPlayingFilenames(mode, mapDir, firstSamples);
                            if (nativeSamples.SequenceEqual(newSamples)) {
                                // Sampleset changes dont change sound
                                tp.SampleSet = newSampleSet;
                            } else {
                                tp.SampleSet = oldSampleSet;
                            }
                        }
                        if (holdIndex) {

                            int oldIndex = tloTo.FenoCustomIndex;
                            int newIndex = tloTo.FenoCustomIndex;
                            double latest = double.NegativeInfinity;
                            foreach (TimingPointsChange tpc in timingPointsChanges) {
                                if (tpc.Index && tpc.MyTP.Offset <= tloTo.Time && tpc.MyTP.Offset >= latest) {
                                    newIndex = tpc.MyTP.SampleIndex;
                                    latest = tpc.MyTP.Offset;
                                }
                            }

                            tp.SampleIndex = newIndex;
                            tloTo.GiveHitsoundTimingPoint(tp);
                            List<string> newSamples = tloTo.GetFirstPlayingFilenames(mode, mapDir, firstSamples);
                            if (nativeSamples.SequenceEqual(newSamples)) {
                                // Index changes dont change sound
                                tp.SampleIndex = newIndex;
                            } else {
                                tp.SampleIndex = oldIndex;
                            }
                        }
                        tloTo.GiveHitsoundTimingPoint(tp);
                    }

                    tp.Offset = tloTo.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: holdSampleset, index: holdIndex, volume: arg.CopyVolumes));
                }
            }
        }

        private void CopyHitsounds(Arguments arg, TimelineObject tloFrom, TimelineObject tloTo) {
            // Copy to this tlo
            tloTo.SampleSet = tloFrom.SampleSet;
            tloTo.AdditionSet = tloFrom.AdditionSet;
            tloTo.Normal = tloFrom.Normal;
            tloTo.Whistle = tloFrom.Whistle;
            tloTo.Finish = tloFrom.Finish;
            tloTo.Clap = tloFrom.Clap;

            if (tloTo.CanCustoms) {
                tloTo.CustomIndex = tloFrom.CustomIndex;
                tloTo.SampleVolume = tloFrom.SampleVolume;
                tloTo.Filename = tloFrom.Filename;
            }

            // Copy sliderbody hitsounds
            if (tloTo.IsSliderHead && tloFrom.IsSliderHead && arg.CopyBodyHitsounds) {
                tloTo.Origin.Hitsounds = tloFrom.Origin.Hitsounds;
                tloTo.Origin.SampleSet = tloFrom.Origin.SampleSet;
                tloTo.Origin.AdditionSet = tloFrom.Origin.AdditionSet;
            }

            tloTo.HitsoundsToOrigin();
            tloTo.canCopy = false;
        }

        private void ResetHitObjectHitsounds(Beatmap beatmap) {
            foreach (HitObject ho in beatmap.HitObjects) {
                // Remove all hitsounds
                ho.Clap = false;
                ho.Whistle = false;
                ho.Finish = false;
                ho.Clap = false;
                ho.SampleSet = 0;
                ho.AdditionSet = 0;
                ho.CustomIndex = 0;
                ho.SampleVolume = 0;
                ho.Filename = "";

                if (ho.IsSlider) {
                    // Remove edge hitsounds
                    ho.EdgeHitsounds = ho.EdgeHitsounds.Select(o => 0).ToList();
                    ho.EdgeSampleSets = ho.EdgeSampleSets.Select(o => SampleSet.Auto).ToList();
                    ho.EdgeAdditionSets = ho.EdgeAdditionSets.Select(o => SampleSet.Auto).ToList();
                }
            }
        }

        private bool FilterMuteTLO(TimelineObject tloTo, Beatmap beatmapTo, Arguments arg) {
            // Check whether it's defined
            if (!tloTo.canCopy)
                return false;

            // Check type
            if (!(tloTo.IsSliderEnd || tloTo.IsSpinnerEnd))
                return false;

            // Check repeats
            if (tloTo.Repeat != 1) {
                return false;
            }

            // Check filter snap
            // It's at least snap x or worse if the time is not a multiple of snap x / 2
            TimingPoint redline = beatmapTo.BeatmapTiming.GetRedlineAtTime(tloTo.Time - 1);
            double resnappedTime = beatmapTo.BeatmapTiming.Resnap(tloTo.Time, arg.Snap1, arg.Snap2, false, redline);
            double beatsFromRedline = (resnappedTime - redline.Offset) / redline.MpB;
            double dist1 = beatsFromRedline * arg.Snap1 / (arg.Snap1 == 1 ? 4 : 2);
            double dist2 = beatsFromRedline * arg.Snap2 / (arg.Snap2 == 1 ? 4 : arg.Snap2 == 3 ? 3 : 2);
            dist1 %= 1;
            dist2 %= 1;
            if (Precision.AlmostEquals(dist1, 0, 1E-7) || Precision.AlmostEquals(dist1, 1, 1E-7) ||
                Precision.AlmostEquals(dist2, 0, 1E-7) || Precision.AlmostEquals(dist2, 1, 1E-7))
                return false;

            // Check filter temporal length
            if (!Precision.AlmostBigger(tloTo.Origin.TemporalLength, arg.MinLength * redline.MpB))
                return false;

            return true;
        }

        private void BeatmapFromBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string[] paths = IOHelper.BeatmapFileDialog();
                if (paths.Length != 0) {
                    BeatmapFromBox.Text = paths[0];
                }
            } catch (Exception) { }
        }

        private void BeatmapFromLoad_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.CurrentBeatmap();
                if (path != "") {
                    BeatmapFromBox.Text = path;
                }
            } catch (Exception) { }
        }

        private void BeatmapToBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string[] paths = IOHelper.BeatmapFileDialog(multiselect:true);
                if (paths.Length != 0) {
                    BeatmapToBox.Text = string.Join("|", paths);
                }
            } catch (Exception) { }
        }

        private void BeatmapToLoad_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.CurrentBeatmap();
                if (path != "") {
                    BeatmapToBox.Text = path;
                }
            } catch (Exception) { }
        }
    }
}
