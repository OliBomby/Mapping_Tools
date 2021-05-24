using System.Windows;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.HitsoundStudio {
    /// <summary>
    /// Interaction logic for HitsoundStudioExportDialog.xaml
    /// </summary>
    public partial class HitsoundStudioExportDialog {
        public HitsoundStudioVm Settings => (HitsoundStudioVm) DataContext;

        public HitsoundStudioExportDialog(HitsoundStudioVm settings) {
            InitializeComponent();
            DataContext = settings;
        }

        private void ExportFolderBrowseButton_OnClick(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                ExportFolderBox.Text = path;
            }
        }
    }
}
