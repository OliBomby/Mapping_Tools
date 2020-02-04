using System;
using System.ComponentModel;
using System.Windows;

namespace Mapping_Tools.Views {
    [HiddenTool]
    public class SingleRunMappingTool : MappingTool {
        protected readonly BackgroundWorker BackgroundWorker;

        private bool _canRun = true;
        public bool CanRun {
            get => _canRun;
            set => Set(ref _canRun, value);
        }

        private int _progress;
        public int Progress {
            get => _progress;
            set => Set(ref _progress, value);
        }

        public SingleRunMappingTool() {
            BackgroundWorker = new BackgroundWorker();
            BackgroundWorker.DoWork += BackgroundWorker_DoWork;
            BackgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            BackgroundWorker.WorkerReportsProgress = true;
            BackgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        }

        protected virtual void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) { }

        protected virtual void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            Progress = e.ProgressPercentage;
        }

        /// <summary>
        /// Displays any errors, displays the result if not empty, resets progress and CanRun
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show($"{e.Error.Message}{Environment.NewLine}{e.Error.StackTrace}", "Error");
            } else if (!string.IsNullOrEmpty(e.Result as string)) {
                MessageBox.Show(e.Result.ToString());
            }

            Progress = 0;
            CanRun = true;
        }

        protected static void UpdateProgressBar(BackgroundWorker worker, int progress) {
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(progress);
            }
        }
    }
}
