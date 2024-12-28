using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.RadialDesigner {
    [SmartQuickRunUsage(SmartQuickRunTargets.MultipleSelection)]
    [VerticalContentScroll]
    [HorizontalContentScroll]
    public partial class RadialDesignerView : IQuickRun, ISavable<RadialDesignerVm> {
        public static readonly string ToolName = "Radial Designer";

        public static readonly string ToolDescription =
            $@"Generate radial patterns by copying and rotating hit objects around a center point.{Environment.NewLine}Adjust the number of copies, distance, and rotation to create various circular patterns.";

        public RadialDesignerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            DataContext = new RadialDesignerVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public RadialDesignerVm ViewModel => (RadialDesignerVm) DataContext;

        public event EventHandler RunFinished;

        public void QuickRun() {
            RunTool(new[] { IOHelper.GetCurrentBeatmapOrCurrentBeatmap() }, true);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Generate_Sliders((RadialDesignerVm) e.Argument, bgw);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps());
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        private string Generate_Sliders(RadialDesignerVm arg, BackgroundWorker worker) {
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException);

            foreach (var path in arg.Paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                if (editorReaderException2 != null) {
                    throw new Exception("Could not fetch selected hit objects.", editorReaderException2);
                }

                var beatmap = editor.Beatmap;

                // Save the file after processing
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, arg.Quick));

            return arg.Quick ? "" : "Successfully generated radial patterns!";
        }

        public RadialDesignerVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(RadialDesignerVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "radialdesignerproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Radial Designer Projects");
    }
}
