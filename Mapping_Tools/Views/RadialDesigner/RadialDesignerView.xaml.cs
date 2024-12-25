using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
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
            // Implement the background work if needed in the future
            // Currently, nothing to do since no functionality is implemented
            e.Result = string.Empty;
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
