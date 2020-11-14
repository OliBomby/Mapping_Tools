using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;

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
            Preferences = (SnappingToolsPreferences)preferences?.Clone() ?? new SnappingToolsPreferences();
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

        private void KeyDownViewModeSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            foreach (var addedItem in e.AddedItems) {
                var item = (ListBoxItem) addedItem;
                Preferences.KeyDownViewMode |= (ViewMode)item.Tag;
            }
            foreach (var removedItem in e.RemovedItems) {
                var item = (ListBoxItem) removedItem;
                Preferences.KeyDownViewMode &= ~(ViewMode)item.Tag;
            }
        }
    }
}
