using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Options;
using Mapping_Tools.Components.Dialogs;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Views.TumourGenerator {
    //[HiddenTool]
    [SmartQuickRunUsage(SmartQuickRunTargets.SingleSelection)]
    public partial class TumourGeneratorView : ISavable<TumourGeneratorVm>, IQuickRun {
        public static readonly string ToolName = "Tumour Generator 2";

        public static readonly string ToolDescription = "Tumour Generator will automatically generate tumours on sliders of your beatmap.";

        private bool _initialized;

        private TumourGeneratorVm ViewModel => (TumourGeneratorVm)DataContext;

        public TumourGeneratorView() {
            InitializeComponent();
            DataContext = new TumourGeneratorVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (_initialized) return;

            ProjectManager.LoadProject(this, message: false);
            _initialized = true;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps()[0]);
        }

        private bool ValidateToolInput(out string message) {
            message = string.Empty;
            return true;
        }

        private async void RunTool(string path, bool quick = false, bool reload = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            if (!ValidateToolInput(out var message)) {
                var dialog = new MessageDialog(message);
                await DialogHost.Show(dialog, "RootDialog");
                return;
            }

            BackupManager.SaveMapBackup(path);

            ViewModel.Path = path;
            ViewModel.Quick = quick;
            ViewModel.Reload = reload;
            foreach (var tumourLayer in ViewModel.TumourLayers) {
                tumourLayer.Freeze();
            }

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = TumourGenerate((TumourGeneratorVm) e.Argument, bgw);
        }

        private string TumourGenerate(TumourGeneratorVm arg, BackgroundWorker worker) {
            
            // Load sliders from the selector

            // Initialize the Tumour Generator

            // Generate copious amounts of tumours on each slider

            // Save the beatmap

            // Complete progressbar
            if (worker is {WorkerReportsProgress: true}) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true,  arg.Reload, arg.Quick));

            return arg.Quick ? string.Empty : "Done!";
        }

        public TumourGeneratorVm GetSaveData() {
            foreach (var tumourLayer in ViewModel.TumourLayers) {
                tumourLayer.Freeze();
            }

            return ViewModel;
        }

        public void SetSaveData(TumourGeneratorVm saveData) {
            DataContext = saveData;
        }
        
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "tumourgeneratorproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Tumour Generator Projects");

        public void QuickRun() {
            var currentMap = IOHelper.GetCurrentBeatmapOrCurrentBeatmap();

            RunTool(currentMap, true, true);
        }

        public event EventHandler RunFinished;
    }
}
