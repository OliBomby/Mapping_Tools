using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Views.RhythmGuide {

    /// <summary>
    /// Interactielogica voor RhythmGuideView.xaml
    /// </summary>
    public partial class RhythmGuideView : ISavable<RhythmGuideVm> {
        public static readonly string ToolName = "Rhythm Guide";

        public static readonly string ToolDescription =
            $@"Make a beatmap with circles from the rhythm of multiple maps, so you have a reference for hitsounding." +
            $@"{Environment.NewLine}You can add the circles to an existing map or make a new map with the circles." +
            $@"{Environment.NewLine}Use the browse button to choose multiple maps at the same time.";

        /// <summary>
        /// 
        /// </summary>
        public RhythmGuideView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new RhythmGuideVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public RhythmGuideVm ViewModel => (RhythmGuideVm) DataContext;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = GenerateRhythmGuide((Classes.Tools.RhythmGuide.RhythmGuideGeneratorArgs) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            foreach (var fileToCopy in ViewModel.GuideGeneratorArgs.Paths) {
                BackupManager.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync(ViewModel.GuideGeneratorArgs);
            CanRun = false;
        }

        private static string GenerateRhythmGuide(Classes.Tools.RhythmGuide.RhythmGuideGeneratorArgs args, BackgroundWorker worker, DoWorkEventArgs _) {
            Classes.Tools.RhythmGuide.GenerateRhythmGuide(args);

            // Complete progress bar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }
            return args.ExportMode == Classes.Tools.RhythmGuide.ExportMode.NewMap ? "" : "Done!";
        }

        public RhythmGuideVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(RhythmGuideVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "rhythmguideproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Rhythm Guide Projects");
    }
}