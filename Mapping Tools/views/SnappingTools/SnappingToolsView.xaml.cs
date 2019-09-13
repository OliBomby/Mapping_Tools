using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Viewmodels;
using Mapping_Tools.Views.SnappingTools;

namespace Mapping_Tools.Views {
    public partial class SnappingToolsView {
        public SnappingToolsView() {
            DataContext = new SnappingToolsVm();
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
        }

        private void PreferencesButton_Click(object sender, System.Windows.RoutedEventArgs e) {
             SnappingToolsPreferencesWindow preferencesWindow = new SnappingToolsPreferencesWindow();
             preferencesWindow.ShowDialog();
        }
    }
}
