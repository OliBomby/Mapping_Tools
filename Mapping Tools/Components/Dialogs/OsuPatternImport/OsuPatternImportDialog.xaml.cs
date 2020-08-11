using System.Windows;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Components.Dialogs.OsuPatternImport {
    /// <summary>
    /// Interaction logic for OsuPatternImportDialog.xaml
    /// </summary>
    public partial class OsuPatternImportDialog {
        public OsuPatternImportDialogViewModel ViewModel => (OsuPatternImportDialogViewModel) DataContext;

        public OsuPatternImportDialog(string initialName = "") {
            InitializeComponent();
            DataContext = new OsuPatternImportDialogViewModel();
            ViewModel.Name = initialName;
        }

        private void OsuPatternImportDialog_OnLoaded(object sender, RoutedEventArgs e) {
            NameBox.Focus();
            NameBox.SelectAll();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e) {

            DialogHost.CloseDialogCommand.Execute(true, this);
        }
    }
}
