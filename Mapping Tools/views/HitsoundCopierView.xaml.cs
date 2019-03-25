using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class HitsoundCopierView :UserControl {
        private BackgroundWorker backgroundWorker;

        public HitsoundCopierView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker") ;
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
            DateTime now = DateTime.Now;
            string fileToCopy = MainWindow.AppWindow.currentMap.Text;
            string destinationDirectory = MainWindow.AppWindow.BackupPath;
            try {
                File.Copy(fileToCopy, Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + System.IO.Path.GetFileName(fileToCopy)));
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return;
            }
            backgroundWorker.RunWorkerAsync(new Arguments(fileToCopy, PathBox.Text));
            start.IsEnabled = false;
        }

        private struct Arguments {
            public string Path;
            public string PathFrom;
            public Arguments(string path, string pathFrom)
            {
                Path = path;
                PathFrom = pathFrom;
            }
        }

        private string Copy_Hitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs e) {
            Editor editorTo = new Editor(arg.Path);
            Editor editorFrom = new Editor(arg.PathFrom);

            // Clean both for the resnaps
            MapCleaner.CleanMap(editorTo.Beatmap, MapCleaner.Arguments.BasicResnap);
            MapCleaner.CleanMap(editorFrom.Beatmap, MapCleaner.Arguments.BasicResnap);

            // replace:
            // sampleset timingpointchanges will only have influence on sliderbodies with special hitsounding
            // samplsesets will be put on hitobjects (sliderbodies from hitobjects)
            // hitsounds will be put on hitobjects (sliderbodies from hitobjects)
            // customindices will be replaced by tlo hitsounds and sliderbody hitsounds
            // volume will be replaced by tlo and sliderbody hitsounds or just all timingpoints and clean after
            
            /* Pseudocode
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
            
            foreach (HitObject ho in editorFrom.Beatmap.HitObjects) {
                // Copy the timingpoitns for sliderbodies
                foreach (TimingPoint tp in ho.BodyTimingPoints) {
                    if (tp.ThisTimingPointIsInABody(editorTo.Beatmap) {
                        timingPointsChanges.Add(new TimingPointsChange(tp, sampleset:true, customindex:true, volume:true));
                    }
                }
                
                // Copy the samplesets and hitsounds for sliderbodies
                if (ho.IsSlider) {
                    HitObject toho = FindTheSliderWithTheSameTime(BeatmapTo, ho);
                    if (toho != null) {
                        toho.Hitsounds = ho.Hitsounds;
                        toho.Sampleset = ho.Sampleset;
                        toho.Additions = ho.Additions;
                    }
                }
            }
            
            TimeLine timeLineTo = editorTo.Beatmap.GetTimeLine();
            TimeLine timeLineFrom = editorFrom.Beatmap.GetTimeLine();\
            
            foreach (TimeLineObject tlofrom in timeLineFrom.TimeLineObjects) {
                TimeLineObject tlo = FindTheTLOWithTheSameTime(timeLineTo, tlofrom);
                if (totlo != null) {
                    // literally the code in map cleaner that puts tlo hitsounds onto hitobjects
                    // Could probably be abstracted and use a case switch
                    if (tlo.Origin.IsCircle) {
                       tlo.Origin.SampleSet = tlo.FenoSampleSet;
                       tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                        if (mode == 3) {
                            tlo.Origin.CustomIndex = tlo.FenoCustomIndex;
                            tlo.Origin.SampleVolume = tlo.FenoSampleVolume;
                        }
                    } else if (tlo.Origin.IsSlider) {
                        tlo.Origin.EdgeHitsounds[tlo.Repeat] = tlo.GetHitsounds();
                        tlo.Origin.EdgeSampleSets[tlo.Repeat] = tlo.FenoSampleSet;
                        tlo.Origin.EdgeAdditionSets[tlo.Repeat] = tlo.FenoAdditionSet;
                        tlo.Origin.SliderExtras = true;
                        if (tlo.Origin.EdgeAdditionSets[tlo.Repeat] == tlo.Origin.EdgeSampleSets[tlo.Repeat])  // Simplify additions to auto
                        {
                            tlo.Origin.EdgeAdditionSets[tlo.Repeat] = 0;
                        }
                    } else if (tlo.Origin.IsSpinner) {
                        if (tlo.Repeat == 1) {
                            tlo.Origin.SampleSet = tlo.FenoSampleSet;
                            tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                        }
                    } else if (tlo.Origin.IsHoldNote) {
                        if (tlo.Repeat == 0) {
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
                        bool vol = !(tp.Volume == 5 && arguments.RemoveSliderendMuting && (tlo.IsSliderEnd || tlo.IsSpinnerEnd));  // Remove volume change if sliderend muting or spinnerend muting
                        timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind));
                    }
                }
            }
            
            // apply timingpointschanges and give timingpoints to hitobject again
            
            MapCleaner.CleanMap(editorTo.Beatmap, MapCleaner.Arguments.BasicResnap);
            */
            
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

        private void Print(string str) {
            Console.WriteLine(str);
        }

        private void Browse_Click(object sender, RoutedEventArgs e) {
            string path = BeatmapFinder.FileDialog();
            if (path != "") { PathBox.Text = path; }
        }

        private void Current_Map_Click(object sender, RoutedEventArgs e) {
            string path = BeatmapFinder.CurrentBeatmap();
            if (path != "") { PathBox.Text = path; }
        }
    }
}
