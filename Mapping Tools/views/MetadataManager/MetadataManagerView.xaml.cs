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
    /// Interactielogica voor MetadataManagerView.xaml
    /// </summary>
    public partial class MetadataManagerView : MappingTool, ISavable<MetadataManagerVM> {
        private readonly BackgroundWorker backgroundWorker;

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "metadataproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Metadata Manager Projects");

        public MetadataManagerView() {
            InitializeComponent();
            DataContext = new MetadataManagerVM();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker");
            ProjectManager.LoadProject(this, message: false);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Metadata((MetadataManagerVM) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
                MessageBox.Show(string.Format("{0}{1}{2}", e.Error.Message, Environment.NewLine, e.Error.StackTrace), "Error");
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
            string[] filesToCopy = ((MetadataManagerVM)DataContext).ExportPath.Split('|');
            foreach (string fileToCopy in filesToCopy) {
                IOHelper.SaveMapBackup(fileToCopy);
            }

            backgroundWorker.RunWorkerAsync((MetadataManagerVM)DataContext);
            start.IsEnabled = false;
        }


        private string Copy_Metadata(MetadataManagerVM arg, BackgroundWorker worker, DoWorkEventArgs _) {
            string[] paths = arg.ExportPath.Split('|');
            int mapsDone = 0;

            bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (string path in paths) {
                BeatmapEditor editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, reader) : new BeatmapEditor(path);
                Beatmap beatmap = editor.Beatmap;

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
            string message = string.Format("Successfully exported metadata to {0} {1}!", mapsDone, mapsDone == 1 ? "beatmap" : "beatmaps");
            return message;
        }

        public MetadataManagerVM GetSaveData() {
            return (MetadataManagerVM)DataContext;
        }

        public void SetSaveData(MetadataManagerVM saveData) {
            DataContext = saveData;
        }
    }
}
