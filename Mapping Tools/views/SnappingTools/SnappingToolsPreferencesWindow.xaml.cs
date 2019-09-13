using System;
using System.Windows;
using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsPreferencesWindow.xaml
    /// </summary>
    public partial class SnappingToolsPreferencesWindow : Window {
        private readonly SnappingToolsPreferences initialSettings = (SnappingToolsPreferences)SettingsManager.Settings.SnappingToolsPreferences.Clone();
        public SnappingToolsPreferencesWindow() {
            SnappingToolsPreferencesVM VM = new SnappingToolsPreferencesVM();
            DataContext = VM;
            InitializeComponent();
        }

        private void CloseWin(object sender, EventArgs e) {
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            SettingsManager.Settings.SnappingToolsPreferences = initialSettings;
            Close();
        }
    }
}
