using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private Sound defaultSound;
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
            e.Result = Make_Hitsounds((Arguments) e.Argument, bgw, e);
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
            backgroundWorker.RunWorkerAsync(new Arguments(MainWindow.AppWindow.ExportPath, baseBeatmap, defaultSound, hitsoundLayers));
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
                    defaultSound = new Sound(SampleSetBox.SelectedIndex + 1, 0, SamplePathBox.Text, int.MaxValue);
                    DefaultSoundCheck.IsChecked = true;
                }
                else {
                    Editor editor = new Editor(MainWindow.AppWindow.currentMap.Text);
                    HitsoundLayer layer = new HitsoundLayer(SampleSetBox.SelectedIndex + 1, HitsoundBox.SelectedIndex, SamplePathBox.Text, hitsoundLayers.Count);

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
                MessageBox.Show(ex.StackTrace);
            }
        }

        private struct Arguments {
            public string ExportFolder;
            public Beatmap BaseBeatmap;
            public Sound DefaultSound;
            public List<HitsoundLayer> HitsoundLayers;
            public Arguments(string exportFolder, Beatmap baseBeatmap, Sound defaultSound, List<HitsoundLayer> hitsoundLayers)
            {
                ExportFolder = exportFolder;
                BaseBeatmap = baseBeatmap;
                DefaultSound = defaultSound;
                HitsoundLayers = hitsoundLayers;
            }
        }

        private string Make_Hitsounds(Arguments arg, BackgroundWorker worker, DoWorkEventArgs e) {
            

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
    }
}
