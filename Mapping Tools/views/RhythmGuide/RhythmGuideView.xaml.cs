using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;
using System;
using System.ComponentModel;
using System.Windows;

namespace Mapping_Tools.Views.RhythmGuide {

    /// <summary>
    /// Interactielogica voor RhythmGuideView.xaml
    /// </summary>
    public partial class RhythmGuideView {
        private readonly RhythmGuideVm settings;

        public static readonly string ToolName = "Rhythm Guide";

        public static readonly string ToolDescription = $@"Make a beatmap with circles from the rhythm of multiple maps, so you have a reference for hitsounding.{Environment.NewLine}You can add the circles to an existing map or make a new map with the circles.{Environment.NewLine}Use the browse button to choose mutliple maps at the same time.";

        public RhythmGuideView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = settings = new RhythmGuideVm();
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = GenerateRhythmGuide((Classes.Tools.RhythmGuide.RhythmGuideGeneratorArgs) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            foreach (var fileToCopy in settings.GuideGeneratorArgs.Paths) {
                IOHelper.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync(settings.GuideGeneratorArgs);
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
    }
}