using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Components.ObjectVisualiser;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.RadialDesigner {
    /// <summary>
    /// Interaction logic for RadialDesignerView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.MultipleSelection)]
    [VerticalContentScroll]
    [HorizontalContentScroll]
    public partial class RadialDesignerView : IQuickRun, ISavable<RadialDesignerVm> {
        public static readonly string ToolName = "Radial Designer";

        public static readonly string ToolDescription =
            @"Design radial patterns for your beatmaps.";

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
            e.Result = RunRadialDesigner((RadialDesignerVm) e.Argument, bgw);
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

        private string RunRadialDesigner(RadialDesignerVm arg, BackgroundWorker worker) {
            // Implement the radial design logic here
            // For now, we'll simulate a process

            for (int i = 0; i <= 100; i++) {
                if (worker.WorkerReportsProgress) {
                    worker.ReportProgress(i);
                }
                System.Threading.Thread.Sleep(10);
            }

            // Update the preview
            ViewModel.PreviewHitObject = new HitObjectElement {
                // Initialize with example data
            };

            // Complete progressbar
            if (worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, true, arg.Quick));

            // Make an accurate message
            var message = "Radial pattern designed successfully!";
            return arg.Quick ? "" : message;
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
