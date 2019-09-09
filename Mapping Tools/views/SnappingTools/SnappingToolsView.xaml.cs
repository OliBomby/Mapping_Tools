using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views {
    public partial class SnappingToolsView {
        public SnappingToolsView() {
            DataContext = new SnappingToolsVm();
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
        }
    }
}
