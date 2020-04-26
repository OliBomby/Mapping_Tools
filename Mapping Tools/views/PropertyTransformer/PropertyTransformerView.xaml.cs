using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.PropertyTransformer {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class PropertyTransformerView : ISavable<PropertyTransformerVm> {
        public static readonly string ToolName = "Property Transformer";

        public static readonly string ToolDescription = $@"Multiple and add to properties of all the timingpoints, hitobjects, bookmarks and storyboarded samples of the current map.{Environment.NewLine}The new value is the old value times the multiplier plus the offset. The multiplier is the left textbox and the offset is the right textbox. The multiplier gets done first.{Environment.NewLine}Resulting values get rounded if they have to be integer.";

        public PropertyTransformerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new PropertyTransformerVm();
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = TransformProperties((PropertyTransformerVm)e.Argument, bgw, e);
        }

        private bool Filter(double value, double time, bool doMatch, bool doRange, double match, double min, double max) {
            return (!doMatch || Precision.AlmostEquals(value, match, 0.01)) && (!doRange || (time >= min && time <= max));
        }

        private string TransformProperties(PropertyTransformerVm vm, BackgroundWorker worker, DoWorkEventArgs _) {
            bool doFilterMatch = vm.MatchFilter != -1 && vm.EnableFilters;
            bool doFilterRange = (vm.MinTimeFilter != -1 || vm.MaxTimeFilter != -1) && vm.EnableFilters;
            double min = vm.MinTimeFilter == -1 ? double.NegativeInfinity : vm.MinTimeFilter;
            double max = vm.MaxTimeFilter == -1 ? double.PositiveInfinity : vm.MaxTimeFilter;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (string path in vm.ExportPaths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                Beatmap beatmap = editor.Beatmap;

                // Count all the total amount of things to loop through
                int loops = 0;
                int totalLoops = beatmap.BeatmapTiming.TimingPoints.Count;
                if (vm.HitObjectTimeMultiplier != 1 || vm.HitObjectTimeOffset != 0)
                    totalLoops += beatmap.HitObjects.Count;
                if (vm.BookmarkTimeMultiplier != 1 || vm.BookmarkTimeOffset != 0)
                    totalLoops += beatmap.GetBookmarks().Count;
                if (vm.SBSampleTimeMultiplier != 1 || vm.SBSampleTimeOffset != 0)
                    totalLoops += beatmap.StoryboardSoundSamples.Count;

                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                foreach (TimingPoint tp in beatmap.BeatmapTiming.TimingPoints) {
                    // Offset
                    if (vm.TimingpointOffsetMultiplier != 1 || vm.TimingpointOffsetOffset != 0) {
                        if (Filter(tp.Offset, tp.Offset, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                            tp.Offset = Math.Round(tp.Offset * vm.TimingpointOffsetMultiplier + vm.TimingpointOffsetOffset);
                        }
                    }

                    // BPM
                    if (vm.TimingpointBPMMultiplier != 1 || vm.TimingpointBPMOffset != 0) {
                        if (tp.Uninherited) {
                            if (Filter(tp.GetBpm(), tp.Offset, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                                double newBPM = tp.GetBpm() * vm.TimingpointBPMMultiplier + vm.TimingpointBPMOffset;
                                newBPM = vm.ClipProperties ? MathHelper.Clamp(newBPM, 15, 10000) : newBPM;  // Clip the value if specified
                                tp.MpB = 60000 / newBPM;
                            }
                        }
                    }

                    // Slider Velocity
                    if (vm.TimingpointSVMultiplier != 1 || vm.TimingpointSVOffset != 0) {
                        if (Filter(beatmap.BeatmapTiming.GetSvMultiplierAtTime(tp.Offset), tp.Offset, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                            TimingPoint tpchanger = tp.Copy();
                            double newSV = beatmap.BeatmapTiming.GetSvMultiplierAtTime(tp.Offset) * vm.TimingpointSVMultiplier + vm.TimingpointSVOffset;
                            newSV = vm.ClipProperties ? MathHelper.Clamp(newSV, 0.1, 10) : newSV;  // Clip the value if specified
                            tpchanger.MpB = -100 / newSV;
                            timingPointsChanges.Add(new TimingPointsChange(tpchanger, mpb: true));
                        }
                    }

                    // Index
                    if (vm.TimingpointIndexMultiplier != 1 || vm.TimingpointIndexOffset != 0) {
                        if (Filter(tp.SampleIndex, tp.Offset, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                            int newIndex = (int)Math.Round(tp.SampleIndex * vm.TimingpointIndexMultiplier + vm.TimingpointIndexOffset);
                            tp.SampleIndex = vm.ClipProperties ? MathHelper.Clamp(newIndex, 0, 100) : newIndex;
                        }
                    }

                    // Volume
                    if (vm.TimingpointVolumeMultiplier != 1 || vm.TimingpointVolumeOffset != 0) {
                        if (Filter(tp.Volume, tp.Offset, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                            int newVolume = (int)Math.Round(tp.Volume * vm.TimingpointVolumeMultiplier + vm.TimingpointVolumeOffset);
                            tp.Volume = vm.ClipProperties ? MathHelper.Clamp(newVolume, 5, 100) : newVolume;
                        }
                    }

                    // Update progress bar
                    loops++;
                    UpdateProgressBar(worker, loops * 100 / totalLoops);
                }

                // Hitobject Time
                if (vm.HitObjectTimeMultiplier != 1 || vm.HitObjectTimeOffset != 0) {
                    foreach (HitObject ho in beatmap.HitObjects) {
                        if (Filter(ho.Time, ho.Time, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                            ho.Time = Math.Round(ho.Time * vm.HitObjectTimeMultiplier + vm.HitObjectTimeOffset);
                        }

                        // Update progress bar
                        loops++;
                        UpdateProgressBar(worker, loops * 100 / totalLoops);
                    }
                }

                // Bookmark Time
                if (vm.BookmarkTimeMultiplier != 1 || vm.BookmarkTimeOffset != 0) {
                    List<double> newBookmarks = new List<double>();
                    List<double> bookmarks = beatmap.GetBookmarks();
                    foreach (double bookmark in bookmarks) {
                        if (Filter(bookmark, bookmark, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                            newBookmarks.Add(Math.Round(bookmark * vm.BookmarkTimeMultiplier + vm.BookmarkTimeOffset));
                        } else {
                            newBookmarks.Add(bookmark);
                        }

                        // Update progress bar
                        loops++;
                        UpdateProgressBar(worker, loops * 100 / totalLoops);
                    }
                    beatmap.SetBookmarks(newBookmarks);
                }

                // Storyboarded sample Time
                if (vm.SBSampleTimeMultiplier != 1 || vm.SBSampleTimeOffset != 0) {
                    foreach (StoryboardSoundSample ss in beatmap.StoryboardSoundSamples) {
                        if (Filter(ss.Time, ss.Time, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                            ss.Time = Math.Round(ss.Time * vm.SBSampleTimeMultiplier + vm.SBSampleTimeOffset);
                        }

                        // Update progress bar
                        loops++;
                        UpdateProgressBar(worker, loops * 100 / totalLoops);
                    }
                }

                // Preview point time
                if (vm.PreviewTimeMultiplier != 1 || vm.PreviewTimeOffset != 0) {
                    if (beatmap.General.ContainsKey("PreviewTime") && beatmap.General["PreviewTime"].IntValue != -1) {
                        var previewTime = beatmap.General["PreviewTime"].DoubleValue;
                        if (Filter(previewTime, previewTime, doFilterMatch, doFilterRange, vm.MatchFilter, min, max)) {
                            var newPreviewTime = Math.Round(previewTime * vm.PreviewTimeMultiplier + vm.PreviewTimeOffset);
                            beatmap.General["PreviewTime"].SetDouble(newPreviewTime);
                        }
                    }
                }

                TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);

                // Save the file
                editor.SaveFile();
            }

            return "Done!";
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Backup
            string[] filesToCopy = MainWindow.AppWindow.GetCurrentMaps();
            IOHelper.SaveMapBackup(filesToCopy);

            ((PropertyTransformerVm)DataContext).ExportPaths = filesToCopy;
            BackgroundWorker.RunWorkerAsync(DataContext);

            CanRun = false;
        }

        public PropertyTransformerVm GetSaveData() {
            return (PropertyTransformerVm) DataContext;
        }

        public void SetSaveData(PropertyTransformerVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "propertytransformerproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Property Transformer Projects");
    }
}
