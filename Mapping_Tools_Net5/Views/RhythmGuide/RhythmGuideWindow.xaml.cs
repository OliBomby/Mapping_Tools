using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mapping_Tools.Views.RhythmGuide {
    /// <summary>
    /// Interaction logic for RhythmGuideWindow.xaml
    /// </summary>
    public partial class RhythmGuideWindow {
        private readonly UserControl innerView;

        public RhythmGuideWindow() {
            InitializeComponent();
            DataContext = innerView = MainWindow.AppWindow.Views.GetView(typeof(RhythmGuideView)) as RhythmGuideView;
        }

        //Close window
        private void CloseWin(object sender, RoutedEventArgs e) {
            Close();
        }

        //Enable drag control of window and set icons when docked
        private void DragWin(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton != MouseButton.Left) return;
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
                if (FindName("toggle_button") is Button bt) bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
            }
            this.DragMove();
            //bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
        }

        private void RhythmGuideWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            innerView.Width = ContentControl.Width;
        }
    }
}
