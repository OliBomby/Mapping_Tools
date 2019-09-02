using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingOverlay.xaml
    /// </summary>
    public partial class SnappingOverlay : Window {
        public SnappingOverlay() {
            InitializeComponent();
        }

        private void Window_Deactivated(object sender, EventArgs e) {
            Window window = (Window)sender;
            window.Topmost = true;
        }
    }
}
