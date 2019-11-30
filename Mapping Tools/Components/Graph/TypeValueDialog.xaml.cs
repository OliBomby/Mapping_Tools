using System.Globalization;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for TypeValueDialog.xaml
    /// </summary>
    public partial class TypeValueDialog : UserControl {
        public TypeValueDialog(double initialValue = 0) {
            InitializeComponent();
            ValueBox.Text = initialValue.ToString(CultureInfo.InvariantCulture);
        }
    }
}
