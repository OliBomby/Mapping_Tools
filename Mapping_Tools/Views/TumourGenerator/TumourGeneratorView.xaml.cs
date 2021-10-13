using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools.SlideratorStuff;
using Mapping_Tools.Components.Dialogs;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

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
            //ViewModel.GraphState = Graph.GetGraphState();
            if (ViewModel.GraphState.CanFreeze) ViewModel.GraphState.Freeze();

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = TumourGenerate((TumourGeneratorVm) e.Argument, bgw);
        }

        private string TumourGenerate(TumourGeneratorVm arg, BackgroundWorker worker) {
            
            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);
            
            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true,  arg.Reload, arg.Quick));

            return arg.Quick ? string.Empty : "Done!";
        }

        public TumourGeneratorVm GetSaveData() {
            //if (ViewModel.GraphState.CanFreeze) ViewModel.GraphState.Freeze();

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
