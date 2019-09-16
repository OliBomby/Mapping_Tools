using System;
using System.IO;
using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;
using Mapping_Tools.Views.SnappingTools;

namespace Mapping_Tools.Views {
    public partial class SnappingToolsView : ISavable<SnappingToolsPreferences> {
        public SnappingToolsView() {
            DataContext = new SnappingToolsVm();
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        private void PreferencesButton_Click(object sender, System.Windows.RoutedEventArgs e) {
            var preferencesWindow = new SnappingToolsPreferencesWindow(GetSaveData());
            var result = preferencesWindow.ShowDialog();
            if (result == true) {
                SetSaveData(preferencesWindow.Preferences);
            } 
        }

        public SnappingToolsPreferences GetSaveData()
        {
            return ((SnappingToolsVm) DataContext).Preferences;
        }

        public void SetSaveData(SnappingToolsPreferences saveData)
        {
            ((SnappingToolsVm) DataContext).Preferences = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "snappingtoolsproject.json");
        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Snapping Tools Projects");
    }
}
