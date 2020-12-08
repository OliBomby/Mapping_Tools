using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Dialogs;
using Mapping_Tools.Viewmodels;
using Mapping_Tools_Core.ToolHelpers;
using Mapping_Tools_Core.Tools.ComboColourStudio;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Views.ComboColourStudio {
    /// <summary>
    /// Interactielogica voor ComboColourStudioView.xaml
    /// </summary>
    public partial class ComboColourStudioView : ISavable<ComboColourStudioVm> {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "combocolourproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Combo Colour Studio Projects");

        public static readonly string ToolName = "Combo Colour Studio";

        public static readonly string ToolDescription = $@"With Combo Colour Studio you can easily customize the combo colours of your beatmap. AKA colour haxing.{Environment.NewLine}You define colored sections much like how you use timing points in the osu! editor. Just add a new colour point and define the sequence of combo colours.{Environment.NewLine}You can also define colour points which only work for one combo, so you can emphasize specific patterns using colour.{Environment.NewLine}You can get started by adding a combo colour using the plus on the bottom left or by importing combo colours from an existing map. The combo colours can be edited by clicking on the coloured circles.{Environment.NewLine}Add a colour point by clicking on the plus on the bottom right. You can edit the colour sequence by double clicking the colour sequence cell.";

        private ComboColourStudioVm ViewModel => (ComboColourStudioVm) DataContext;

        public ComboColourStudioView() {
            InitializeComponent();
            DataContext = new ComboColourStudioVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Export_ComboColours((ComboColourStudioVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            ViewModel.ExportPath = MainWindow.AppWindow.GetCurrentMapsString();

            var filesToCopy = ViewModel.ExportPath.Split('|');
            foreach (var fileToCopy in filesToCopy) {
                BackupManager.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }


        private static string Export_ComboColours(ComboColourStudioVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            var paths = arg.ExportPath.Split('|');
            var mapsDone = 0;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (var path in paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                var beatmap = editor.Beatmap;

                ColourHaxExporter.ExportColourHax(arg, beatmap);

                // Save the file
                editor.SaveFile();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            // Make an accurate message
            var message = $"Successfully exported colours to {mapsDone} {(mapsDone == 1 ? "beatmap" : "beatmaps")}!";
            return message;
        }

        public ComboColourStudioVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(ComboColourStudioVm saveData) {
            DataContext = saveData;
        }
    }
}
