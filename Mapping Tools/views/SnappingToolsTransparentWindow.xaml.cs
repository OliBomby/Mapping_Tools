using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SnappingToolsTransparentWindow : Window {
        private double widthWin, heightWin; //Set default sizes of window
        private int index;
        public HitsoundLayer HitsoundLayer;

        public SnappingToolsTransparentWindow() {
            InitializeComponent();
            widthWin = ActualWidth; // Set width to window
            heightWin = ActualHeight; // Set height to window
            index = 0;
            BeatmapPathBox.Text = MainWindow.AppWindow.GetCurrentMap();
        }

        public SnappingToolsTransparentWindow(int i) {
            InitializeComponent();
            widthWin = ActualWidth; // Set width to window
            heightWin = ActualHeight; // Set height to window
            index = i;
            NameBox.Text = String.Format("Layer {0}", index + 1);
            BeatmapPathBox.Text = MainWindow.AppWindow.GetCurrentMap();
        }

        private void BeatmapBrowse_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.BeatmapFileDialog();
            if (path != "") { BeatmapPathBox.Text = path; }
        }

        private void BeatmapLoad_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.CurrentBeatmap();
            if (path != "") { BeatmapPathBox.Text = path; }
        }

        private void SampleBrowse_Click(object sender, RoutedEventArgs e) {
            string path = FileFinder.AudioFileDialog();
            if (path != "") { SamplePathBox.Text = path; }
        }

        private void Add_Click(object sender, RoutedEventArgs e) {
            HitsoundLayer layer = new HitsoundLayer(NameBox.Text, BeatmapPathBox.Text, XCoordBox.GetDouble(), YCoordBox.GetDouble(),
                                            SampleSetBox.SelectedIndex + 1, HitsoundBox.SelectedIndex, SamplePathBox.Text, index);
            HitsoundLayer = layer;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        //Close window
        private void CloseWin(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
