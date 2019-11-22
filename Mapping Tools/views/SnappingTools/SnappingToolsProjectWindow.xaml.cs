using System.Windows;
using Mapping_Tools.Classes.SnappingTools.Serialization;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsPreferencesWindow.xaml
    /// </summary>
    public partial class SnappingToolsProjectWindow {
        public SnappingToolsProject Project {
            get => (SnappingToolsProject)DataContext;
            set => DataContext = value;
        }

        public SnappingToolsProjectWindow(SnappingToolsProject project = null) {
            Project = new SnappingToolsProject();
            project?.CopyTo(Project);
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

        private SnappingToolsPreferences GetSelectedPreferences() {
            return (SnappingToolsPreferences)SaveSlotsGrid.SelectedItem;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            ((SnappingToolsProject)DataContext).SaveSlots.Add(new SnappingToolsPreferences());
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e) {
            ((SnappingToolsProject)DataContext).SaveSlots.Remove(GetSelectedPreferences());
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            System.Console.WriteLine(sender);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e) {

        }
    }
}
