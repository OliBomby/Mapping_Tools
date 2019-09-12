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
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsPreferencesWindow.xaml
    /// </summary>
    public partial class SnappingToolsPreferencesWindow : Window {
        public SnappingToolsPreferencesWindow() {
            DataContext = SnappingToolsView.Settings;
            InitializeComponent();
        }

        private void CloseWin(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
