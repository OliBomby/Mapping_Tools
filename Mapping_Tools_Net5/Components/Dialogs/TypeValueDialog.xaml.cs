using System.Globalization;
using System.Windows;

namespace Mapping_Tools.Components.Dialogs {
    /// <summary>
    /// Interaction logic for TypeValueDialog.xaml
    /// </summary>
    public partial class TypeValueDialog {
        public TypeValueDialog(double initialValue = 0) {
            InitializeComponent();
            ValueBox.Text = initialValue.ToString(CultureInfo.InvariantCulture);
        }

        private void TypeValueDialog_OnLoaded(object sender, RoutedEventArgs e) {
            ValueBox.Focus();
            ValueBox.SelectAll();
        }
    }
}
