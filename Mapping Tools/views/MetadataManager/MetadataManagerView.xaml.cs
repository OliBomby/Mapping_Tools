using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor MetadataManagerView.xaml
    /// </summary>
    public partial class MetadataManagerView : ISavable<MetadataManagerVm> {
        private readonly BackgroundWorker backgroundWorker;

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "metadataproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Metadata Manager Projects");

        public MetadataManagerView() {
            InitializeComponent();
            DataContext = new MetadataManagerVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker");
            ProjectManager.LoadProject(this, message: false);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Metadata((MetadataManagerVm) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
                MessageBox.Show($"{e.Error.Message}{Environment.NewLine}{e.Error.StackTrace}", "Error");
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
            var filesToCopy = ((MetadataManagerVm)DataContext).ExportPath.Split('|');
            foreach (var fileToCopy in filesToCopy) {
                IOHelper.SaveMapBackup(fileToCopy);
            }

            backgroundWorker.RunWorkerAsync((MetadataManagerVm)DataContext);
            start.IsEnabled = false;
        }


        private static string Copy_Metadata(MetadataManagerVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            var paths = arg.ExportPath.Split('|');
            var mapsDone = 0;

            var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (var path in paths) {
                var editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, reader) : new BeatmapEditor(path);
                var beatmap = editor.Beatmap;

                beatmap.Metadata["ArtistUnicode"].StringValue = arg.Artist;
                beatmap.Metadata["Artist"].StringValue = arg.RomanisedArtist;
                beatmap.Metadata["TitleUnicode"].StringValue = arg.Title;
                beatmap.Metadata["Title"].StringValue = arg.RomanisedTitle;
                beatmap.Metadata["Creator"].StringValue = arg.BeatmapCreator;
                beatmap.Metadata["Source"].StringValue = arg.Source;
                beatmap.Metadata["Tags"].StringValue = arg.Tags;

                // Save the file
                editor.SaveFile();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            // Make an accurate message
            var message = $"Successfully exported metadata to {mapsDone} {(mapsDone == 1 ? "beatmap" : "beatmaps")}!";
            return message;
        }

        public MetadataManagerVm GetSaveData() {
            return (MetadataManagerVm)DataContext;
        }

        public void SetSaveData(MetadataManagerVm saveData) {
            DataContext = saveData;
        }
    }
}
