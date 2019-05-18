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
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.ViewSettings;
using NAudio.Wave;

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
            Num_Layers_Changed();
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
            try {
                string path = FileFinder.AudioFileDialog();
                if (path != "") {
                    Settings.HitsoundLayers[LayersList.SelectedIndex].SamplePath = path;
                }
            } catch (Exception) { }
        }

        private void SelectedBaseBeatmapBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string path = FileFinder.BeatmapFileDialog();
                if (path != "") {
                    Settings.HitsoundLayers[LayersList.SelectedIndex].Path = path;
                    }
            } catch (Exception) { }
        }

        private void SelectedBaseBeatmapLoad_Click(object sender, RoutedEventArgs e) {
            try {
                string path = FileFinder.CurrentBeatmap();
                if (path != "") {
                    Settings.HitsoundLayers[LayersList.SelectedIndex].Path = path;
                    }
            } catch (Exception) { }
        }

        private void DefaultSampleBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string path = FileFinder.AudioFileDialog();
                if (path != "") {
                    Settings.DefaultSample.SamplePath = path;
                    DefaultSamplePathBox.Text = path;
                    }
            } catch (Exception) { }
        }

        private void BaseBeatmapBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string path = FileFinder.BeatmapFileDialog();
                if (path != "") {
                    Settings.BaseBeatmap = path;
                    BaseBeatmapPathBox.Text = path;
                    }
            } catch (Exception) { }
        }

        private void BaseBeatmapLoad_Click(object sender, RoutedEventArgs e) {
            try {
                string path = FileFinder.CurrentBeatmap();
                if (path != "") {
                    Settings.BaseBeatmap = path;
                    BaseBeatmapPathBox.Text = path;
                    }
            } catch (Exception) { }
        }

        private void ReloadFromSource_Click(object sender, RoutedEventArgs e) {
            try {
                Settings.HitsoundLayers[LayersList.SelectedIndex].ImportMap();
            } catch (Exception) { }
        }

        private void ReloadAllFromSource_Click(object sender, RoutedEventArgs e) {
            try {
                foreach (HitsoundLayer hl in Settings.HitsoundLayers) {
                    hl.ImportMap();
                }
            } catch (Exception) { }
        }

        void HitsoundLayer_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            try {
                int index = LayersList.SelectedIndex;
                if (index < 0 || index > Settings.HitsoundLayers.Count - 1) { return; }
                WaveStream mainOutputStream = new MediaFoundationReader(Settings.HitsoundLayers[index].SamplePath);
                WaveChannel32 volumeStream = new WaveChannel32(mainOutputStream);

                WaveOutEvent player = new WaveOutEvent();

                player.Init(volumeStream);

                player.Play();
            } catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void Num_Layers_Changed() {
            if (Settings.HitsoundLayers.Count == 0) {
                FirstGrid.ColumnDefinitions[0].Width = new GridLength(0);
                EditPanel.IsEnabled = false;
            } else if (FirstGrid.ColumnDefinitions[0].Width.Value < 100) {
                FirstGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                FirstGrid.ColumnDefinitions[2].Width = new GridLength(2, GridUnitType.Star);
                EditPanel.IsEnabled = true;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e) {
            try {
                HitsoundLayerImportWindow importWindow = new HitsoundLayerImportWindow(Settings.HitsoundLayers.Count);
                importWindow.ShowDialog();
                foreach (HitsoundLayer layer in importWindow.HitsoundLayers) {
                    if (layer != null) {
                        Settings.HitsoundLayers.Add(layer);
                        LayersList.SelectedIndex = LayersList.Items.IndexOf(layer);
                    }
                }
                
                RecalculatePriorities();
                Num_Layers_Changed();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e) {
            try {
                // Ask for confirmation
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo);
                if (messageBoxResult != MessageBoxResult.Yes) { return; }

                var selected = LayersList.SelectedItems;
                if (selected.Count == 0 || selected == null) { return; }
                int index = LayersList.Items.IndexOf(selected[0]);
                List<HitsoundLayer> removeList = new List<HitsoundLayer>();
                foreach (var item in selected) {
                    removeList.Add((HitsoundLayer)item);
                }
                foreach (HitsoundLayer hsl in removeList) {
                    Settings.HitsoundLayers.Remove(hsl);
                }
                LayersList.SelectedIndex = Math.Max(Math.Min(index - 1, Settings.HitsoundLayers.Count - 1), 0);

                RecalculatePriorities();
                Num_Layers_Changed();
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
                List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(Settings.HitsoundLayers.ToList(), Settings.DefaultSample);

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
            List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(arg.HitsoundLayers, arg.DefaultSample);
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
