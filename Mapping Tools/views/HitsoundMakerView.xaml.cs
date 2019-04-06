using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.ViewSettings;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundMakerView : UserControl {
        private BackgroundWorker backgroundWorker;
        private HitsoundMakerSettings Settings;

        public HitsoundMakerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker");
            
            if (MainWindow.AppWindow.settingsManager.settings.HitsoundMakerSettings != null) {
                Settings = MainWindow.AppWindow.settingsManager.settings.HitsoundMakerSettings;
            } else {
                Settings = new HitsoundMakerSettings();
            }

            LayersList.ItemsSource = Settings.HitsoundLayers;
            DataContext = Settings;
            LayersList.SelectedIndex = 0;
        }

        public HitsoundMakerSettings GetSettings() {
            return Settings;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            Make_Hitsounds((Arguments) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
                MessageBox.Show(e.Error.Message);
            }
            else {
                progress.Value = 0;
            }
            start.IsEnabled = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            if (Settings.BaseBeatmap == null || Settings.DefaultSample == null) {
                MessageBox.Show("Please import a base beatmap and default hitsound first.");
                return;
            }
            backgroundWorker.RunWorkerAsync(new Arguments(MainWindow.AppWindow.ExportPath, Settings.BaseBeatmap, Settings.DefaultSample, Settings.HitsoundLayers.ToList()));
            start.IsEnabled = false;
        }

        private void SelectedDefaultSampleBrowse_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.AudioFileDialog();
            if (path != "") {
                Settings.HitsoundLayers[LayersList.SelectedIndex].SamplePath = path;
            }
        }

        private void SelectedBaseBeatmapBrowse_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.BeatmapFileDialog();
            if (path != "") {
                Settings.HitsoundLayers[LayersList.SelectedIndex].Path = path;
            }
        }

        private void SelectedBaseBeatmapLoad_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.CurrentBeatmap();
            if (path != "") {
                Settings.HitsoundLayers[LayersList.SelectedIndex].Path = path;
            }
        }

        private void DefaultSampleBrowse_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.AudioFileDialog();
            if (path != "") {
                Settings.DefaultSample.SamplePath = path;
                DefaultSamplePathBox.Text = path;
            }
        }

        private void BaseBeatmapBrowse_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.BeatmapFileDialog();
            if (path != "") {
                Settings.BaseBeatmap = path;
                BaseBeatmapPathBox.Text = path;
            }
        }

        private void BaseBeatmapLoad_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.CurrentBeatmap();
            if (path != "") {
                Settings.BaseBeatmap = path;
                BaseBeatmapPathBox.Text = path;
            }
        }

        private void ReloadFromSource_Click(object sender, RoutedEventArgs e) {
            Settings.HitsoundLayers[LayersList.SelectedIndex].ImportMap();
        }

        private void Add_Click(object sender, RoutedEventArgs e) {
            try {
                HitsoundLayerImportWindow importWindow = new HitsoundLayerImportWindow(Settings.HitsoundLayers.Count);
                importWindow.ShowDialog();
                HitsoundLayer layer = importWindow.HitsoundLayer;
                if (layer != null) {
                    Settings.HitsoundLayers.Add(layer);
                    LayersList.SelectedIndex = LayersList.Items.IndexOf(layer);
                }
                RecalculatePriorities();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e) {
            try {
                int index = LayersList.SelectedIndex;
                if (index < 0 || index > Settings.HitsoundLayers.Count - 1) { return; }

                Settings.HitsoundLayers.RemoveAt(index);

                LayersList.SelectedIndex = Math.Max(index - 1, 0);

                RecalculatePriorities();
            } catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void Raise_Click(object sender, RoutedEventArgs e) {
            try {
                int index = LayersList.SelectedIndex;
                if (index <= 0) { return; }

                var item = Settings.HitsoundLayers[index];
                Settings.HitsoundLayers.RemoveAt(index);
                Settings.HitsoundLayers.Insert(index - 1, item);

                LayersList.SelectedIndex = index - 1;

                RecalculatePriorities();
            } catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void Lower_Click(object sender, RoutedEventArgs e) {
            try {
                int index = LayersList.SelectedIndex;
                if (index >= Settings.HitsoundLayers.Count - 1) { return; }

                var item = Settings.HitsoundLayers[index];
                Settings.HitsoundLayers.RemoveAt(index);
                Settings.HitsoundLayers.Insert(index + 1, item);

                LayersList.SelectedIndex = index + 1;

                RecalculatePriorities();
            } catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void RecalculatePriorities() {
            for (int i = 0; i < Settings.HitsoundLayers.Count; i++) {
                Settings.HitsoundLayers[i].SetPriority(i);
            }
        }

        private void Startish_Click(object sender, RoutedEventArgs e) {
            try {
                // Convert the multiple layers into packages that have the samples from all the layers at one specific time
                List<SamplePackage> samplePackages = HitsoundConverter.MixLayers(Settings.HitsoundLayers.ToList(), Settings.DefaultSample);

                // Convert the packages to hitsounds that fit on an osu standard map
                CompleteHitsounds completeHitsounds = HitsoundConverter.ConvertPackages(samplePackages);

                int samples = 0;
                foreach (CustomIndex ci in completeHitsounds.CustomIndices) {
                    foreach (HashSet<string> h in ci.Samples.Values) {
                        if (h.Count > 0) {
                            samples++;
                        }
                    }
                }

                MessageBox.Show(String.Format("Number of sample indices: {0}, Number of samples: {1}", completeHitsounds.CustomIndices.Count, samples));
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private struct Arguments {
            public string ExportFolder;
            public string BaseBeatmap;
            public Sample DefaultSample;
            public List<HitsoundLayer> HitsoundLayers;
            public Arguments(string exportFolder, string baseBeatmap, Sample defaultSample, List<HitsoundLayer> hitsoundLayers)
            {
                ExportFolder = exportFolder;
                BaseBeatmap = baseBeatmap;
                DefaultSample = defaultSample;
                HitsoundLayers = hitsoundLayers;
            }
        }

        private void Make_Hitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs e) {
            // Convert the multiple layers into packages that have the samples from all the layers at one specific time
            List<SamplePackage> samplePackages = HitsoundConverter.MixLayers(arg.HitsoundLayers, arg.DefaultSample);
            UpdateProgressBar(worker, 20);

            // Convert the packages to hitsounds that fit on an osu standard map
            CompleteHitsounds completeHitsounds = HitsoundConverter.ConvertPackages(samplePackages);
            UpdateProgressBar(worker, 40);

            // Delete all files in the export folder before filling it again
            DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }
            UpdateProgressBar(worker, 60);

            // Export the hitsound .osu and sound samples
            HitsoundExporter.ExportHitsounds(arg.ExportFolder, arg.BaseBeatmap, completeHitsounds);
            UpdateProgressBar(worker, 80);

            // Open export folder
            Process.Start(arg.ExportFolder);
            UpdateProgressBar(worker, 100);
        }

        private void UpdateProgressBar(BackgroundWorker worker, int progress) {
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(progress);
            }
        }
    }
}
