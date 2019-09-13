using Mapping_Tools.Viewmodels;
using Mapping_Tools.Views.SnappingTools;

namespace Mapping_Tools.Views {
    public partial class SnappingToolsView {
        public SnappingToolsPreferencesWindow preferencesWindow;
        public SnappingToolsView() {
            DataContext = new SnappingToolsVm();
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
        }

        private void PreferencesButton_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (preferencesWindow == null) {
                preferencesWindow = new SnappingToolsPreferencesWindow();
                preferencesWindow.Show();
                preferencesWindow.Closed += PreferencesWindow_Closed;
            } else {
                preferencesWindow.Activate();
            }
        }

        private void PreferencesWindow_Closed(object sender, System.EventArgs e) {
            preferencesWindow = null;
        }
    }
}
