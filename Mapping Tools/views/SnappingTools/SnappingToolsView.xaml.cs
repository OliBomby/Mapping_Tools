using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
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
            if (result.GetValueOrDefault()) {
                SetSaveData(preferencesWindow.Preferences);
            } 
        }

        public SnappingToolsPreferences GetSaveData()
        {
            return ((SnappingToolsVm) DataContext).Preferences;
        }

        public void SetSaveData(SnappingToolsPreferences saveData) {
            saveData?.CopyTo(((SnappingToolsVm)DataContext).Preferences);
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "snappingtoolsproject.json");
        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Snapping Tools Projects");

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
