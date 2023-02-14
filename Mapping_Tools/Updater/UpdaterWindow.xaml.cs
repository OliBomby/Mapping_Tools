using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace Mapping_Tools.Updater {

    public enum UpdateAction {
        Skip,
        Restart,
        Wait
    }

    public partial class UpdaterWindow {

        public event EventHandler<UpdateAction> ActionSelected;

        public UpdaterWindow(Progress<double> progress, bool downloadImmediately = false) {
            InitializeComponent();

            progress.ProgressChanged += OnDownloadProgressChanged;

            if (downloadImmediately) {
                GoToDownload();
            } else {
                _ = LoadReleaseNotes();
            }
        }

        private async Task LoadReleaseNotes() {
            string responseString;
            using (HttpResponseMessage response = await MainWindow.HttpClient.GetAsync("https://api.github.com/repos/OliBomby/Mapping_Tools/releases/latest")) {
                responseString = await response.Content.ReadAsStringAsync();
            }
            dynamic json = JsonConvert.DeserializeObject(responseString);

            if (json == null)
                return;

            Dispatcher.Invoke(() => {
                ReleaseTitleTextBlock.Text = json["name"];
                ReleaseBodyTextBlock.Text = json["body"];
            });
        }

        private void OnDownloadProgressChanged(object sender, double progress) {
            Dispatcher.Invoke(() => ProgressBar.Value = progress);
        }

        private void RestartBtn_Click(object sender, RoutedEventArgs _) {
            ActionSelected?.Invoke(this, UpdateAction.Restart);
            GoToDownload();
        }

        private void WaitBtn_Click(object sender, RoutedEventArgs _) {
            ActionSelected?.Invoke(this, UpdateAction.Wait);
            GoToDownload();
        }

        private void GoToDownload() {
            Dispatcher.Invoke(() => {
                ReadyPanel.Visibility = Visibility.Collapsed;
                DownloadPanel.Visibility = Visibility.Visible;
            });
        }

        private void SkipBtn_Click(object sender, RoutedEventArgs _) {
            ActionSelected?.Invoke(this, UpdateAction.Skip);
        }
    }
}