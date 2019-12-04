using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor ComboColourStudioView.xaml
    /// </summary>
    public partial class ComboColourStudioView : ISavable<ComboColourStudioVm> {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "combocolourproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Combo Colour Studio Projects");

        public static readonly string ToolName = "Combo Colour Studio";

        public static readonly string ToolDescription = $@"With Combo Colour Studio you can easily customize the combo colours of your beatmap.{Environment.NewLine}You define colored sections much like how you use timing points in the osu! editor. Just add a new colour point and define the sequence of combo colours.{Environment.NewLine}You can also define colour points which only work for one combo, so you can emphasize specific patterns using colour.";

        private ComboColourStudioVm Settings => (ComboColourStudioVm) DataContext;

        public ComboColourStudioView() {
            InitializeComponent();
            DataContext = new ComboColourStudioVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Metadata((ComboColourStudioVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            var filesToCopy = ((MetadataManagerVm)DataContext).ExportPath.Split('|');
            foreach (var fileToCopy in filesToCopy) {
                IOHelper.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync((MetadataManagerVm)DataContext);
            CanRun = false;
        }


        private static string Copy_Metadata(ComboColourStudioVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            var paths = arg.ExportPath.Split('|');
            var mapsDone = 0;

            var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (var path in paths) {
                var editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, reader) : new BeatmapEditor(path);
                var beatmap = editor.Beatmap;

                beatmap.ComboColours = new List<ComboColour>(arg.Project.ComboColours);

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

        public ComboColourStudioVm GetSaveData() {
            return Settings;
        }

        public void SetSaveData(ComboColourStudioVm saveData) {
            DataContext = saveData;
        }
    }
}
