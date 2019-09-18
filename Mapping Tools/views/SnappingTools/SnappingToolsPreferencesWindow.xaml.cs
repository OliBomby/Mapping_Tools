using System;
using System.Windows;
using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsPreferencesWindow.xaml
    /// </summary>
    public partial class SnappingToolsPreferencesWindow
    {
        public SnappingToolsPreferences Preferences {
            get => (SnappingToolsPreferences)DataContext;
            set => DataContext = value;
        }

        public SnappingToolsPreferencesWindow(SnappingToolsPreferences preferences = null) {
            Preferences = preferences ?? new SnappingToolsPreferences();
            InitializeComponent();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
