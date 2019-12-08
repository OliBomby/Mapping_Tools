using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundCopierView {
        public static readonly string ToolName = "Hitsound Copier";

        /// <summary>
        /// 
        /// </summary>
        [UsedImplicitly] public static readonly string ToolDescription =
            $@"Copies hitsounds from A to B.{Environment.NewLine}There are 2 modes. " +
            $@"First mode is overwrite everything. " +
            $@"This will basically first remove the hitsounds from the map you’re copying to and then copy the hitsounds." +
            $@"{Environment.NewLine}Second mode is copying only the defined hitsounds." +
            $@" A defined hitsound is when there is something there in the map you’re copying from." +
            $@" This mode will copy over all the hitsounds from the map you’re copying from. " +
            $@"Anything in the map you’re copying to that has not been defined in the map you’re copying from will not change. " +
            $@"For instance muted sliderends will remain there.";

        /// <inheritdoc />
        public HitsoundCopierView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Hitsounds((Arguments) e.Argument, bgw);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            foreach (string fileToCopy in BeatmapToBox.Text.Split('|')) {
                IOHelper.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync(new Arguments(BeatmapToBox.Text, BeatmapFromBox.Text,
                CopyModeBox.SelectedIndex, LeniencyBox.GetDouble(5),
                CopyHitsoundsBox.IsChecked.GetValueOrDefault(), CopyBodyBox.IsChecked.GetValueOrDefault(),
                CopySamplesetBox.IsChecked.GetValueOrDefault(),
                CopyVolumeBox.IsChecked.GetValueOrDefault(), AlwaysPreserve5VolumeBox.IsChecked.GetValueOrDefault(),
                CopyStoryboardedSamplesBox.IsChecked.GetValueOrDefault(),
                IgnoreHitsoundSatisfiedSamplesBox.IsChecked.GetValueOrDefault(),
                MuteSliderendBox.IsChecked.GetValueOrDefault(),
                int.Parse(MutedSnap1.Text.Split('/')[1]), int.Parse(MutedSnap2.Text.Split('/')[1]),
                MutedMinLengthBox.GetDouble(0), MutedCustomIndexBox.GetInt(),
                (SampleSet) (MutedSampleSetBox.SelectedIndex + 1)));
            CanRun = false;
        }

        private struct Arguments {
            public readonly string PathTo;
            public readonly string PathFrom;
            public readonly int CopyMode;
            public readonly double TemporalLeniency;
            public readonly bool CopyHitsounds;
            public readonly bool CopyBodyHitsounds;
            public readonly bool CopySamplesets;
            public readonly bool CopyVolumes;
            public readonly bool AlwaysPreserve5Volume;
            public readonly bool CopyStoryboardedSamples;
            public readonly bool IgnoreHitsoundSatisfiedSamples;
            public readonly bool MuteSliderends;
            public readonly int Snap1;
            public readonly int Snap2;
            public readonly double MinLength;
            public readonly int MutedIndex;
            public readonly SampleSet MutedSampleset;

            public Arguments(string pathTo, string pathFrom, int copyMode, double temporalLeniency, bool copyHitsounds,
                bool copyBodyHitsounds, bool copySamplesets, bool copyVolumes, bool alwaysPreserve5Volume,
                bool copyStoryboardedSamples, bool ignoreHitsoundSatisfiedSamples, bool muteSliderends, int snap1,
                int snap2, double minLength, int mutedIndex, SampleSet mutedSampleset) {
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

        private string Copy_Hitsounds(Arguments arg, BackgroundWorker worker) {
            var copyMode = arg.CopyMode;
            var temporalLeniency = arg.TemporalLeniency;
            var copyHitsounds = arg.CopyHitsounds;
            var copySliderbodychanges = arg.CopyBodyHitsounds;
            var copyVolumes = arg.CopyVolumes;
            var copySbSamples = arg.CopyStoryboardedSamples;
            var ignoreHssbSamples = arg.IgnoreHitsoundSatisfiedSamples;
            var copySamplesets = arg.CopySamplesets;
            var muteSliderends = arg.MuteSliderends;
            var doMutedIndex = arg.MutedIndex >= 0;

            var paths = arg.PathTo.Split('|');
            var mapsDone = 0;

            var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (var pathTo in paths) {
                var editorTo = editorRead
                    ? EditorReaderStuff.GetNewestVersion(pathTo, reader)
                    : new BeatmapEditor(pathTo);
                var editorFrom = editorRead
                    ? EditorReaderStuff.GetNewestVersion(arg.PathFrom, reader)
                    : new BeatmapEditor(arg.PathFrom);

                var beatmapTo = editorTo.Beatmap;
                var beatmapFrom = editorFrom.Beatmap;

                Timeline processedTimeline;

                if (copyMode == 0) {
                    // Every defined hitsound and sampleset on hitsound gets copied to their copyTo destination
                    // Timelines
                    var tlTo = beatmapTo.GetTimeline();
                    var tlFrom = beatmapFrom.GetTimeline();

                    var volumeMuteTimes = arg.CopyVolumes && arg.AlwaysPreserve5Volume ? new List<double>() : null;

                    if (copyHitsounds) {
                        ResetHitObjectHitsounds(beatmapTo);
                        CopyHitsounds(arg, tlFrom, tlTo);
                    }

                    // Save tlo times where volume is 5%
                    if (volumeMuteTimes != null) {
                        // Timingpointchange all the undefined tlo from copyFrom
                        volumeMuteTimes.AddRange(from tloTo in tlTo.TimelineObjects
                            where tloTo.CanCopy && Math.Abs(tloTo.FenoSampleVolume - 5) < Precision.DOUBLE_EPSILON
                            select tloTo.Time);
                    }

                    // Volumes and samplesets and customindices greenlines get copied with timingpointchanges and allafter enabled
                    var timingPointsChanges = beatmapFrom.BeatmapTiming.TimingPoints.Select(tp =>
                        new TimingPointsChange(tp, sampleset: copySamplesets, index: copySamplesets,
                            volume: copyVolumes)).ToList();

                    // Apply the timingpoint changes
                    TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChanges, true);

                    processedTimeline = tlTo;

                    // Return 5% volume to tlo that had it before
                    if (volumeMuteTimes != null) {
                        var timingPointsChangesMute = new List<TimingPointsChange>();
                        processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                        foreach (var tloTo in processedTimeline.TimelineObjects) {
                            if (volumeMuteTimes.Contains(tloTo.Time)) {
                                // Add timingpointschange to copy timingpoint hitsounds
                                var tp = tloTo.HitsoundTimingPoint.Copy();
                                tp.Offset = tloTo.Time;
                                tp.Volume = 5;
                                timingPointsChangesMute.Add(new TimingPointsChange(tp, volume: true));
                            } else {
                                // Add timingpointschange to preserve index and volume
                                var tp = tloTo.HitsoundTimingPoint.Copy();
                                tp.Offset = tloTo.Time;
                                tp.Volume = tloTo.FenoSampleVolume;
                                timingPointsChangesMute.Add(new TimingPointsChange(tp, volume: true));
                            }
                        }

                        // Apply the timingpoint changes
                        TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChangesMute);
                    }
                } else {
                    // Smarty mode
                    // Copy the defined hitsounds literally (not feno, that will be reserved for cleaner). Only the tlo that have been defined by copyFrom get overwritten.
                    var tlTo = beatmapTo.GetTimeline();
                    var tlFrom = beatmapFrom.GetTimeline();

                    var timingPointsChanges = new List<TimingPointsChange>();
                    var mode = (GameMode) beatmapTo.General["Mode"].Value;
                    var mapDir = editorTo.GetBeatmapFolder();
                    var firstSamples = HitsoundImporter.AnalyzeSamples(mapDir);

                    if (copyHitsounds) {
                        CopyHitsounds(arg, tlFrom, tlTo, timingPointsChanges, mode, mapDir, firstSamples);
                    }

                    if (copySliderbodychanges) {
                        // Remove timingpoints in beatmapTo that are in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                        foreach (var tp in from ho in beatmapTo.HitObjects
                            from tp in ho.BodyHitsounds
                            where beatmapFrom.HitObjects.Any(o => o.Time < tp.Offset && o.EndTime > tp.Offset)
                            where !tp.Uninherited
                            select tp) {
                            beatmapTo.BeatmapTiming.TimingPoints.Remove(tp);
                        }

                        // Get timingpointschanges for every timingpoint from beatmapFrom that is in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                        timingPointsChanges.AddRange(from ho in beatmapFrom.HitObjects
                            from tp in ho.BodyHitsounds
                            where beatmapTo.HitObjects.Any(o => o.Time < tp.Offset && o.EndTime > tp.Offset)
                            select new TimingPointsChange(tp.Copy(), sampleset: copySamplesets, index: copySamplesets,
                                volume: copyVolumes));
                    }

                    // Apply the timingpoint changes
                    TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChanges);

                    processedTimeline = tlTo;
                }

                if (copySbSamples) {
                    if (copyMode == 0) {
                        beatmapTo.StoryboardSoundSamples.Clear();
                    }

                    beatmapTo.GiveObjectsGreenlines();
                    processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                    var mapDir = editorTo.GetBeatmapFolder();
                    var firstSamples = HitsoundImporter.AnalyzeSamples(mapDir, true);

                    var samplesTo = new HashSet<StoryboardSoundSample>(beatmapTo.StoryboardSoundSamples);
                    var mode = (GameMode) beatmapTo.General["Mode"].Value;

                    foreach (var sampleFrom in beatmapFrom.StoryboardSoundSamples) {
                        if (ignoreHssbSamples) {
                            var tloHere = processedTimeline.TimelineObjects.FindAll(o =>
                                Math.Abs(o.Time - sampleFrom.Time) <= temporalLeniency);
                            var samplesHere = new HashSet<string>();
                            foreach (var tlo in tloHere) {
                                foreach (var filename in tlo.GetPlayingFilenames(mode)) {
                                    var samplePath = Path.Combine(mapDir, filename);
                                    var fullPathExtLess = Path.Combine(Path.GetDirectoryName(samplePath),
                                        Path.GetFileNameWithoutExtension(samplePath));

                                    if (firstSamples.Keys.Contains(fullPathExtLess)) {
                                        samplePath = firstSamples[fullPathExtLess];
                                    }

                                    samplesHere.Add(samplePath);
                                }
                            }

                            var sbSamplePath = Path.Combine(mapDir, sampleFrom.FilePath);
                            var sbFullPathExtLess = Path.Combine(Path.GetDirectoryName(sbSamplePath),
                                Path.GetFileNameWithoutExtension(sbSamplePath));

                            if (firstSamples.Keys.Contains(sbFullPathExtLess)) {
                                sbSamplePath = firstSamples[sbFullPathExtLess];
                            }

                            if (samplesHere.Contains(sbSamplePath))
                                continue;
                        }

                        // Add the StoryboardSoundSamples from beatmapFrom to beatmapTo if it doesn't already have the sample
                        if (!samplesTo.Contains(sampleFrom)) {
                            beatmapTo.StoryboardSoundSamples.Add(sampleFrom);
                        }
                    }

                    // Sort the storyboarded samples
                    beatmapTo.StoryboardSoundSamples = beatmapTo.StoryboardSoundSamples.OrderBy(o => o.Time).ToList();
                }

                if (muteSliderends) {
                    var timingPointsChanges = new List<TimingPointsChange>();
                    beatmapTo.GiveObjectsGreenlines();
                    processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                    foreach (var tloTo in processedTimeline.TimelineObjects) {
                        if (FilterMuteTlo(tloTo, beatmapTo, arg)) {
                            // Set volume to 5%, remove all hitsounds, apply customindex and sampleset
                            tloTo.SampleSet = arg.MutedSampleset;
                            tloTo.AdditionSet = 0;
                            tloTo.Normal = false;
                            tloTo.Whistle = false;
                            tloTo.Finish = false;
                            tloTo.Clap = false;

                            tloTo.HitsoundsToOrigin();

                            // Add timingpointschange to copy timingpoint hitsounds
                            var tp = tloTo.HitsoundTimingPoint.Copy();
                            tp.Offset = tloTo.Time;
                            tp.SampleSet = arg.MutedSampleset;
                            tp.SampleIndex = arg.MutedIndex;
                            tp.Volume = 5;
                            timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: true, index: doMutedIndex,
                                volume: true));
                        } else {
                            // Add timingpointschange to preserve index and volume and sampleset
                            var tp = tloTo.HitsoundTimingPoint.Copy();
                            tp.Offset = tloTo.Time;
                            timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: true, index: doMutedIndex,
                                volume: true));
                        }
                    }

                    // Apply the timingpoint changes
                    TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChanges);
                }

                // Save the file
                editorTo.SaveFile();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            return "Done!";
        }

        private static void CopyHitsounds(Arguments arg, Timeline tlFrom, Timeline tlTo) {
            foreach (var tloFrom in tlFrom.TimelineObjects) {
                var tloTo = tlTo.GetNearestTlo(tloFrom.Time, true);

                if (tloTo != null &&
                    Math.Abs(Math.Round(tloFrom.Time) - Math.Round(tloTo.Time)) <= arg.TemporalLeniency) {
                    // Copy to this tlo
                    CopyHitsounds(arg, tloFrom, tloTo);
                }

                tloFrom.CanCopy = false;
            }
        }

        private void CopyHitsounds(Arguments arg, Timeline tlFrom, Timeline tlTo,
            List<TimingPointsChange> timingPointsChanges, GameMode mode, string mapDir,
            Dictionary<string, string> firstSamples) {
            foreach (var tloFrom in tlFrom.TimelineObjects) {
                var tloTo = tlTo.GetNearestTlo(tloFrom.Time, true);

                if (tloTo != null &&
                    Math.Abs(Math.Round(tloFrom.Time) - Math.Round(tloTo.Time)) <= arg.TemporalLeniency) {
                    // Copy to this tlo
                    CopyHitsounds(arg, tloFrom, tloTo);

                    // Add timingpointschange to copy timingpoint hitsounds
                    var tp = tloFrom.HitsoundTimingPoint.Copy();
                    tp.Offset = tloTo.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: arg.CopySamplesets,
                        index: arg.CopySamplesets, volume: arg.CopyVolumes));
                }

                tloFrom.CanCopy = false;
            }

            // Timingpointchange all the undefined tlo from copyFrom
            foreach (var tloTo in tlTo.TimelineObjects) {
                if (!tloTo.CanCopy) continue;
                var tp = tloTo.HitsoundTimingPoint.Copy();
                var holdSampleset = arg.CopySamplesets && tloTo.SampleSet == SampleSet.Auto;
                var holdIndex = arg.CopySamplesets && !(tloTo.CanCustoms && tloTo.CustomIndex != 0);

                // Dont hold indexes or sampleset if the sample it plays currently is the same as the sample it would play without conserving
                if (holdSampleset || holdIndex) {
                    var nativeSamples = tloTo.GetFirstPlayingFilenames(mode, mapDir, firstSamples);

                    if (holdSampleset) {
                        var oldSampleSet = tloTo.FenoSampleSet;
                        var newSampleSet = tloTo.FenoSampleSet;
                        var latest = double.NegativeInfinity;
                        foreach (TimingPointsChange tpc in timingPointsChanges) {
                            if (!tpc.Sampleset || !(tpc.MyTP.Offset <= tloTo.Time) || !(tpc.MyTP.Offset >= latest))
                                continue;
                            newSampleSet = tpc.MyTP.SampleSet;
                            latest = tpc.MyTP.Offset;
                        }

                        tp.SampleSet = newSampleSet;
                        tloTo.GiveHitsoundTimingPoint(tp);
                        var newSamples = tloTo.GetFirstPlayingFilenames(mode, mapDir, firstSamples);
                        tp.SampleSet = nativeSamples.SequenceEqual(newSamples) ? newSampleSet : oldSampleSet;
                    }

                    if (holdIndex) {
                        var oldIndex = tloTo.FenoCustomIndex;
                        var newIndex = tloTo.FenoCustomIndex;
                        var latest = double.NegativeInfinity;
                        foreach (var tpc in timingPointsChanges) {
                            if (!tpc.Index || !(tpc.MyTP.Offset <= tloTo.Time) || !(tpc.MyTP.Offset >= latest))
                                continue;
                            newIndex = tpc.MyTP.SampleIndex;
                            latest = tpc.MyTP.Offset;
                        }

                        tp.SampleIndex = newIndex;
                        tloTo.GiveHitsoundTimingPoint(tp);
                        var newSamples = tloTo.GetFirstPlayingFilenames(mode, mapDir, firstSamples);
                        tp.SampleIndex = nativeSamples.SequenceEqual(newSamples) ? newIndex : oldIndex;
                    }

                    tloTo.GiveHitsoundTimingPoint(tp);
                }

                tp.Offset = tloTo.Time;
                timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: holdSampleset, index: holdIndex,
                    volume: arg.CopyVolumes));
            }
        }

        private static void CopyHitsounds(Arguments arg, TimelineObject tloFrom, TimelineObject tloTo) {
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
            tloTo.CanCopy = false;
        }

        private static void ResetHitObjectHitsounds(Beatmap beatmap) {
            foreach (var ho in beatmap.HitObjects) {
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

                if (!ho.IsSlider) continue;
                // Remove edge hitsounds
                ho.EdgeHitsounds = ho.EdgeHitsounds.Select(o => 0).ToList();
                ho.EdgeSampleSets = ho.EdgeSampleSets.Select(o => SampleSet.Auto).ToList();
                ho.EdgeAdditionSets = ho.EdgeAdditionSets.Select(o => SampleSet.Auto).ToList();
            }
        }

        private static bool FilterMuteTlo(TimelineObject tloTo, Beatmap beatmapTo, Arguments arg) {
            // Check whether it's defined
            if (!tloTo.CanCopy)
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
            var timingPoint = beatmapTo.BeatmapTiming.GetRedlineAtTime(tloTo.Time - 1);
            var resnappedTime = beatmapTo.BeatmapTiming.Resnap(tloTo.Time, arg.Snap1, arg.Snap2, false, timingPoint);
            var beatsFromRedline = (resnappedTime - timingPoint.Offset) / timingPoint.MpB;
            var dist1 = beatsFromRedline * arg.Snap1 / (arg.Snap1 == 1 ? 4 : 2);
            var dist2 = beatsFromRedline * arg.Snap2 / (arg.Snap2 == 1 ? 4 : arg.Snap2 == 3 ? 3 : 2);
            dist1 %= 1;
            dist2 %= 1;
            if (Precision.AlmostEquals(dist1, 0) || Precision.AlmostEquals(dist1, 1) ||
                Precision.AlmostEquals(dist2, 0) || Precision.AlmostEquals(dist2, 1))
                return false;

            // Check filter temporal length
            return Precision.AlmostBigger(tloTo.Origin.TemporalLength, arg.MinLength * timingPoint.MpB);
        }

        private void BeatmapFromBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                var paths = IOHelper.BeatmapFileDialog();
                if (paths.Length != 0) {
                    BeatmapFromBox.Text = paths[0];
                }
            } catch (Exception) {
                // ignored
            }
        }

        private void BeatmapFromLoad_Click(object sender, RoutedEventArgs e) {
            try {
                var path = IOHelper.GetCurrentBeatmap();
                if (path != "") {
                    BeatmapFromBox.Text = path;
                }
            } catch (Exception) {
                // ignored
            }
        }

        private void BeatmapToBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                var paths = IOHelper.BeatmapFileDialog(multiselect: true);
                if (paths.Length != 0) {
                    BeatmapToBox.Text = string.Join("|", paths);
                }
            } catch (Exception) {
                // ignored
            }
        }

        private void BeatmapToLoad_Click(object sender, RoutedEventArgs e) {
            try {
                var path = IOHelper.GetCurrentBeatmap();
                if (path != "") {
                    BeatmapToBox.Text = path;
                }
            } catch (Exception) {
                // ignored
            }
        }
    }
}