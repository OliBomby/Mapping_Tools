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
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundPreviewHelperView : UserControl, ISavable<HitsoundPreviewHelperVM> {
        private readonly BackgroundWorker backgroundWorker;

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "hspreviewproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Hitsound Preview Projects");

        public HitsoundPreviewHelperView() {
            InitializeComponent();
            DataContext = new HitsoundPreviewHelperVM();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker)FindResource("backgroundWorker");
            ProjectManager.LoadProject(this, message:false);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = PlaceHitsounds((Arguments)e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(string.Format("{0}{1}{2}", e.Error.Message, Environment.NewLine, e.Error.StackTrace), "Error");
            } else {
                if (e.Result.ToString() != "")
                    MessageBox.Show(e.Result.ToString());
                progress.Value = 0;
            }
            start.IsEnabled = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void UpdateProgressBar(BackgroundWorker worker, int progress) {
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(progress);
            }
        }

        private struct Arguments
        {
            public string[] Paths;
            public List<HitsoundZone> Zones;
            public Arguments(string[] paths, List<HitsoundZone> zones) {
                Paths = paths;
                Zones = zones;
            }
        }

        private string PlaceHitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs _) {
            if (arg.Zones.Count == 0)
                return "There are no zones!";

            bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (string path in arg.Paths) {
                BeatmapEditor editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, reader) : new BeatmapEditor(path);
                Beatmap beatmap = editor.Beatmap;
                Timeline timeline = beatmap.GetTimeline();

                for (int i = 0; i < timeline.TimelineObjects.Count; i++) {
                    var tlo = timeline.TimelineObjects[i];

                    var column = arg.Zones.FirstOrDefault();
                    double best = double.MaxValue;
                    foreach (var c in arg.Zones) {
                        double dist = c.Distance(tlo.Origin.Pos);
                        if (dist < best) {
                            best = dist;
                            column = c;
                        }
                    }


                    tlo.Filename = column.Filename;
                    tlo.SampleSet = column.SampleSet;
                    tlo.AdditionSet = SampleSet.Auto;
                    tlo.SetHitsound(column.Hitsound);
                    tlo.HitsoundsToOrigin();

                    UpdateProgressBar(worker, (int)(100f * i / beatmap.HitObjects.Count));
                }

                // Save the file
                editor.SaveFile();
            }

            return "";
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Backup
            string[] filesToCopy = MainWindow.AppWindow.GetCurrentMaps();
            IOHelper.SaveMapBackup(filesToCopy);

            backgroundWorker.RunWorkerAsync(new Arguments(filesToCopy, ((HitsoundPreviewHelperVM)DataContext).Items.ToList()));

            start.IsEnabled = false;
        }

        public HitsoundPreviewHelperVM GetSaveData() {
            return (HitsoundPreviewHelperVM)DataContext;
        }

        public void SetSaveData(HitsoundPreviewHelperVM saveData) {
            DataContext = saveData;
        }
    }
}
