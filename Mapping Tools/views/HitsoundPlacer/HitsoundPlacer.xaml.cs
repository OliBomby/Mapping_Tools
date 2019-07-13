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
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class HitsoundPlacerView : UserControl {
        private BackgroundWorker backgroundWorker;

        public HitsoundPlacerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker)FindResource("backgroundWorker");
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = TransformProperties((string)e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(String.Format("{0}:{1}{2}", e.Error.Message, Environment.NewLine, e.Error.StackTrace), "Error");
            } else {
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

        private string TransformProperties(string path, BackgroundWorker worker, DoWorkEventArgs e) {
            Editor editor = new Editor(path);
            Beatmap beatmap = editor.Beatmap;

            List<HitsoundColumn> hitsoundColumns = new List<HitsoundColumn>() {
                new HitsoundColumn(14, "soft-hitclap.wav"),
                new HitsoundColumn(42, "normal-hitclap.wav"),
                new HitsoundColumn(71,"drum-hitclap.wav"),
                new HitsoundColumn(99,"normal-hitnormal.wav"),
                new HitsoundColumn(128,"drum-hitnormal.wav"),
                new HitsoundColumn(156,"drum-hitfinish.wav"),
                new HitsoundColumn(184,"drum-hitfinish2.wav"),
                new HitsoundColumn(213,"normal-hitfinish.wav"),
                new HitsoundColumn(241,"normal-hitwhistle.wav"),
                new HitsoundColumn(270,"normal-hitwhistle2.wav"),
                new HitsoundColumn(298,"soft-hitfinish.wav"),
                new HitsoundColumn(327,"soft-hitwhistle2.wav"),
                new HitsoundColumn(355,"drum-hitwhistle.wav"),
                new HitsoundColumn(384,"soft-hitwhistle.wav"),
                new HitsoundColumn(412,"normal-hitfinish4.wav"),
                new HitsoundColumn(440,"soft-hitfinish4.wav"),
                new HitsoundColumn(469,"normal-hitclap4.wav"),
                new HitsoundColumn(497,"drum-hitwhistle4.wav")
            };

            foreach (var ho in beatmap.HitObjects) {
                if (ho.IsCircle || ho.IsHoldNote) {
                    HitsoundColumn column = hitsoundColumns[0];
                    double best = double.MaxValue;
                    foreach (HitsoundColumn c in hitsoundColumns) {
                        double dist = c.Distance(ho.Pos.X);
                        if (dist < best) {
                            best = dist;
                            column = c;
                        }
                    }

                    ho.Filename = column.Filename;
                }
            }

            // Save the file
            editor.SaveFile();

            return "Done!";
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Backup
            string fileToCopy = MainWindow.AppWindow.currentMap.Text;
            IOHelper.SaveMapBackup(fileToCopy);

            backgroundWorker.RunWorkerAsync(fileToCopy);

            start.IsEnabled = false;
        }

        private class HitsoundColumn {
            public double X { get; set; }
            public string Filename { get; set; }

            public HitsoundColumn(double x, string filename) {
                X = x;
                Filename = filename;
            }

            public double Distance(double other) {
                return Math.Abs(other - X);
            }
        }
    }
}
