using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundCopierView :UserControl {
        private BackgroundWorker backgroundWorker;

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
            string fileToCopy = BeatmapToBox.Text;
            IOHelper.SaveMapBackup(fileToCopy);

            backgroundWorker.RunWorkerAsync(new Arguments(fileToCopy, BeatmapFromBox.Text, CopyModeBox.SelectedIndex, LeniencyBox.GetDouble(5), 
                                                          (bool)CopyHitsoundsBox.IsChecked, (bool)CopyBodyBox.IsChecked, (bool)CopySamplesetBox.IsChecked,
                                                          (bool)CopyVolumeBox.IsChecked, (bool)MuteSliderendBox.IsChecked,
                                                          int.Parse(MutedSnap1.Text.Split('/')[1]), int.Parse(MutedSnap2.Text.Split('/')[1]),
                                                          MutedMinLengthBox.GetDouble(0), MutedCustomIndexBox.GetInt(-1), MutedSampleSetBox.SelectedIndex + 1));
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
            public bool MuteSliderends;
            public int Snap1;
            public int Snap2;
            public double MinLength;
            public int MutedIndex;
            public int MutedSampleset;
            public Arguments(string pathTo, string pathFrom, int copyMode, double temporalLeniency, bool copyHitsounds, bool copyBodyHitsounds, bool copySamplesets, bool copyVolumes,
                bool muteSliderends, int snap1, int snap2, double minLength, int mutedIndex, int mutedSampleset)
            {
                PathTo = pathTo;
                PathFrom = pathFrom;
                CopyMode = copyMode;
                TemporalLeniency = temporalLeniency;
                CopyHitsounds = copyHitsounds;
                CopyBodyHitsounds = copyBodyHitsounds;
                CopySamplesets = copySamplesets;
                CopyVolumes = copyVolumes;
                MuteSliderends = muteSliderends;
                Snap1 = snap1;
                Snap2 = snap2;
                MinLength = minLength;
                MutedIndex = mutedIndex;
                MutedSampleset = mutedSampleset;
            }
        }

        private string Copy_Hitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs e) {
            int mode = arg.CopyMode;
            double temporalLeniency = arg.TemporalLeniency;
            bool copyHitsounds = arg.CopyHitsounds;
            bool copySliderbodychanges = arg.CopyBodyHitsounds;
            bool copyVolumes = arg.CopyVolumes;
            bool copySamplesets = arg.CopySamplesets;
            bool muteSliderends = arg.MuteSliderends;
            bool doMutedIndex = arg.MutedIndex >= 0;

            Editor editorTo = new Editor(arg.PathTo);
            Editor editorFrom = new Editor(arg.PathFrom);

            Beatmap beatmapTo = editorTo.Beatmap;
            Beatmap beatmapFrom = editorFrom.Beatmap;

            Timeline processedTimeline;

            if (mode == 0) {
                foreach (HitObject ho in beatmapTo.HitObjects) {
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
                        ho.EdgeHitsounds = ho.EdgeHitsounds.Select(o => 0).ToArray();
                        ho.EdgeSampleSets = ho.EdgeSampleSets.Select(o => 0).ToArray();
                        ho.EdgeAdditionSets = ho.EdgeAdditionSets.Select(o => 0).ToArray();
                        ho.SliderExtras = false;
                    }
                }

                // Every defined hitsound and sampleset on hitsound gets copied to their copyTo destination
                // Timelines
                Timeline tlTo = beatmapTo.GetTimeline();
                Timeline tlFrom = beatmapFrom.GetTimeline();

                if (copyHitsounds) {
                    foreach (TimelineObject tloFrom in tlFrom.TimeLineObjects) {
                        TimelineObject tloTo = tlTo.GetNearestTLO(tloFrom.Time, true);

                        if (tloTo != null && Math.Abs(tloFrom.Time - tloTo.Time) <= temporalLeniency) {
                            // Copy to this tlo
                            tloTo.SampleSet = tloFrom.SampleSet;
                            tloTo.AdditionSet = tloFrom.AdditionSet;
                            tloTo.Normal = tloFrom.Normal;
                            tloTo.Whistle = tloFrom.Whistle;
                            tloTo.Finish = tloFrom.Finish;
                            tloTo.Clap = tloFrom.Clap;
                            tloTo.CustomIndex = tloFrom.CustomIndex;
                            tloTo.SampleVolume = tloFrom.SampleVolume;
                            tloTo.Filename = tloFrom.Filename;

                            // Copy sliderbody hitsounds
                            if (tloTo.IsSliderHead && tloFrom.IsSliderHead && copySliderbodychanges) {
                                tloTo.Origin.Hitsounds = tloFrom.Origin.Hitsounds;
                                tloTo.Origin.SampleSet = tloFrom.Origin.SampleSet;
                                tloTo.Origin.AdditionSet = tloFrom.Origin.AdditionSet;
                            }

                            tloTo.HitsoundsToOrigin();

                            tloTo.canCopy = false;
                        }
                        tloFrom.canCopy = false;
                    }
                }

                // Volumes and samplesets and customindices greenlines get copied with timingpointchanges and allafter enabled
                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

                foreach (TimingPoint tp in beatmapFrom.BeatmapTiming.TimingPoints) {
                    TimingPointsChange tpc = new TimingPointsChange(tp, sampleset: copySamplesets, index: copySamplesets, volume: copyVolumes);
                    timingPointsChanges.Add(tpc);
                }

                // Apply the greenline changes
                foreach (TimingPointsChange c in timingPointsChanges) {
                    c.AddChange(beatmapTo.BeatmapTiming.TimingPoints, true);
                }

                processedTimeline = tlTo;
            } 
            else {
                // Smarty mode
                // Copy the defined hitsounds literally (not feno, that will be reserved for cleaner). Only the tlo that have been defined by copyFrom get overwritten.
                Timeline tlTo = beatmapTo.GetTimeline();
                Timeline tlFrom = beatmapFrom.GetTimeline();

                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

                if (copyHitsounds) {
                    foreach (TimelineObject tloFrom in tlFrom.TimeLineObjects) {
                        TimelineObject tloTo = tlTo.GetNearestTLO(tloFrom.Time, true);

                        if (tloTo != null && Math.Abs(tloFrom.Time - tloTo.Time) <= temporalLeniency) {
                            // Copy to this tlo
                            tloTo.SampleSet = tloFrom.SampleSet;
                            tloTo.AdditionSet = tloFrom.AdditionSet;
                            tloTo.Normal = tloFrom.Normal;
                            tloTo.Whistle = tloFrom.Whistle;
                            tloTo.Finish = tloFrom.Finish;
                            tloTo.Clap = tloFrom.Clap;
                            tloTo.CustomIndex = tloFrom.CustomIndex;
                            tloTo.SampleVolume = tloFrom.SampleVolume;
                            tloTo.Filename = tloFrom.Filename;

                            // Copy sliderbody hitsounds
                            if (tloTo.IsSliderHead && tloFrom.IsSliderHead && copySliderbodychanges) {
                                tloTo.Origin.Hitsounds = tloFrom.Origin.Hitsounds;
                                tloTo.Origin.SampleSet = tloFrom.Origin.SampleSet;
                                tloTo.Origin.AdditionSet = tloFrom.Origin.AdditionSet;
                            }

                            tloTo.HitsoundsToOrigin();

                            // Add timingpointschange to copy timingpoint hitsounds
                            TimingPoint tp = tloFrom.Origin.HitsoundTP.Copy();
                            tp.Offset = tloTo.Time;
                            timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: copySamplesets, index: copySamplesets, volume: copyVolumes));

                            tloTo.canCopy = false;
                        }
                        tloFrom.canCopy = false;
                    }

                    // Timingpointchange all the undefined tlo from copyFrom
                    foreach (TimelineObject tloTo in tlTo.TimeLineObjects) {
                        if (tloTo.canCopy) {
                            TimingPoint tp = tloTo.Origin.HitsoundTP.Copy();
                            tp.Offset = tloTo.Time;
                            timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: copySamplesets, index: copySamplesets, volume: copyVolumes));
                        }
                    }
                }

                if (copySliderbodychanges) {
                    // Remove timingpoints in beatmapTo that are in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                    foreach (HitObject ho in beatmapTo.HitObjects) {
                        foreach (TimingPoint tp in ho.BodyHitsounds) {
                            // Every timingpoint in a body in beatmapTo
                            if (beatmapFrom.HitObjects.Any(o => o.Time < tp.Offset && o.EndTime > tp.Offset)) {
                                // Timingpoint is in a body for both beatmaps
                                beatmapTo.BeatmapTiming.TimingPoints.Remove(tp);
                            }
                        }
                    }

                    // Get timingpointschanges for every timingpoint from beatmapFrom that is in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                    foreach (HitObject ho in beatmapFrom.HitObjects) {
                        foreach (TimingPoint tp in ho.BodyHitsounds) {
                            // Every timingpoint in a body in beatmapFrom
                            if (beatmapTo.HitObjects.Any(o => o.Time < tp.Offset && o.EndTime > tp.Offset)) {
                                // Timingpoint is in a body for both beatmaps
                                timingPointsChanges.Add(new TimingPointsChange(tp.Copy(), sampleset: copySamplesets, index: copySamplesets, volume: copyVolumes));
                            }
                        }
                    }
                }

                // Apply the greenline changes
                timingPointsChanges = timingPointsChanges.OrderBy(o => o.MyTP.Offset).ToList();
                foreach (TimingPointsChange c in timingPointsChanges) {
                    c.AddChange(beatmapTo.BeatmapTiming.TimingPoints, false);
                }
                
                processedTimeline = tlTo;
            }

            if (muteSliderends) {
                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                processedTimeline.GiveTimingPoints(beatmapTo.BeatmapTiming);

                foreach (TimelineObject tloTo in processedTimeline.TimeLineObjects) {
                    if (FilterMuteTLO(tloTo, beatmapTo, arg)) {
                        // Set volume to 5%, remove all hitsounds, apply customindex and sampleset
                        tloTo.SampleSet = arg.MutedSampleset;
                        tloTo.AdditionSet = 0;
                        tloTo.Normal = false;
                        tloTo.Whistle = false;
                        tloTo.Finish = false;
                        tloTo.Clap = false;

                        tloTo.HitsoundsToOrigin();

                        // Add timingpointschange to copy timingpoint hitsounds
                        TimingPoint tp = tloTo.Origin.HitsoundTP.Copy();
                        tp.Offset = tloTo.Time;
                        tp.Volume = 5;
                        tp.SampleIndex = arg.MutedIndex;
                        timingPointsChanges.Add(new TimingPointsChange(tp, index: doMutedIndex, volume: true));
                    } else {
                        // Add timingpointschange to preserve index and volume
                        TimingPoint tp = tloTo.Origin.HitsoundTP.Copy();
                        tp.Offset = tloTo.Time;
                        timingPointsChanges.Add(new TimingPointsChange(tp, index: doMutedIndex, volume: true));
                    }
                }

                // Apply the greenline changes
                foreach (TimingPointsChange c in timingPointsChanges) {
                    c.AddChange(beatmapTo.BeatmapTiming.TimingPoints, false);
                }
            }

            // Save the file
            editorTo.SaveFile();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }

            // Make an accurate message
            string message = "";
            message += "Done!";
            return message;
        }

        private bool FilterMuteTLO(TimelineObject tloTo, Beatmap beatmapTo, Arguments arg) {
            // Check whether it's defined
            if (!tloTo.canCopy)
                return false;

            // Check type
            if (!(tloTo.IsSliderEnd || tloTo.IsSpinnerEnd))
                return false;

            // Check filter snap
            // It's at least snap x or worse if the time is not a multiple of snap x / 2
            TimingPoint redline = beatmapTo.BeatmapTiming.GetRedlineAtTime(tloTo.Time - 1);
            double resnappedTime = beatmapTo.BeatmapTiming.Resnap(tloTo.Time, arg.Snap1, arg.Snap2, false, redline);
            double beatsFromRedline = (resnappedTime - redline.Offset) / redline.MpB;
            double dist1 = beatsFromRedline * arg.Snap1 / (arg.Snap1 == 1 ? 4 : 2);
            double dist2 = beatsFromRedline * arg.Snap2 / (arg.Snap2 == 1 ? 4 : arg.Snap2 == 3 ? 3 : 2);
            if (Precision.AlmostEquals(dist1 % 1, 0, 0.1) || Precision.AlmostEquals(dist2 % 1, 0, 0.1))
                return false;

            // Check filter temporal length
            if (!Precision.AlmostBigger(tloTo.Origin.TemporalLength, arg.MinLength * redline.MpB))
                return false;

            return true;
        }

        private void BeatmapFromBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.BeatmapFileDialog();
                if (path != "") {
                    BeatmapFromBox.Text = path;
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
                string path = IOHelper.BeatmapFileDialog();
                if (path != "") {
                    BeatmapToBox.Text = path;
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

        private void MuteSliderendBox_Checked(object sender, RoutedEventArgs e) {
            if (SliderendMutingConfigPanel == null)
                return;
            SliderendMutingConfigPanel.Visibility = Visibility.Visible;
        }

        private void MuteSliderendBox_Unchecked(object sender, RoutedEventArgs e) {
            if (SliderendMutingConfigPanel == null)
                return;
            SliderendMutingConfigPanel.Visibility = Visibility.Collapsed;
        }
    }
}
