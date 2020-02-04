using System.Globalization;
using System.Windows;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interaction logic for HitsoundStudioExportDialog.xaml
    /// </summary>
    public partial class HitsoundStudioExportDialog {
        public HitsoundStudioExportDialog(double initialValue = 0) {
            InitializeComponent();
            ValueBox.Text = initialValue.ToString(CultureInfo.InvariantCulture);
        }

        private void TypeValueDialog_OnLoaded(object sender, RoutedEventArgs e) {
            ValueBox.Focus();
            ValueBox.SelectAll();
        }
    }
}
