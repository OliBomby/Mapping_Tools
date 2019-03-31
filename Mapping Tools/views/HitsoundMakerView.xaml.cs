using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundMakerView :UserControl {
        private BackgroundWorker backgroundWorker;
        private Beatmap baseBeatmap;
        private Sample defaultSample;
        private List<HitsoundLayer> hitsoundLayers;

        public HitsoundMakerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker");
            hitsoundLayers = new List<HitsoundLayer>();
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
            if (baseBeatmap == null || defaultSample == null) {
                MessageBox.Show("Please import a base beatmap and default hitsound first.");
                return;
            }
            backgroundWorker.RunWorkerAsync(new Arguments(MainWindow.AppWindow.ExportPath, baseBeatmap, defaultSample, hitsoundLayers));
            start.IsEnabled = false;
        }

        private void SampleBrowse_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.AudioFileDialog();
            if (path != "") { SamplePathBox.Text = path; }
        }

        private void Import_Click(object sender, RoutedEventArgs e) {
            try {
                if (ImportModeBox.Text == "Base Beatmap + Volumes") {
                    Editor editor = new Editor(MainWindow.AppWindow.currentMap.Text);
                    baseBeatmap = editor.Beatmap;
                    BaseBeatmapCheck.IsChecked = true;
                }
                else if (ImportModeBox.Text == "Default Sound") {
                    defaultSample = new Sample(SampleSetBox.SelectedIndex + 1, 0, SamplePathBox.Text, int.MaxValue-1);
                    DefaultSoundCheck.IsChecked = true;
                }
                else {
                    Editor editor = new Editor(MainWindow.AppWindow.currentMap.Text);
                    HitsoundLayer layer = new HitsoundLayer(SampleSetBox.SelectedIndex + 1, HitsoundBox.SelectedIndex, SamplePathBox.Text, LayersList.Items.Count);

                    bool xIgnore = XCoordBox.Text == "";
                    bool yIgnore = YCoordBox.Text == "";
                    double x = XCoordBox.GetDouble();
                    double y = YCoordBox.GetDouble();

                    foreach (HitObject ho in editor.Beatmap.HitObjects) {
                        if ((Math.Abs(ho.Pos.X - x) < 3 || xIgnore) && (Math.Abs(ho.Pos.Y - y) < 3 || yIgnore)) {
                            layer.Times.Add(ho.Time);
                        }
                    }

                    hitsoundLayers.Add(layer);

                    TextBlock item = new TextBlock {
                        Text = String.Format("{0} Sounds, {1} Sampleset, {2} Hitsound, {3}", layer.Times.Count, SampleSetBox.Text, HitsoundBox.Text, layer.SamplePath)
                    };
                    LayersList.Items.Add(item);
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e) {
            int index = LayersList.SelectedIndex;
            if (index < 0 || index > LayersList.Items.Count - 1) { return; }
            hitsoundLayers.RemoveAt(index);
            LayersList.Items.RemoveAt(index);
            LayersList.SelectedIndex = Math.Max(index - 1, 0);

            RecalculatePriorities();
        }

        private void Raise_Click(object sender, RoutedEventArgs e) {
            int index = LayersList.SelectedIndex;
            if (index == 0) { return; }

            var layer = hitsoundLayers[index];
            hitsoundLayers.RemoveAt(index);
            hitsoundLayers.Insert(index - 1, layer);

            var item = LayersList.Items[index];
            LayersList.Items.RemoveAt(index);
            LayersList.Items.Insert(index - 1, item);
            LayersList.SelectedIndex = index - 1;

            RecalculatePriorities();
        }

        private void Lower_Click(object sender, RoutedEventArgs e) {
            int index = LayersList.SelectedIndex;
            if (index == LayersList.Items.Count - 1) { return; }

            var layer = hitsoundLayers[index];
            hitsoundLayers.RemoveAt(index);
            hitsoundLayers.Insert(index + 1, layer);

            var item = LayersList.Items[index];
            LayersList.Items.RemoveAt(index);
            LayersList.Items.Insert(index + 1, item);
            LayersList.SelectedIndex = index + 1;

            RecalculatePriorities();
        }

        private void RecalculatePriorities() {
            for (int i = 0; i < LayersList.Items.Count; i++) {
                hitsoundLayers[i].SetPriority(i);
            }
        }

        private struct Arguments {
            public string ExportFolder;
            public Beatmap BaseBeatmap;
            public Sample DefaultSample;
            public List<HitsoundLayer> HitsoundLayers;
            public Arguments(string exportFolder, Beatmap baseBeatmap, Sample defaultSample, List<HitsoundLayer> hitsoundLayers)
            {
                ExportFolder = exportFolder;
                BaseBeatmap = baseBeatmap;
                DefaultSample = defaultSample;
                HitsoundLayers = hitsoundLayers;
            }
        }

        private void Make_Hitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs e) {
            List<SamplePackage> samplePackages = HitsoundConverter.MixLayers(arg.HitsoundLayers, arg.DefaultSample);

            Console.WriteLine("I packaged hitsounds");
            CompleteHitsounds completeHitsounds = HitsoundConverter.ConvertPackages(samplePackages);

            Console.WriteLine("I converted packages");
            Console.WriteLine("i made this many customindices: " + completeHitsounds.CustomIndices.Count);

            // Delete all files in the export folder before filling it again
            DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }

            Console.WriteLine("I removed files");

            try {
                HitsoundExporter.ExportHitsounds(arg.ExportFolder, arg.BaseBeatmap, completeHitsounds);
            }catch(Exception ex) {

                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            
            Process.Start(arg.ExportFolder);
            Console.WriteLine("I exported");

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }
        }
    }
}
