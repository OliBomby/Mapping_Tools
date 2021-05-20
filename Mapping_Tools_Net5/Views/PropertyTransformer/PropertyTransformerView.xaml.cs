using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Events;
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
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = TransformProperties((PropertyTransformerVm)e.Argument, bgw, e);
        }

        private bool Filter(double value, double time, PropertyTransformerVm vm) {
            bool doFilterMatch = vm.MatchFilter.Length > 0 && vm.EnableFilters;
            bool doFilterUnmatch = vm.UnmatchFilter.Length > 0 && vm.EnableFilters;
            bool doFilterRange = (vm.MinTimeFilter != -1 || vm.MaxTimeFilter != -1) && vm.EnableFilters && !double.IsNaN(time);
            double min = vm.MinTimeFilter == -1 ? double.NegativeInfinity : vm.MinTimeFilter;
            double max = vm.MaxTimeFilter == -1 ? double.PositiveInfinity : vm.MaxTimeFilter;

            return (!doFilterMatch || vm.MatchFilter.Any(o => Precision.AlmostEquals(value, o, 0.001))) && 
                   (!doFilterUnmatch || !vm.UnmatchFilter.Any(o => Precision.AlmostEquals(value, o, 0.001))) &&
                   (!doFilterRange || (time >= min && time <= max));
        }

        private string TransformProperties(PropertyTransformerVm vm, BackgroundWorker worker, DoWorkEventArgs _) {
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (string path in vm.ExportPaths) {
                Editor editor;
                if (Path.GetExtension(path) == ".osb") {
                    editor = new StoryboardEditor(path);
                } else {
                    editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                }

                if (editor is BeatmapEditor beatmapEditor) {
                    Beatmap beatmap = beatmapEditor.Beatmap;

                    List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                    foreach (TimingPoint tp in beatmap.BeatmapTiming.TimingPoints) {
                        // Offset
                        if (vm.TimingpointOffsetMultiplier != 1 || vm.TimingpointOffsetOffset != 0) {
                            if (Filter(tp.Offset, tp.Offset, vm)) {
                                tp.Offset = Math.Round(tp.Offset * vm.TimingpointOffsetMultiplier +
                                                       vm.TimingpointOffsetOffset);
                            }
                        }

                        // BPM
                        if (vm.TimingpointBPMMultiplier != 1 || vm.TimingpointBPMOffset != 0) {
                            if (tp.Uninherited) {
                                if (Filter(tp.GetBpm(), tp.Offset, vm)) {
                                    double newBPM = tp.GetBpm() * vm.TimingpointBPMMultiplier + vm.TimingpointBPMOffset;
                                    newBPM = vm.ClipProperties
                                        ? MathHelper.Clamp(newBPM, 15, 10000)
                                        : newBPM; // Clip the value if specified
                                    tp.MpB = 60000 / newBPM;
                                }
                            }
                        }

                        // Slider Velocity
                        if (vm.TimingpointSVMultiplier != 1 || vm.TimingpointSVOffset != 0) {
                            if (Filter(beatmap.BeatmapTiming.GetSvMultiplierAtTime(tp.Offset), tp.Offset, vm)) {
                                TimingPoint tpchanger = tp.Copy();
                                double newSV =
                                    beatmap.BeatmapTiming.GetSvMultiplierAtTime(tp.Offset) *
                                    vm.TimingpointSVMultiplier + vm.TimingpointSVOffset;
                                newSV = vm.ClipProperties
                                    ? MathHelper.Clamp(newSV, 0.1, 10)
                                    : newSV; // Clip the value if specified
                                tpchanger.MpB = -100 / newSV;
                                timingPointsChanges.Add(new TimingPointsChange(tpchanger, mpb: true));
                            }
                        }

                        // Index
                        if (vm.TimingpointIndexMultiplier != 1 || vm.TimingpointIndexOffset != 0) {
                            if (Filter(tp.SampleIndex, tp.Offset, vm)) {
                                int newIndex =
                                    (int) Math.Round(tp.SampleIndex * vm.TimingpointIndexMultiplier +
                                                     vm.TimingpointIndexOffset);
                                tp.SampleIndex = vm.ClipProperties ? MathHelper.Clamp(newIndex, 0, int.MaxValue) : newIndex;
                            }
                        }

                        // Volume
                        if (vm.TimingpointVolumeMultiplier != 1 || vm.TimingpointVolumeOffset != 0) {
                            if (Filter(tp.Volume, tp.Offset, vm)) {
                                int newVolume =
                                    (int) Math.Round(tp.Volume * vm.TimingpointVolumeMultiplier +
                                                     vm.TimingpointVolumeOffset);
                                tp.Volume = vm.ClipProperties ? MathHelper.Clamp(newVolume, 5, 100) : newVolume;
                            }
                        }
                    }

                    UpdateProgressBar(worker, 20);

                    // Hitobject time
                    if (vm.HitObjectTimeMultiplier != 1 || vm.HitObjectTimeOffset != 0) {
                        foreach (HitObject ho in beatmap.HitObjects) {
                            // Get the end time early because the start time gets modified
                            double oldEndTime = ho.GetEndTime(false);

                            if (Filter(ho.Time, ho.Time, vm)) {
                                ho.Time = Math.Round(ho.Time * vm.HitObjectTimeMultiplier + vm.HitObjectTimeOffset);
                            }

                            // Transform end time of hold notes and spinner
                            if ((ho.IsHoldNote || ho.IsSpinner) &&
                                Filter(oldEndTime, oldEndTime, vm)) {
                                ho.EndTime = Math.Round(oldEndTime * vm.HitObjectTimeMultiplier + vm.HitObjectTimeOffset);
                            }
                        }
                    }

                    UpdateProgressBar(worker, 30);

                    // Bookmark time
                    if (vm.BookmarkTimeMultiplier != 1 || vm.BookmarkTimeOffset != 0) {
                        List<double> newBookmarks = new List<double>();
                        List<double> bookmarks = beatmap.GetBookmarks();
                        foreach (double bookmark in bookmarks) {
                            if (Filter(bookmark, bookmark, vm)) {
                                newBookmarks.Add(
                                    Math.Round(bookmark * vm.BookmarkTimeMultiplier + vm.BookmarkTimeOffset));
                            }
                            else {
                                newBookmarks.Add(bookmark);
                            }
                        }

                        beatmap.SetBookmarks(newBookmarks);
                    }

                    UpdateProgressBar(worker, 40);

                    // Storyboarded event time
                    if (vm.SBEventTimeMultiplier != 1 || vm.SBEventTimeOffset != 0) {
                        foreach (Event ev in beatmap.StoryboardLayerBackground.Concat(beatmap.StoryboardLayerFail)
                            .Concat(beatmap.StoryboardLayerPass).Concat(beatmap.StoryboardLayerForeground)
                            .Concat(beatmap.StoryboardLayerOverlay)) {
                            TransformEventTime(ev, vm.SBEventTimeMultiplier, vm.SBEventTimeOffset, vm);
                        }
                    }

                    UpdateProgressBar(worker, 50);

                    // Storyboarded sample time
                    if (vm.SBSampleTimeMultiplier != 1 || vm.SBSampleTimeOffset != 0) {
                        foreach (StoryboardSoundSample ss in beatmap.StoryboardSoundSamples) {
                            if (Filter(ss.StartTime, ss.StartTime, vm)) {
                                ss.StartTime =
                                    (int) Math.Round(ss.StartTime * vm.SBSampleTimeMultiplier + vm.SBSampleTimeOffset);
                            }
                        }
                    }

                    UpdateProgressBar(worker, 60);

                    // Break time
                    if (vm.BreakTimeMultiplier != 1 || vm.BreakTimeOffset != 0) {
                        foreach (Break br in beatmap.BreakPeriods) {
                            if (Filter(br.StartTime, br.StartTime, vm)) {
                                br.StartTime =
                                    (int) Math.Round(br.StartTime * vm.BreakTimeMultiplier + vm.BreakTimeOffset);
                            }

                            if (Filter(br.EndTime, br.EndTime, vm)) {
                                br.EndTime = (int) Math.Round(br.EndTime * vm.BreakTimeMultiplier + vm.BreakTimeOffset);
                            }
                        }
                    }

                    UpdateProgressBar(worker, 70);

                    // Video start time
                    if (vm.VideoTimeMultiplier != 1 || vm.VideoTimeOffset != 0) {
                        foreach (Event ev in beatmap.BackgroundAndVideoEvents) {
                            if (ev is Video video) {
                                if (Filter(video.StartTime, video.StartTime, vm)) {
                                    video.StartTime =
                                        (int) Math.Round(video.StartTime * vm.VideoTimeMultiplier + vm.VideoTimeOffset);
                                }
                            }
                        }
                    }

                    UpdateProgressBar(worker, 80);

                    // Preview point time
                    if (vm.PreviewTimeMultiplier != 1 || vm.PreviewTimeOffset != 0) {
                        if (beatmap.General.ContainsKey("PreviewTime") &&
                            beatmap.General["PreviewTime"].IntValue != -1) {
                            var previewTime = beatmap.General["PreviewTime"].DoubleValue;
                            if (Filter(previewTime, previewTime, vm)) {
                                var newPreviewTime =
                                    Math.Round(previewTime * vm.PreviewTimeMultiplier + vm.PreviewTimeOffset);
                                beatmap.General["PreviewTime"].SetDouble(newPreviewTime);
                            }
                        }
                    }

                    UpdateProgressBar(worker, 90);

                    TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);

                    // Save the file
                    beatmapEditor.SaveFile();

                    UpdateProgressBar(worker, 100);
                } else if (editor is StoryboardEditor storyboardEditor) {
                    StoryBoard storyboard = storyboardEditor.StoryBoard;
                    
                    // Storyboarded event time
                    if (vm.SBEventTimeMultiplier != 1 || vm.SBEventTimeOffset != 0) {
                        foreach (Event ev in storyboard.StoryboardLayerBackground.Concat(storyboard.StoryboardLayerFail)
                            .Concat(storyboard.StoryboardLayerPass).Concat(storyboard.StoryboardLayerForeground)
                            .Concat(storyboard.StoryboardLayerOverlay)) {
                            TransformEventTime(ev, vm.SBEventTimeMultiplier, vm.SBEventTimeOffset, vm);
                        }
                    }

                    UpdateProgressBar(worker, 50);

                    // Storyboarded sample time
                    if (vm.SBSampleTimeMultiplier != 1 || vm.SBSampleTimeOffset != 0) {
                        foreach (StoryboardSoundSample ss in storyboard.StoryboardSoundSamples) {
                            if (Filter(ss.StartTime, ss.StartTime, vm)) {
                                ss.StartTime =
                                    (int)Math.Round(ss.StartTime * vm.SBSampleTimeMultiplier + vm.SBSampleTimeOffset);
                            }
                        }
                    }

                    UpdateProgressBar(worker, 70);

                    // Video start time
                    if (vm.VideoTimeMultiplier != 1 || vm.VideoTimeOffset != 0) {
                        foreach (Event ev in storyboard.BackgroundAndVideoEvents) {
                            if (ev is Video video) {
                                if (Filter(video.StartTime, video.StartTime, vm)) {
                                    video.StartTime =
                                        (int)Math.Round(video.StartTime * vm.VideoTimeMultiplier + vm.VideoTimeOffset);
                                }
                            }
                        }
                    }

                    UpdateProgressBar(worker, 90);

                    // Save the file
                    storyboardEditor.SaveFile();

                    UpdateProgressBar(worker, 100);
                }
            }

            return "Done!";
        }

        private void TransformEventTime(Event ev, double multiplier, double offset, PropertyTransformerVm vm) {
            // Commands under loops use relative time so they shouldn't get offset
            if (ev.ParentEvent is StandardLoop || ev.ParentEvent is TriggerLoop) {
                if (ev is IHasStartTime st && Filter(st.StartTime, st.StartTime, vm)) {
                    st.StartTime = (int) Math.Round(st.StartTime * multiplier);
                }
                if (ev is IHasEndTime et && Filter(et.EndTime, et.EndTime, vm)) {
                    et.EndTime = (int) Math.Round(et.EndTime * multiplier);
                }
                if (ev is IHasDuration d && Filter(d.Duration, double.NaN, vm)) {  // Just a duration doesnt have a time to filter
                    d.Duration *= multiplier;
                }
            } else {
                if (ev is IHasStartTime st && Filter(st.StartTime, st.StartTime, vm)) {
                    st.StartTime = (int)Math.Round(st.StartTime * multiplier + offset);
                }
                if (ev is IHasEndTime et && Filter(et.EndTime, et.EndTime, vm)) {
                    et.EndTime = (int)Math.Round(et.EndTime * multiplier + offset);
                }
                if (ev is IHasDuration d && Filter(d.Duration, double.NaN, vm)) {  // Just a duration doesnt have a time to filter
                    d.Duration *= multiplier;
                }
            }

            // Recurse to also transform all the children events
            foreach (var child in ev.ChildEvents) {
                TransformEventTime(child, multiplier, offset, vm);
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            // Backup
            string[] filesToCopy = MainWindow.AppWindow.GetCurrentMaps();
            BackupManager.SaveMapBackup(filesToCopy);

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
