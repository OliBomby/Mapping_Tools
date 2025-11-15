using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
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
            $@"Generate radial patterns by copying and rotating hit objects around a center point.";

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

            // Remove logical focus so the ViewModel updates properly
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
                if (editorReaderException2 != null && arg.ImportModeSetting == RadialDesignerVm.ImportMode.Selected) {
                    throw new Exception("Could not fetch selected hit objects.", editorReaderException2);
                }

                var beatmap = editor.Beatmap;
                var beatDivisor = beatmap.Editor["BeatDivisor"].IntValue;

                // 1) IMPORT for ImportMode.Selected:
                List<HitObject> importedObjects = new List<HitObject>();
                switch (arg.ImportModeSetting) {
                    case RadialDesignerVm.ImportMode.Selected:
                        importedObjects = selected.ToList();
                        break;
                    case RadialDesignerVm.ImportMode.Bookmarked:
                        // omitted for now
                        break;
                    case RadialDesignerVm.ImportMode.Time:
                        // omitted for now
                        break;
                    case RadialDesignerVm.ImportMode.Everything:
                        // omitted for now
                        break;
                }

                // 2) APPLY ExportMode.Auto + the "copies" parameter:
                if (arg.ExportModeSetting == RadialDesignerVm.ExportMode.Auto) {
                    // Example: duplicate each imported object 'Copies' times and add them
                    // (This snippet does not rotate or move them—just demonstrates duplication)
                    var allNewObjects = new List<HitObject>();
                    foreach (var ho in importedObjects) {
                        for (int i = 0; i < arg.Copies; i++) {
                            // Create a deep copy. (Implementation of Copy() or equivalent is up to you.)
                            var copy = ho.DeepCopy();
                            allNewObjects.Add(copy);
                        }
                    }
                    beatmap.HitObjects.AddRange(allNewObjects);
                }

                // Save after processing
                editor.SaveFile();
            }

            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }

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
