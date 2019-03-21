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
            MapCleaner.CleanMap(editorTo.Beatmap, MapCleaner.Arguments.BasicResnap(arg.Path));
            MapCleaner.CleanMap(editorFrom.Beatmap, MapCleaner.Arguments.BasicResnap(arg.PathFrom));

            // Save the file
            editorTo.SaveFile();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress)
            {
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
