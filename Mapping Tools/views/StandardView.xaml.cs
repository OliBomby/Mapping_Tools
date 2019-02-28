using Mapping_Tools.Classes.SystemTools;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mapping_Tools.Views {
    public partial class StandardView :UserControl {
        public StandardView() {
            InitializeComponent();

            foreach( string[] s in MainWindow.AppWindow.settingsManager.settings.RecentMaps ) {
                // Populate list
                this.recentList.Items.Add(new MyItem { Path = s[0], Date = s[1] });
            }
        }

        public class MyItem {
            public string Path { get; set; }

            public string Date { get; set; }
        }
    }
}
