using System;
using System.Windows;
using System.Windows.Threading;

namespace Mapping_Tools_Net5.Updater {

    public enum UpdateAction {
        Skip,
        Restart,
        Wait
    }

    public partial class UpdaterWindow : Window {

        public event EventHandler<UpdateAction> ActionSelected;

        public UpdaterWindow(Progress<double> progress) {
            InitializeComponent();

            progress.ProgressChanged += OnDownloadProgressChanged;
        }

        private void OnDownloadProgressChanged(object sender, double progress) {
            Dispatcher.Invoke(() => ProgressBar.Value = progress);
        }

        private void RestartBtn_Click(object sender, RoutedEventArgs _) {
            ActionSelected?.Invoke(this, UpdateAction.Restart);

            Dispatcher.Invoke(() => {
                ReadyPanel.Visibility = Visibility.Hidden;
                DownloadText.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Visible;
            });
        }

        private void WaitBtn_Click(object sender, RoutedEventArgs _) {
            ActionSelected?.Invoke(this, UpdateAction.Wait);

            Dispatcher.Invoke(() => {
                ReadyPanel.Visibility = Visibility.Hidden;
                DownloadText.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Visible;
            });
        }

        private void SkipBtn_Click(object sender, RoutedEventArgs _) {
            ActionSelected?.Invoke(this, UpdateAction.Skip);
        }
    }
}