using Mapping_Tools;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
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

            _ = LoadReleaseNotes();
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

            Dispatcher.Invoke(() => {
                ReadyPanel.Visibility = Visibility.Hidden;
                ReadyPanel2.Visibility = Visibility.Hidden;
                DownloadText.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Visible;
            });
        }

        private void WaitBtn_Click(object sender, RoutedEventArgs _) {
            ActionSelected?.Invoke(this, UpdateAction.Wait);

            Dispatcher.Invoke(() => {
                ReadyPanel.Visibility = Visibility.Hidden;
                ReadyPanel2.Visibility = Visibility.Hidden;
                DownloadText.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Visible;
            });
        }

        private void SkipBtn_Click(object sender, RoutedEventArgs _) {
            ActionSelected?.Invoke(this, UpdateAction.Skip);
        }
    }
}