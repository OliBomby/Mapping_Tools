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
using Mapping_Tools.Viewmodels;
using NAudio.Wave;
using NAudio.Vorbis;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundMakerView : UserControl {
        private BackgroundWorker backgroundWorker;
        private HitsoundMakerVM Settings;

        private bool suppressEvents = false;

        private List<HitsoundLayer> selectedLayers;
        private HitsoundLayer selectedLayer;

        public HitsoundMakerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker");
            
            if (MainWindow.AppWindow.settingsManager.settings.HitsoundMakerVM != null) {
                Settings = MainWindow.AppWindow.settingsManager.settings.HitsoundMakerVM;
            } else {
                Settings = new HitsoundMakerVM();
            }
            
            DataContext = Settings;
            LayersList.SelectedIndex = 0;
            Num_Layers_Changed();
            GetSelectedLayers();
        }

        public HitsoundMakerVM GetSettings() {
            return Settings;
        }

        public void SetSettings(HitsoundMakerVM settings) {
            Settings = settings;
            DataContext = Settings;
            LayersList.SelectedIndex = 0;
            Num_Layers_Changed();
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

        private void GetSelectedLayers() {
            selectedLayers = new List<HitsoundLayer>();

            if (LayersList.SelectedItems.Count == 0) {
                selectedLayer = null;
                return;
            }

            foreach (HitsoundLayer hsl in LayersList.SelectedItems) {
                selectedLayers.Add(hsl);
            }

            selectedLayer = selectedLayers[0];
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            if (Settings.BaseBeatmap == null || Settings.DefaultSample == null) {
                MessageBox.Show("Please import a base beatmap and default hitsound first.");
                return;
            }
            backgroundWorker.RunWorkerAsync(new Arguments(MainWindow.AppWindow.ExportPath, Settings.BaseBeatmap, Settings.DefaultSample, Settings.HitsoundLayers.ToList()));
            start.IsEnabled = false;
        }

        private void SelectedSamplePathBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.AudioFileDialog();
                if (path != "") {
                    SelectedSamplePathBox.Text = path;
                }
            } catch (Exception) { }
        }

        private void SelectedSourcePathBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.BeatmapFileDialog();
                if (path != "") {
                    SelectedSourcePathBox.Text = path;
                    }
            } catch (Exception) { }
        }

        private void SelectedSourcePathLoad_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.CurrentBeatmap();
                if (path != "") {
                    SelectedSourcePathBox.Text = path;
                }
            } catch (Exception) { }
        }

        private void DefaultSampleBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.AudioFileDialog();
                if (path != "") {
                    Settings.DefaultSample.SamplePath = path;
                    DefaultSamplePathBox.Text = path;
                    }
            } catch (Exception) { }
        }

        private void BaseBeatmapBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.BeatmapFileDialog();
                if (path != "") {
                    Settings.BaseBeatmap = path;
                    }
            } catch (Exception) { }
        }

        private void BaseBeatmapLoad_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.CurrentBeatmap();
                if (path != "") {
                    Settings.BaseBeatmap = path;
                    }
            } catch (Exception) { }
        }

        private void ReloadFromSource_Click(object sender, RoutedEventArgs e) {
            try {
                HashSet<string> paths = new HashSet<string>(selectedLayers.Select(o => o.Path));
                List<HitsoundLayer> layers = new List<HitsoundLayer>();

                if (selectedLayers.Any(o => o.ImportType == "Hitsounds")) {
                    foreach (string path in paths) {
                        layers.AddRange(HitsoundImporter.LayersFromHitsounds(path));
                    }
                }
                if (selectedLayers.Any(o => o.ImportType == "MIDI")) {
                    foreach (string path in paths) {
                        layers.AddRange(HitsoundImporter.ImportMIDI(path, true));
                    }
                }

                foreach (HitsoundLayer hl in selectedLayers) {
                    try {
                        hl.Import(layers);
                    } catch (Exception) { }
                }
            } catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void LayersList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (suppressEvents) return;

            GetSelectedLayers();
            UpdateEditingField();
        }

        private void UpdateEditingField() {
            if (selectedLayer == null) { return; }

            suppressEvents = true;

            // Populate the editing fields
            if (selectedLayers.TrueForAll(o => o.Name == selectedLayer.Name)) {
                SelectedNameBox.Text = selectedLayer.Name;
            } else {
                SelectedNameBox.Text = "";
            }
            if (selectedLayers.TrueForAll(o => o.SamplePath == selectedLayer.SamplePath)) {
                SelectedSamplePathBox.Text = selectedLayer.SamplePath;
            } else {
                SelectedSamplePathBox.Text = "";
            }
            if (selectedLayers.TrueForAll(o => o.SampleSet == selectedLayer.SampleSet)) {
                SelectedSampleSetBox.Text = selectedLayer.SampleSetString;
            } else {
                SelectedSampleSetBox.Text = "";
            }
            if (selectedLayers.TrueForAll(o => o.Hitsound == selectedLayer.Hitsound)) {
                SelectedHitsoundBox.Text = selectedLayer.HitsoundString;
            } else {
                SelectedHitsoundBox.Text = "";
            }
            if (selectedLayers.TrueForAll(o => o.ImportType == selectedLayer.ImportType)) {
                ImportTypeBox.Text = selectedLayer.ImportType;
            } else {
                ImportTypeBox.Text = "";
            }
            if (selectedLayers.TrueForAll(o => o.Path == selectedLayer.Path)) {
                SelectedSourcePathBox.Text = selectedLayer.Path;
            } else {
                SelectedSourcePathBox.Text = "";
            }
            if (selectedLayers.TrueForAll(o => o.X == selectedLayer.X)) {
                SelectedXCoordBox.Text = selectedLayer.X.ToString();
            } else {
                SelectedXCoordBox.Text = "";
            }
            if (selectedLayers.TrueForAll(o => o.Y == selectedLayer.Y)) {
                SelectedYCoordBox.Text = selectedLayer.Y.ToString();
            } else {
                SelectedYCoordBox.Text = "";
            }
            if (selectedLayers.TrueForAll(o => o.Keysound == selectedLayer.Keysound)) {
                KeysoundBox.IsChecked = selectedLayer.Keysound;
            } else {
                KeysoundBox.IsChecked = null;
            }

            // Update visibility
            if (selectedLayers.Any(o => o.ImportType == "Stack")) {
                SelectedCoordinatePanel.Visibility = Visibility.Visible;
            } else {
                SelectedCoordinatePanel.Visibility = Visibility.Collapsed;
            }
            if (selectedLayers.Any(o => o.ImportType == "MIDI")) {
                KeysoundBox.Visibility = Visibility.Visible;
            } else {
                KeysoundBox.Visibility = Visibility.Collapsed;
            }

            suppressEvents = false;
        }

        void HitsoundLayer_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            try {
                string path = selectedLayer.SamplePath;
                WaveStream mainOutputStream = Path.GetExtension(path) == ".ogg" ? (WaveStream) new VorbisWaveReader(path) : new MediaFoundationReader(path);
                WaveChannel32 volumeStream = new WaveChannel32(mainOutputStream);

                WaveOutEvent player = new WaveOutEvent();

                player.Init(volumeStream);

                player.Play();
            } catch (Exception) { }
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

                LayersList.SelectedItems.Clear();
                foreach (HitsoundLayer layer in importWindow.HitsoundLayers) {
                    if (layer != null) {
                        Settings.HitsoundLayers.Add(layer);
                        LayersList.SelectedItems.Add(layer);
                    }
                }
                
                RecalculatePriorities();
                Num_Layers_Changed();
                GetSelectedLayers();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e) {
            try {
                // Ask for confirmation
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo);
                if (messageBoxResult != MessageBoxResult.Yes) { return; }

                if (selectedLayers.Count == 0 || selectedLayers == null) { return; }

                suppressEvents = true;

                int index = Settings.HitsoundLayers.IndexOf(selectedLayer);

                foreach (HitsoundLayer hsl in selectedLayers) {
                    Settings.HitsoundLayers.Remove(hsl);
                }
                suppressEvents = false;

                LayersList.SelectedIndex = Math.Max(Math.Min(index - 1, Settings.HitsoundLayers.Count - 1), 0);

                RecalculatePriorities();
                Num_Layers_Changed();

            } catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void Raise_Click(object sender, RoutedEventArgs e) {
            try {
                suppressEvents = true;

                int selectedIndex = Settings.HitsoundLayers.IndexOf(selectedLayer);
                List<HitsoundLayer> moveList = new List<HitsoundLayer>();
                foreach (HitsoundLayer hsl in selectedLayers) {
                    moveList.Add(hsl);
                }

                foreach (HitsoundLayer hsl in Settings.HitsoundLayers) {
                    if (moveList.Contains(hsl)) {
                        moveList.Remove(hsl);
                    }
                    else
                        break;
                }

                foreach (HitsoundLayer hsl in moveList) {
                    int index = Settings.HitsoundLayers.IndexOf(hsl);

                    //Dont move left if it is the first item in the list or it is not in the list
                    if (index <= 0)
                        continue;

                    //Swap with this item with the one to its left
                    Settings.HitsoundLayers.Remove(hsl);
                    Settings.HitsoundLayers.Insert(index - 1, hsl);
                }

                LayersList.SelectedItems.Clear();
                foreach (HitsoundLayer hsl in selectedLayers) {
                    LayersList.SelectedItems.Add(hsl);
                }

                suppressEvents = false;

                RecalculatePriorities();
                GetSelectedLayers();
            } catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void Lower_Click(object sender, RoutedEventArgs e) {
            try {
                suppressEvents = true;

                int selectedIndex = Settings.HitsoundLayers.IndexOf(selectedLayer);
                List<HitsoundLayer> moveList = new List<HitsoundLayer>();
                foreach (HitsoundLayer hsl in selectedLayers) {
                    moveList.Add(hsl);
                }

                for (int i = Settings.HitsoundLayers.Count - 1; i >= 0; i--) {
                    HitsoundLayer hsl = Settings.HitsoundLayers[i];
                    if (moveList.Contains(hsl)) {
                        moveList.Remove(hsl);
                    }
                    else
                        break;
                }

                for (int i = moveList.Count - 1; i >= 0; i--) {
                    HitsoundLayer hsl = moveList[i];
                    int index = Settings.HitsoundLayers.IndexOf(hsl);

                    //Dont move left if it is the first item in the list or it is not in the list
                    if (index >= Settings.HitsoundLayers.Count - 1)
                        continue;

                    //Swap with this item with the one to its left
                    Settings.HitsoundLayers.Remove(hsl);
                    Settings.HitsoundLayers.Insert(index + 1, hsl);
                }

                LayersList.SelectedItems.Clear();
                foreach (HitsoundLayer hsl in selectedLayers) {
                    LayersList.SelectedItems.Add(hsl);
                }

                suppressEvents = false;

                RecalculatePriorities();
                GetSelectedLayers();
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
                        if (h.Any(o => File.Exists(o))) {
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

        private void SelectedNameBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.Name = t;
            }
        }

        private void SelectedSamplePathBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.SamplePath = t;
            }
        }

        private void SelectedSampleSetBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.SampleSetString = t;
            }
        }

        private void SelectedHitsoundBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.HitsoundString = t;
            }
        }

        private void ImportTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportType = t;
            }
        }

        private void SelectedSourcePathBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.Path = t;
            }
        }

        private void SelectedXCoordBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.X = t;
            }
        }

        private void SelectedYCoordBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.Y = t;
            }
        }

        private void KeysoundBox_Changed(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            bool t = (bool)(sender as CheckBox).IsChecked;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.Keysound = t;
            }
        }
    }
}
