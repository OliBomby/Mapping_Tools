using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.Tools.RhythmGuide;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
            e.Result = GenerateRhythmGuide((RhythmGuideVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            if (ViewModel.ExportMode == ExportMode.AddToMap) {
                BackupManager.SaveMapBackup(ViewModel.ExportPath);
            }

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        private static string GenerateRhythmGuide(RhythmGuideVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            if (args.ExportPath == null) {
                throw new ArgumentNullException(nameof(args.ExportPath));
            }

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();
            var rhythmGuideArgs = new RhythmGuideArgs {
                BeatDivisors = args.BeatDivisors,
                InputBeatmaps = args.Paths.Select(o => EditorReaderStuff.GetNewestVersionOrNot(o, reader).Beatmap),
                NcEverything = args.NcEverything,
                SelectionMode = args.SelectionMode
            };

            switch (args.ExportMode) {
                case ExportMode.NewMap:
                    var templateBeatmap = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader).Beatmap;
                    var beatmap = RhythmGuideGenerator.NewRhythmGuide(rhythmGuideArgs, templateBeatmap, args.OutputGameMode, args.OutputName);

                    var editor = new Editor { TextFile = beatmap, Path = args.ExportPath };
                    editor.SaveFile();

                    ShowSelectedInExplorer.FileOrFolder(args.ExportPath);
                    break;
                case ExportMode.AddToMap:
                    var editor2 = EditorReaderStuff.GetNewestVersionOrNot(args.ExportPath, reader);

                    RhythmGuideGenerator.AddRhythmGuideToBeatmap(editor2.Beatmap, rhythmGuideArgs);

                    editor2.SaveFile();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Complete progress bar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }

            return args.ExportMode == ExportMode.NewMap ? "" : "Done!";
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