using System.Windows.Controls;

namespace Mapping_Tools.Views {
    public partial class StandardView :UserControl {
        public StandardView() {
            InitializeComponent();

            SetRecentList();
        }

        public void SetRecentList() {
            if( MainWindow.AppWindow.settingsManager.GetRecentMaps().Count > 0 ) {
                foreach( string[] s in MainWindow.AppWindow.settingsManager.GetRecentMaps() ) {
                    // Populate listview in the component
                    recentList.Items.Add(new MyItem { Path = s[0], Date = s[1] });
                }
            }
        }
        public class MyItem {
            public string Path { get; set; }

            public string Date { get; set; }
        }

        private void RecentList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            MainWindow.AppWindow.SetCurrentMap(((MyItem)recentList.SelectedItem).Path);
        }
    }
}
