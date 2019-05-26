using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class HitsoundLayerImportWindow : Window {
        private double widthWin, heightWin; //Set default sizes of window
        private int index;
        public List<HitsoundLayer> HitsoundLayers;

        public HitsoundLayerImportWindow() {
            InitializeComponent();
            widthWin = ActualWidth; // Set width to window
            heightWin = ActualHeight; // Set height to window
            HitsoundLayers = new List<HitsoundLayer>();
            index = 0;
            BeatmapPathBox.Text = MainWindow.AppWindow.GetCurrentMap();
            BeatmapPathBox2.Text = MainWindow.AppWindow.GetCurrentMap();
        }

        public HitsoundLayerImportWindow(int i) {
            InitializeComponent();
            widthWin = ActualWidth; // Set width to window
            heightWin = ActualHeight; // Set height to window
            HitsoundLayers = new List<HitsoundLayer>();
            index = i;
            NameBox.Text = String.Format("Layer {0}", index + 1);
            NameBox2.Text = String.Format("Layer {0}", index + 1);
            BeatmapPathBox.Text = MainWindow.AppWindow.GetCurrentMap();
            BeatmapPathBox2.Text = MainWindow.AppWindow.GetCurrentMap();
        }

        private void BeatmapBrowse_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.BeatmapFileDialog();
            if (path != "") { BeatmapPathBox.Text = path; }
        }

        private void BeatmapLoad_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.CurrentBeatmap();
            if (path != "") { BeatmapPathBox.Text = path; }
        }

        private void BeatmapBrowse2_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.BeatmapFileDialog();
            if (path != "") { BeatmapPathBox2.Text = path; }
        }

        private void BeatmapLoad2_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.CurrentBeatmap();
            if (path != "") { BeatmapPathBox2.Text = path; }
        }

        private void MIDIBrowse3_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.MIDIFileDialog();
            if (path != "") { BeatmapPathBox3.Text = path; }
        }

        private void SampleBrowse_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.AudioFileDialog();
            if (path != "") { SamplePathBox.Text = path; }
        }

        private void Add_Click(object sender, RoutedEventArgs e) {
            if (Tabs.SelectedIndex == 0) {
                // Import one layer
                HitsoundLayer layer = new HitsoundLayer(NameBox.Text, "Stack", BeatmapPathBox.Text, XCoordBox.GetDouble(), YCoordBox.GetDouble(),
                                            SampleSetBox.SelectedIndex + 1, HitsoundBox.SelectedIndex, SamplePathBox.Text, index);
                HitsoundLayers.Add(layer);
            } else if (Tabs.SelectedIndex == 1) {
                // Import complete hitsounds
                HitsoundLayers = HitsoundImporter.LayersFromHitsounds(BeatmapPathBox2.Text);
                HitsoundLayers.ForEach(o => o.Name = String.Format("{0}: {1}", NameBox2.Text, o.Name));
            } else {
                // Import MIDI
                HitsoundLayers = HitsoundImporter.ImportMIDI(BeatmapPathBox3.Text, (bool)KeysoundBox3.IsChecked, SampleFolderBox3.Text);
                HitsoundLayers.ForEach(o => o.Name = String.Format("{0}: {1}", NameBox3.Text, o.Name));
            }
            
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        //Close window
        private void CloseWin(object sender, RoutedEventArgs e) {
            Close();
        }

        //Enable drag control of window and set icons when docked
        private void DragWin(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                Button bt = this.FindName("toggle_button") as Button;
                if (WindowState == WindowState.Maximized) {
                    var point = PointToScreen(e.MouseDevice.GetPosition(this));

                    if (point.X <= RestoreBounds.Width / 2)
                        Left = 0;

                    else if (point.X >= RestoreBounds.Width)
                        Left = point.X - (RestoreBounds.Width - (this.ActualWidth - point.X));

                    else
                        Left = point.X - (RestoreBounds.Width / 2);

                    Top = point.Y - (((FrameworkElement)sender).ActualHeight / 2);
                    WindowState = WindowState.Normal;
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
                }
                this.DragMove();
                //bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            }
        }
    }
}
