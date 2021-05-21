using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Dialogs {
    /// <summary>
    /// Interaction logic for BeatmapImportDialog.xaml
    /// </summary>
    public partial class BeatmapImportDialog : INotifyPropertyChanged {
        private string _path;

        public string Path {
            get => _path;
            set {
                if (_path == value) return;
                _path = value;
                OnPropertyChanged();
            }
        }

        public BeatmapImportDialog() {
            InitializeComponent();
            DataContext = this;
            Path = MainWindow.AppWindow.GetCurrentMaps().FirstOrDefault() ?? "";
        }

        private void BeatmapBrowse_Click(object sender, RoutedEventArgs e) {
            try {
                string[] paths =
                    IOHelper.BeatmapFileDialog(restore: !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                if (paths.Length != 0) {
                    Path = paths[0];
                }
            }
            catch (Exception ex) {
                ex.Show();
            }
        }

        private void BeatmapLoad_Click(object sender, RoutedEventArgs e) {
            try {
                string path = IOHelper.GetCurrentBeatmap();
                if( path != "" ) { Path = path; }
            }
            catch (Exception ex) {
                ex.Show();
            }
}

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
