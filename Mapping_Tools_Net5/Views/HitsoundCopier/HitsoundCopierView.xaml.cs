using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.Events;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Views.HitsoundCopier {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundCopierView : ISavable<HitsoundCopierVm> {
        public static readonly string ToolName = "Hitsound Copier";

        /// <summary>
        /// 
        /// </summary>
        [UsedImplicitly] 
        public static readonly string ToolDescription =
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
            DataContext = new HitsoundCopierVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public HitsoundCopierVm ViewModel => (HitsoundCopierVm) DataContext;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Hitsounds((HitsoundCopierVm) e.Argument, bgw);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            foreach (string fileToCopy in BeatmapToBox.Text.Split('|')) {
                BackupManager.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        private string Copy_Hitsounds(HitsoundCopierVm arg, BackgroundWorker worker) {
            var doMutedIndex = arg.MutedIndex >= 0;

            var paths = arg.PathTo.Split('|');
            var mapsDone = 0;
            var sampleSchema = new SampleSchema();

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (var pathTo in paths) {
                BeatmapEditor editorTo = EditorReaderStuff.GetNewestVersionOrNot(pathTo, reader);;
                Beatmap beatmapTo = editorTo.Beatmap;
                Beatmap beatmapFrom;

                if (!string.IsNullOrEmpty(arg.PathFrom)) {
                    var editorFrom = EditorReaderStuff.GetNewestVersionOrNot(arg.PathFrom, reader);
                    beatmapFrom = editorFrom.Beatmap;
                } else {
                    // Copy from an empty beatmap similar to the map to copy to
                    beatmapFrom = beatmapTo.DeepCopy();
                    beatmapFrom.HitObjects.Clear();
                    beatmapFrom.BeatmapTiming.Clear();
                }

                Timeline processedTimeline;

                if (arg.CopyMode == 0) {
                    // Every defined hitsound and sampleset on hitsound gets copied to their copyTo destination
                    // Timelines
                    var tlTo = beatmapTo.GetTimeline();
                    var tlFrom = beatmapFrom.GetTimeline();

                    var volumeMuteTimes = arg.CopyVolumes && arg.AlwaysPreserve5Volume ? new List<double>() : null;

                    if (arg.CopyHitsounds) {
                        ResetHitObjectHitsounds(beatmapTo);
                        CopyHitsounds(arg, tlFrom, tlTo);
                    }

                    // Save tlo times where timingpoint volume is 5%
                    // Timingpointchange all the undefined tlo from copyFrom
                    volumeMuteTimes?.AddRange(from tloTo in tlTo.TimelineObjects
                        where tloTo.CanCopy && Math.Abs(tloTo.SampleVolume) < Precision.DOUBLE_EPSILON
                                            && Math.Abs(tloTo.FenoSampleVolume - 5) < Precision.DOUBLE_EPSILON
                        select tloTo.Time);

                    // Volumes and samplesets and customindices greenlines get copied with timingpointchanges and allafter enabled
                    var timingPointsChanges = beatmapFrom.BeatmapTiming.TimingPoints.Select(tp =>
                        new TimingPointsChange(tp, sampleset: arg.CopySampleSets, index: arg.CopySampleSets,
                            volume: arg.CopyVolumes)).ToList();

                    // Apply the timingpoint changes
                    TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChanges, true);

                    processedTimeline = tlTo;

                    // Return 5% volume to tlo that had it before
                    if (volumeMuteTimes != null) {
                        var timingPointsChangesMute = new List<TimingPointsChange>();
                        processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                        // Exclude objects which use their own sample volume property instead
                        foreach (var tloTo in processedTimeline.TimelineObjects.Where(o => Math.Abs(o.SampleVolume) < Precision.DOUBLE_EPSILON)) {
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
                    var mode = (GameMode) beatmapTo.General["Mode"].IntValue;
                    var mapDir = editorTo.GetParentFolder();
                    var firstSamples = HitsoundImporter.AnalyzeSamples(mapDir);

                    if (arg.CopyHitsounds) {
                        CopyHitsounds(arg, beatmapTo, tlFrom, tlTo, timingPointsChanges, mode, mapDir, firstSamples, ref sampleSchema);
                    }

                    if (arg.CopyBodyHitsounds) {
                        // Remove timingpoints in beatmapTo that are in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                        foreach (var tp in from ho in beatmapTo.HitObjects
                            from tp in ho.BodyHitsounds
                            where beatmapFrom.HitObjects.Any(o => o.Time < tp.Offset && o.EndTime > tp.Offset)
                            where !tp.Uninherited
                            select tp) {
                            beatmapTo.BeatmapTiming.Remove(tp);
                        }

                        // Get timingpointschanges for every timingpoint from beatmapFrom that is in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                        timingPointsChanges.AddRange(from ho in beatmapFrom.HitObjects
                            from tp in ho.BodyHitsounds
                            where beatmapTo.HitObjects.Any(o => o.Time < tp.Offset && o.EndTime > tp.Offset)
                            select new TimingPointsChange(tp.Copy(), sampleset: arg.CopySampleSets, index: arg.CopySampleSets,
                                volume: arg.CopyVolumes));
                    }

                    // Apply the timingpoint changes
                    TimingPointsChange.ApplyChanges(beatmapTo.BeatmapTiming, timingPointsChanges);

                    processedTimeline = tlTo;
                }

                if (arg.CopyStoryboardedSamples) {
                    if (arg.CopyMode == 0) {
                        beatmapTo.StoryboardSoundSamples.Clear();
                    }

                    beatmapTo.GiveObjectsGreenlines();
                    processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                    var mapDir = editorTo.GetParentFolder();
                    var firstSamples = HitsoundImporter.AnalyzeSamples(mapDir, true);

                    var samplesTo = new HashSet<StoryboardSoundSample>(beatmapTo.StoryboardSoundSamples);
                    var mode = (GameMode) beatmapTo.General["Mode"].IntValue;

                    foreach (var sampleFrom in beatmapFrom.StoryboardSoundSamples) {
                        if (arg.IgnoreHitsoundSatisfiedSamples) {
                            var tloHere = processedTimeline.TimelineObjects.FindAll(o =>
                                Math.Abs(o.Time - sampleFrom.StartTime) <= arg.TemporalLeniency);
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
                    beatmapTo.StoryboardSoundSamples.Sort();
                }

                if (arg.MuteSliderends) {
                    var timingPointsChanges = new List<TimingPointsChange>();
                    beatmapTo.GiveObjectsGreenlines();
                    processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                    foreach (var tloTo in processedTimeline.TimelineObjects) {
                        if (FilterMuteTlo(tloTo, beatmapTo, arg)) {
                            // Set volume to 5%, remove all hitsounds, apply customindex and sampleset
                            tloTo.SampleSet = arg.MutedSampleSet;
                            tloTo.AdditionSet = 0;
                            tloTo.Normal = false;
                            tloTo.Whistle = false;
                            tloTo.Finish = false;
                            tloTo.Clap = false;

                            tloTo.HitsoundsToOrigin();

                            // Add timingpointschange to copy timingpoint hitsounds
                            var tp = tloTo.HitsoundTimingPoint.Copy();
                            tp.Offset = tloTo.Time;
                            tp.SampleSet = arg.MutedSampleSet;
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

                // Export the sample schema if there are samples
                if (sampleSchema.Count > 0) {
                    string exportFolder = MainWindow.ExportPath;

                    DirectoryInfo di = new DirectoryInfo(exportFolder);
                    foreach (FileInfo file in di.GetFiles()) {
                        file.Delete();
                    }

                    HitsoundExporter.ExportSampleSchema(sampleSchema, exportFolder);

                    System.Diagnostics.Process.Start(exportFolder);
                }

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            return "Done!";
        }

        private static void CopyHitsounds(HitsoundCopierVm arg, Timeline tlFrom, Timeline tlTo) {
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

        private void CopyHitsounds(HitsoundCopierVm arg, Beatmap beatmapTo, 
            Timeline tlFrom, Timeline tlTo,
            List<TimingPointsChange> timingPointsChanges, GameMode mode, string mapDir,
            Dictionary<string, string> firstSamples, ref SampleSchema sampleSchema) {

            var CustomSampledTimes = new HashSet<int>();
            var tloToSliderSlide = new List<TimelineObject>();

            foreach (var tloFrom in tlFrom.TimelineObjects) {
                var tloTo = tlTo.GetNearestTlo(tloFrom.Time, true);

                if (tloTo != null &&
                    Math.Abs(Math.Round(tloFrom.Time) - Math.Round(tloTo.Time)) <= arg.TemporalLeniency) {
                    // Copy to this tlo
                    CopyHitsounds(arg, tloFrom, tloTo);

                    // Add timingpointschange to copy timingpoint hitsounds
                    var tp = tloFrom.HitsoundTimingPoint.Copy();
                    tp.Offset = tloTo.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: arg.CopySampleSets,
                        index: arg.CopySampleSets, volume: arg.CopyVolumes));
                }
                // Try to find a slider tick in range to copy the sample to instead.
                // This slider tick gets a custom sample and timingpoints change to imitate the copied hitsound.
                else 
                if (arg.CopyToSliderTicks && 
                           FindSliderTickInRange(beatmapTo, tloFrom.Time - arg.TemporalLeniency, tloFrom.Time + arg.TemporalLeniency, out var sliderTickTime, out var tickSlider) &&
                           !CustomSampledTimes.Contains((int) sliderTickTime)) {
                    // Add a new custom sample to this slider tick to represent the hitsounds
                    List<string> sampleFilenames = tloFrom.GetFirstPlayingFilenames(mode, mapDir, firstSamples, false);
                    List<SampleGeneratingArgs> samples = sampleFilenames
                        .Select(o => new SampleGeneratingArgs(Path.Combine(mapDir, o)))
                        .Where(o => SampleImporter.ValidateSampleArgs(o, true))
                        .ToList();

                    if (samples.Count > 0) {
                        if (sampleSchema.AddHitsound(samples, "slidertick", tloFrom.FenoSampleSet,
                            out int index, out var sampleSet, arg.StartIndex)) {
                            // Add a copy of the slider slide sound to this index if necessary
                            var oldIndex = tloFrom.HitsoundTimingPoint.SampleIndex;
                            var oldSampleSet = tloFrom.HitsoundTimingPoint.SampleSet;
                            var oldSlideFilename =
                                $"{oldSampleSet.ToString().ToLower()}-sliderslide{(oldIndex == 1 ? string.Empty : oldIndex.ToInvariant())}";
                            var oldSlidePath = Path.Combine(mapDir, oldSlideFilename);

                            if (firstSamples.ContainsKey(oldSlidePath)) {
                                oldSlidePath = firstSamples[oldSlidePath];
                                var slideGeneratingArgs = new SampleGeneratingArgs(oldSlidePath);
                                var newSlideFilename =
                                    $"{sampleSet.ToString().ToLower()}-sliderslide{index.ToInvariant()}";

                                sampleSchema.Add(newSlideFilename,
                                    new List<SampleGeneratingArgs> {slideGeneratingArgs});
                            }
                        }

                        // Make sure the slider with the slider ticks uses auto sampleset so the customized greenlines control the hitsounds
                        tickSlider.SampleSet = SampleSet.Auto;

                        // Add timingpointschange
                        var tp = tloFrom.HitsoundTimingPoint.Copy();
                        tp.Offset = sliderTickTime;
                        tp.SampleIndex = index;
                        tp.SampleSet = sampleSet;
                        tp.Volume = tloFrom.FenoSampleVolume;
                        timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: arg.CopySampleSets,
                            index: arg.CopySampleSets, volume: arg.CopyVolumes));

                        // Add timingpointschange 5ms later to revert the stuff back to whatever it should be
                        var tp2 = tloFrom.HitsoundTimingPoint.Copy();
                        tp2.Offset = sliderTickTime + 5;
                        timingPointsChanges.Add(new TimingPointsChange(tp2, sampleset: arg.CopySampleSets,
                            index: arg.CopySampleSets, volume: arg.CopyVolumes));

                        CustomSampledTimes.Add((int) sliderTickTime);
                    }
                }
                // If the there is no slidertick to be found, then try copying it to the slider slide
                else 
                if (arg.CopyToSliderSlides) {
                    tloToSliderSlide.Add(tloFrom);
                }

                tloFrom.CanCopy = false;
            }

            // Do the sliderslide hitsounds after because the ticks need to add sliderslides with strict indices.
            foreach (var tlo in tloToSliderSlide) {
                if (!FindSliderAtTime(beatmapTo, tlo.Time, out var slideSlider) ||
                      CustomSampledTimes.Contains((int)tlo.Time)) 
                    continue;

                // Add a new custom sample to this slider slide to represent the hitsounds
                List<string> sampleFilenames = tlo.GetFirstPlayingFilenames(mode, mapDir, firstSamples, false);
                List<SampleGeneratingArgs> samples = sampleFilenames
                    .Select(o => new SampleGeneratingArgs(Path.Combine(mapDir, o)))
                    .Where(o => SampleImporter.ValidateSampleArgs(o))
                    .ToList();

                if (samples.Count > 0) {
                    sampleSchema.AddHitsound(samples, "sliderslide", tlo.FenoSampleSet,
                        out int index, out var sampleSet, arg.StartIndex);

                    // Add timingpointschange
                    var tp = tlo.HitsoundTimingPoint.Copy();
                    tp.Offset = tlo.Time;
                    tp.SampleIndex = index;
                    tp.SampleSet = sampleSet;
                    tp.Volume = tlo.FenoSampleVolume;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: arg.CopySampleSets,
                        index: arg.CopySampleSets, volume: arg.CopyVolumes));

                    // Make sure the slider with the slider ticks uses auto sampleset so the customized greenlines control the hitsounds
                    slideSlider.SampleSet = SampleSet.Auto;
                }
            }

            // Timingpointchange all the undefined tlo from copyFrom
            foreach (var tloTo in tlTo.TimelineObjects) {
                if (!tloTo.CanCopy) continue;
                var tp = tloTo.HitsoundTimingPoint.Copy();
                var holdSampleset = arg.CopySampleSets && tloTo.SampleSet == SampleSet.Auto;
                var holdIndex = arg.CopySampleSets && !(tloTo.CanCustoms && tloTo.CustomIndex != 0);

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

        private static bool FindSliderTickInRange(Beatmap beatmap, double startTime, double endTime, out double sliderTickTime, out HitObject tickSlider) {
            var tickrate = beatmap.Difficulty.ContainsKey("SliderTickRate")
                ? beatmap.Difficulty["SliderTickRate"].DoubleValue : 1.0;

            // Check all sliders in range and exclude those which have NaN SV, because those dont have slider ticks
            foreach (var slider in beatmap.HitObjects.Where(o => o.IsSlider && 
                                                                 !double.IsNaN(o.SliderVelocity) && 
                                                                 (o.Time < endTime || o.EndTime > startTime))) {
                var timeBetweenTicks = slider.UnInheritedTimingPoint.MpB / tickrate;

                sliderTickTime = slider.Time + timeBetweenTicks;
                while (sliderTickTime < slider.EndTime - 5) {  // This -5 is to make sure the +5 ms timingpoint that reverts the change is still inside the slider
                    if (sliderTickTime >= startTime && sliderTickTime <= endTime) {
                        tickSlider = slider;
                        return true;
                    }
                    sliderTickTime += timeBetweenTicks;
                }
            }

            sliderTickTime = -1;
            tickSlider = null;
            return false;
        }

        private static bool FindSliderAtTime(Beatmap beatmap, double time, out HitObject slider) {
            slider = beatmap.HitObjects.FirstOrDefault(ho => ho.IsSlider && ho.Time < time && ho.EndTime > time);
            return slider != null;
        }

        private static void CopyHitsounds(HitsoundCopierVm arg, TimelineObject tloFrom, TimelineObject tloTo) {
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

        private static bool FilterMuteTlo(TimelineObject tloTo, Beatmap beatmapTo, HitsoundCopierVm arg) {
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

            // Check if this tlo has hitsounds
            if (tloTo.Whistle || tloTo.Finish || tloTo.Clap || 
                (arg.MutedSampleSet != SampleSet.Auto && tloTo.FenoSampleSet != arg.MutedSampleSet)) {
                return false;
            }

            // Check filter snap
            var allBeatDivisors = arg.BeatDivisors;

            var timingPoint = beatmapTo.BeatmapTiming.GetRedlineAtTime(tloTo.Time - 1);
            var resnappedTime = beatmapTo.BeatmapTiming.Resnap(tloTo.Time, allBeatDivisors, false, tp: timingPoint);
            var beatsFromRedline = (resnappedTime - timingPoint.Offset) / timingPoint.MpB;

            // Get all the divisors which the sliderend could possibly be snapped to
            var possibleDivisors =
                allBeatDivisors.Where(d => Precision.AlmostEquals(beatsFromRedline % d.GetValue(), 0) ||
                                           Precision.AlmostEquals(beatsFromRedline % d.GetValue(), 1));

            // Make sure all the possible beat divisors of lower priority are in the muted category
            if (possibleDivisors.TakeWhile(d => !arg.MutedDivisors.Contains(d)).Any()) {
                return false;
            }

            // Check filter temporal length
            return Precision.AlmostBigger(tloTo.Origin.TemporalLength, arg.MinLength * timingPoint.MpB);
        }

        public HitsoundCopierVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(HitsoundCopierVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "hitsoundcopierproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Hitsound Copier Projects");
    }
}