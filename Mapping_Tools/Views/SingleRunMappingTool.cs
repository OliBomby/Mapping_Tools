using Mapping_Tools.Classes;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace Mapping_Tools.Views {
    [HiddenTool]
    public class SingleRunMappingTool : MappingTool {
        protected readonly BackgroundWorker BackgroundWorker;

        private bool canRun = true;
        public bool CanRun {
            get => canRun;
            set => Set(ref canRun, value);
        }

        private int progress;
        public int Progress {
            get => progress;
            set => Set(ref progress, value);
        }

        private bool verbose;
        public bool Verbose {
            get => verbose;
            set => Set(ref verbose, value);
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
                e.Error.Show();
            } else if (!string.IsNullOrEmpty(e.Result as string)) {
                if (Verbose) {
                    MessageBox.Show(e.Result.ToString());
                } else {
                    Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue(e.Result.ToString(), true));
                }
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
