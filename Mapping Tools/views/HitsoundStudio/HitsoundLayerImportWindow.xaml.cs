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
        private readonly int index;
        public List<HitsoundLayer> HitsoundLayers;

        public HitsoundLayerImportWindow() {
            InitializeComponent();
            HitsoundLayers = new List<HitsoundLayer>();
            index = 0;
            BeatmapPathBox.Text = MainWindow.AppWindow.GetCurrentMap();
            BeatmapPathBox2.Text = MainWindow.AppWindow.GetCurrentMap();
        }

        public HitsoundLayerImportWindow(int i) {
            InitializeComponent();
            HitsoundLayers = new List<HitsoundLayer>();
            index = i;
            NameBox0.Text = string.Format("Layer {0}", index + 1);
            NameBox.Text = string.Format("Layer {0}", index + 1);
            NameBox2.Text = string.Format("Layer {0}", index + 1);
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

        private void SampleBrowse0_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.SampleFileDialog();
            if (path != "") { SamplePathBox0.Text = path; }
        }

        private void SampleBrowse_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.SampleFileDialog();
            if (path != "") { SamplePathBox.Text = path; }
        }

        private void Add_Click(object sender, RoutedEventArgs e) {
            if (Tabs.SelectedIndex == 1) {
                // Import one layer
                HitsoundLayer layer = HitsoundImporter.ImportStack(BeatmapPathBox.Text, XCoordBox.GetDouble(), YCoordBox.GetDouble());
                layer.Name = NameBox.Text;
                layer.SampleSet = (SampleSet)(SampleSetBox.SelectedIndex + 1);
                layer.Hitsound = (Hitsound)HitsoundBox.SelectedIndex;
                layer.SampleArgs.Path = SamplePathBox.Text;

                HitsoundLayers.Add(layer);
            } else if (Tabs.SelectedIndex == 2) {
                // Import complete hitsounds
                HitsoundLayers = HitsoundImporter.ImportHitsounds(BeatmapPathBox2.Text);
                HitsoundLayers.ForEach(o => o.Name = string.Format("{0}: {1}", NameBox2.Text, o.Name));
            } else if (Tabs.SelectedIndex == 3) {
                // Import MIDI
                HitsoundLayers = HitsoundImporter.ImportMIDI(BeatmapPathBox3.Text, (bool)InstrumentBox3.IsChecked, (bool)KeysoundBox3.IsChecked, (bool)LengthBox3.IsChecked, LengthRoughnessBox3.GetDouble(2), (bool)VelocityBox3.IsChecked, VelocityRoughnessBox3.GetDouble(10));
                HitsoundLayers.ForEach(o => o.Name = string.Format("{0}: {1}", NameBox3.Text, o.Name));
            } else {
                // Import none
                HitsoundLayer layer = new HitsoundLayer(NameBox0.Text, ImportType.None, (SampleSet)(SampleSetBox0.SelectedIndex + 1), (Hitsound)HitsoundBox0.SelectedIndex, SamplePathBox0.Text);
                HitsoundLayers.Add(layer);
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
